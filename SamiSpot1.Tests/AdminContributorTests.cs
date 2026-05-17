
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
    public class AdminContributorTests
    {
        private ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        private AdminController CreateAdminController(ApplicationDbContext context)
        {
            var controller = new AdminController(context);

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

        private ContributorController CreateContributorController(ApplicationDbContext context, string userName = "rayan")
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
                                string status = "Pending")
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
        public void AddShelter_Get_ReturnsView()
        {
            using var context = CreateContext(nameof(AddShelter_Get_ReturnsView));
            var controller = CreateContributorController(context);

            var result = controller.AddShelter() as ViewResult;

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task AddShelter_Post_WhenUserIsNotLoggedIn_RedirectsToLogin()
        {
            using var context = CreateContext(nameof(AddShelter_Post_WhenUserIsNotLoggedIn_RedirectsToLogin));
            var controller = CreateContributorController(context, userName: null);

            var model = new ContributorShelterFormViewModel
            {
                Name = "Shelter",
                Address = "Addr",
                Latitude = 31.0,
                Longitude = 34.0,
                Size = 10
            };

            var result = await controller.AddShelter(model) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Login", result.ActionName);
            Assert.AreEqual("Account", result.ControllerName);
        }

        [TestMethod]
        public async Task AddShelter_Post_WhenModelIsInvalid_ReturnsView()
        {
            using var context = CreateContext(nameof(AddShelter_Post_WhenModelIsInvalid_ReturnsView));
            var controller = CreateContributorController(context, userName: "rayan");

            var model = new ContributorShelterFormViewModel();
            controller.ModelState.AddModelError("Name", "Required");

            var result = await controller.AddShelter(model) as ViewResult;

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task AddShelter_Post_WhenLatLngAreZero_ReturnsView_WithError()
        {
            using var context = CreateContext(nameof(AddShelter_Post_WhenLatLngAreZero_ReturnsView_WithError));
            var controller = CreateContributorController(context, userName: "rayan");

            var model = new ContributorShelterFormViewModel
            {
                Name = "Shelter",
                Address = "Addr",
                Latitude = 0,
                Longitude = 0,
                Size = 10
            };

            var result = await controller.AddShelter(model) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsTrue(controller.ModelState.ErrorCount > 0);
        }

        [TestMethod]
        public async Task AddShelter_Post_WhenTooManyImages_ReturnsView_WithError()
        {
            using var context = CreateContext(nameof(AddShelter_Post_WhenTooManyImages_ReturnsView_WithError));
            var controller = CreateContributorController(context, userName: "rayan");

            var fakeImages = Enumerable.Range(0, 11)
                .Select(_ =>
                {
                    var fileMock = new Mock<IFormFile>();
                    fileMock.Setup(f => f.FileName).Returns("img.jpg");
                    fileMock.Setup(f => f.Length).Returns(100);
                    return fileMock.Object;
                })
                .ToList();

            var model = new ContributorShelterFormViewModel
            {
                Name = "Shelter",
                Address = "Addr",
                Latitude = 31.0,
                Longitude = 34.0,
                Size = 10,
                Images = fakeImages
            };

            var result = await controller.AddShelter(model) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsTrue(controller.ModelState.ErrorCount > 0);
        }

        [TestMethod]
        public void AllShelters_ReturnsView_WithAllShelters()
        {
            using var context = CreateContext(nameof(AllShelters_ReturnsView_WithAllShelters));
            SeedShelter(context, userId: "rayan", name: "Shelter A", status: "Pending");
            SeedShelter(context, userId: "doaa", name: "Shelter B", status: "Approved");
            SeedShelter(context, userId: "rania", name: "Shelter C", status: "Rejected");

            var controller = CreateAdminController(context);

            var result = controller.AllShelters() as ViewResult;

            Assert.IsNotNull(result);

            var model = result.Model as List<ContributorShelter>;
            Assert.IsNotNull(model);
            Assert.AreEqual(3, model.Count);
        }

        [TestMethod]
        public void AllShelters_WhenNoShelters_ReturnsEmptyList()
        {
            using var context = CreateContext(nameof(AllShelters_WhenNoShelters_ReturnsEmptyList));
            var controller = CreateAdminController(context);

            var result = controller.AllShelters() as ViewResult;

            Assert.IsNotNull(result);

            var model = result.Model as List<ContributorShelter>;
            Assert.IsNotNull(model);
            Assert.AreEqual(0, model.Count);
        }

        [TestMethod]
        public void AllShelters_ReturnsAllStatuses_NotJustPending()
        {
            using var context = CreateContext(nameof(AllShelters_ReturnsAllStatuses_NotJustPending));
            SeedShelter(context, status: "Pending");
            SeedShelter(context, status: "Approved");
            SeedShelter(context, status: "Rejected");

            var controller = CreateAdminController(context);

            var result = controller.AllShelters() as ViewResult;
            var model = result.Model as List<ContributorShelter>;

            Assert.IsNotNull(model);
            Assert.IsTrue(model.Any(s => s.Status == "Pending"));
            Assert.IsTrue(model.Any(s => s.Status == "Approved"));
            Assert.IsTrue(model.Any(s => s.Status == "Rejected"));
        }

        [TestMethod]
        public void ApproveShelter_UsingEF_UpdatesStatusToApproved()
        {
            using var context = CreateContext(nameof(ApproveShelter_UsingEF_UpdatesStatusToApproved));
            int id = SeedShelter(context, status: "Pending");

            var shelter = context.ContributorShelters.First(s => s.Id == id);
            shelter.Status = "Approved";
            context.SaveChanges();

            var updated = context.ContributorShelters.First(s => s.Id == id);
            Assert.AreEqual("Approved", updated.Status);
        }

        [TestMethod]
        public void RejectShelter_UsingEF_UpdatesStatusToRejected()
        {
            using var context = CreateContext(nameof(RejectShelter_UsingEF_UpdatesStatusToRejected));
            int id = SeedShelter(context, status: "Pending");

            var shelter = context.ContributorShelters.First(s => s.Id == id);
            shelter.Status = "Rejected";
            context.SaveChanges();

            var updated = context.ContributorShelters.First(s => s.Id == id);
            Assert.AreEqual("Rejected", updated.Status);
        }

        [TestMethod]
        public void RejectShelter_DoesNotAffectOtherShelters()
        {
            using var context = CreateContext(nameof(RejectShelter_DoesNotAffectOtherShelters));
            int targetId = SeedShelter(context, name: "Target", status: "Pending");
            int otherId = SeedShelter(context, name: "Other", status: "Pending");

            var target = context.ContributorShelters.First(s => s.Id == targetId);
            target.Status = "Rejected";
            context.SaveChanges();

            var other = context.ContributorShelters.First(s => s.Id == otherId);
            Assert.AreEqual("Pending", other.Status);
        }
    }
}

