namespace Ecommerce.Core.Models
{
    public class ProductImage
    {
        public int ProductImageId { get; set; }
        public int ProductId { get; set; }
        public string Url { get; set; }
        public int DisplayOrder { get; set; }
    }
}
