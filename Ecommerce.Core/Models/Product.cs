using System;

namespace Ecommerce.Core.Models
{
    /// <summary>
    /// Domain POCO (service/view boundary). Separate from Ecommerce.Data EDMX entity.
    /// </summary>
    public class Product
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string ThumbnailUrl { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CategoryName { get; set; }
    }
}
