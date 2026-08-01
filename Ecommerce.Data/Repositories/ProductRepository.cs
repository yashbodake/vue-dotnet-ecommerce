using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Ecommerce.Data.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(EcommerceEntities context)
            : base(context)
        {
        }

        public IEnumerable<Product> GetByCategoryWithVariants(int categoryId)
        {
            return Context.Products
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId && p.IsActive)
                .OrderBy(p => p.Name)
                .ToList();
        }

        public Product GetByIdWithDetails(int productId)
        {
            return Context.Products
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .FirstOrDefault(p => p.ProductId == productId);
        }

        public IEnumerable<Product> GetActiveProducts()
        {
            return Context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToList();
        }

        public IQueryable<Product> Query()
        {
            return Context.Products.AsQueryable();
        }
    }
}
