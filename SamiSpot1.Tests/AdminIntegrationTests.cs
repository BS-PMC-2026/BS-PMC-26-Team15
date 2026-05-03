using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamiSpot.Data;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SamiSpot1.Tests
{
    [TestClass]
    public class AdminIntegrationTests
    {
        private HttpClient _client;

        [TestInitialize]
        public void Setup()
        {
            var factory = new WebApplicationFactory<Program>();

            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false // ✅ FIX
            });
        }

        [TestMethod]
        public async Task ManageUsers_PageLoads()
        {
            var response = await _client.GetAsync("/Admin/ManageUsers");

            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        [TestMethod]
        public async Task Contributors_PageLoads()
        {
            var response = await _client.GetAsync("/Admin/Contributors");

            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        [TestMethod]
        public async Task AddUser_Post_AddsUserToDatabase()
        {
            var formData = new FormUrlEncodedContent(new[]
            {
        new KeyValuePair<string, string>("UserName", "intUser"),
        new KeyValuePair<string, string>("Email", "intuser@gmail.com"),
        new KeyValuePair<string, string>("Password", "Abcd1234"),
        new KeyValuePair<string, string>("ConfirmPassword", "Abcd1234"),
        new KeyValuePair<string, string>("RoleType", "User")
    });

            await _client.PostAsync("/Admin/AddUser", formData);

            // create scope to access DB
            using var scope = new WebApplicationFactory<Program>().Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = context.Users.FirstOrDefault(u => u.UserName == "intUser");

            Assert.IsNotNull(user); // ✅ THIS is real integration validation
        }
    }
}