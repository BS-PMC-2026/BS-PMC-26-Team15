using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamiSpot.Controllers;
using SamiSpot.Data;
using SamiSpot.Models;

namespace SamiSpot1.Tests
{
    [TestClass]
    public class US28EditShelterTests
    {
        private ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        private AdminController CreateController(ApplicationDbContext context)
        {
            var httpContext = new DefaultHttpContext();

            return new AdminController(context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                },
                TempData = new TempDataDictionary(httpContext, new US28FakeTempDataProvider())
            };
        }

        [TestMethod]
        public void US28_EditShelter_Get_ReturnsView_WhenShelterExists()
        {
            using var context = CreateContext(nameof(US28_EditShelter_Get_ReturnsView_WhenShelterExists));

            context.ContributorShelters.Add(new ContributorShelter
            {
                Id = 1,
                Name = "Old Shelter",
                Address = "Old Address",
                Latitude = 31.1,
                Longitude = 34.1,
                UserId = "cont1",
                Status = "Approved",
                CreatedAt = DateTime.Now
            });

            context.SaveChanges();

            var controller = CreateController(context);

            var result = controller.EditShelter(1);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void US28_EditShelter_Get_LoadsCurrentShelterData()
        {
            using var context = CreateContext(nameof(US28_EditShelter_Get_LoadsCurrentShelterData));

            context.ContributorShelters.Add(new ContributorShelter
            {
                Id = 1,
                Name = "Current Name",
                Address = "Current Address",
                Latitude = 31.1,
                Longitude = 34.1,
                Description = "Current Description",
                Size = 30,
                IsAvailable = true,
                UserId = "cont1",
                Status = "Approved",
                CreatedAt = DateTime.Now
            });

            context.SaveChanges();

            var controller = CreateController(context);

            var result = controller.EditShelter(1) as ViewResult;
            var model = result?.Model as ContributorShelterFormViewModel;

            Assert.IsNotNull(model);
            Assert.AreEqual("Current Name", model.Name);
            Assert.AreEqual("Current Address", model.Address);
            Assert.AreEqual("Current Description", model.Description);
            Assert.AreEqual(30, model.Size);
            Assert.IsTrue(model.IsAvailable);
        }

        [TestMethod]
        public void US28_EditShelter_Post_UpdatesShelterDetails()
        {
            using var context = CreateContext(nameof(US28_EditShelter_Post_UpdatesShelterDetails));

            context.ContributorShelters.Add(new ContributorShelter
            {
                Id = 1,
                Name = "Old Name",
                Address = "Old Address",
                Latitude = 31.1,
                Longitude = 34.1,
                Description = "Old Description",
                Size = 20,
                IsAvailable = false,
                UserId = "cont1",
                Status = "Approved",
                CreatedAt = DateTime.Now
            });

            context.SaveChanges();

            var controller = CreateController(context);

            var model = new ContributorShelterFormViewModel
            {
                Name = "New Name",
                Address = "New Address",
                Latitude = 32.2,
                Longitude = 35.2,
                Description = "New Description",
                Size = 50,
                IsAvailable = true
            };

            var result = controller.EditShelter(1, model) as RedirectToActionResult;

            var updated = context.ContributorShelters.First(s => s.Id == 1);

            Assert.IsNotNull(result);
            Assert.AreEqual("AllShelters", result.ActionName);
            Assert.AreEqual("New Name", updated.Name);
            Assert.AreEqual("New Address", updated.Address);
            Assert.AreEqual(32.2, updated.Latitude);
            Assert.AreEqual(35.2, updated.Longitude);
            Assert.AreEqual("New Description", updated.Description);
            Assert.AreEqual(50, updated.Size);
            Assert.IsTrue(updated.IsAvailable);
        }

        [TestMethod]
        public void US28_EditShelter_Post_DeletesSelectedImage_WhenSaved()
        {
            using var context = CreateContext(nameof(US28_EditShelter_Post_DeletesSelectedImage_WhenSaved));

            context.ContributorShelters.Add(new ContributorShelter
            {
                Id = 1,
                Name = "Shelter",
                Address = "Address",
                Latitude = 31.1,
                Longitude = 34.1,
                UserId = "cont1",
                Status = "Approved",
                CreatedAt = DateTime.Now,
                Images = new List<ContributorShelterImage>
                {
                    new ContributorShelterImage
                    {
                        Id = 10,
                        ContributorShelterId = 1,
                        ImageUrl = "/uploads/img1.jpg"
                    }
                }
            });

            context.SaveChanges();

            var controller = CreateController(context);

            var model = new ContributorShelterFormViewModel
            {
                Name = "Shelter",
                Address = "Address",
                Latitude = 31.1,
                Longitude = 34.1,
                IsAvailable = true,
                DeletedImageIds = "10"
            };

            var result = controller.EditShelter(1, model) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("AllShelters", result.ActionName);
            Assert.AreEqual(0, context.ContributorShelterImages.Count());
        }

        [TestMethod]
        public void US28_EditShelter_ReturnsNotFound_WhenShelterDoesNotExist()
        {
            using var context = CreateContext(nameof(US28_EditShelter_ReturnsNotFound_WhenShelterDoesNotExist));

            var controller = CreateController(context);

            var result = controller.EditShelter(999);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }
    }

    public class US28FakeTempDataProvider : ITempDataProvider
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