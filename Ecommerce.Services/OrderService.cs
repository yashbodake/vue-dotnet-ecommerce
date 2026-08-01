using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Transactions;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.Models;
using Ecommerce.Core.ViewModels;
using Ecommerce.Data;
using Ecommerce.Data.Repositories;
using Ecommerce.Services.Mapping;
using CoreOrder = Ecommerce.Core.Models.Order;
using DataOrder = Ecommerce.Data.Order;
using DataOrderItem = Ecommerce.Data.OrderItem;
using DataProductVariant = Ecommerce.Data.ProductVariant;

namespace Ecommerce.Services
{
    public class OrderService : IOrderService
    {
        private readonly EcommerceEntities _context;
        private readonly IProductRepository _productRepository;
        private readonly IRepository<DataOrder> _orderRepository;

        public OrderService(
            EcommerceEntities context,
            IProductRepository productRepository,
            IRepository<DataOrder> orderRepository)
        {
            _context = context;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
        }

        public CoreOrder CreateOrder(string userId, string shippingAddress, CartViewModel cart)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User id is required.", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(shippingAddress))
            {
                throw new ArgumentException("Shipping address is required.", nameof(shippingAddress));
            }

            if (cart == null || cart.Lines == null || cart.Lines.Count == 0)
            {
                throw new InvalidOperationException("Cannot create an order from an empty cart.");
            }

            using (var scope = new TransactionScope())
            {
                var order = new DataOrder
                {
                    UserId = userId,
                    ShippingAddress = shippingAddress.Trim(),
                    OrderDate = DateTime.Now,
                    Status = "Pending",
                    TotalAmount = 0
                };

                decimal total = 0;

                foreach (var line in cart.Lines)
                {
                    if (line.Quantity <= 0)
                    {
                        throw new InvalidOperationException("Cart line quantity must be at least 1.");
                    }

                    // Re-verify against DB — never trust cart unit price at checkout.
                    var product = _productRepository.GetById(line.ProductId);
                    if (product == null || !product.IsActive)
                    {
                        throw new InvalidOperationException("A product in the cart is no longer available.");
                    }

                    DataProductVariant variant = null;
                    if (line.ProductVariantId.HasValue)
                    {
                        variant = _context.ProductVariants.FirstOrDefault(v =>
                            v.ProductVariantId == line.ProductVariantId.Value &&
                            v.ProductId == product.ProductId);

                        if (variant == null)
                        {
                            throw new InvalidOperationException(
                                string.Format("Variant not found for product {0}.", product.Name));
                        }

                        if (variant.Stock < line.Quantity)
                        {
                            throw new InvalidOperationException(
                                string.Format("Insufficient stock for {0} ({1}).", product.Name, variant.Name));
                        }
                    }

                    if (product.Stock < line.Quantity)
                    {
                        throw new InvalidOperationException(
                            string.Format("Insufficient stock for {0}", product.Name));
                    }

                    product.Stock -= line.Quantity;
                    _productRepository.Update(product);

                    if (variant != null)
                    {
                        variant.Stock -= line.Quantity;
                        _context.Entry(variant).State = EntityState.Modified;
                    }

                    var unitPrice = EntityMapper.ResolveUnitPrice(product, variant);
                    order.OrderItems.Add(new DataOrderItem
                    {
                        ProductId = product.ProductId,
                        ProductVariantId = line.ProductVariantId,
                        Quantity = line.Quantity,
                        UnitPrice = unitPrice
                    });

                    total += unitPrice * line.Quantity;
                }

                order.TotalAmount = total;
                _orderRepository.Add(order);
                _orderRepository.Save();
                scope.Complete();

                return EntityMapper.ToCore(order);
            }
        }

        public IEnumerable<CoreOrder> GetOrderHistory(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Enumerable.Empty<CoreOrder>();
            }

            return _context.Orders
                .Include(o => o.OrderItems.Select(i => i.Product))
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList()
                .Select(EntityMapper.ToCore)
                .ToList();
        }

        public CoreOrder GetOrderDetail(int orderId, string userId)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems.Select(i => i.Product))
                .Include(o => o.OrderItems.Select(i => i.ProductVariant))
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null || order.UserId != userId)
            {
                return null; // caller maps to 403/404 (Spec 07)
            }

            return EntityMapper.ToCore(order);
        }

        public IEnumerable<CoreOrder> GetAllOrders(string statusFilter = null)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems.Select(i => i.Product))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                var status = statusFilter.Trim();
                query = query.Where(o => o.Status == status);
            }

            return query
                .OrderByDescending(o => o.OrderDate)
                .ToList()
                .Select(EntityMapper.ToCore)
                .ToList();
        }

        public CoreOrder UpdateOrderStatus(int orderId, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                throw new ArgumentException("Status is required.", nameof(status));
            }

            var allowed = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
            var normalized = status.Trim();
            if (!allowed.Contains(normalized))
            {
                throw new InvalidOperationException("Invalid order status.");
            }

            var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order == null)
            {
                throw new InvalidOperationException("Order not found.");
            }

            order.Status = normalized;
            _orderRepository.Update(order);
            _orderRepository.Save();
            return EntityMapper.ToCore(order);
        }
    }
}
