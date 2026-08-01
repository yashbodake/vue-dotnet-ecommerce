using System;
using System.Web.Mvc;
using Ecommerce.Core.Interfaces;
using Ecommerce.Web.Helpers;

namespace Ecommerce.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        private string CurrentUserId()
        {
            return CartUserHelper.GetUserId(Session, HttpContext);
        }

        // Used by layout (@Html.Action) and by $('#mini-cart-container').load(...)
        // No [HttpGet] — child actions during POST (e.g. failed PlaceOrder) must still render.
        public ActionResult MiniCart()
        {
            var count = _cartService.GetItemCount(CurrentUserId());
            return PartialView("_MiniCart", count);
        }

        [HttpGet]
        public ActionResult Index()
        {
            var cart = _cartService.GetCart(CurrentUserId());
            ViewBag.MiniCartCount = cart.ItemCount;
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(int productId, int? variantId, int quantity = 1)
        {
            try
            {
                _cartService.AddItem(CurrentUserId(), productId, variantId, quantity);
                var cart = _cartService.GetCart(CurrentUserId());
                return Json(new { success = true, itemCount = cart.ItemCount, total = cart.Total });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int cartItemId, int quantity)
        {
            try
            {
                _cartService.UpdateQuantity(CurrentUserId(), cartItemId, quantity);
                var cart = _cartService.GetCart(CurrentUserId());
                var line = cart.Lines.Find(l => l.CartItemId == cartItemId);
                return Json(new
                {
                    success = true,
                    itemCount = cart.ItemCount,
                    total = cart.Total,
                    lineTotal = line != null ? line.LineTotal : 0m,
                    quantity = line != null ? line.Quantity : 0,
                    removed = line == null
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveItem(int cartItemId)
        {
            try
            {
                _cartService.RemoveItem(CurrentUserId(), cartItemId);
                var cart = _cartService.GetCart(CurrentUserId());
                return Json(new { success = true, itemCount = cart.ItemCount, total = cart.Total });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
