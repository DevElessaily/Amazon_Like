namespace portofolio3.Models
{
    public class SellerProduct
    {
        public int SellerId { get; set; }
        public Seller Seller { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
