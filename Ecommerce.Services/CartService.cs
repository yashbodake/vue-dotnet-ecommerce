using System;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.ViewModels;

namespace Ecommerce.Services
{
    /// <summary>
    /// Spec 02 contract stub. Business rules implemented in Spec 04 / Spec 06.
    /// </summary>
    public class CartService : ICartService
    {
        public CartViewModel GetCart(string userId)
        {
            throw new NotImplementedException("CartService.GetCart — Spec 04/06");
        }

        public void AddItem(string userId, int productId, int? variantId, int quantity)
        {
            throw new NotImplementedException("CartService.AddItem — Spec 04/06");
        }

        public void RemoveItem(string userId, int cartItemId)
        {
            throw new NotImplementedException("CartService.RemoveItem — Spec 04/06");
        }

        public void UpdateQuantity(string userId, int cartItemId, int quantity)
        {
            throw new NotImplementedException("CartService.UpdateQuantity — Spec 04/06");
        }
    }
}
