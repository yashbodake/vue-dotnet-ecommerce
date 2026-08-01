using System;
using System.Collections.Generic;
using System.Linq;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.Models;
using Ecommerce.Core.ViewModels;
using Ecommerce.Data;
using CoreProduct = Ecommerce.Core.Models.Product;
using DataProduct = Ecommerce.Data.Product;

namespace Ecommerce.Services
{
    public class ProductService : IProductService
    {
        private readonly IRepository<DataProduct> _productRepository;

        public ProductService(IRepository<DataProduct> productRepository)
        {
            _productRepository = productRepository;
        }

        public int GetProductCount()
        {
            return _productRepository.GetAll().Count();
        }

        public IEnumerable<CoreProduct> GetByCategory(int categoryId)
        {
            // Full filter/mapping logic lands in Spec 04.
            throw new NotImplementedException("ProductService.GetByCategory — Spec 04");
        }

        public ProductDetailViewModel GetDetail(int productId)
        {
            throw new NotImplementedException("ProductService.GetDetail — Spec 04");
        }

        public PagedResult<CoreProduct> Filter(ProductFilterCriteria criteria)
        {
            throw new NotImplementedException("ProductService.Filter — Spec 04");
        }
    }
}
