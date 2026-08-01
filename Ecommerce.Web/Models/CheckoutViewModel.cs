using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Web.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [Display(Name = "Full name")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Address line is required.")]
        [Display(Name = "Address")]
        [StringLength(200)]
        public string AddressLine1 { get; set; }

        [Display(Name = "Address line 2")]
        [StringLength(200)]
        public string AddressLine2 { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100)]
        public string City { get; set; }

        [Required(ErrorMessage = "State / region is required.")]
        [Display(Name = "State / Region")]
        [StringLength(100)]
        public string State { get; set; }

        [Required(ErrorMessage = "Postal code is required.")]
        [Display(Name = "Postal code")]
        [StringLength(20)]
        public string PostalCode { get; set; }

        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100)]
        public string Country { get; set; }

        [Required(ErrorMessage = "Choose a shipping method.")]
        [Display(Name = "Shipping method")]
        public string ShippingMethod { get; set; }

        [Display(Name = "Cardholder name")]
        [StringLength(100)]
        public string CardName { get; set; }

        [Display(Name = "Card number")]
        [StringLength(19)]
        public string CardNumber { get; set; }

        [Display(Name = "Expiry (MM/YY)")]
        [StringLength(5)]
        public string CardExpiry { get; set; }

        [Display(Name = "CVV")]
        [StringLength(4)]
        public string CardCvv { get; set; }

        public string FormattedShippingAddress
        {
            get
            {
                var line2 = string.IsNullOrWhiteSpace(AddressLine2) ? "" : AddressLine2 + ", ";
                return string.Format("{0}, {1}{2}, {3}, {4} {5}, {6}",
                    FullName,
                    line2,
                    AddressLine1,
                    City,
                    State,
                    PostalCode,
                    Country).Replace("  ", " ").Trim();
            }
        }

        public int CompletedStep { get; set; }
    }
}
