using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamiSpot.Controllers;
using SamiSpot.Data;
using SamiSpot.Models;
using SamiSpot.ViewModels;

namespace SamiSpot1.Tests
{
    [TestClass]
    public class US21AdminLoginTests
    {
        private ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        private AccountController CreateController(ApplicationDbContext context, US21TestSession session)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Session = session;

            var controller = new AccountController(context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                },
                TempData = new TempDataDictionary(httpContext, new US21FakeTempDataProvider())
            };

            return controller;
        }

        // ================= TESTS =================

        [TestMethod]
        public void US21_AdminLogin_RedirectsToAdminDashboard()
        {
            using var context = CreateContext(nameof(US21_AdminLogin_RedirectsToAdminDashboard));

            context.Users.Add(new User
            {
                UserName = "admin1",
                Email = "admin@gmail.com",
                Password = "Admin123",
                RoleType = "Admin"
            });
            context.SaveChanges();

            var session = new US21TestSession();
            var controller = CreateController(context, session);

            var model = new LoginViewModel
            {
                Email = "admin@gmail.com",
                Password = "Admin123"
            };

            var result = controller.Login(model, null) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Admindashboard", result.ActionName);
            Assert.AreEqual("Account", result.ControllerName);
        }

        [TestMethod]
        public void US21_ContributorLogin_RedirectsToContributorDashboard()
        {
            using var context = CreateContext(nameof(US21_ContributorLogin_RedirectsToContributorDashboard));

            context.Users.Add(new User
            {
                UserName = "cont1",
                Email = "cont@gmail.com",
                Password = "Cont1234",
                RoleType = "Contributor"
            });
            context.SaveChanges();

            var session = new US21TestSession();
            var controller = CreateController(context, session);

            var model = new LoginViewModel
            {
                Email = "cont@gmail.com",
                Password = "Cont1234"
            };

            var result = controller.Login(model, null) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Contributordashboard", result.ActionName);
            Assert.AreEqual("Account", result.ControllerName);
        }

        [TestMethod]
        public void US21_RegularUserLogin_RedirectsToMapIndex()
        {
            using var context = CreateContext(nameof(US21_RegularUserLogin_RedirectsToMapIndex));

            context.Users.Add(new User
            {
                UserName = "user1",
                Email = "user@gmail.com",
                Password = "User1234",
                RoleType = "User"
            });
            context.SaveChanges();

            var session = new US21TestSession();
            var controller = CreateController(context, session);

            var model = new LoginViewModel
            {
                Email = "user@gmail.com",
                Password = "User1234"
            };

            var result = controller.Login(model, null) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Map", result.ControllerName);
        }

        [TestMethod]
        public void US21_InvalidLogin_ReturnsLoginViewWithError()
        {
            using var context = CreateContext(nameof(US21_InvalidLogin_ReturnsLoginViewWithError));

            var session = new US21TestSession();
            var controller = CreateController(context, session);

            var model = new LoginViewModel
            {
                Email = "wrong@gmail.com",
                Password = "Wrong123"
            };

            var result = controller.Login(model, null) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(controller.ModelState.IsValid);
        }

        [TestMethod]
        public void US21_LoginStoresUserNameAndRoleTypeInSession()
        {
            using var context = CreateContext(nameof(US21_LoginStoresUserNameAndRoleTypeInSession));

            context.Users.Add(new User
            {
                UserName = "admin1",
                Email = "admin@gmail.com",
                Password = "Admin123",
                RoleType = "Admin"
            });
            context.SaveChanges();

            var session = new US21TestSession();
            var controller = CreateController(context, session);

            var model = new LoginViewModel
            {
                Email = "admin@gmail.com",
                Password = "Admin123"
            };

            controller.Login(model, null);

            Assert.AreEqual("admin1", session.GetString("UserName"));
            Assert.AreEqual("Admin", session.GetString("RoleType"));
        }
    }

    // ================= SESSION =================

    public class US21TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _storage = new();

        public IEnumerable<string> Keys => _storage.Keys;
        public string Id { get; } = Guid.NewGuid().ToString();
        public bool IsAvailable => true;

        public void Clear() => _storage.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _storage.Remove(key);

        public void Set(string key, byte[] value) => _storage[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _storage.TryGetValue(key, out value);
    }

    // ================= TEMP DATA =================

    public class US21FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}