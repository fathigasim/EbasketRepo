using System.ComponentModel.DataAnnotations;

namespace SecureApi.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public List<Product> Products { get; set; }

    }
}
