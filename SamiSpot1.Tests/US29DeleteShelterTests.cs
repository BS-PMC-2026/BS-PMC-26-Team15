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
    public class US29DeleteShelterTests
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
                TempData = new TempDataDictionary(httpContext, new US29FakeTempDataProvider())
            };
        }

        [TestMethod]
        public void US29_DeleteShelter_RemovesShelter_WhenShelterExists()
        {
            using var context = CreateContext(nameof(US29_DeleteShelter_RemovesShelter_WhenShelterExists));

            context.ContributorShelters.Add(new ContributorShelter
            {
                Id = 1,
                Name = "Old Shelter",
                Address = "Old Address",
                Latitude = 31.1,
                Longitude = 34.1,
                UserId = "contributor1",
                Status = "Approved",
                CreatedAt = DateTime.Now
            });

            context.SaveChanges();

            var controller = CreateController(context);

            controller.DeleteShelter(1);

            Assert.AreEqual(0, context.ContributorShelters.Count());
        }

        [TestMethod]
        public void US29_DeleteShelter_RemovesRelatedImages_WhenShelterHasImages()
        {
            using var context = CreateContext(nameof(US29_DeleteShelter_RemovesRelatedImages_WhenShelterHasImages));

            context.ContributorShelters.Add(new ContributorShelter
            {
                Id = 1,
                Name = "Shelter With Images",
                Address = "Address",
                Latitude = 31.1,
                Longitude = 34.1,
                UserId = "contributor1",
                Status = "Approved",
                CreatedAt = DateTime.Now,
                Images = new List<ContributorShelterImage>
                {
                    new ContributorShelterImage
                    {
                        Id = 10,
                        ContributorShelterId = 1,
                        ImageUrl = "/uploads/img1.jpg"
                    },
                    new ContributorShelterImage
                    {
                        Id = 11,
                        ContributorShelterId = 1,
                        ImageUrl = "/uploads/img2.jpg"
                    }
                }
            });

            context.SaveChanges();

            var controller = CreateController(context);

            controller.DeleteShelter(1);

            Assert.AreEqual(0, context.ContributorShelterImages.Count());
        }

        [TestMethod]
        public void US29_DeleteShelter_RedirectsToAllShelters_AfterSuccessfulDelete()
        {
            using var context = CreateContext(nameof(US29_DeleteShelter_RedirectsToAllShelters_AfterSuccessfulDelete));

            context.ContributorShelters.Add(new ContributorShelter
            {
                Id = 1,
                Name = "Shelter",
                Address = "Address",
                Latitude = 31.1,
                Longitude = 34.1,
                UserId = "contributor1",
                Status = "Approved",
                CreatedAt = DateTime.Now
            });

            context.SaveChanges();

            var controller = CreateController(context);

            var result = controller.DeleteShelter(1) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("AllShelters", result.ActionName);
        }

        [TestMethod]
        public void US29_DeleteShelter_ReturnsNotFound_WhenShelterDoesNotExist()
        {
            using var context = CreateContext(nameof(US29_DeleteShelter_ReturnsNotFound_WhenShelterDoesNotExist));

            var controller = CreateController(context);

            var result = controller.DeleteShelter(999);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public void US29_AllSheltersView_ContainsDeleteButtonAndConfirmationPopup()
        {
            var viewPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..",
                "..",
                "..",
                "..",
                "SamiSpot",
                "Views",
                "Admin",
                "AllShelters.cshtml"
            );

            var html = File.ReadAllText(viewPath);

            Assert.IsTrue(html.Contains("DeleteShelter"));
            Assert.IsTrue(html.Contains("confirm("));
            Assert.IsTrue(html.Contains("Delete"));
        }
    }

    public class US29FakeTempDataProvider : ITempDataProvider
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