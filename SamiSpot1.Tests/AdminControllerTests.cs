using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SamiSpot.Controllers;
using SamiSpot.Data;
using SamiSpot.Models;
using System.Linq;

namespace SamiSpot1.Tests
{
    [TestClass]
    public class AdminControllerTests
    {
        private ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        // ✅ TEST 1: Add user successfully
        [TestMethod]
        public void AddUser_WhenValid_AddsUserAndRedirects()
        {
            using var context = CreateContext(nameof(AddUser_WhenValid_AddsUserAndRedirects));
            var controller = new AdminController(context);

            var user = new User
            {
                UserName = "test1",
                Email = "test1@gmail.com",
                Password = "Abcd1234",
                RoleType = "User"
            };

            var result = controller.AddUser(user, "Abcd1234") as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(1, context.Users.Count());
            Assert.AreEqual("ManageUsers", result.ActionName);
        }

        // ❌ TEST 2: Duplicate email
        [TestMethod]
        public void AddUser_WhenEmailExists_ReturnsView()
        {
            using var context = CreateContext(nameof(AddUser_WhenEmailExists_ReturnsView));

            context.Users.Add(new User
            {
                UserName = "existing",
                Email = "test@gmail.com",
                Password = "Abcd1234",
                RoleType = "User"
            });
            context.SaveChanges();

            var controller = new AdminController(context);

            var user = new User
            {
                UserName = "new",
                Email = "test@gmail.com", // duplicate
                Password = "Abcd1234"
            };

            var result = controller.AddUser(user, "User") as ViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(1, context.Users.Count()); // no new user added
        }

        // ❌ TEST 3: Password mismatch
        [TestMethod]
        public void AddUser_WhenPasswordMismatch_ReturnsView()
        {
            using var context = CreateContext(nameof(AddUser_WhenPasswordMismatch_ReturnsView));
            var controller = new AdminController(context);

            var user = new User
            {
                UserName = "test",
                Email = "test@gmail.com",
                Password = "Abcd1234"
            };

            // simulate mismatch (depends on your logic)
            controller.ModelState.AddModelError("", "Passwords do not match");

            var result = controller.AddUser(user, "User") as ViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(0, context.Users.Count());
        }

        // ✅ TEST 4: Add contributor redirects correctly
        [TestMethod]
        public void AddUser_WhenContributor_RedirectsToContributors()
        {
            using var context = CreateContext(nameof(AddUser_WhenContributor_RedirectsToContributors));
            var controller = new AdminController(context);

            var user = new User
            {
                UserName = "contrib",
                Email = "contrib@gmail.com",
                Password = "Abcd1234",
                RoleType = "Contributor"
            };

            var result = controller.AddUser(user, "Abcd1234") as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Contributors", result.ActionName);
        }

        // ✅ TEST 5: ManageUsers returns all non-contributors
        [TestMethod]
        public void ManageUsers_ReturnsOnlyRegularUsers()
        {
            using var context = CreateContext(nameof(ManageUsers_ReturnsOnlyRegularUsers));

            context.Users.AddRange(
                new User { UserName = "u1", Email = "u1@gmail.com", Password = "Abcd1234", RoleType = "User" },
                new User { UserName = "u2", Email = "u2@gmail.com", Password = "Abcd1234", RoleType = "User" },
                new User { UserName = "c1", Email = "c1@gmail.com", Password = "Abcd1234", RoleType = "Contributor" }
            );
            context.SaveChanges();

            var controller = new AdminController(context);

            // 🔥 FIX: don't cast directly
            var result = controller.ManageUsers();

            Assert.IsInstanceOfType(result, typeof(ViewResult));

            var viewResult = result as ViewResult;
            var model = viewResult.Model as List<User>;

            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Count);
            Assert.IsTrue(model.All(u => u.RoleType == "User"));
        }


        // ✅ TEST 6: Contributors returns only contributors
        [TestMethod]
        public void Contributors_ReturnsOnlyContributors()
        {
            using var context = CreateContext(nameof(Contributors_ReturnsOnlyContributors));

            context.Users.AddRange(
                new User { UserName = "u1", Email = "u1@gmail.com", Password = "Abcd1234", RoleType = "User" },
                new User { UserName = "c1", Email = "c1@gmail.com", Password = "Abcd1234", RoleType = "Contributor" },
                new User { UserName = "c2", Email = "c2@gmail.com", Password = "Abcd1234", RoleType = "Contributor" }
            );
            context.SaveChanges();

            var controller = new AdminController(context);

            var result = controller.Contributors();

            Assert.IsInstanceOfType(result, typeof(ViewResult));

            var viewResult = result as ViewResult;
            var model = viewResult.Model as List<User>;

            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Count);
            Assert.IsTrue(model.All(u => u.RoleType == "Contributor"));
        }
    }
}