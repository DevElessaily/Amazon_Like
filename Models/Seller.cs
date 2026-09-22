using System.ComponentModel.DataAnnotations;

namespace portofolio3.Models
{
    public class Seller
    {
        public int Id { get; set; }
        public required string FullName { get; set; }

        public required string ShopName { get; set; }

        public required string Email { get; set; }

        public required string PasswordHash { get; set; }

        public string? Phone { get; set; }
        [Required]
        public string? ProfileImage { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsApproved { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}