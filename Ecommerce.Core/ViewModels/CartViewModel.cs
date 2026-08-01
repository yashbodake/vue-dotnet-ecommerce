using System.Collections.Generic;
using System.Linq;

namespace Ecommerce.Core.ViewModels
{
    public class CartViewModel
    {
        public List<CartLineViewModel> Lines { get; set; } = new List<CartLineViewModel>();
        public decimal Total => Lines == null ? 0 : Lines.Sum(l => l.LineTotal);
        public int ItemCount => Lines == null ? 0 : Lines.Sum(l => l.Quantity);
    }
}
