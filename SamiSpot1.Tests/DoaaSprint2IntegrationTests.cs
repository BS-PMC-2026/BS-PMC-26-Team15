using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace SamiSpot1.Tests
{
    [TestClass]
    public class DoaaSprint2IntegrationTests
    {
        [TestMethod]
        public async Task US21_AdminDashboard_Page_ReturnsSuccess()
        {
            await using var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient();

            var response = await client.GetAsync("/Account/Admindashboard");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task US28_AdminAllShelters_Page_ReturnsSuccess()
        {
            await using var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient();

            var response = await client.GetAsync("/Admin/AllShelters");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task US29_DeleteShelter_WithoutAntiForgeryToken_ReturnsBadRequest()
        {
            await using var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient();

            var response = await client.PostAsync("/Admin/DeleteShelter/999", null);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}