namespace portofolio3.Models
{
    public class Order
    {
        public int Id { get; set; }

        public DateTime OrderDate { get; set; }

        public decimal TotalAmount { get; set; }

        public required string Status { get; set; }

        public string? ShippingAddress { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}