using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.Models;
using Ecommerce.Core.ViewModels;
using Ecommerce.Web.ViewModels;

namespace Ecommerce.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        // GET: / or /Product
        public ActionResult Index()
        {
            var criteria = new ProductFilterCriteria();
            var model = new ProductIndexViewModel
            {
                Categories = _productService.GetCategories().ToList(),
                Criteria = criteria,
                InitialResults = _productService.Filter(criteria)
            };
            return View(model);
        }

        // GET: /Product/Filter (AJAX)
        public ActionResult Filter(ProductFilterCriteria criteria)
        {
            // Checkboxes may post as CategoryIds or as multiple values; also support comma-less binder defaults.
            if (criteria == null)
            {
                criteria = new ProductFilterCriteria();
            }

            if (criteria.CategoryIds == null)
            {
                criteria.CategoryIds = new List<int>();
            }

            // MVC sometimes binds a single checkbox oddly — normalize from form if needed
            var raw = Request.QueryString.GetValues("CategoryIds");
            if (raw != null && raw.Length > 0)
            {
                criteria.CategoryIds = raw
                    .Select(v => { int id; return int.TryParse(v, out id) ? (int?)id : null; })
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .Distinct()
                    .ToList();
            }

            var result = _productService.Filter(criteria);
            return PartialView("_ProductList", result);
        }

        // GET: /Product/Detail/5
        public ActionResult Detail(int id)
        {
            ProductDetailViewModel model = _productService.GetDetail(id);
            if (model == null)
            {
                return HttpNotFound();
            }

            return View(model);
        }
    }
}
