using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SamiSpot.Controllers;
using SamiSpot.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SamiSpot1.Tests
{
    [TestClass]
    public class HomeControllerTests
    {
        private HomeController CreateController()
        {
            var loggerMock = new Mock<ILogger<HomeController>>();

            var controller = new HomeController(loggerMock.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            return controller;
        }

        [TestMethod]
        public void Index_ReturnsViewResult()
        {
            var controller = CreateController();
            var result = controller.Index();

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void Privacy_ReturnsViewResult()
        {
            var controller = CreateController();
            var result = controller.Privacy();

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }
        [TestClass]
        public class ViewPagesTests
        {
            private static WebApplicationFactory<Program> _factory = null!;
            private static HttpClient _client = null!;

            [ClassInitialize]
            public static void Setup(TestContext context)
            {
                _factory = new WebApplicationFactory<Program>();
                _client = _factory.CreateClient();
            }

            [TestMethod]
            public async Task LoginPage_ShouldContainLoginTitle()
            {
                var response = await _client.GetAsync("/Account/Login");
                var html = await response.Content.ReadAsStringAsync();

                Assert.IsTrue(response.IsSuccessStatusCode);
                Assert.IsTrue(html.Contains("Login"));
            }

            [TestMethod]
            public async Task RegisterPage_ShouldContainSignUpTitle()
            {
                var response = await _client.GetAsync("/Account/Register");
                var html = await response.Content.ReadAsStringAsync();

                Assert.IsTrue(response.IsSuccessStatusCode);
                Assert.IsTrue(html.Contains("Sign Up"));
            }

            [TestMethod]
            public async Task EmergencyPage_ShouldContainEmergencyContacts()
            {
                var response = await _client.GetAsync("/Home/Emergency");
                var html = await response.Content.ReadAsStringAsync();

                Assert.IsTrue(response.IsSuccessStatusCode);
                Assert.IsTrue(html.Contains("Emergency Contacts"));
            }

            [TestMethod]
            public async Task PrivacyPage_ShouldContainPrivacySafety()
            {
                var response = await _client.GetAsync("/Home/Privacy");
                var html = await response.Content.ReadAsStringAsync();

                Assert.IsTrue(response.IsSuccessStatusCode);
                Assert.IsTrue(html.Contains("Privacy & Safety"));
            }
        }
        [TestMethod]
        public void Emergency_ReturnsViewResult()
        {
            var controller = CreateController();
            var result = controller.Emergency();

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void DataSources_ReturnsViewResult()
        {
            var controller = CreateController();
            var result = controller.DataSources();

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void Error_ReturnsErrorViewModel()
        {
            var controller = CreateController();
            controller.HttpContext.TraceIdentifier = "trace-123";

            var result = controller.Error() as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Model);
            Assert.IsInstanceOfType(result.Model, typeof(ErrorViewModel));

            var model = (ErrorViewModel)result.Model;
            Assert.AreEqual("trace-123", model.RequestId);
        }
    }
}