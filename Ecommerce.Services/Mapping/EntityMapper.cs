using System.Collections.Generic;
using System.Linq;
using Ecommerce.Core.Models;
using Ecommerce.Core.ViewModels;
using DataOrder = Ecommerce.Data.Order;
using DataOrderItem = Ecommerce.Data.OrderItem;
using DataProduct = Ecommerce.Data.Product;
using DataProductImage = Ecommerce.Data.ProductImage;
using DataProductVariant = Ecommerce.Data.ProductVariant;

namespace Ecommerce.Services.Mapping
{
    internal static class EntityMapper
    {
        public static Product ToCore(DataProduct entity)
        {
            if (entity == null) return null;
            return new Product
            {
                ProductId = entity.ProductId,
                CategoryId = entity.CategoryId,
                Name = entity.Name,
                Description = entity.Description,
                Price = entity.Price,
                ThumbnailUrl = entity.ThumbnailUrl,
                Stock = entity.Stock,
                IsActive = entity.IsActive,
                CreatedDate = entity.CreatedDate,
                CategoryName = entity.Category != null ? entity.Category.Name : null
            };
        }

        public static ProductImage ToCore(DataProductImage entity)
        {
            if (entity == null) return null;
            return new ProductImage
            {
                ProductImageId = entity.ProductImageId,
                ProductId = entity.ProductId,
                Url = entity.Url,
                DisplayOrder = entity.DisplayOrder
            };
        }

        public static ProductVariant ToCore(DataProductVariant entity)
        {
            if (entity == null) return null;
            return new ProductVariant
            {
                ProductVariantId = entity.ProductVariantId,
                ProductId = entity.ProductId,
                Name = entity.Name,
                SkuSuffix = entity.SkuSuffix,
                Stock = entity.Stock,
                PriceAdjustment = entity.PriceAdjustment
            };
        }

        public static Order ToCore(DataOrder entity)
        {
            if (entity == null) return null;
            return new Order
            {
                OrderId = entity.OrderId,
                UserId = entity.UserId,
                OrderDate = entity.OrderDate,
                Status = entity.Status,
                ShippingAddress = entity.ShippingAddress,
                TotalAmount = entity.TotalAmount,
                Items = entity.OrderItems == null
                    ? new List<OrderItem>()
                    : entity.OrderItems.Select(ToCore).ToList()
            };
        }

        public static OrderItem ToCore(DataOrderItem entity)
        {
            if (entity == null) return null;
            return new OrderItem
            {
                OrderItemId = entity.OrderItemId,
                OrderId = entity.OrderId,
                ProductId = entity.ProductId,
                ProductVariantId = entity.ProductVariantId,
                Quantity = entity.Quantity,
                UnitPrice = entity.UnitPrice,
                ProductName = entity.Product != null ? entity.Product.Name : null
            };
        }

        public static decimal ResolveUnitPrice(DataProduct product, DataProductVariant variant)
        {
            var adjustment = variant != null ? variant.PriceAdjustment : 0m;
            return product.Price + adjustment;
        }

        public static CartLineViewModel ToCartLine(
            int cartItemId,
            DataProduct product,
            DataProductVariant variant,
            int quantity)
        {
            return new CartLineViewModel
            {
                CartItemId = cartItemId,
                ProductId = product.ProductId,
                ProductVariantId = variant != null ? (int?)variant.ProductVariantId : null,
                ProductName = product.Name,
                VariantName = variant != null ? variant.Name : null,
                UnitPrice = ResolveUnitPrice(product, variant),
                Quantity = quantity
            };
        }
    }
}
