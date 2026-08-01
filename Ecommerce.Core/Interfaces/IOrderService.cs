using System.Collections.Generic;
using Ecommerce.Core.Models;
using Ecommerce.Core.ViewModels;

namespace Ecommerce.Core.Interfaces
{
    public interface IOrderService
    {
        Order CreateOrder(string userId, string shippingAddress, CartViewModel cart);
        IEnumerable<Order> GetOrderHistory(string userId);
        Order GetOrderDetail(int orderId, string userId);

        // Spec 09 — admin
        IEnumerable<Order> GetAllOrders(string statusFilter = null);
        Order UpdateOrderStatus(int orderId, string status);
    }
}
