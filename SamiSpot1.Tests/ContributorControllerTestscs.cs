
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SamiSpot.Controllers;
using SamiSpot.Data;
using SamiSpot.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace SamiSpot1.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ContributorControllerTests
    {
        private ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        private ContributorController CreateController(ApplicationDbContext context, string userName = "rayan")
        {
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

            var controller = new ContributorController(context, mockEnv.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();

            if (userName != null)
                httpContext.Session.SetString("UserName", userName);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            controller.TempData = new TempDataDictionary(
                httpContext,
                Mock.Of<ITempDataProvider>());

            return controller;
        }

        private int SeedShelter(ApplicationDbContext context,
                                string userId = "rayan",
                                string name = "Test Shelter",
                                string status = "Approved")
        {
            var shelter = new ContributorShelter
            {
                Name = name,
                Address = "123 Test St",
                Latitude = 31.0,
                Longitude = 34.0,
                Description = "A test shelter",
                Size = 50,
                IsAvailable = true,
                UserId = userId,
                Status = status,
                CreatedAt = DateTime.Now
            };

            context.ContributorShelters.Add(shelter);
            context.SaveChanges();
            return shelter.Id;
        }

        [TestMethod]
        public void MyShelters_WhenUserIsNotLoggedIn_RedirectsToLogin()
        {
            using var context = CreateContext(nameof(MyShelters_WhenUserIsNotLoggedIn_RedirectsToLogin));
            var controller = CreateController(context, userName: null);

            var result = controller.MyShelters() as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Login", result.ActionName);
            Assert.AreEqual("Account", result.ControllerName);
        }

        [TestMethod]
        public void EditShelter_Get_WhenUserIsNotLoggedIn_RedirectsToLogin()
        {
            using var context = CreateContext(nameof(EditShelter_Get_WhenUserIsNotLoggedIn_RedirectsToLogin));
            var controller = CreateController(context, userName: null);

            var result = controller.EditShelter(1) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Login", result.ActionName);
            Assert.AreEqual("Account", result.ControllerName);
        }

        [TestMethod]
        public void EditShelter_Get_WhenShelterDoesNotExist_ReturnsNotFound()
        {
            using var context = CreateContext(nameof(EditShelter_Get_WhenShelterDoesNotExist_ReturnsNotFound));
            var controller = CreateController(context, userName: "rayan");

            var result = controller.EditShelter(999);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public void EditShelter_Get_WhenShelterBelongsToAnotherUser_ReturnsNotFound()
        {
            using var context = CreateContext(nameof(EditShelter_Get_WhenShelterBelongsToAnotherUser_ReturnsNotFound));
            int id = SeedShelter(context, userId: "doaa");

            var controller = CreateController(context, userName: "rayan");

            var result = controller.EditShelter(id);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public void EditShelter_Get_WhenShelterExists_AndOwned_ReturnsViewWithModel()
        {
            using var context = CreateContext(nameof(EditShelter_Get_WhenShelterExists_AndOwned_ReturnsViewWithModel));
            int id = SeedShelter(context, userId: "rayan", name: "My Shelter");

            var controller = CreateController(context, userName: "rayan");

            var result = controller.EditShelter(id) as ViewResult;

            Assert.IsNotNull(result);

            var model = result.Model as ContributorShelterFormViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual("My Shelter", model.Name);
            Assert.AreEqual("123 Test St", model.Address);
        }

        [TestMethod]
        public async Task EditShelter_Post_WhenUserIsNotLoggedIn_RedirectsToLogin()
        {
            using var context = CreateContext(nameof(EditShelter_Post_WhenUserIsNotLoggedIn_RedirectsToLogin));
            var controller = CreateController(context, userName: null);

            var model = new ContributorShelterFormViewModel
            {
                Name = "X",
                Address = "X",
                Latitude = 31.0,
                Longitude = 34.0,
                Size = 10
            };

            var result = await controller.EditShelter(1, model) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Login", result.ActionName);
            Assert.AreEqual("Account", result.ControllerName);
        }

        [TestMethod]
        public async Task EditShelter_Post_WhenModelIsInvalid_ReturnsView()
        {
            using var context = CreateContext(nameof(EditShelter_Post_WhenModelIsInvalid_ReturnsView));
            int id = SeedShelter(context, userId: "rayan");
            var controller = CreateController(context, userName: "rayan");

            var model = new ContributorShelterFormViewModel();

            controller.ModelState.AddModelError("Name", "Required");

            var result = await controller.EditShelter(id, model) as ViewResult;

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task EditShelter_Post_WhenLatLngAreZero_ReturnsView_WithError()
        {
            using var context = CreateContext(nameof(EditShelter_Post_WhenLatLngAreZero_ReturnsView_WithError));
            int id = SeedShelter(context, userId: "rayan");
            var controller = CreateController(context, userName: "rayan");

            var model = new ContributorShelterFormViewModel
            {
                Name = "Shelter",
                Address = "Addr",
                Latitude = 0,
                Longitude = 0,
                Size = 10
            };

            var result = await controller.EditShelter(id, model) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsTrue(controller.ModelState.ErrorCount > 0);
        }

        [TestMethod]
        public void DeleteShelter_WhenUserIsNotLoggedIn_RedirectsToLogin()
        {
            using var context = CreateContext(nameof(DeleteShelter_WhenUserIsNotLoggedIn_RedirectsToLogin));
            var controller = CreateController(context, userName: null);

            var result = controller.DeleteShelter(1) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Login", result.ActionName);
            Assert.AreEqual("Account", result.ControllerName);
        }
    }
}

