using System.Collections.Generic;
using System.Linq;
using Ecommerce.Core.Interfaces;

namespace Ecommerce.Data.Repositories
{
    /// <summary>
    /// Product-specific queries beyond generic CRUD (eager loading for catalog/detail).
    /// Lives in Data so it can return EF entities; Core keeps only the open <see cref="IRepository{T}"/>.
    /// </summary>
    public interface IProductRepository : IRepository<Product>
    {
        IEnumerable<Product> GetByCategoryWithVariants(int categoryId);
        Product GetByIdWithDetails(int productId);
        IEnumerable<Product> GetActiveProducts();
        IQueryable<Product> Query();
    }
}
