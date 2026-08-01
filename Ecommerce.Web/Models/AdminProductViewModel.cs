using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Web.Models
{
    public class AdminProductViewModel
    {
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Required]
        [Range(0, 999999)]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Display(Name = "Thumbnail URL")]
        [StringLength(300)]
        public string ThumbnailUrl { get; set; }

        [Required]
        [Range(0, 999999)]
        public int Stock { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
