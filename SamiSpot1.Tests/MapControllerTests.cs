using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SamiSpot.Controllers;
using SamiSpot.Data;
using SamiSpot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SamiSpot1.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class MapControllerTests
    {
        private ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        [TestMethod]
        public void Index_ReturnsViewResult()
        {
            using var context = CreateContext(nameof(Index_ReturnsViewResult));
            var controller = new MapController(context);

            var result = controller.Index();

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public async Task GetShelters_ReturnsOnlyActiveShelters()
        {
            using var context = CreateContext(nameof(GetShelters_ReturnsOnlyActiveShelters));

            context.Shelters.AddRange(
                new Shelter
                {
                    Name = "Active Shelter",
                    Address = "Addr 1",
                    City = "Ashkelon",
                    Latitude = 31.1,
                    Longitude = 34.1,
                    Source = "GovMap",
                    IsActive = true
                },
                new Shelter
                {
                    Name = "Inactive Shelter",
                    Address = "Addr 2",
                    City = "Ashdod",
                    Latitude = 31.3,
                    Longitude = 34.3,
                    Source = "GovMap",
                    IsActive = false
                }
            );

            context.SaveChanges();

            var controller = new MapController(context);

            var result = await controller.GetShelters() as OkObjectResult;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);

            var shelters = ((IEnumerable<object>)result.Value).Cast<object>().ToList();
            Assert.AreEqual(1, shelters.Count);
        }

        [TestMethod]
        public void GetCityRisks_AlertLast15Minutes_ShouldBeRed()
        {
            using var context = CreateContext(nameof(GetCityRisks_AlertLast15Minutes_ShouldBeRed));

            context.CityLocations.Add(new CityLocation
            {
                HebrewName = "אשקלון",
                Latitude = 31.6,
                Longitude = 34.5
            });

            context.Alerts.Add(new Alert
            {
                CityHebrew = "אשקלון",
                AlertTimeUtc = DateTime.UtcNow
            });

            context.SaveChanges();

            var controller = new MapController(context);

            var result = controller.GetCityRisks() as JsonResult;

            Assert.IsNotNull(result);

            var list = (IEnumerable<CityRiskDto>)result.Value;
            Assert.AreEqual("red", list.First().Color);
        }

        [TestMethod]
        public void GetLatestRedAlerts_ReturnsOnlyAlertsFromLast15Minutes()
        {
            using var context = CreateContext(nameof(GetLatestRedAlerts_ReturnsOnlyAlertsFromLast15Minutes));

            context.Alerts.Add(new Alert
            {
                CityHebrew = "אשקלון",
                AlertTimeUtc = DateTime.UtcNow.AddMinutes(-5)
            });

            context.Alerts.Add(new Alert
            {
                CityHebrew = "אשדוד",
                AlertTimeUtc = DateTime.UtcNow.AddHours(-1)
            });

            context.SaveChanges();

            var controller = new MapController(context);

            var result = controller.GetLatestRedAlerts() as JsonResult;

            Assert.IsNotNull(result);

            var alerts = ((IEnumerable<object>)result.Value).Cast<object>().ToList();
            Assert.AreEqual(1, alerts.Count);
        }
        [TestMethod]
        public void GetClosestShelters_WhenNoSheltersExist_ReturnsEmptyList()
        {
            using var context = CreateContext(nameof(GetClosestShelters_WhenNoSheltersExist_ReturnsEmptyList));

            var controller = new MapController(context);

            var req = new MapController.LocationRequest
            {
                Latitude = 31.5,
                Longitude = 34.5,
                Count = 3
            };

            var result = controller.GetClosestShelters(req) as JsonResult;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);

            var shelters = ((IEnumerable<object>)result.Value).Cast<object>().ToList();
            Assert.AreEqual(0, shelters.Count);
        }
        [TestMethod]
        public void GetClosestShelters_ReturnsRequestedCount()
        {
            using var context = CreateContext(nameof(GetClosestShelters_ReturnsRequestedCount));

            context.Shelters.AddRange(
                new Shelter
                {
                    Name = "Shelter 1",
                    Latitude = 31.50,
                    Longitude = 34.50,
                    IsActive = true,
                    Source = "GovMap"
                },
                new Shelter
                {
                    Name = "Shelter 2",
                    Latitude = 31.51,
                    Longitude = 34.51,
                    IsActive = true,
                    Source = "GovMap"
                },
                new Shelter
                {
                    Name = "Shelter 3",
                    Latitude = 31.52,
                    Longitude = 34.52,
                    IsActive = true,
                    Source = "GovMap"
                }
            );

            context.SaveChanges();

            var controller = new MapController(context);

            var req = new MapController.LocationRequest
            {
                Latitude = 31.5,
                Longitude = 34.5,
                Count = 2
            };

            var result = controller.GetClosestShelters(req) as JsonResult;

            Assert.IsNotNull(result);

            var shelters = ((IEnumerable<object>)result.Value).Cast<object>().ToList();
            Assert.AreEqual(2, shelters.Count);
        }
    }
}