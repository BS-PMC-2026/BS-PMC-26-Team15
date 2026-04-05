using Microsoft.AspNetCore.Mvc;
using SamiSpot.Data;
using SamiSpot.Models;
using SamiSpot.ViewModels;

namespace SamiSpot.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= REGISTER =================

        public IActionResult Register(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [HttpPost]
        public IActionResult Register(RegisterViewModel model, string? returnUrl)
        {
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match ❌");
                return View(model);
            }

            if (!IsValidPassword(model.Password))
            {
                ModelState.AddModelError("", "Password must be valid");
                return View(model);
            }

            // 🔥 CHECK duplicate username
            if (_context.Users.Any(u => u.UserName == model.UserName))
            {
                ModelState.AddModelError("", "Username already exists ❌");
                return View(model);
            }

            // 🔥 CHECK gmail
            if (!model.Email.EndsWith("@gmail.com"))
            {
                ModelState.AddModelError("", "Email must be a Gmail address ❌");
                return View(model);
            }

            var user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                Password = model.Password,
                RoleType = model.RoleType
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // ✅ 👉 ADD IT HERE
            TempData["Success"] = "Account created successfully 🎉 Please login.";

            // 🔥 KEEP returnUrl
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect("/Account/Login?returnUrl=" + returnUrl);
            }

            return RedirectToAction("Login");
        }

        // ================= LOGIN =================

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model, string returnUrl)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password ❌");
                return View(model);
            }

            HttpContext.Session.SetString("UserName", user.UserName);

            TempData["Success"] = "Welcome back, " + user.UserName + " 👋";

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Map");
        }

        // ================= LOGOUT =================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Map");
        }

        // ================= PASSWORD VALIDATION =================

        private bool IsValidPassword(string password)
        {
            if (password.Length < 8) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;

            return true;
        }
    }
}