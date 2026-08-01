using System;
using System.Collections.Generic;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.Models;
using Ecommerce.Core.ViewModels;

namespace Ecommerce.Services
{
    /// <summary>
    /// Spec 02 contract stub. Business rules (transactions, stock) implemented in Spec 04.
    /// </summary>
    public class OrderService : IOrderService
    {
        public Order CreateOrder(string userId, string shippingAddress, CartViewModel cart)
        {
            throw new NotImplementedException("OrderService.CreateOrder — Spec 04");
        }

        public IEnumerable<Order> GetOrderHistory(string userId)
        {
            throw new NotImplementedException("OrderService.GetOrderHistory — Spec 04");
        }

        public Order GetOrderDetail(int orderId, string userId)
        {
            throw new NotImplementedException("OrderService.GetOrderDetail — Spec 04");
        }
    }
}
