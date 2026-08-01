using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce.Data
{
    [Table("ProductVariant")]
    public partial class ProductVariant
    {
        [Key]
        public int ProductVariantId { get; set; }

        public int ProductId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(50)]
        public string SkuSuffix { get; set; }

        public int Stock { get; set; }

        public decimal PriceAdjustment { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}
