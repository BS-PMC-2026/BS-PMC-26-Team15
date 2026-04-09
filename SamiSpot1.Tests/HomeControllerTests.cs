using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SamiSpot.Controllers;
using SamiSpot.Models;


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