using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.Models;
using Ecommerce.Core.ViewModels;
using Ecommerce.Data.Repositories;
using Ecommerce.Services.Mapping;
using CoreCategory = Ecommerce.Core.Models.Category;
using CoreProduct = Ecommerce.Core.Models.Product;
using DataCategory = Ecommerce.Data.Category;

namespace Ecommerce.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IRepository<DataCategory> _categoryRepository;

        public ProductService(
            IProductRepository productRepository,
            IRepository<DataCategory> categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public int GetProductCount()
        {
            return _productRepository.Query().Count(p => p.IsActive);
        }

        public IEnumerable<CoreCategory> GetCategories()
        {
            return _categoryRepository.GetAll()
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new CoreCategory
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    ParentCategoryId = c.ParentCategoryId,
                    DisplayOrder = c.DisplayOrder
                })
                .ToList();
        }

        public IEnumerable<CoreProduct> GetByCategory(int categoryId)
        {
            return _productRepository
                .GetByCategoryWithVariants(categoryId)
                .Select(EntityMapper.ToCore)
                .ToList();
        }

        public ProductDetailViewModel GetDetail(int productId)
        {
            var entity = _productRepository.GetByIdWithDetails(productId);
            if (entity == null || !entity.IsActive)
            {
                return null;
            }

            return new ProductDetailViewModel
            {
                Product = EntityMapper.ToCore(entity),
                Images = entity.ProductImages
                    .OrderBy(i => i.DisplayOrder)
                    .Select(EntityMapper.ToCore)
                    .ToList(),
                Variants = entity.ProductVariants
                    .Select(EntityMapper.ToCore)
                    .ToList(),
                SelectedVariantId = entity.ProductVariants
                    .Select(v => v.ProductVariantId)
                    .FirstOrDefault()
            };
        }

        public PagedResult<CoreProduct> Filter(ProductFilterCriteria criteria)
        {
            criteria = criteria ?? new ProductFilterCriteria();
            var page = criteria.Page < 1 ? 1 : criteria.Page;
            var pageSize = criteria.PageSize < 1 ? 12 : criteria.PageSize;

            var query = _productRepository.Query()
                .Include(p => p.Category)
                .Where(p => p.IsActive);

            if (criteria.CategoryIds != null && criteria.CategoryIds.Count > 0)
            {
                var ids = criteria.CategoryIds.ToList();
                query = query.Where(p => ids.Contains(p.CategoryId));
            }

            if (criteria.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= criteria.MinPrice.Value);
            }

            if (criteria.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= criteria.MaxPrice.Value);
            }

            if (criteria.InStockOnly)
            {
                query = query.Where(p => p.Stock > 0);
            }

            if (!string.IsNullOrWhiteSpace(criteria.Search))
            {
                var term = criteria.Search.Trim();
                query = query.Where(p =>
                    p.Name.Contains(term) ||
                    (p.Description != null && p.Description.Contains(term)));
            }

            switch ((criteria.SortBy ?? "name").ToLowerInvariant())
            {
                case "price_asc":
                    query = query.OrderBy(p => p.Price).ThenBy(p => p.Name);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(p => p.Price).ThenBy(p => p.Name);
                    break;
                case "newest":
                    query = query.OrderByDescending(p => p.CreatedDate).ThenBy(p => p.Name);
                    break;
                default:
                    query = query.OrderBy(p => p.Name);
                    break;
            }

            var totalCount = query.Count();
            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList()
                .Select(EntityMapper.ToCore)
                .ToList();

            return new PagedResult<CoreProduct>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
