using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using portofolio3.Data;
using portofolio3.Models;

namespace portofolio3.Controllers
{
    public class ShopController : Controller
    {
        private readonly AppDbContext _context;
        public ShopController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index(int? categoryId, string? search,
            string? sortBy, string? brand)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.Images)
                .Where(p => p.IsApproved)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search)
                    || (p.Description != null && p.Description.Contains(search)));
            if (!string.IsNullOrWhiteSpace(brand))
                query = query.Where(p => p.Seller.ShopName.Contains(brand));

            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderBy(p => p.Name)
            };

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Search = search;
            ViewBag.Brand = brand;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SortBy = sortBy;
            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.Images)
                .Include(p => p.Reviews).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsApproved);
            if (product == null) return NotFound();
            return View(product);
        }
    }
}