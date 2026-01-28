using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureApi.Models.DTOs
{
    public class ProductDto
    {
     
        public string? Id { get; set; } 
        [Required(ErrorMessage ="Name_is_Required")]
        public string? Name { get; set; }
       [Range(1,20,ErrorMessage ="Less_than_or_exceeded_range_price")]
        [DataType(DataType.Currency, ErrorMessage = "Wrong_Format_Entry")]
        public decimal Price { get; set; }
        [Required(ErrorMessage = "Image_is_Required")]
        public IFormFile? Image { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImagePath { get; set; }
        public bool IsActive { get; set; } = true;
        [Range(1, 9999, ErrorMessage = "Less_than_or_exceeded_range_price")]
        public int? Stock { get; set; }
        public int? DiscountPercentage { get; set; }
        public bool IsOnSale { get; set; } = true;
        public int ReservedStock { get; set; } = 0;
        public int SoldCount { get; set; } = 0;
        public DateTime? UpdatedAt { get; set; }
        public int? CategoryId { get; set; }
   


    }
}
