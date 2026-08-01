using System;
using System.Web.Mvc;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.ViewModels;
using Ecommerce.Web.Helpers;
using Ecommerce.Web.Models;
using Microsoft.AspNet.Identity;

namespace Ecommerce.Web.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private const string CheckoutSessionKey = "CheckoutState";

        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;

        public CheckoutController(ICartService cartService, IOrderService orderService)
        {
            _cartService = cartService;
            _orderService = orderService;
        }

        private string CurrentUserId()
        {
            return User.Identity.GetUserId() ?? CartUserHelper.GetUserId(Session, HttpContext);
        }

        private CheckoutViewModel GetState()
        {
            return Session[CheckoutSessionKey] as CheckoutViewModel;
        }

        private void SaveState(CheckoutViewModel state)
        {
            Session[CheckoutSessionKey] = state;
        }

        private void ClearState()
        {
            Session.Remove(CheckoutSessionKey);
        }

        private CartViewModel CurrentCart()
        {
            return _cartService.GetCart(CurrentUserId());
        }

        private ActionResult EnsureCartNotEmpty()
        {
            var cart = CurrentCart();
            if (cart == null || cart.Lines == null || cart.ItemCount == 0)
            {
                TempData["CheckoutError"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            return null;
        }

        // GET: /Checkout/Address
        [HttpGet]
        public ActionResult Address()
        {
            var empty = EnsureCartNotEmpty();
            if (empty != null) return empty;

            var state = GetState() ?? new CheckoutViewModel();
            ViewBag.Cart = CurrentCart();
            ViewBag.Step = 1;
            return View(state);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Address(CheckoutViewModel model)
        {
            var empty = EnsureCartNotEmpty();
            if (empty != null) return empty;

            // Only validate address fields on this step
            ModelState.Remove("ShippingMethod");
            ModelState.Remove("CardName");
            ModelState.Remove("CardNumber");
            ModelState.Remove("CardExpiry");
            ModelState.Remove("CardCvv");

            if (!ModelState.IsValid)
            {
                ViewBag.Cart = CurrentCart();
                ViewBag.Step = 1;
                return View(model);
            }

            var state = GetState() ?? new CheckoutViewModel();
            state.FullName = model.FullName;
            state.AddressLine1 = model.AddressLine1;
            state.AddressLine2 = model.AddressLine2;
            state.City = model.City;
            state.State = model.State;
            state.PostalCode = model.PostalCode;
            state.Country = model.Country;
            state.CompletedStep = Math.Max(state.CompletedStep, 1);
            SaveState(state);

            return RedirectToAction("Shipping");
        }

        // GET: /Checkout/Shipping
        [HttpGet]
        public ActionResult Shipping()
        {
            var empty = EnsureCartNotEmpty();
            if (empty != null) return empty;

            var state = GetState();
            if (state == null || state.CompletedStep < 1)
            {
                return RedirectToAction("Address");
            }

            if (state.ShippingMethod != "Standard" && state.ShippingMethod != "Express")
            {
                state.ShippingMethod = "Standard";
            }

            ViewBag.Cart = CurrentCart();
            ViewBag.Step = 2;
            return View(state);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Shipping(CheckoutViewModel model)
        {
            var empty = EnsureCartNotEmpty();
            if (empty != null) return empty;

            var state = GetState();
            if (state == null || state.CompletedStep < 1)
            {
                return RedirectToAction("Address");
            }

            ModelState.Remove("FullName");
            ModelState.Remove("AddressLine1");
            ModelState.Remove("AddressLine2");
            ModelState.Remove("City");
            ModelState.Remove("State");
            ModelState.Remove("PostalCode");
            ModelState.Remove("Country");
            ModelState.Remove("CardName");
            ModelState.Remove("CardNumber");
            ModelState.Remove("CardExpiry");
            ModelState.Remove("CardCvv");

            // Accept only known codes (labels in the view; avoids Unicode dash encoding issues)
            var method = (model.ShippingMethod ?? string.Empty).Trim();
            if (method != "Standard" && method != "Express")
            {
                ModelState.AddModelError("ShippingMethod", "Choose a shipping method.");
            }

            if (!ModelState.IsValid)
            {
                // Keep prior address fields on the model for redisplay
                model.FullName = state.FullName;
                model.AddressLine1 = state.AddressLine1;
                model.AddressLine2 = state.AddressLine2;
                model.City = state.City;
                model.State = state.State;
                model.PostalCode = state.PostalCode;
                model.Country = state.Country;
                model.ShippingMethod = method == "Express" ? "Express" : "Standard";
                ViewBag.Cart = CurrentCart();
                ViewBag.Step = 2;
                return View(model);
            }

            state.ShippingMethod = method;
            state.CompletedStep = Math.Max(state.CompletedStep, 2);
            SaveState(state);

            return RedirectToAction("Payment");
        }

        // GET: /Checkout/Payment
        [HttpGet]
        public ActionResult Payment()
        {
            var empty = EnsureCartNotEmpty();
            if (empty != null) return empty;

            var state = GetState();
            if (state == null || state.CompletedStep < 1)
            {
                return RedirectToAction("Address");
            }

            if (state.CompletedStep < 2)
            {
                return RedirectToAction("Shipping");
            }

            ViewBag.Cart = CurrentCart();
            ViewBag.Step = 3;
            return View(state);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(CheckoutViewModel model)
        {
            var empty = EnsureCartNotEmpty();
            if (empty != null) return empty;

            var state = GetState();
            if (state == null || state.CompletedStep < 2)
            {
                return RedirectToAction("Address");
            }

            ModelState.Remove("FullName");
            ModelState.Remove("AddressLine1");
            ModelState.Remove("AddressLine2");
            ModelState.Remove("City");
            ModelState.Remove("State");
            ModelState.Remove("PostalCode");
            ModelState.Remove("Country");
            ModelState.Remove("ShippingMethod");

            // Fake payment — require basic card fields for the demo form
            if (string.IsNullOrWhiteSpace(model.CardName))
            {
                ModelState.AddModelError("CardName", "Cardholder name is required.");
            }
            if (string.IsNullOrWhiteSpace(model.CardNumber) || model.CardNumber.Replace(" ", "").Length < 12)
            {
                ModelState.AddModelError("CardNumber", "Enter a valid card number (demo).");
            }
            if (string.IsNullOrWhiteSpace(model.CardExpiry))
            {
                ModelState.AddModelError("CardExpiry", "Expiry is required.");
            }
            if (string.IsNullOrWhiteSpace(model.CardCvv))
            {
                ModelState.AddModelError("CardCvv", "CVV is required.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Cart = CurrentCart();
                ViewBag.Step = 3;
                // Keep address/shipping from session
                model.FullName = state.FullName;
                model.AddressLine1 = state.AddressLine1;
                model.AddressLine2 = state.AddressLine2;
                model.City = state.City;
                model.State = state.State;
                model.PostalCode = state.PostalCode;
                model.Country = state.Country;
                model.ShippingMethod = state.ShippingMethod;
                return View("Payment", model);
            }

            state.CardName = model.CardName;
            state.CardNumber = model.CardNumber;
            state.CardExpiry = model.CardExpiry;
            state.CardCvv = model.CardCvv;
            SaveState(state);

            var userId = CurrentUserId();
            var cart = CurrentCart();
            var shippingAddress = state.FormattedShippingAddress + " (" + state.ShippingMethod + ")";

            try
            {
                var order = _orderService.CreateOrder(userId, shippingAddress, cart);
                _cartService.ClearCart(userId);
                ClearState();
                return RedirectToAction("Confirmation", new { orderId = order.OrderId });
            }
            catch (InvalidOperationException ex)
            {
                // Stock race / business rule — friendly message, no YSOD; PRG so layout child actions work
                TempData["CheckoutError"] = ex.Message;
                return RedirectToAction("Payment");
            }
            catch (Exception)
            {
                TempData["CheckoutError"] = "We could not place your order. Please try again.";
                return RedirectToAction("Payment");
            }
        }

        // GET: /Checkout/Confirmation/5
        [HttpGet]
        public ActionResult Confirmation(int orderId)
        {
            var userId = CurrentUserId();
            var order = _orderService.GetOrderDetail(orderId, userId);
            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }
    }
}
