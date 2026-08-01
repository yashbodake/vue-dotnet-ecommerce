using System.Web.Mvc;
using Ecommerce.Core.Interfaces;
using Ecommerce.Web.Helpers;

namespace Ecommerce.Web.Filters
{
    /// <summary>
    /// Sets ViewBag.MiniCartCount so the layout can render the badge without Html.Action
    /// (nested child actions + session are a common source of sluggish full-page loads).
    /// </summary>
    public class MiniCartCountAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext == null || filterContext.IsChildAction)
            {
                return;
            }

            var controller = filterContext.Controller as Controller;
            if (controller == null)
            {
                return;
            }

            if (controller.ViewBag.MiniCartCount != null)
            {
                return;
            }

            // Partials/JSON/content results still use the layout only for ViewResult
            if (!(filterContext.Result is ViewResult))
            {
                return;
            }

            var cartService = DependencyResolver.Current.GetService<ICartService>();
            if (cartService == null)
            {
                controller.ViewBag.MiniCartCount = 0;
                return;
            }

            var userId = CartUserHelper.GetUserId(controller.Session, controller.HttpContext);
            controller.ViewBag.MiniCartCount = cartService.GetItemCount(userId);
        }
    }
}
