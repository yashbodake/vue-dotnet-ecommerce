using Ecommerce.Core.ViewModels;

namespace Ecommerce.Core.Interfaces
{
    public interface ICartService
    {
        CartViewModel GetCart(string userId);
        void AddItem(string userId, int productId, int? variantId, int quantity);
        void RemoveItem(string userId, int cartItemId);
        void UpdateQuantity(string userId, int cartItemId, int quantity);
        void ClearCart(string userId);
        void MergeGuestCart(string guestUserId, string authenticatedUserId);
    }
}
