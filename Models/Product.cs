using System.ComponentModel.DataAnnotations;

namespace SecureApi.Models
{
    public class Product
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        // [Display(Name="Name"),Required(ErrorMessage ="Name_is_Required")]
        public string Name { get; set; } = default!;
        //[Display(Name = "Name"), Range(1,20,ErrorMessage ="Less_than_or_exceeded_range_price")]
        public decimal Price { get; set; }
        public string ImagePath { get; set; } =default!;
        public bool IsActive { get; set; } = true;
        public int? Stock { get; set; }
        public int? DiscountPercentage { get; set; }
        public bool IsOnSale { get; set; } = true;
        public DateTime? UpdatedAt { get; set; }
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        public List<BasketItems> basketItems { get; set; } = new List<BasketItems>();

    }
}
