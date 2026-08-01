using System.Web.Mvc;
using Ecommerce.Core.Interfaces;

namespace Ecommerce.Web.Controllers
{
    /// <summary>
    /// Throwaway Spec 01 smoke test: proves Web → Services → Data → SQL Express via Unity DI.
    /// </summary>
    public class TestController : Controller
    {
        private readonly IProductService _productService;

        public TestController(IProductService productService)
        {
            _productService = productService;
        }

        // GET: /Test
        public ActionResult Index()
        {
            var count = _productService.GetProductCount();
            return Content("OK — product count: " + count);
        }
    }
}
