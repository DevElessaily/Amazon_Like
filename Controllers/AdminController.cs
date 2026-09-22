using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using portofolio3.Data;
using portofolio3.Models;

namespace portofolio3.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(AppDbContext context, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalSales = await _context.Orders.AnyAsync()
                ? await _context.Orders.SumAsync(o => o.TotalAmount) : 0m;
            ViewBag.SellerCount = await _context.Sellers.CountAsync();
            ViewBag.UserCount = await _context.Users.CountAsync();
            ViewBag.ProductCount = await _context.Products.CountAsync();
            ViewBag.OrderCount = await _context.Orders.CountAsync();
            ViewBag.PendingSellers = await _context.Sellers.CountAsync(s => !s.IsApproved);
            ViewBag.PendingProducts = await _context.Products.CountAsync(p => !p.IsApproved);
            ViewBag.LowStock = await _context.Products.CountAsync(p => p.Stock <= 5);
            ViewBag.RecentOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();
            return View();
        }

        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.OrderBy(u => u.Name).ToListAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                var identityUser = await _userManager.Users.FirstOrDefaultAsync(u => u.UserProfileId == id);
                if (identityUser != null)
                    await _userManager.DeleteAsync(identityUser);
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> Sellers()
        {
            var sellers = await _context.Sellers
                .Include(s => s.Products)
                .OrderBy(s => s.FullName)
                .ToListAsync();
            return View(sellers);
        }

        public async Task<IActionResult> SellerDetails(int? id)
        {
            if (id == null) return NotFound();
            var seller = await _context.Sellers
                .Include(s => s.Products).ThenInclude(p => p.Category)
                .Include(s => s.Products).ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (seller == null) return NotFound();
            return View(seller);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveSeller(int id)
        {
            var seller = await _context.Sellers.FindAsync(id);
            if (seller != null)
            {
                seller.IsApproved = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Sellers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectSeller(int id)
        {
            var seller = await _context.Sellers.FindAsync(id);
            if (seller != null)
            {
                seller.IsApproved = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Sellers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSeller(int id)
        {
            var seller = await _context.Sellers
                .Include(s => s.Products).ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (seller != null)
            {
                var identityUser = await _userManager.Users.FirstOrDefaultAsync(u => u.SellerProfileId == id);
                if (identityUser != null)
                    await _userManager.DeleteAsync(identityUser);

                if (seller.Products.Any())
                {
                    foreach (var p in seller.Products)
                        foreach (var img in p.Images)
                            DeleteFile(img.ImageUrl);
                    _context.Products.RemoveRange(seller.Products);
                }
                if (!string.IsNullOrWhiteSpace(seller.ProfileImage))
                    DeleteFile(seller.ProfileImage);

                _context.Sellers.Remove(seller);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Sellers));
        }

        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkOrderDelivered(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = "Delivered";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Orders));
        }

        public async Task<IActionResult> Payments()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            ViewBag.Orders = orders;
            ViewBag.TotalSales = orders.Any() ? orders.Sum(o => o.TotalAmount) : 0m;
            ViewBag.Commission = ViewBag.TotalSales * 0.10m;
            var paymentStatuses = orders.GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count(), Amount = g.Sum(o => o.TotalAmount) })
                .ToList();
            ViewBag.PaymentBreakdown = paymentStatuses;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefundOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = "Refunded";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Payments));
        }

        public async Task<IActionResult> Reviews()
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .OrderByDescending(r => r.Rating)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Reviews));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsApproved = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Reports));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsApproved = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Reports));
        }

        public async Task<IActionResult> Reports()
        {
            ViewBag.FlaggedReviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .Where(r => r.Rating <= 2)
                .ToListAsync();
            ViewBag.PendingProducts = await _context.Products
                .Include(p => p.Seller)
                .Where(p => !p.IsApproved)
                .ToListAsync();
            ViewBag.PendingSellers = await _context.Sellers
                .Where(s => !s.IsApproved)
                .ToListAsync();
            ViewBag.LowStockProducts = await _context.Products
                .Include(p => p.Seller)
                .Where(p => p.Stock <= 5)
                .ToListAsync();
            return View();
        }

        private string? ValidateImage(IFormFile file)
        {
            string[] allowed = { ".jpg", ".jpeg", ".png", ".webp" };
            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return "Only image files are allowed (.jpg, .jpeg, .png, .webp).";
            if (file.Length > 2 * 1024 * 1024)
                return "Image must be smaller than 2 MB.";
            return null;
        }

        private async Task<string> SaveFile(IFormFile file)
        {
            string folder = Path.Combine(_env.WebRootPath, "uploads", "sellerimages");
            Directory.CreateDirectory(folder);
            string name = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(folder, name);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return "/uploads/sellerimages/" + name;
        }

        private void DeleteFile(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;
            string cleaned = relativePath.TrimStart('/');
            string full = Path.Combine(_env.WebRootPath, cleaned);
            if (System.IO.File.Exists(full))
            {
                System.IO.File.Delete(full);
            }
        }
    }
}