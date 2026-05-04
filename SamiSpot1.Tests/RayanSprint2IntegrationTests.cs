using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace SamiSpot1.Tests
{
    [TestClass]
    public class RayanSprint2IntegrationTests
    {
        [TestMethod]
        public async Task US16_MyShelters_Page_ReturnsSuccess()
        {
            await using var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient();

            var response = await client.GetAsync("/Contributor/MyShelters");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task US14_EditShelter_Page_ReturnsSuccess()
        {
            await using var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient();

            var response = await client.GetAsync("/Contributor/EditShelter/1");

            // could be OK or redirect to login (both acceptable)
            Assert.IsTrue(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Redirect
            );
        }

        [TestMethod]
        public async Task US15_DeleteShelter_Post_RedirectsToLogin_WhenNotLoggedIn()
        {
            await using var factory = new WebApplicationFactory<Program>();

            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var response = await client.PostAsync("/Contributor/DeleteShelter/1", null);

            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.IsTrue(response.Headers.Location.ToString().Contains("/Account/Login"));
        }
        [TestMethod]
        public async Task US17_EditShelter_Post_ReturnsBadRequest_WithoutToken()
        {
            await using var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient();

            var response = await client.PostAsync("/Contributor/EditShelter/1", null);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}