namespace portofolio3.Models
{
    public class User
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public required string Email { get; set; }

        public required string PasswordHash { get; set; }

        public string? Phone { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<Address> Addresses { get; set; } = new List<Address>();

        public ICollection<Cart> Carts { get; set; } = new List<Cart>();

        public ICollection<Order> Orders { get; set; } = new List<Order>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}