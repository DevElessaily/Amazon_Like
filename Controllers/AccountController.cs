using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using portofolio3.Dtos;
using portofolio3.Models;
namespace portofolio3.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        public AccountController(
                  UserManager<ApplicationUser> userManager,
                  SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        //Get:/Account/Login

        [HttpGet]
        [AllowAnonymous]

        public IActionResult Login(string? returnUrl=null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        //Post:/Account/Login

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult>Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }
            await _signInManager.SignInAsync(user, model.RememberMe);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return RedirectToAction("Dashboard", "Admin");
            if (await _userManager.IsInRoleAsync(user, "Seller"))
                return RedirectToAction("Index", "Seller");

            return RedirectToAction("Index", "Home");

        }
        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
