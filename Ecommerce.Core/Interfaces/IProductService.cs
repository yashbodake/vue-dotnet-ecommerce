using System.Collections.Generic;
using Ecommerce.Core.Models;
using Ecommerce.Core.ViewModels;

namespace Ecommerce.Core.Interfaces
{
    public interface IProductService
    {
        /// <summary>Spec 01 smoke-test helper — counts products in the catalog.</summary>
        int GetProductCount();

        IEnumerable<Category> GetCategories();
        IEnumerable<Product> GetByCategory(int categoryId);
        ProductDetailViewModel GetDetail(int productId);
        PagedResult<Product> Filter(ProductFilterCriteria criteria);

        // Spec 09 — admin
        IEnumerable<Product> GetAllForAdmin();
        Product GetByIdForAdmin(int productId);
        Product CreateProduct(Product product);
        Product UpdateProduct(Product product);
        void SoftDeleteProduct(int productId);
    }
}
