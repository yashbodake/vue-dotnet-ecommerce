using System.Collections.Generic;
using Ecommerce.Core.Models;

namespace Ecommerce.Core.ViewModels
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; }
        public List<ProductImage> Images { get; set; } = new List<ProductImage>();
        public List<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public int SelectedVariantId { get; set; }
    }
}
