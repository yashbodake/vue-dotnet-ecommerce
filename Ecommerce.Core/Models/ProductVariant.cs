namespace Ecommerce.Core.Models
{
    public class ProductVariant
    {
        public int ProductVariantId { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string SkuSuffix { get; set; }
        public int Stock { get; set; }
        public decimal PriceAdjustment { get; set; }
    }
}
