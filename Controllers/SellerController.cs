using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using portofolio3.Data;
using portofolio3.Models;

namespace portofolio3.Controllers
{
    [Authorize(Roles = "Seller")]
    public class SellerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public SellerController(AppDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        private async Task<Seller?> CurrentSeller()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Sellers.FirstOrDefaultAsync(s => s.Email == user.Email);
        }

        public async Task<IActionResult> Index()
        {
            var seller = await CurrentSeller();
            if (seller == null) return NotFound();
            if (!seller.IsApproved)
            {
                ViewBag.NotApproved = true;
                return View(seller);
            }

            var products = await _context.Products
                .Where(p => p.SellerId == seller.Id)
                .Include(p => p.Category)
                .ToListAsync();
            var productIds = products.Select(p => p.Id).ToList();

            var soldItems = await _context.OrderItems
                .Where(oi => productIds.Contains(oi.ProductId))
                .Include(oi => oi.Order).ThenInclude(o => o.User)
                .Include(oi => oi.Product)
                .ToListAsync();

            var valid = soldItems.Where(i => i.Order.Status != "Cancelled" && i.Order.Status != "Refunded").ToList();
            var recentOrderIds = soldItems.Select(i => i.OrderId).Distinct().OrderByDescending(x => x).Take(5).ToList();
            var recentOrders = await _context.Orders
                .Where(o => recentOrderIds.Contains(o.Id))
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .ToListAsync();

            ViewBag.Products = products;
            ViewBag.TotalRevenue = valid.Sum(i => i.Quantity * i.UnitPrice);
            ViewBag.UnitsSold = valid.Sum(i => i.Quantity);
            ViewBag.OrderCount = valid.Select(i => i.OrderId).Distinct().Count();
            ViewBag.ApprovedProducts = products.Count(p => p.IsApproved);
            ViewBag.PendingProducts = products.Count(p => !p.IsApproved);
            ViewBag.TotalStock = products.Sum(p => p.Stock);
            ViewBag.RecentOrders = recentOrders;

            return View(seller);
        }

        public async Task<IActionResult> Products()
        {
            var seller = await CurrentSeller();
            if (seller == null) return NotFound();
            var products = await _context.Products
                .Where(p => p.SellerId == seller.Id)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> CreateProduct()
        {
            var seller = await CurrentSeller();
            if (seller == null) return NotFound();
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product model, IFormFile? NewImage)
        {
            var seller = await CurrentSeller();
            if (seller == null) return NotFound();

            if (model.Price <= 0) ModelState.AddModelError(nameof(model.Price), "Price must be greater than zero.");
            if (model.Stock < 0) ModelState.AddModelError(nameof(model.Stock), "Stock cannot be negative.");
            if (!await _context.Categories.AnyAsync(c => c.Id == model.CategoryId))
                ModelState.AddModelError(nameof(model.CategoryId), "Select a valid category.");
            if (NewImage != null && ValidateImage(NewImage) != null)
                ModelState.AddModelError(nameof(NewImage), ValidateImage(NewImage)!);

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                return View(model);
            }

            var product = new Product
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                Stock = model.Stock,
                CategoryId = model.CategoryId,
                SellerId = seller.Id,
                IsApproved = false,
                CreatedAt = DateTime.Now
            };
            if (NewImage != null)
                product.Images.Add(new ProductImage { ImageUrl = await SaveProductImage(NewImage) });

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Products));
        }

        public async Task<IActionResult> EditProduct(int id)
        {
            var seller = await CurrentSeller();
            if (seller == null) return NotFound();
            var product = await _context.Products.Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == seller.Id);
            if (product == null) return NotFound();
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, Product model, IFormFile? NewImage)
        {
            var seller = await CurrentSeller();
            if (seller == null) return NotFound();
            var product = await _context.Products.Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == seller.Id);
            if (product == null) return NotFound();

            if (model.Price <= 0) ModelState.AddModelError(nameof(model.Price), "Price must be greater than zero.");
            if (model.Stock < 0) ModelState.AddModelError(nameof(model.Stock), "Stock cannot be negative.");
            if (NewImage != null && ValidateImage(NewImage) != null)
                ModelState.AddModelError(nameof(NewImage), ValidateImage(NewImage)!);

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                model.Images = product.Images;
                return View(model);
            }

            product.Name = model.Name;
            product.Description = model.Description;
            product.Price = model.Price;
            product.Stock = model.Stock;
            product.CategoryId = model.CategoryId;
            product.IsApproved = false;

            if (NewImage != null)
                product.Images.Add(new ProductImage { ImageUrl = await SaveProductImage(NewImage) });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var seller = await CurrentSeller();
            if (seller == null) return NotFound();
            var product = await _context.Products.Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == seller.Id);
            if (product == null) return NotFound();

            if (await _context.OrderItems.AnyAsync(oi => oi.ProductId == product.Id) ||
                await _context.CartItems.AnyAsync(ci => ci.ProductId == product.Id))
            {
                TempData["Error"] = "This product cannot be deleted because it has orders or sits in someone's cart.";
                return RedirectToAction(nameof(Products));
            }

            foreach (var img in product.Images)
                DeleteImageFile(img.ImageUrl);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int productId, int imageId)
        {
            var seller = await CurrentSeller();
            if (seller == null) return NotFound();
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == seller.Id);
            if (product == null) return NotFound();
            var img = await _context.ProductImages.FindAsync(imageId);
            if (img != null && img.ProductId == product.Id)
            {
                _context.ProductImages.Remove(img);
                await _context.SaveChangesAsync();
                DeleteImageFile(img.ImageUrl);
            }
            return RedirectToAction(nameof(EditProduct), new { id = productId });
        }

        public async Task<IActionResult> Orders()
        {
            var seller = await CurrentSeller();
            if (seller == null) return NotFound();
            ViewBag.SellerId = seller.Id;
            var orders = await _context.Orders
                .Where(o => o.Items.Any(i => i.Product.SellerId == seller.Id))
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
            var seller = await CurrentSeller();
            if (seller == null) return NotFound();
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && o.Items.Any(i => i.Product.SellerId == seller.Id));
            if (order != null)
            {
                order.Status = "Delivered";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Orders));
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

        private async Task<string> SaveProductImage(IFormFile file)
        {
            string folder = Path.Combine(_env.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(folder);
            string name = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(folder, name);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return "/uploads/products/" + name;
        }

        private void DeleteImageFile(string relativePath)
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