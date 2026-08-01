# Legacy eCommerce Platform — Project Plan & Spec-Driven Development Specs
**.NET Framework 4.7 · ASP.NET MVC 5 · Razor · jQuery 3.4.1 · EF6 (Database-First) · SQL Server Express**

This document has two parts:

1. **Project Plan** — phased build order, dependencies between phases, and definition of done for each phase.
2. **Detailed Specs** — one spec per module, written to be implemented independently ("spec-driven development"). Each spec is self-contained: give it to a developer (or an AI coding assistant, one spec at a time) and it should be buildable without needing the whole document as context.

---

## PART 1 — PROJECT PLAN

### Solution architecture (recap)

```
Ecommerce.sln
├── Ecommerce.Core       → POCOs, interfaces, enums. No dependencies on anything else.
├── Ecommerce.Data       → EF6 DbContext (Database-First/EDMX), Repository implementations.
├── Ecommerce.Services   → Business logic. Depends on Core + Data.
└── Ecommerce.Web        → MVC 5 controllers, Razor views, jQuery. Depends on Core + Services (never directly on Data).
```

**Dependency rule:** `Web → Services → Data → Core`. Web must never reference `Ecommerce.Data` directly — it only knows about interfaces defined in `Core` and resolves implementations through Unity.

### Build phases

| Phase | Deliverable | Depends on |
|---|---|---|
| 0 | SQL Express database + schema created | — |
| 1 | Solution skeleton: 4 projects, references wired, Unity DI registered, EDMX generated from DB | Phase 0 |
| 2 | Core domain models + repository/service interfaces in `Ecommerce.Core` | Phase 1 |
| 3 | Repository implementations in `Ecommerce.Data` (wraps EDMX-generated context) | Phase 1, 2 |
| 4 | Service layer (`ProductService`, `CartService`, `OrderService`, `CustomerService`) | Phase 2, 3 |
| 5 | Product Catalog module (Web) | Phase 4 |
| 6 | Shopping Cart module (Web) | Phase 4, 5 |
| 7 | User Account module (ASP.NET Identity + Orders history) | Phase 4 |
| 8 | Checkout module (multi-step wizard) | Phase 6, 7 |
| 9 | Admin module (product CRUD, order management) | Phase 4, 7 |
| 10 | Cross-cutting: security hardening, bundling/minification, error pages, logging | All above |

**Why this order:** Core/Data/Services first means Web never gets built against a moving target underneath it. Catalog before Cart before Checkout mirrors the actual user journey and each module's controller/service can be smoke-tested before the next depends on it.

### Definition of done (per phase)
A phase is "done" when:
- Code compiles with zero warnings related to that phase
- Controller actions in that module return correct views/partials for both the happy path and at least one edge case (empty cart, invalid login, out-of-stock product, etc.)
- Any new DB objects (tables, stored procs) exist in SQL Express and are reflected in the EDMX
- Anti-forgery tokens and `[Authorize]` are applied wherever the spec calls for them

---

## PART 2 — DETAILED SPECS

---

### SPEC 00 — Database Schema (SQL Server Express)

**Goal:** Create `LegacyEcommerceDb` on `.\SQLEXPRESS` with the tables below. This is built *before* the EDMX, since the project is Database-First.

**Tables:**

```sql
CREATE TABLE Category (
    CategoryId INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    ParentCategoryId INT NULL FOREIGN KEY REFERENCES Category(CategoryId),
    DisplayOrder INT NOT NULL DEFAULT 0
);

CREATE TABLE Product (
    ProductId INT IDENTITY PRIMARY KEY,
    CategoryId INT NOT NULL FOREIGN KEY REFERENCES Category(CategoryId),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Price DECIMAL(18,2) NOT NULL,
    ThumbnailUrl NVARCHAR(300) NULL,
    Stock INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE TABLE ProductImage (
    ProductImageId INT IDENTITY PRIMARY KEY,
    ProductId INT NOT NULL FOREIGN KEY REFERENCES Product(ProductId),
    Url NVARCHAR(300) NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0
);

CREATE TABLE ProductVariant (
    ProductVariantId INT IDENTITY PRIMARY KEY,
    ProductId INT NOT NULL FOREIGN KEY REFERENCES Product(ProductId),
    Name NVARCHAR(100) NOT NULL,      -- e.g. "Size: L / Color: Red"
    SkuSuffix NVARCHAR(50) NULL,
    Stock INT NOT NULL DEFAULT 0,
    PriceAdjustment DECIMAL(18,2) NOT NULL DEFAULT 0
);

CREATE TABLE CartItem (
    CartItemId INT IDENTITY PRIMARY KEY,
    UserId NVARCHAR(128) NULL,        -- nullable: guest carts keyed by SessionId instead
    SessionId NVARCHAR(100) NULL,
    ProductId INT NOT NULL FOREIGN KEY REFERENCES Product(ProductId),
    ProductVariantId INT NULL FOREIGN KEY REFERENCES ProductVariant(ProductVariantId),
    Quantity INT NOT NULL,
    AddedDate DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE TABLE Orders (
    OrderId INT IDENTITY PRIMARY KEY,
    UserId NVARCHAR(128) NOT NULL,
    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',  -- Pending, Paid, Shipped, Cancelled
    ShippingAddress NVARCHAR(500) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL
);

CREATE TABLE OrderItem (
    OrderItemId INT IDENTITY PRIMARY KEY,
    OrderId INT NOT NULL FOREIGN KEY REFERENCES Orders(OrderId),
    ProductId INT NOT NULL FOREIGN KEY REFERENCES Product(ProductId),
    ProductVariantId INT NULL FOREIGN KEY REFERENCES ProductVariant(ProductVariantId),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL
);
```

**Notes:**
- ASP.NET Identity tables (`AspNetUsers`, `AspNetRoles`, etc.) are generated separately when you scaffold Identity — don't hand-write these.
- `CartItem.UserId` nullable + `SessionId` supports guest checkout later as a stretch goal; for the learning project you can start with logged-in-only carts and simplify.

**Acceptance criteria:** All tables created in SSMS, FKs verified, and at least 5 sample products across 2 categories seeded via `INSERT` statements for testing.

---

### SPEC 01 — Solution Skeleton & Dependency Injection

**Goal:** Four projects created and wired together with Unity resolving interfaces to implementations.

**Tasks:**
1. Create solution `Ecommerce.sln` with 4 Class Library / MVC projects as named above.
2. `Ecommerce.Web` → add NuGet: `Unity.Mvc5`, `Microsoft.AspNet.Mvc` 5.2.7, `EntityFramework` 6.4.4.
3. `Ecommerce.Data` → right-click → Add → New Item → **ADO.NET Entity Data Model** → Database First → point at `LegacyEcommerceDb` → generates `EcommerceModel.edmx` + entity classes + `EcommerceEntities : DbContext`.
4. In `Ecommerce.Core`, define repository interfaces:
   ```csharp
   public interface IRepository<T> where T : class
   {
       T GetById(int id);
       IEnumerable<T> GetAll();
       void Add(T entity);
       void Update(T entity);
       void Delete(T entity);
       void Save();
   }
   ```
5. In `Ecommerce.Data`, implement a generic `Repository<T>` wrapping `EcommerceEntities`.
6. `UnityConfig.cs` in `Ecommerce.Web/App_Start/`:
   ```csharp
   public static class UnityConfig
   {
       public static void RegisterComponents()
       {
           var container = new UnityContainer();
           container.RegisterType<IRepository<Product>, Repository<Product>>();
           container.RegisterType<IProductService, ProductService>();
           container.RegisterType<ICartService, CartService>();
           container.RegisterType<IOrderService, OrderService>();
           DependencyResolver.SetResolver(new UnityDependencyResolver(container));
       }
   }
   ```
   Called from `Global.asax.cs` → `Application_Start()`.

**Acceptance criteria:** Solution builds. A throwaway `TestController` can resolve `IProductService` via constructor injection and return `Ok` with a count of products from the DB, proving the whole chain (Web → Services → Data → SQL Express) works end-to-end.

---

### SPEC 02 — Core Domain Models & Interfaces

**Goal:** `Ecommerce.Core` contains plain models and service contracts, with zero EF or MVC references.

**Models** (hand-written POCOs, separate from EDMX-generated entities — used for ViewModels and service boundaries):
```csharp
public class ProductDetailViewModel
{
    public Product Product { get; set; }
    public List<ProductImage> Images { get; set; }
    public List<ProductVariant> Variants { get; set; }
    public int SelectedVariantId { get; set; }
}

public class CartViewModel
{
    public List<CartLineViewModel> Lines { get; set; }
    public decimal Total => Lines.Sum(l => l.LineTotal);
    public int ItemCount => Lines.Sum(l => l.Quantity);
}

public class CartLineViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}
```

**Service interfaces:**
```csharp
public interface IProductService
{
    IEnumerable<Product> GetByCategory(int categoryId);
    ProductDetailViewModel GetDetail(int productId);
    PagedResult<Product> Filter(ProductFilterCriteria criteria);
}

public interface ICartService
{
    CartViewModel GetCart(string userId);
    void AddItem(string userId, int productId, int? variantId, int quantity);
    void RemoveItem(string userId, int cartItemId);
    void UpdateQuantity(string userId, int cartItemId, int quantity);
}

public interface IOrderService
{
    Order CreateOrder(string userId, string shippingAddress, CartViewModel cart);
    IEnumerable<Order> GetOrderHistory(string userId);
    Order GetOrderDetail(int orderId, string userId);
}
```

**Acceptance criteria:** Project compiles with only these + BCL references — no `System.Web.Mvc`, no `EntityFramework` package referenced here. This is what enforces the layering.

---

### SPEC 03 — Repository Layer (`Ecommerce.Data`)

**Goal:** Implement `IRepository<T>` and any specialized repositories, wrapping the EDMX-generated `EcommerceEntities` context.

```csharp
public class Repository<T> : IRepository<T> where T : class
{
    private readonly EcommerceEntities _context;
    private readonly DbSet<T> _dbSet;

    public Repository(EcommerceEntities context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public T GetById(int id) => _dbSet.Find(id);
    public IEnumerable<T> GetAll() => _dbSet.ToList();
    public void Add(T entity) => _dbSet.Add(entity);
    public void Update(T entity) => _context.Entry(entity).State = EntityState.Modified;
    public void Delete(T entity) => _dbSet.Remove(entity);
    public void Save() => _context.SaveChanges();
}
```

Add a specialized `IProductRepository` if you need queries beyond generic CRUD (e.g. `GetByCategoryWithVariants`), implemented with `.Include()` for eager loading of `ProductImage`/`ProductVariant`.

**Acceptance criteria:** Unit test (or manual console test) proves `Add` → `Save` → `GetById` round-trips correctly against SQL Express.

---

### SPEC 04 — Service Layer (`Ecommerce.Services`)

**Goal:** Implement the interfaces from Spec 02, containing the actual business rules (not just pass-through CRUD).

**Key business rules to encode here (not in controllers):**
- `ProductService.Filter()` — pagination, category filter, price range, in-stock-only toggle
- `CartService.AddItem()` — if item already in cart, increment quantity instead of duplicating row; validate against `Product.Stock`
- `OrderService.CreateOrder()` — snapshot `UnitPrice` from `Product.Price` at time of order (never trust cart price at checkout time — re-verify against DB), decrement `Product.Stock`, wrap in a transaction

```csharp
public Order CreateOrder(string userId, string shippingAddress, CartViewModel cart)
{
    using (var scope = new TransactionScope())
    {
        var order = new Order { UserId = userId, ShippingAddress = shippingAddress, OrderDate = DateTime.Now, Status = "Pending" };
        decimal total = 0;
        foreach (var line in cart.Lines)
        {
            var product = _productRepository.GetById(line.ProductId);
            if (product.Stock < line.Quantity) throw new InvalidOperationException($"Insufficient stock for {product.Name}");
            product.Stock -= line.Quantity;
            _productRepository.Update(product);

            var orderItem = new OrderItem { ProductId = product.ProductId, Quantity = line.Quantity, UnitPrice = product.Price };
            order.OrderItem.Add(orderItem);
            total += product.Price * line.Quantity;
        }
        order.TotalAmount = total;
        _orderRepository.Add(order);
        _orderRepository.Save();
        scope.Complete();
        return order;
    }
}
```

**Acceptance criteria:** Insufficient-stock scenario throws and rolls back with zero DB side effects; happy path decrements stock and creates matching `OrderItem` rows atomically.

---

### SPEC 05 — Product Catalog Module

**Routes / Controller actions:**
| Action | Route | Returns |
|---|---|---|
| `ProductController.Index()` | `/Product` or `/` | Full view with filter form + empty `#product-grid` |
| `ProductController.Filter(ProductFilterCriteria criteria)` | `/Product/Filter` (AJAX GET) | `PartialView("_ProductList", pagedResult)` |
| `ProductController.Detail(int id)` | `/Product/Detail/5` | Full view, `ProductDetailViewModel` |

**Views:**
- `Index.cshtml` — filter sidebar (category checkboxes, price range, sort dropdown), `<div id="product-grid">`
- `_ProductList.cshtml` — loops `_ProductCard.cshtml`, includes pagination partial
- `_ProductCard.cshtml` — as shown in the spec PDF (image, name, price, Add to Cart button with `data-product-id`)
- `Detail.cshtml` — gallery (Fancybox), variant dropdown, quantity input, Add to Cart

**jQuery pattern (Index.cshtml `@section Scripts`):**
```javascript
$(function () {
    function loadProducts() {
        $.get('/Product/Filter', $('#filter-form').serialize(), function (html) {
            $('#product-grid').html(html);
        });
    }
    $('#filter-form').on('change', 'input, select', loadProducts);
    loadProducts(); // initial load
});
```

**Acceptance criteria:**
- Changing any filter control re-renders `#product-grid` without a full page reload
- Direct navigation to `/Product/Detail/5` works standalone (no dependency on prior AJAX state)
- Out-of-stock products show a disabled "Add to Cart" button instead of the active one

---

### SPEC 06 — Shopping Cart Module

**Routes / Controller actions:**
| Action | Route | Returns |
|---|---|---|
| `CartController.MiniCart()` | Child action, called via `@Html.Action("MiniCart","Cart")` | `PartialView("_MiniCart", cartViewModel)` |
| `CartController.AddToCart(int productId, int? variantId, int quantity)` | `/Cart/AddToCart` (AJAX POST) | `JsonResult { success, itemCount }` |
| `CartController.Index()` | `/Cart` | Full cart page view |
| `CartController.UpdateQuantity(int cartItemId, int quantity)` | `/Cart/UpdateQuantity` (AJAX POST) | `JsonResult` with updated line + cart totals |
| `CartController.RemoveItem(int cartItemId)` | `/Cart/RemoveItem` (AJAX POST) | `JsonResult` |

**jQuery pattern for add-to-cart (event delegation, since cards are re-rendered by the catalog's own AJAX):**
```javascript
$(document).on('click', '.add-to-cart-btn', function () {
    var productId = $(this).data('product-id');
    $.ajax({
        url: '/Cart/AddToCart',
        type: 'POST',
        data: { productId: productId, quantity: 1 },
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        success: function (result) {
            if (result.success) {
                $('#mini-cart-container').load('/Cart/MiniCart');
            }
        }
    });
});
```

**Security requirement:** `[ValidateAntiForgeryToken]` on `AddToCart`, `UpdateQuantity`, `RemoveItem`. Since these are AJAX POSTs (not form submits), the token must be read from a hidden field/global var and sent as a header — add a global `$.ajaxSetup` in the layout's script section rather than repeating it per call.

**Acceptance criteria:**
- Adding the same product twice increments quantity, doesn't duplicate the cart row
- Mini-cart badge count updates immediately after add, without a full page reload
- Cart page quantity change updates line total and grand total via AJAX, no reload

---

### SPEC 07 — User Account Module

**Setup:** Scaffold with the standard "Individual User Accounts" MVC template option — this generates `AccountController`, `IdentityConfig.cs`, `Startup.Auth.cs` (OWIN), and the `AspNetUsers`/`AspNetRoles` tables automatically. Don't hand-build these.

**Additions beyond the scaffold:**
| Action | Route | Returns |
|---|---|---|
| `AccountController.Orders()` | `/Account/Orders` `[Authorize]` | List of past orders via `IOrderService.GetOrderHistory` |
| `AccountController.OrderDetail(int id)` | `/Account/OrderDetail/5` `[Authorize]` | Single order, must verify `order.UserId == User.Identity.GetUserId()` before returning (prevent IDOR) |

**Views:**
- `Orders.cshtml` — DataTables jQuery plugin over the order list (sortable by date/status)
- Reuse scaffolded `Login.cshtml` / `Register.cshtml`, restyle with Bootstrap 3 to match the rest of the site

**Acceptance criteria:**
- Unauthenticated request to `/Account/Orders` redirects to `/Account/Login?ReturnUrl=...`
- Attempting to view another user's order by guessing the ID returns 403/404, not the order

---

### SPEC 08 — Checkout Module (multi-step wizard)

**Routes / Controller actions:**
| Action | Route | Returns |
|---|---|---|
| `CheckoutController.Address()` | `/Checkout/Address` `[Authorize]` | Step 1 view |
| `CheckoutController.Shipping()` | `/Checkout/Shipping` `[Authorize]` | Step 2 view |
| `CheckoutController.Payment()` | `/Checkout/Payment` `[Authorize]` | Step 3 view (fake payment — no real gateway) |
| `CheckoutController.PlaceOrder(CheckoutViewModel model)` | `/Checkout/PlaceOrder` POST `[Authorize][ValidateAntiForgeryToken]` | Calls `IOrderService.CreateOrder`, redirects to confirmation |
| `CheckoutController.Confirmation(int orderId)` | `/Checkout/Confirmation/5` | Final "Thank you" view |

**State across steps:** Since each step is a separate action/view, persist the in-progress checkout data (address, shipping method) in `TempData` or `Session["CheckoutState"]` until `PlaceOrder` commits it — don't write to the DB until the final step.

**jQuery pattern:** step tabs via jQuery UI Tabs or a simple show/hide wizard; `jQuery Validate` + Unobtrusive for client-side validation on each step's form before allowing "Next".

**Acceptance criteria:**
- Cannot reach `/Checkout/Payment` directly without having completed `/Checkout/Address` first (check `Session["CheckoutState"]` is populated, redirect back to step 1 if not)
- `PlaceOrder` re-validates stock via `IOrderService` (Spec 04's transaction logic) — a race condition where stock changed between adding to cart and checkout must fail gracefully with a message, not throw an unhandled exception
- Cart is cleared only after `PlaceOrder` succeeds

---

### SPEC 09 — Admin Module

**Routes, all under `[Authorize(Roles = "Admin")]`:**
| Action | Route |
|---|---|
| `AdminController.Products()` | `/Admin/Products` — list with edit/delete links |
| `AdminController.CreateProduct()` GET/POST | `/Admin/CreateProduct` |
| `AdminController.EditProduct(int id)` GET/POST | `/Admin/EditProduct/5` |
| `AdminController.DeleteProduct(int id)` POST | `/Admin/DeleteProduct/5` |
| `AdminController.Orders()` | `/Admin/Orders` — all orders, filter by status |
| `AdminController.UpdateOrderStatus(int orderId, string status)` | `/Admin/UpdateOrderStatus` POST |

**Setup requirement:** Seed one `Admin` role and one admin user manually (via `Startup`/seed method or SQL insert into `AspNetRoles`/`AspNetUserRoles`) since there's no self-service admin signup.

**Acceptance criteria:**
- Non-admin authenticated user hitting any `/Admin/*` route gets 403, not a login redirect (they ARE logged in, just not authorized)
- Deleting a product that has existing `OrderItem` rows referencing it either soft-deletes (`IsActive = 0`) or is blocked with a clear error — **do not hard-delete products with order history**, this breaks `OrderItem` FK integrity

---

### SPEC 10 — Cross-Cutting Concerns

**Security:**
- `@Html.AntiForgeryToken()` in every Razor form; `[ValidateAntiForgeryToken]` on every POST action
- Global AJAX header setup in `_Layout.cshtml`:
  ```javascript
  var token = $('input[name="__RequestVerificationToken"]').val();
  $.ajaxSetup({ headers: { 'RequestVerificationToken': token } });
  ```
- All `[Authorize]` boundaries re-verified server-side even when the UI already hides the button/link (never trust client-side hiding as the only control)

**Performance / Express constraints:**
- Add non-clustered indexes on `Product.CategoryId`, `CartItem.UserId`, `Orders.UserId` — Express has no SQL Agent, so there's no automated index maintenance; document a manual `ALTER INDEX ... REBUILD` script to run periodically
- Watch total DB size against the 10GB Express cap — plan an archiving strategy for old `Orders`/`OrderItem` rows as a stretch goal, not required for the learning project

**Bundling (`BundleConfig.cs`):**
```csharp
bundles.Add(new ScriptBundle("~/bundles/jquery").Include("~/Scripts/jquery-3.4.1.js"));
bundles.Add(new StyleBundle("~/Content/css").Include("~/Content/bootstrap.css", "~/Content/site.css"));
```

**Error handling:**
- Custom `Error.cshtml` for `HandleErrorAttribute`, plus explicit try/catch around `CreateOrder` in `CheckoutController.PlaceOrder` to show a friendly "stock changed" message instead of a YSOD (yellow screen of death)

**Acceptance criteria:** Running the full user journey (browse → filter → detail → add to cart → checkout → order confirmation → view in order history) end-to-end with dev tools open shows no unhandled JS errors, no missing anti-forgery token warnings, and no full-page reloads where the spec calls for AJAX.

---

## How to use this for spec-driven development

Work through Spec 00 → 10 in order. For each spec:
1. Paste just that spec section to your assistant (Claude Code, or Claude in this chat) as the task definition.
2. Implement until its **Acceptance criteria** pass.
3. Only then move to the next spec — later specs assume earlier ones are functionally complete, not just "written."

If you want, the next step can be turning any one of these specs into actual scaffolded code.
