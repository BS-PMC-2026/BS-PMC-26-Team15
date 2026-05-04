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

            controller.ModelState.AddModelError("", "Passwords do not match");

            var result = controller.AddUser(user, "User") as ViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(0, context.Users.Count());
        }

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

            var result = controller.ManageUsers();

            Assert.IsInstanceOfType(result, typeof(ViewResult));

            var viewResult = result as ViewResult;
            var model = viewResult.Model as List<User>;

            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Count);
            Assert.IsTrue(model.All(u => u.RoleType == "User"));
        }


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

        [TestMethod]
        public void DeactivateUser_Should_Set_IsActive_False()
        {
            using var context = CreateContext(nameof(DeactivateUser_Should_Set_IsActive_False));

            context.Users.Add(new User
            {
                Id = 1,
                UserName = "test",
                Email = "test@gmail.com",
                Password = "Abcd1234",
                RoleType = "User",
                IsActive = true
            });
            context.SaveChanges();

            var controller = new AdminController(context);

            controller.DeactivateUser(1);

            var user = context.Users.Find(1);

            Assert.IsNotNull(user);
            Assert.IsFalse(user.IsActive);
        }

        [TestMethod]
        public void ActivateUser_Should_Set_IsActive_True()
        {
            using var context = CreateContext(nameof(ActivateUser_Should_Set_IsActive_True));

            context.Users.Add(new User
            {
                Id = 1,
                UserName = "test",
                Email = "test@gmail.com",
                Password = "Abcd1234",
                RoleType = "User",
                IsActive = false
            });
            context.SaveChanges();

            var controller = new AdminController(context);

            controller.ActivateUser(1);

            var user = context.Users.Find(1);

            Assert.IsNotNull(user);
            Assert.IsTrue(user.IsActive);
        }

        [TestMethod]
        public void DeleteUser_Should_Remove_User()
        {
            using var context = CreateContext(nameof(DeleteUser_Should_Remove_User));

            context.Users.Add(new User
            {
                Id = 1,
                UserName = "test",
                Email = "test@gmail.com",
                Password = "Abcd1234",
                RoleType = "User",
                IsActive = true
            });
            context.SaveChanges();

            var controller = new AdminController(context);

            controller.DeleteUser(1);

            var user = context.Users.Find(1);

            Assert.IsNull(user);
        }

        [TestMethod]
        public void DeactivateUser_WhenUserNotFound_ShouldNotCrash()
        {
            using var context = CreateContext(nameof(DeactivateUser_WhenUserNotFound_ShouldNotCrash));

            var controller = new AdminController(context);

            var result = controller.DeactivateUser(999);

            Assert.IsNotNull(result);
        }
    }
}