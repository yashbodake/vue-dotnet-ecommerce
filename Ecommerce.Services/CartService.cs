using System;
using System.Data.Entity;
using System.Linq;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.ViewModels;
using Ecommerce.Data;
using Ecommerce.Data.Repositories;
using Ecommerce.Services.Mapping;

namespace Ecommerce.Services
{
    public class CartService : ICartService
    {
        private readonly EcommerceEntities _context;
        private readonly IProductRepository _productRepository;
        private readonly IRepository<CartItem> _cartRepository;

        public CartService(
            EcommerceEntities context,
            IProductRepository productRepository,
            IRepository<CartItem> cartRepository)
        {
            _context = context;
            _productRepository = productRepository;
            _cartRepository = cartRepository;
        }

        public CartViewModel GetCart(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new CartViewModel();
            }

            var items = _context.CartItems
                .AsNoTracking()
                .Include(c => c.Product)
                .Include(c => c.ProductVariant)
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.AddedDate)
                .ToList();

            var cart = new CartViewModel();
            foreach (var item in items)
            {
                if (item.Product == null) continue;
                cart.Lines.Add(EntityMapper.ToCartLine(
                    item.CartItemId,
                    item.Product,
                    item.ProductVariant,
                    item.Quantity));
            }

            return cart;
        }

        public int GetItemCount(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return 0;
            }

            return _context.CartItems
                .Where(c => c.UserId == userId)
                .Select(c => (int?)c.Quantity)
                .Sum() ?? 0;
        }

        public void AddItem(string userId, int productId, int? variantId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User id is required.", nameof(userId));
            }

            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least 1.");
            }

            var product = _productRepository.GetByIdWithDetails(productId);
            if (product == null || !product.IsActive)
            {
                throw new InvalidOperationException("Product not found or inactive.");
            }

            ProductVariant variant = null;
            if (variantId.HasValue)
            {
                variant = product.ProductVariants.FirstOrDefault(v => v.ProductVariantId == variantId.Value);
                if (variant == null)
                {
                    throw new InvalidOperationException("Selected variant was not found for this product.");
                }
            }

            var available = variant != null ? Math.Min(product.Stock, variant.Stock) : product.Stock;

            var existing = _context.CartItems.FirstOrDefault(c =>
                c.UserId == userId &&
                c.ProductId == productId &&
                c.ProductVariantId == variantId);

            var newQty = (existing != null ? existing.Quantity : 0) + quantity;
            if (newQty > available)
            {
                throw new InvalidOperationException(
                    string.Format("Insufficient stock for {0}. Available: {1}.", product.Name, available));
            }

            if (existing != null)
            {
                existing.Quantity = newQty;
                _cartRepository.Update(existing);
            }
            else
            {
                _cartRepository.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    ProductVariantId = variantId,
                    Quantity = quantity,
                    AddedDate = DateTime.Now
                });
            }

            _cartRepository.Save();
        }

        public void RemoveItem(string userId, int cartItemId)
        {
            var item = _cartRepository.GetById(cartItemId);
            if (item == null || item.UserId != userId)
            {
                throw new InvalidOperationException("Cart item not found.");
            }

            _cartRepository.Delete(item);
            _cartRepository.Save();
        }

        public void UpdateQuantity(string userId, int cartItemId, int quantity)
        {
            if (quantity <= 0)
            {
                RemoveItem(userId, cartItemId);
                return;
            }

            var item = _context.CartItems
                .Include(c => c.Product)
                .Include(c => c.ProductVariant)
                .FirstOrDefault(c => c.CartItemId == cartItemId && c.UserId == userId);

            if (item == null || item.Product == null)
            {
                throw new InvalidOperationException("Cart item not found.");
            }

            var available = item.ProductVariant != null
                ? Math.Min(item.Product.Stock, item.ProductVariant.Stock)
                : item.Product.Stock;

            if (quantity > available)
            {
                throw new InvalidOperationException(
                    string.Format("Insufficient stock for {0}. Available: {1}.", item.Product.Name, available));
            }

            item.Quantity = quantity;
            _cartRepository.Update(item);
            _cartRepository.Save();
        }

        /// <summary>Used by checkout after a successful order (Spec 08).</summary>
        public void ClearCart(string userId)
        {
            var items = _context.CartItems.Where(c => c.UserId == userId).ToList();
            foreach (var item in items)
            {
                _cartRepository.Delete(item);
            }

            if (items.Count > 0)
            {
                _cartRepository.Save();
            }
        }

        public void MergeGuestCart(string guestUserId, string authenticatedUserId)
        {
            if (string.IsNullOrWhiteSpace(guestUserId) ||
                string.IsNullOrWhiteSpace(authenticatedUserId) ||
                guestUserId == authenticatedUserId)
            {
                return;
            }

            var guestItems = _context.CartItems.Where(c => c.UserId == guestUserId).ToList();
            foreach (var item in guestItems)
            {
                var existing = _context.CartItems.FirstOrDefault(c =>
                    c.UserId == authenticatedUserId &&
                    c.ProductId == item.ProductId &&
                    c.ProductVariantId == item.ProductVariantId);

                if (existing != null)
                {
                    existing.Quantity += item.Quantity;
                    _cartRepository.Delete(item);
                }
                else
                {
                    item.UserId = authenticatedUserId;
                    _cartRepository.Update(item);
                }
            }

            if (guestItems.Count > 0)
            {
                _cartRepository.Save();
            }
        }
    }
}
