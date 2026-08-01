using System;
using System.Web.Mvc;

namespace Ecommerce.Web.Controllers
{
    /// <summary>Spec 10 — intentional fault used to verify HandleErrorAttribute / Error.cshtml.</summary>
    public class ErrorDemoController : Controller
    {
        [HttpGet]
        public ActionResult Boom()
        {
            throw new InvalidOperationException("Spec 10 error-page demo.");
        }
    }
}
