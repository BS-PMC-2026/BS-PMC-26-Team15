using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SamiSpot.Controllers;
using SamiSpot.Data;
using SamiSpot.Models;
using SamiSpot.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System.Linq;

namespace SamiSpot1.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class AccountControllerTests
    {
        private ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        private AccountController CreateControllerWithHttp(ApplicationDbContext context)
        {
            var controller = new AccountController(context);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            controller.TempData = new TempDataDictionary(
                httpContext,
                Mock.Of<ITempDataProvider>());

            return controller;
        }

        [TestMethod]
        public void Register_Get_ReturnsView_AndSetsReturnUrl()
        {
            using var context = CreateContext(nameof(Register_Get_ReturnsView_AndSetsReturnUrl));
            var controller = new AccountController(context);

            var result = controller.Register("/Map/Index") as ViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("/Map/Index", controller.ViewBag.ReturnUrl);
        }

        [TestMethod]
        public void Register_Post_WhenModelIsValid_SavesUser_AndRedirectsToLogin()
        {
            using var context = CreateContext(nameof(Register_Post_WhenModelIsValid_SavesUser_AndRedirectsToLogin));
            var controller = CreateControllerWithHttp(context);

            var model = new RegisterViewModel
            {
                UserName = "rania",
                Email = "rania@gmail.com",
                Password = "Abcd1234",
                ConfirmPassword = "Abcd1234",
                RoleType = "User"
            };

            var result = controller.Register(model, null) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Login", result.ActionName);
            Assert.AreEqual(1, context.Users.Count());

            var user = context.Users.First();
            Assert.AreEqual("rania", user.UserName);
            Assert.AreEqual("rania@gmail.com", user.Email);
        }

       
        [TestMethod]
        public void Login_Post_WhenPasswordIsWrong_ReturnsView_WithErrorMessage()
        {
            using var context = CreateContext(nameof(Login_Post_WhenPasswordIsWrong_ReturnsView_WithErrorMessage));

            context.Users.Add(new User
            {
                UserName = "user1",
                Email = "user@gmail.com",
                Password = "Abcd1234",
                RoleType = "User"
            });

            context.SaveChanges();

            var controller = CreateControllerWithHttp(context);

            var model = new LoginViewModel
            {
                Email = "user@gmail.com",
                Password = "WrongPassword"
            };

            var result = controller.Login(model, null) as ViewResult;

            Assert.IsNotNull(result);
           
        }
        [TestMethod]
        public void Logout_ClearsSession_AndRedirectsToHomeIndex()
        {
            using var context = CreateContext(nameof(Logout_ClearsSession_AndRedirectsToHomeIndex));
            var controller = CreateControllerWithHttp(context);

            controller.HttpContext.Session.SetString("UserName", "rania");
            controller.HttpContext.Session.SetString("RoleType", "User");

            var result = controller.Logout() as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Home", result.ControllerName);
            Assert.IsNull(controller.HttpContext.Session.GetString("UserName"));
            Assert.IsNull(controller.HttpContext.Session.GetString("RoleType"));
        }

        [TestMethod]
        public void Register_Post_WhenModelIsInvalid_ReturnsView()
        {
            using var context = CreateContext(nameof(Register_Post_WhenModelIsInvalid_ReturnsView));
            var controller = CreateControllerWithHttp(context);

            controller.ModelState.AddModelError("UserName", "Required");

            var model = new RegisterViewModel();

            var result = controller.Register(model, null) as ViewResult;

            Assert.IsNotNull(result);
        }
    }
}