using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using portofolio3.Data;
using portofolio3.Models;

namespace portofolio3.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public CartController(AppDbContext context, UserManager<ApplicationUser> userManager)
        { _context = context; _userManager = userManager; }

        public async Task<IActionResult> Index()
        {
            var userId = (await _userManager.GetUserAsync(User))?.UserProfileId;
            var cart = await _context.Carts
                .Include(c => c.Items).ThenInclude(ci => ci.Product).ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(c => c.UserId == userId);
            return View(cart ?? new Cart { Items = new List<CartItem>() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsApproved) return NotFound();

            var userId = (await _userManager.GetUserAsync(User))?.UserProfileId;
            var cart = await _context.Carts.Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId ?? 0, CreatedAt = DateTime.Now, Items = new List<CartItem>() };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var existing = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (existing != null) existing.Quantity += quantity;
            else cart.Items.Add(new CartItem { CartId = cart.Id, ProductId = productId, Quantity = quantity });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int cartItemId, int quantity)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item != null)
            {
                item.Quantity = quantity <= 0 ? 1 : quantity;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Checkout()
        {
            var userId = (await _userManager.GetUserAsync(User))?.UserProfileId;
            var cart = await _context.Carts
                .Include(c => c.Items).ThenInclude(ci => ci.Product).ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(c => c.UserId == userId);
            var items = cart?.Items.Where(i => i.Product != null).ToList() ?? new List<CartItem>();
            if (items.Count == 0) return RedirectToAction(nameof(Index));
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string? ShippingAddress, string? PaymentMethod)
        {
            var userId = (await _userManager.GetUserAsync(User))?.UserProfileId;
            var cart = await _context.Carts
                .Include(c => c.Items).ThenInclude(ci => ci.Product).ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(c => c.UserId == userId);
            var items = cart?.Items.Where(i => i.Product != null).ToList() ?? new List<CartItem>();
            if (items.Count == 0) return RedirectToAction(nameof(Index));

            if (string.IsNullOrWhiteSpace(ShippingAddress))
                ModelState.AddModelError(nameof(ShippingAddress), "Shipping address is required.");

            if (!ModelState.IsValid)
            {
                ViewBag.PaymentMethod = PaymentMethod;
                return View("Checkout", items);
            }

            var order = new Order
            {
                UserId = userId ?? 0,
                OrderDate = DateTime.Now,
                Status = "Pending",
                ShippingAddress = ShippingAddress!.Trim(),
                TotalAmount = items.Sum(i => i.Quantity * i.Product.Price),
                Items = items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.Product.Price
                }).ToList()
            };

            foreach (var item in items)
            {
                item.Product.Stock = Math.Max(0, item.Product.Stock - item.Quantity);
            }

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Confirmation), new { id = order.Id });
        }

        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = (await _userManager.GetUserAsync(User))?.UserProfileId;
            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}