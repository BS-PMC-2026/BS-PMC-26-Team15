using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamiSpot.Data;
using SamiSpot.Models;
using SamiSpot.Services;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SamiSpot.Tests.Services
{
    [TestClass]
    public class ServicesTests
    {
        private ApplicationDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private HttpClient CreateHttpClient(string json)
        {
            var handler = new FakeHttpMessageHandler(json);
            return new HttpClient(handler);
        }

        [TestMethod]
        public void WebMercatorToLatLng_ShouldReturnZeroForZeroInput()
        {
            var result = CoordinateHelper.WebMercatorToLatLng(0, 0);

            Assert.AreEqual(0, result.Latitude, 0.0001);
            Assert.AreEqual(0, result.Longitude, 0.0001);
        }

        [TestMethod]
        public void UpdateCityLatLng_ShouldUpdateOnlyCitiesWithZeroCoordinates()
        {
            using var context = CreateInMemoryDb();

            context.CityLocations.Add(new CityLocation
            {
                HebrewName = "עיר א",
                EnglishName = "CityA",
                X = 0,
                Y = 0,
                Latitude = 0,
                Longitude = 0
            });

            context.CityLocations.Add(new CityLocation
            {
                HebrewName = "עיר ב",
                EnglishName = "CityB",
                X = 1000,
                Y = 1000,
                Latitude = 31.5,
                Longitude = 34.6
            });

            context.SaveChanges();

            var service = new CityCoordinateService(context);

            service.UpdateCityLatLng();

            var updatedCity = context.CityLocations.First(c => c.EnglishName == "CityA");
            var unchangedCity = context.CityLocations.First(c => c.EnglishName == "CityB");

            Assert.AreEqual(0, updatedCity.Latitude, 0.0001);
            Assert.AreEqual(0, updatedCity.Longitude, 0.0001);

            Assert.AreEqual(31.5, unchangedCity.Latitude, 0.0001);
            Assert.AreEqual(34.6, unchangedCity.Longitude, 0.0001);
        }

        [TestMethod]
        public void ImportCitiesFromCsv_ShouldImportValidCities()
        {
            using var context = CreateInMemoryDb();

            var tempFile = Path.GetTempFileName();

            try
            {
                File.WriteAllLines(tempFile, new[]
                {
                    "X,Y,a,b,c,HebrewName,d,e,f,EnglishName",
                    "1000,2000,x,x,x,אשקלון,x,x,x,Ashkelon",
                    "3000,4000,x,x,x,באר שבע,x,x,x,Beer Sheva"
                });

                var service = new CityImportService(context);

                service.ImportCitiesFromCsv(tempFile);

                Assert.AreEqual(2, context.CityLocations.Count());
                Assert.IsTrue(context.CityLocations.Any(c => c.HebrewName == "אשקלון"));
                Assert.IsTrue(context.CityLocations.Any(c => c.EnglishName == "Beer Sheva"));
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [TestMethod]
        public async Task FetchAndSaveAlertsAsync_ShouldSaveValidAlert()
        {
            using var context = CreateInMemoryDb();

            string json = @"{
                ""alertsHistory"": [
                    {
                        ""id"": 1,
                        ""description"": ""test"",
                        ""alerts"": [
                            {
                                ""time"": 1712000000,
                                ""cities"": [""Ashkelon""],
                                ""threat"": 2,
                                ""isDrill"": false
                            }
                        ]
                    }
                ]
            }";

            var httpClient = CreateHttpClient(json);
            var service = new RedAlertService(httpClient, context);

            await service.FetchAndSaveAlertsAsync();

            Assert.AreEqual(1, context.Alerts.Count());

            var alert = context.Alerts.First();
            Assert.AreEqual("Ashkelon", alert.CityHebrew);
            Assert.AreEqual(2, alert.Threat);
            Assert.AreEqual(false, alert.IsDrill);
        }

        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _json;

            public FakeHttpMessageHandler(string json)
            {
                _json = json;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_json)
                };

                return Task.FromResult(response);
            }
        }
    }
}