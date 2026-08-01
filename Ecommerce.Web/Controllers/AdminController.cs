using System;
using System.Linq;
using System.Web.Mvc;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.Models;
using Ecommerce.Web.Filters;
using Ecommerce.Web.Models;

namespace Ecommerce.Web.Controllers
{
    [AuthorizeAdmin]
    public class AdminController : Controller
    {
        private static readonly string[] OrderStatuses =
        {
            "Pending", "Processing", "Shipped", "Delivered", "Cancelled"
        };

        private readonly IProductService _productService;
        private readonly IOrderService _orderService;

        public AdminController(IProductService productService, IOrderService orderService)
        {
            _productService = productService;
            _orderService = orderService;
        }

        // GET: /Admin/Products
        [HttpGet]
        public ActionResult Products()
        {
            var products = _productService.GetAllForAdmin();
            return View(products);
        }

        // GET: /Admin/CreateProduct
        [HttpGet]
        public ActionResult CreateProduct()
        {
            PopulateCategories();
            return View(new AdminProductViewModel { IsActive = true, Stock = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProduct(AdminProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateCategories();
                return View(model);
            }

            try
            {
                _productService.CreateProduct(ToCore(model));
                TempData["AdminMessage"] = "Product created.";
                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                PopulateCategories();
                return View(model);
            }
        }

        // GET: /Admin/EditProduct/5
        [HttpGet]
        public ActionResult EditProduct(int id)
        {
            var product = _productService.GetByIdForAdmin(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            PopulateCategories();
            return View(ToViewModel(product));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProduct(AdminProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateCategories();
                return View(model);
            }

            try
            {
                _productService.UpdateProduct(ToCore(model));
                TempData["AdminMessage"] = "Product updated.";
                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                PopulateCategories();
                return View(model);
            }
        }

        // POST: /Admin/DeleteProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteProduct(int id)
        {
            try
            {
                _productService.SoftDeleteProduct(id);
                TempData["AdminMessage"] = "Product deactivated (soft-deleted).";
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = ex.Message;
            }

            return RedirectToAction("Products");
        }

        // GET: /Admin/Orders?status=Pending
        [HttpGet]
        public ActionResult Orders(string status)
        {
            ViewBag.Statuses = OrderStatuses;
            ViewBag.SelectedStatus = status;
            var orders = _orderService.GetAllOrders(status);
            return View(orders);
        }

        // POST: /Admin/UpdateOrderStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateOrderStatus(int orderId, string status)
        {
            try
            {
                _orderService.UpdateOrderStatus(orderId, status);
                TempData["AdminMessage"] = "Order #" + orderId + " updated to " + status + ".";
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = ex.Message;
            }

            return RedirectToAction("Orders", new { status = Request.Form["filterStatus"] });
        }

        private void PopulateCategories()
        {
            ViewBag.Categories = _productService.GetCategories()
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                })
                .ToList();
        }

        private static Product ToCore(AdminProductViewModel model)
        {
            return new Product
            {
                ProductId = model.ProductId,
                CategoryId = model.CategoryId,
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                ThumbnailUrl = model.ThumbnailUrl,
                Stock = model.Stock,
                IsActive = model.IsActive
            };
        }

        private static AdminProductViewModel ToViewModel(Product product)
        {
            return new AdminProductViewModel
            {
                ProductId = product.ProductId,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ThumbnailUrl = product.ThumbnailUrl,
                Stock = product.Stock,
                IsActive = product.IsActive
            };
        }
    }
}
