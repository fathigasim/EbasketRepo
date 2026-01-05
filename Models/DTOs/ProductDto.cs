using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureApi.Models.DTOs
{
    public class ProductDto
    {
        public string? Id { get; set; }
        //[Required]
        [Required(
         ErrorMessageResourceName = "ProductNameRequired")]
        //     ,
        //ErrorMessageResourceType = typeof(CommonResources))]
        public string? Name { get; set; }

        [Display(Name = "Name")]
        [Range(1, 20)]
        //ErrorMessageResourceName = "Less_than_or_exceeded_range_price"
        //   , ErrorMessageResourceType = typeof(CommonResources))]
        [Required(
         ErrorMessageResourceName = "PriceRequired")]
       //     ,
       //ErrorMessageResourceType = typeof(CommonResources))]
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImagePath { get; set; }
        [NotMapped]
        [Required(
         ErrorMessageResourceName = "ImageRequired")]
        //     ,
        //ErrorMessageResourceType = typeof(CommonResources))]
        public IFormFile Image { get; set; } = default!;

        public bool IsActive { get; set; } =true;
        public int Stock { get; set; }
        public int DiscountPercentage { get; set; }
        public bool IsOnSale { get; set; }=true;
        public DateTime? UpdatedAt { get; set; }

    }
}
