using System.Web.Mvc;

namespace Ecommerce.Web.Filters
{
    /// <summary>
    /// Spec 09: authenticated users without the role get HTTP 403 (not a login redirect).
    /// </summary>
    public class AuthorizeAdminAttribute : AuthorizeAttribute
    {
        public AuthorizeAdminAttribute()
        {
            Roles = "Admin";
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User != null &&
                filterContext.HttpContext.User.Identity != null &&
                filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                filterContext.Result = new HttpStatusCodeResult(403, "Forbidden");
            }
            else
            {
                base.HandleUnauthorizedRequest(filterContext);
            }
        }
    }
}
