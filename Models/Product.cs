namespace portofolio3.Models
{
    public class Product
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public DateTime CreatedAt { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public int SellerId { get; set; }
        public Seller Seller { get; set; } = null!;

        public bool IsApproved { get; set; }

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}