
using Microsoft.AspNetCore.Identity;

namespace portofolio3.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int? UserProfileId { get; set; }
        public int? SellerProfileId { get; set; }
        public bool IsAdmin { get; set; }
    }
}
