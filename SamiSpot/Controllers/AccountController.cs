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
        public IActionResult Register(RegisterViewModel model, string? returnUrl)
        {
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match ❌");
                return View(model);
            }
            if (_context.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("", "This Gmail is already registered with another account ❌");
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
                return Redirect(returnUrl); // go to the requested page

            }

            return RedirectToAction("Login");
        }

        // ================= LOGIN =================

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model, string? returnUrl)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password ❌");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Your account is deactivated. Contact admin.");
                return View(model);
            }

            if (user.RoleType != model.Role)
            {
                ModelState.AddModelError("", "Selected role does not match this account ❌");
                return View(model);
            }

            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserName", user.UserName);
            HttpContext.Session.SetString("RoleType", user.RoleType);

            TempData["Success"] = "Welcome back, " + user.UserName + " 👋";

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (user.RoleType == "Admin")
            {
                return RedirectToAction("Admindashboard", "Account");
            }

            if (user.RoleType == "Contributor")
            {
                return RedirectToAction("Contributordashboard", "Account");
            }

            return RedirectToAction("Index", "Map");
        }
        public IActionResult Admindashboard()
        {
            if (HttpContext.Session.GetString("RoleType") != "Admin")
                return RedirectToAction("Login");

            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.PendingApprovals = _context.ContributorShelters.Count(s => s.Status == "Pending");
            ViewBag.TotalShelters = _context.ContributorShelters.Count(s => s.Status == "Approved")
                                     + _context.Shelters.Count();
            ViewBag.Contributors = _context.Users.Count(u => u.RoleType == "Contributor");

            return View();
        }
        public IActionResult Contributordashboard()
        {
            return View();
        }
        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ================= PASSWORD VALIDATION =================
        private bool IsValidPassword(string? password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            if (password.Length < 8) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;

            return true;
        }

        public IActionResult SelectRole()
        {
            return View();
        }
    }
}