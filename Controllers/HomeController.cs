using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using portofolio3.Data;
using portofolio3.Models;

namespace portofolio3.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(int? category, string? sort, string? search)
        {
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.Images)
                .Where(p => p.IsApproved);

            if (!string.IsNullOrWhiteSpace(search))
                products = products.Where(p => (p.Name + " " + (p.Description ?? "")).ToLower().Contains(search.ToLower()));
            if (category.HasValue)
                products = products.Where(p => p.CategoryId == category.Value);

            products = sort switch
            {
                "price-asc" => products.OrderBy(p => p.Price),
                "price-desc" => products.OrderByDescending(p => p.Price),
                "newest" => products.OrderByDescending(p => p.CreatedAt),
                "name" => products.OrderBy(p => p.Name),
                _ => products.OrderByDescending(p => p.CreatedAt)
            };

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.CurrentCategory = category?.ToString();
            ViewBag.CurrentSort = sort;
            ViewBag.Search = search;
            return View(await products.Take(50).ToListAsync());
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}