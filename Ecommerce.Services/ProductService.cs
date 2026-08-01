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
            // Only categories that currently have active products (keeps filters meaningful after reseeds)
            var activeCategoryIds = _productRepository.Query()
                .Where(p => p.IsActive)
                .Select(p => p.CategoryId)
                .Distinct()
                .ToList();

            return _categoryRepository.GetAll()
                .Where(c => activeCategoryIds.Contains(c.CategoryId))
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

        public IEnumerable<CoreProduct> GetAllForAdmin()
        {
            return _productRepository.Query()
                .Include(p => p.Category)
                .OrderByDescending(p => p.IsActive)
                .ThenBy(p => p.Name)
                .ToList()
                .Select(EntityMapper.ToCore)
                .ToList();
        }

        public CoreProduct GetByIdForAdmin(int productId)
        {
            var entity = _productRepository.Query()
                .Include(p => p.Category)
                .FirstOrDefault(p => p.ProductId == productId);
            return entity == null ? null : EntityMapper.ToCore(entity);
        }

        public CoreProduct CreateProduct(CoreProduct product)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            ValidateProduct(product);

            var entity = new Ecommerce.Data.Product
            {
                CategoryId = product.CategoryId,
                Name = product.Name.Trim(),
                Description = product.Description,
                Price = product.Price,
                ThumbnailUrl = string.IsNullOrWhiteSpace(product.ThumbnailUrl) ? null : product.ThumbnailUrl.Trim(),
                Stock = product.Stock,
                IsActive = product.IsActive,
                CreatedDate = DateTime.Now
            };

            _productRepository.Add(entity);
            _productRepository.Save();
            return EntityMapper.ToCore(entity);
        }

        public CoreProduct UpdateProduct(CoreProduct product)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            ValidateProduct(product);

            var entity = _productRepository.GetById(product.ProductId);
            if (entity == null)
            {
                throw new InvalidOperationException("Product not found.");
            }

            entity.CategoryId = product.CategoryId;
            entity.Name = product.Name.Trim();
            entity.Description = product.Description;
            entity.Price = product.Price;
            entity.ThumbnailUrl = string.IsNullOrWhiteSpace(product.ThumbnailUrl) ? null : product.ThumbnailUrl.Trim();
            entity.Stock = product.Stock;
            entity.IsActive = product.IsActive;

            _productRepository.Update(entity);
            _productRepository.Save();
            return EntityMapper.ToCore(entity);
        }

        public void SoftDeleteProduct(int productId)
        {
            var entity = _productRepository.GetById(productId);
            if (entity == null)
            {
                throw new InvalidOperationException("Product not found.");
            }

            // Soft-delete only — never hard-delete (OrderItem FK integrity).
            entity.IsActive = false;
            _productRepository.Update(entity);
            _productRepository.Save();
        }

        private static void ValidateProduct(CoreProduct product)
        {
            if (string.IsNullOrWhiteSpace(product.Name))
            {
                throw new ArgumentException("Product name is required.");
            }

            if (product.CategoryId <= 0)
            {
                throw new ArgumentException("Category is required.");
            }

            if (product.Price < 0)
            {
                throw new ArgumentException("Price cannot be negative.");
            }

            if (product.Stock < 0)
            {
                throw new ArgumentException("Stock cannot be negative.");
            }
        }
    }
}
