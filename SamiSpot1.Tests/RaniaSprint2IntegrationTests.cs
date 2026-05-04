using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace SamiSpot1.Tests
{
    [TestClass]
    public class RaniaSprint2IntegrationTests
    {
        [TestMethod]
        public async Task US13_AddShelter_Page_ReturnsSuccess()
        {
            await using var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient();

            var response = await client.GetAsync("/Contributor/AddShelter");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task US25_AdminPendingShelters_Page_ReturnsSuccess()
        {
            await using var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient();

            var response = await client.GetAsync("/Admin/PendingShelters");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task US26_ApproveShelter_Post_RedirectsToPendingShelters()
        {
            await using var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var response = await client.PostAsync("/Admin/ApproveShelter/999", null);

            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.IsTrue(response.Headers.Location.ToString().Contains("PendingShelters"));
        }

        [TestMethod]
        public async Task US27_RejectShelter_Post_RedirectsToPendingShelters()
        {
            await using var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var response = await client.PostAsync("/Admin/RejectShelter/999", null);

            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.IsTrue(response.Headers.Location.ToString().Contains("PendingShelters"));
        }
    }
}