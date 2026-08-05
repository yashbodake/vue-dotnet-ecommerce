using System.Data;
using Ecommerce.Api.Contracts;
using Ecommerce.Api.Data;
using Microsoft.Data.SqlClient;

namespace Ecommerce.Api.Services;

/// <summary>
/// Cart service using native parameterized SQL queries.
/// Parity with legacy CartService. Guest carts are keyed by the
/// <c>SessionId</c> column (cookie owner); authenticated carts by <c>UserId</c>.
/// </summary>
public sealed class CartService
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public CartService(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Load the cart for a given owner (JWT user id or guest cookie value).
    /// isGuest selects the SessionId column (guest cookie) vs UserId (authenticated).
    /// </summary>
    public CartDto GetCart(string ownerId, bool isGuest)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return EmptyCart();
        }

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        var ownerColumn = ResolveOwnerColumn(isGuest);

        var sql = "SELECT " +
            "ci.CartItemId, " +
            "ci.ProductId, " +
            "p.Name AS ProductName, " +
            "p.Price AS ProductPrice, " +
            "ci.ProductVariantId, " +
            "pv.Name AS VariantName, " +
            "pv.SkuSuffix AS VariantSkuSuffix, " +
            "COALESCE(pv.PriceAdjustment, 0) AS PriceAdjustment, " +
            "ci.Quantity, " +
            "p.Stock AS ProductStock, " +
            "COALESCE(pv.Stock, p.Stock) AS EffectiveStock " +
            "FROM dbo.CartItem ci " +
            "INNER JOIN dbo.Product p ON ci.ProductId = p.ProductId " +
            "LEFT JOIN dbo.ProductVariant pv ON ci.ProductVariantId = pv.ProductVariantId " +
            "WHERE ci." + ownerColumn + " = @OwnerId " +
            "ORDER BY ci.AddedDate";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@OwnerId", ownerId);

        var items = new List<CartItemDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(MapCartItem(reader));
        }

        return BuildCart(items);
    }

    /// <summary>
    /// Sum quantities for the owner. Returns 0 if owner id is empty.
    /// </summary>
    public int GetItemCount(string ownerId, bool isGuest)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return 0;
        }

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        var ownerColumn = ResolveOwnerColumn(isGuest);

        var sql = "SELECT COALESCE(SUM(ci.Quantity), 0) " +
            "FROM dbo.CartItem ci " +
            "WHERE ci." + ownerColumn + " = @OwnerId";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@OwnerId", ownerId);

        return (int)command.ExecuteScalar()!;
    }

    /// <summary>
    /// Add an item to the cart. Merges with existing matching line when present.
    /// Throws InvalidOperationException with a descriptive message on inactive product,
    /// invalid variant, or insufficient stock.
    /// </summary>
    public void AddItem(string ownerId, bool isGuest, int productId, int? variantId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new InvalidOperationException("Cart owner is required.");
        }

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var validation = ValidateProductAndStock(
                connection, transaction, productId, variantId, ownerId, isGuest);

            if (!validation.Active)
            {
                throw new InvalidOperationException("Product is not available.");
            }

            if (variantId.HasValue && !validation.VariantValid)
            {
                throw new InvalidOperationException("Selected product variant is not valid.");
            }

            if (validation.EffectiveStock < quantity)
            {
                throw new InvalidOperationException("Not enough stock available.");
            }

            var ownerColumn = ResolveOwnerColumn(isGuest);

            // Look for an existing cart line with the same product + variant.
            int? existingCartItemId = null;
            int existingQuantity = 0;
            var findSql = "SELECT CartItemId, Quantity " +
                "FROM dbo.CartItem " +
                "WHERE " + ownerColumn + " = @OwnerId " +
                "AND ProductId = @ProductId " +
                "AND (ProductVariantId = @ProductVariantId OR (ProductVariantId IS NULL AND @ProductVariantId IS NULL))";

            using (var findCommand = new SqlCommand(findSql, connection, transaction))
            {
                findCommand.Parameters.AddWithValue("@OwnerId", ownerId);
                findCommand.Parameters.AddWithValue("@ProductId", productId);
                findCommand.Parameters.AddWithValue("@ProductVariantId", variantId ?? (object)DBNull.Value);

                using var reader = findCommand.ExecuteReader();
                if (reader.Read())
                {
                    existingCartItemId = reader.GetInt32(0);
                    existingQuantity = reader.GetInt32(1);
                }
            }

            if (existingCartItemId.HasValue)
            {
                // Merge quantities, re-checking stock.
                var newQuantity = existingQuantity + quantity;
                if (validation.EffectiveStock < newQuantity)
                {
                    throw new InvalidOperationException("Not enough stock available.");
                }

                using var updateCommand = new SqlCommand(
                    "UPDATE dbo.CartItem SET Quantity = @Quantity WHERE CartItemId = @CartItemId",
                    connection, transaction);
                updateCommand.Parameters.AddWithValue("@Quantity", newQuantity);
                updateCommand.Parameters.AddWithValue("@CartItemId", existingCartItemId.Value);
                updateCommand.ExecuteNonQuery();
            }
            else
            {
                using var insertCommand = new SqlCommand(
                    "INSERT INTO dbo.CartItem (UserId, SessionId, ProductId, ProductVariantId, Quantity, AddedDate) " +
                    "VALUES (@UserId, @SessionId, @ProductId, @ProductVariantId, @Quantity, @AddedDate);",
                    connection, transaction);
                insertCommand.Parameters.AddWithValue("@UserId", isGuest ? (object)DBNull.Value : ownerId);
                insertCommand.Parameters.AddWithValue("@SessionId", isGuest ? ownerId : (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@ProductId", productId);
                insertCommand.Parameters.AddWithValue("@ProductVariantId", variantId ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@Quantity", quantity);
                insertCommand.Parameters.AddWithValue("@AddedDate", DateTime.Now);
                insertCommand.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Update the quantity of an existing cart item. Removes the item when quantity &lt;= 0.
    /// Throws InvalidOperationException on oversell. Throws KeyNotFoundException when the
    /// item does not exist or is not owned by the given owner.
    /// </summary>
    public void UpdateQuantity(string ownerId, bool isGuest, int cartItemId, int quantity)
    {
        if (quantity <= 0)
        {
            RemoveItem(ownerId, isGuest, cartItemId);
            return;
        }

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new InvalidOperationException("Cart owner is required.");
        }

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var ownerColumn = ResolveOwnerColumn(isGuest);

            // Verify ownership and read product/variant for stock re-validation.
            int productId;
            int? variantId;
            var ownershipSql = "SELECT ProductId, ProductVariantId, Quantity " +
                "FROM dbo.CartItem " +
                "WHERE CartItemId = @CartItemId AND " + ownerColumn + " = @OwnerId";

            using (var ownershipCommand = new SqlCommand(ownershipSql, connection, transaction))
            {
                ownershipCommand.Parameters.AddWithValue("@CartItemId", cartItemId);
                ownershipCommand.Parameters.AddWithValue("@OwnerId", ownerId);

                using var reader = ownershipCommand.ExecuteReader();
                if (!reader.Read())
                {
                    throw new KeyNotFoundException("Cart item not found.");
                }

                productId = reader.GetInt32(0);
                variantId = reader.IsDBNull(1) ? null : reader.GetInt32(1);
            }

            var validation = ValidateProductAndStock(
                connection, transaction, productId, variantId, ownerId, isGuest, cartItemId);

            if (validation.EffectiveStock < quantity)
            {
                throw new InvalidOperationException("Not enough stock available.");
            }

            using var updateCommand = new SqlCommand(
                "UPDATE dbo.CartItem SET Quantity = @Quantity WHERE CartItemId = @CartItemId",
                connection, transaction);
            updateCommand.Parameters.AddWithValue("@Quantity", quantity);
            updateCommand.Parameters.AddWithValue("@CartItemId", cartItemId);
            updateCommand.ExecuteNonQuery();

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Remove a cart item. Throws KeyNotFoundException when the item does not exist
    /// or is not owned by the given owner.
    /// </summary>
    public void RemoveItem(string ownerId, bool isGuest, int cartItemId)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new InvalidOperationException("Cart owner is required.");
        }

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        var ownerColumn = ResolveOwnerColumn(isGuest);

        var sql = "DELETE FROM dbo.CartItem " +
            "WHERE CartItemId = @CartItemId AND " + ownerColumn + " = @OwnerId";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CartItemId", cartItemId);
        command.Parameters.AddWithValue("@OwnerId", ownerId);

        var rows = command.ExecuteNonQuery();
        if (rows == 0)
        {
            throw new KeyNotFoundException("Cart item not found.");
        }
    }

    /// <summary>
    /// Clear all cart items for the owner.
    /// </summary>
    public void ClearCart(string ownerId, bool isGuest)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return;
        }

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        var ownerColumn = ResolveOwnerColumn(isGuest);

        var sql = "DELETE FROM dbo.CartItem WHERE " + ownerColumn + " = @OwnerId";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@OwnerId", ownerId);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Merge the guest cart into the authenticated user's cart. Guest items are
    /// either added to existing matching lines (summing quantities with stock check)
    /// or reassigned to the authenticated user. No-op if the ids are empty or equal.
    /// </summary>
    public void MergeGuestCart(string guestOwnerId, string authUserId)
    {
        if (string.IsNullOrWhiteSpace(guestOwnerId) || string.IsNullOrWhiteSpace(authUserId))
        {
            return;
        }

        if (guestOwnerId == authUserId)
        {
            return;
        }

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Load all guest items (guest owner is always in SessionId column).
            var guestItems = new List<CartItem>();
            using (var loadCommand = new SqlCommand(
                "SELECT CartItemId, ProductId, ProductVariantId, Quantity " +
                "FROM dbo.CartItem " +
                "WHERE SessionId = @GuestOwnerId",
                connection, transaction))
            {
                loadCommand.Parameters.AddWithValue("@GuestOwnerId", guestOwnerId);
                using var reader = loadCommand.ExecuteReader();
                while (reader.Read())
                {
                    guestItems.Add(new CartItem
                    {
                        CartItemId = reader.GetInt32(0),
                        ProductId = reader.GetInt32(1),
                        ProductVariantId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                        Quantity = reader.GetInt32(3)
                    });
                }
            }

            if (guestItems.Count == 0)
            {
                transaction.Commit();
                return;
            }

            // Load existing user items to match against.
            var userItems = new List<CartItem>();
            using (var loadUserCommand = new SqlCommand(
                "SELECT CartItemId, ProductId, ProductVariantId, Quantity " +
                "FROM dbo.CartItem " +
                "WHERE UserId = @AuthUserId",
                connection, transaction))
            {
                loadUserCommand.Parameters.AddWithValue("@AuthUserId", authUserId);
                using var reader = loadUserCommand.ExecuteReader();
                while (reader.Read())
                {
                    userItems.Add(new CartItem
                    {
                        CartItemId = reader.GetInt32(0),
                        ProductId = reader.GetInt32(1),
                        ProductVariantId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                        Quantity = reader.GetInt32(3)
                    });
                }
            }

            // Build O(1) lookup over existing user items so matching is linear in guest count.
            var userItemLookup = new Dictionary<(int ProductId, int? ProductVariantId), CartItem>();
            foreach (var userItem in userItems)
            {
                var key = (userItem.ProductId, userItem.ProductVariantId);
                userItemLookup[key] = userItem;
            }

            // Batch-load product and variant stock info for all guest items in two round-trips.
            var productLookup = LoadMergeProductInfo(connection, transaction, guestItems);
            var variantLookup = LoadMergeVariantInfo(connection, transaction, guestItems);

            // Compute the reserved quantity already held by the authenticated user for each
            // product+variant combination in a single pass.
            var reservedLookup = new Dictionary<(int ProductId, int? ProductVariantId), int>();
            foreach (var userItem in userItems)
            {
                var key = (userItem.ProductId, userItem.ProductVariantId);
                reservedLookup[key] = reservedLookup.GetValueOrDefault(key) + userItem.Quantity;
            }

            foreach (var guestItem in guestItems)
            {
                var key = (guestItem.ProductId, guestItem.ProductVariantId);
                userItemLookup.TryGetValue(key, out var match);

                int effectiveStock = ComputeEffectiveStock(
                    guestItem.ProductId,
                    guestItem.ProductVariantId,
                    productLookup,
                    variantLookup,
                    reservedLookup,
                    match);

                if (match != null)
                {
                    var newQuantity = match.Quantity + guestItem.Quantity;
                    if (effectiveStock < newQuantity)
                    {
                        newQuantity = effectiveStock; // clamp to available stock
                    }

                    using var updateCommand = new SqlCommand(
                        "UPDATE dbo.CartItem SET Quantity = @Quantity WHERE CartItemId = @CartItemId",
                        connection, transaction);
                    updateCommand.Parameters.AddWithValue("@Quantity", newQuantity);
                    updateCommand.Parameters.AddWithValue("@CartItemId", match.CartItemId);
                    updateCommand.ExecuteNonQuery();

                    using var deleteCommand = new SqlCommand(
                        "DELETE FROM dbo.CartItem WHERE CartItemId = @CartItemId",
                        connection, transaction);
                    deleteCommand.Parameters.AddWithValue("@CartItemId", guestItem.CartItemId);
                    deleteCommand.ExecuteNonQuery();
                }
                else
                {
                    var reassignedQuantity = guestItem.Quantity;
                    if (effectiveStock < reassignedQuantity)
                    {
                        reassignedQuantity = effectiveStock;
                    }

                    if (reassignedQuantity > 0)
                    {
                        using var updateCommand = new SqlCommand(
                            "UPDATE dbo.CartItem SET UserId = @AuthUserId, SessionId = NULL WHERE CartItemId = @CartItemId",
                            connection, transaction);
                        updateCommand.Parameters.AddWithValue("@AuthUserId", authUserId);
                        updateCommand.Parameters.AddWithValue("@CartItemId", guestItem.CartItemId);
                        updateCommand.ExecuteNonQuery();

                        if (reassignedQuantity != guestItem.Quantity)
                        {
                            using var qtyCommand = new SqlCommand(
                                "UPDATE dbo.CartItem SET Quantity = @Quantity WHERE CartItemId = @CartItemId",
                                connection, transaction);
                            qtyCommand.Parameters.AddWithValue("@Quantity", reassignedQuantity);
                            qtyCommand.Parameters.AddWithValue("@CartItemId", guestItem.CartItemId);
                            qtyCommand.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using var deleteCommand = new SqlCommand(
                            "DELETE FROM dbo.CartItem WHERE CartItemId = @CartItemId",
                            connection, transaction);
                        deleteCommand.Parameters.AddWithValue("@CartItemId", guestItem.CartItemId);
                        deleteCommand.ExecuteNonQuery();
                    }
                }
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static CartDto EmptyCart() => new()
    {
        Items = [],
        ItemCount = 0,
        Total = 0m
    };

    private static CartDto BuildCart(List<CartItemDto> items)
    {
        var itemCount = items.Sum(i => i.Quantity);
        var total = items.Sum(i => i.LineTotal);
        return new CartDto
        {
            Items = items,
            ItemCount = itemCount,
            Total = total
        };
    }

    private static CartItemDto MapCartItem(IDataReader reader)
    {
        var productPrice = reader.GetDecimal(3);
        var priceAdjustment = reader.GetDecimal(7);
        var quantity = reader.GetInt32(8);
        var effectiveStock = reader.GetInt32(10);

        return new CartItemDto
        {
            CartItemId = reader.GetInt32(0),
            ProductId = reader.GetInt32(1),
            ProductName = reader.GetString(2),
            ProductPrice = productPrice,
            VariantId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
            VariantName = reader.IsDBNull(5) ? null : reader.GetString(5),
            VariantSkuSuffix = reader.IsDBNull(6) ? null : reader.GetString(6),
            PriceAdjustment = priceAdjustment,
            Quantity = quantity,
            LineTotal = (productPrice + priceAdjustment) * quantity,
            Stock = effectiveStock
        };
    }

    /// <summary>
    /// Selects the owner column: SessionId for guest cookie carts,
    /// UserId for authenticated user carts. The endpoint knows the auth state,
    /// so this does not guess from string format.
    /// </summary>
    private static string ResolveOwnerColumn(bool isGuest) => isGuest ? "SessionId" : "UserId";

    /// <summary>
    /// Validate product active state, variant ownership, and available stock.
    /// For add/merge the requested quantity is checked; for update pass the cartItemId
    /// so the existing line's own quantity is excluded from the available stock check.
    /// </summary>
    private static StockValidationResult ValidateProductAndStock(
        SqlConnection connection,
        SqlTransaction transaction,
        int productId,
        int? variantId,
        string ownerId,
        bool isGuest,
        int? excludeCartItemId = null)
    {
        bool active;
        int productStock;
        using (var productCommand = new SqlCommand(
            "SELECT IsActive, Stock FROM dbo.Product WHERE ProductId = @ProductId",
            connection, transaction))
        {
            productCommand.Parameters.AddWithValue("@ProductId", productId);
            using var reader = productCommand.ExecuteReader();
            if (!reader.Read())
            {
                return new StockValidationResult(false, 0, false, null, 0);
            }

            active = reader.GetBoolean(0);
            productStock = reader.GetInt32(1);
        }

        bool variantValid = true;
        int? variantStock = null;
        if (variantId.HasValue)
        {
            using var variantCommand = new SqlCommand(
                "SELECT Stock FROM dbo.ProductVariant WHERE ProductVariantId = @ProductVariantId AND ProductId = @ProductId",
                connection, transaction);
            variantCommand.Parameters.AddWithValue("@ProductVariantId", variantId.Value);
            variantCommand.Parameters.AddWithValue("@ProductId", productId);

            using var reader = variantCommand.ExecuteReader();
            if (reader.Read())
            {
                variantStock = reader.GetInt32(0);
            }
            else
            {
                variantValid = false;
            }
        }

        var effectiveStock = variantStock ?? productStock;

        // Subtract any existing quantity already held by this owner for the same product+variant
        // (excluding the line being updated, if any) so we do not count it twice.
        var ownerColumn = ResolveOwnerColumn(isGuest);
        var excludeClause = excludeCartItemId.HasValue ? "AND CartItemId != @ExcludeCartItemId" : "";
        var reservedSql = "SELECT COALESCE(SUM(Quantity), 0) " +
            "FROM dbo.CartItem " +
            "WHERE " + ownerColumn + " = @OwnerId " +
            "AND ProductId = @ProductId " +
            "AND (ProductVariantId = @ProductVariantId OR (ProductVariantId IS NULL AND @ProductVariantId IS NULL)) " +
            excludeClause;

        using (var reservedCommand = new SqlCommand(reservedSql, connection, transaction))
        {
            reservedCommand.Parameters.AddWithValue("@OwnerId", ownerId);
            reservedCommand.Parameters.AddWithValue("@ProductId", productId);
            reservedCommand.Parameters.AddWithValue("@ProductVariantId", variantId ?? (object)DBNull.Value);
            if (excludeCartItemId.HasValue)
            {
                reservedCommand.Parameters.AddWithValue("@ExcludeCartItemId", excludeCartItemId.Value);
            }

            var reserved = (int)reservedCommand.ExecuteScalar()!;
            effectiveStock -= reserved;
            if (effectiveStock < 0)
            {
                effectiveStock = 0;
            }
        }

        return new StockValidationResult(active, productStock, variantValid, variantStock, effectiveStock);
    }

    private readonly record struct StockValidationResult(
        bool Active,
        int ProductStock,
        bool VariantValid,
        int? VariantStock,
        int EffectiveStock);

    private readonly record struct MergeProductInfo(bool IsActive, int Stock);
    private readonly record struct MergeVariantInfo(int Stock);

    private static Dictionary<int, MergeProductInfo> LoadMergeProductInfo(
        SqlConnection connection,
        SqlTransaction transaction,
        List<CartItem> guestItems)
    {
        var lookup = new Dictionary<int, MergeProductInfo>();
        var productIds = guestItems
            .Select(g => g.ProductId)
            .Distinct()
            .ToList();

        if (productIds.Count == 0)
        {
            return lookup;
        }

        var parameterNames = string.Join(", ", productIds.Select((_, i) => $"@ProductId{i}"));
        using var command = new SqlCommand(
            "SELECT ProductId, IsActive, Stock " +
            $"FROM dbo.Product WHERE ProductId IN ({parameterNames})",
            connection, transaction);

        for (var i = 0; i < productIds.Count; i++)
        {
            command.Parameters.AddWithValue($"@ProductId{i}", productIds[i]);
        }

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lookup[reader.GetInt32(0)] = new MergeProductInfo(
                reader.GetBoolean(1),
                reader.GetInt32(2));
        }

        return lookup;
    }

    private static Dictionary<(int ProductId, int VariantId), MergeVariantInfo> LoadMergeVariantInfo(
        SqlConnection connection,
        SqlTransaction transaction,
        List<CartItem> guestItems)
    {
        var lookup = new Dictionary<(int ProductId, int VariantId), MergeVariantInfo>();
        var variants = guestItems
            .Where(g => g.ProductVariantId.HasValue)
            .Select(g => (g.ProductId, VariantId: g.ProductVariantId!.Value))
            .Distinct()
            .ToList();

        if (variants.Count == 0)
        {
            return lookup;
        }

        var parameterNames = string.Join(", ", variants.Select((_, i) => $"@VariantId{i}"));
        using var command = new SqlCommand(
            "SELECT ProductId, ProductVariantId, Stock " +
            $"FROM dbo.ProductVariant WHERE ProductVariantId IN ({parameterNames})",
            connection, transaction);

        for (var i = 0; i < variants.Count; i++)
        {
            command.Parameters.AddWithValue($"@VariantId{i}", variants[i].VariantId);
        }

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lookup[(reader.GetInt32(0), reader.GetInt32(1))] = new MergeVariantInfo(
                reader.GetInt32(2));
        }

        return lookup;
    }

    private static int ComputeEffectiveStock(
        int productId,
        int? variantId,
        Dictionary<int, MergeProductInfo> productLookup,
        Dictionary<(int ProductId, int VariantId), MergeVariantInfo> variantLookup,
        Dictionary<(int ProductId, int? ProductVariantId), int> reservedLookup,
        CartItem? matchedUserItem)
    {
        var productStock = 0;
        if (productLookup.TryGetValue(productId, out var productInfo))
        {
            if (!productInfo.IsActive)
            {
                return 0;
            }
            productStock = productInfo.Stock;
        }

        int? variantStock = null;
        if (variantId.HasValue
            && variantLookup.TryGetValue((productId, variantId.Value), out var variantInfo))
        {
            variantStock = variantInfo.Stock;
        }

        var effectiveStock = variantStock ?? productStock;

        var key = (productId, variantId);
        if (reservedLookup.TryGetValue(key, out var reserved))
        {
            // When merging into an existing user line, that line's own quantity is about
            // to be replaced by the merged quantity, so we must not count it as reserved.
            // The original per-item helper excluded the matched CartItemId from the reserved
            // sum; we replicate that exactly here. userItemLookup guarantees at most one line
            // per product+variant, so reserved == matchedUserItem.Quantity in that case.
            var reservedWithoutMatch = reserved - (matchedUserItem?.Quantity ?? 0);
            if (reservedWithoutMatch < 0)
            {
                reservedWithoutMatch = 0;
            }

            effectiveStock -= reservedWithoutMatch;
        }

        if (effectiveStock < 0)
        {
            effectiveStock = 0;
        }

        return effectiveStock;
    }
}