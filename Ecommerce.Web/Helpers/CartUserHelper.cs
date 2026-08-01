using System.Web;
using Microsoft.AspNet.Identity;

namespace Ecommerce.Web.Helpers
{
    /// <summary>
    /// Cart keyed by Identity user id when authenticated; otherwise a stable guest session id.
    /// </summary>
    public static class CartUserHelper
    {
        private const string SessionKey = "CartUserId";

        public static string GetUserId(HttpSessionStateBase session, HttpContextBase httpContext)
        {
            if (httpContext != null &&
                httpContext.User != null &&
                httpContext.User.Identity != null &&
                httpContext.User.Identity.IsAuthenticated)
            {
                var id = httpContext.User.Identity.GetUserId();
                if (!string.IsNullOrWhiteSpace(id))
                {
                    return id;
                }
            }

            if (session == null)
            {
                return "guest-anonymous";
            }

            var existing = session[SessionKey] as string;
            if (!string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }

            var guestId = "guest-" + System.Guid.NewGuid().ToString("N");
            session[SessionKey] = guestId;
            return guestId;
        }
    }
}
