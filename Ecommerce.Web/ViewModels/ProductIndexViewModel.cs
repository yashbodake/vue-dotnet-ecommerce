using System.Collections.Generic;
using Ecommerce.Core.Models;

namespace Ecommerce.Web.ViewModels
{
    public class ProductIndexViewModel
    {
        public IList<Category> Categories { get; set; } = new List<Category>();
        public ProductFilterCriteria Criteria { get; set; } = new ProductFilterCriteria();
    }
}
