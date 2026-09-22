using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using portofolio3.Data;
using portofolio3.Models;

namespace portofolio3.Controllers
{
    public class UserController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public UserController(AppDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ---- Register ----
        public IActionResult Register() => View();
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(portofolio3.Models.User model, string Password, string ConfirmPassword)
        {
            if (Password != ConfirmPassword)
                ModelState.AddModelError("", "Passwords do not match.");
            if (Password is null || Password.Length < 6)
                ModelState.AddModelError("", "Password must be at least 6 characters.");

            if (_context.Users.Any(u => u.Email == model.Email))
                ModelState.AddModelError("Email", "This email is already registered.");

            if (!ModelState.IsValid) return View(model);

            var identityUser = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email
            };
            var result = await _userManager.CreateAsync(identityUser, Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(identityUser, "User");

            model.PasswordHash = _userManager.PasswordHasher.HashPassword(identityUser, Password);
            _context.Users.Add(model);
            await _context.SaveChangesAsync();

            // link the identity account to the profile
            identityUser.UserProfileId = model.Id;
            await _userManager.UpdateAsync(identityUser);

            await _signInManager.SignInAsync(identityUser, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Email, string Password, string? ReturnUrl)
        {
            var result = await _signInManager.PasswordSignInAsync(Email, Password, false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View();
            }
            return string.IsNullOrEmpty(ReturnUrl) ? RedirectToAction("Index", "Home")
                                                   : LocalRedirect(ReturnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ---- Profile ----
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null) return Challenge();
            var profile = _context.Users
                .Include(u => u.Addresses)
                .Include(u => u.Orders)
                .Include(u => u.Reviews)
                .FirstOrDefault(u => u.Id == appUser.UserProfileId);
            return View(profile);
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(portofolio3.Models.User model)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null) return Challenge();

            var profile = await _context.Users.FindAsync(appUser.UserProfileId);
            if (profile == null) return NotFound();

            profile.Name = model.Name;
            profile.Phone = model.Phone;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Profile));
        }

        // ---- Change Password ----
        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string CurrentPassword, string NewPassword, string ConfirmNewPassword)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null) return Challenge();

            if (NewPassword != ConfirmNewPassword)
            {
                ModelState.AddModelError("", "New passwords do not match.");
                return RedirectToAction(nameof(Profile));
            }
            var result = await _userManager.ChangePasswordAsync(appUser, CurrentPassword, NewPassword);
            if (!result.Succeeded) ModelState.AddModelError("", "Could not change password.");
            return RedirectToAction(nameof(Profile));
        }

        // ---- Addresses ----
        [Authorize]
        public IActionResult Addresses()
        {
            return View(_context.Addresses.Where(a => a.UserId == UserId()).ToList());
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(Address address)
        {
            address.UserId = UserId() ?? 0;
            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Addresses));
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var a = await _context.Addresses.FindAsync(id);
            if (a != null && a.UserId == UserId())
            {
                _context.Addresses.Remove(a);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Addresses));
        }

        private int? UserId() => _userManager.GetUserAsync(User).Result?.UserProfileId;

        private async Task<int?> GetProfileUserId()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.UserProfileId;
        }

        // ================= Orders =================
        public async Task<IActionResult> Orders()
        {
            var userId = await GetProfileUserId();
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var userId = await GetProfileUserId();
            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userId = await GetProfileUserId();
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
            if (order != null && order.Status == "Pending")
            {
                order.Status = "Cancelled";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Orders));
        }

        // ================= Reviews =================
        public async Task<IActionResult> MyReviews()
        {
            var userId = await GetProfileUserId();
            var reviews = await _context.Reviews
                .Include(r => r.Product).ThenInclude(p => p.Images)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(reviews);
        }
    }
}