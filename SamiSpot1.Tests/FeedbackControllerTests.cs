using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SamiSpot.Controllers;
using SamiSpot.Data;
using SamiSpot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SamiSpot1.Tests
{
    [TestClass]
    public class FeedbackControllerTests
    {
        private ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        private FeedbackController CreateControllerWithSession(ApplicationDbContext context, string? userName = null)
        {
            var controller = new FeedbackController(context);

            var httpContext = new DefaultHttpContext();
            var session = new TestSession();
            httpContext.Session = session;

            if (userName != null)
            {
                session.SetString("UserName", userName);
            }

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        [TestMethod]
        public void Add_WhenInputIsValid_SavesFeedbackAndReturnsOk()
        {
            using var context = CreateContext(nameof(Add_WhenInputIsValid_SavesFeedbackAndReturnsOk));
            var controller = CreateControllerWithSession(context, "rania");

            var feedback = new Feedback
            {
                ShelterId = 1,
                Comment = "safe shelter"
            };

            var result = controller.Add(feedback);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            Assert.AreEqual(1, context.Feedbacks.Count());

            var saved = context.Feedbacks.First();
            Assert.AreEqual("rania", saved.UserName);
            Assert.AreEqual("safe shelter", saved.Comment);
        }

        [TestMethod]
        public void AddReply_WhenInputIsValid_SavesReplyAndReturnsOk()
        {
            using var context = CreateContext(nameof(AddReply_WhenInputIsValid_SavesReplyAndReturnsOk));

            context.Feedbacks.Add(new Feedback
            {
                Id = 1,
                ShelterId = 5,
                UserName = "user1",
                Comment = "good place",
                CreatedAt = DateTime.Now
            });
            context.SaveChanges();

            var controller = CreateControllerWithSession(context, "rania");

            var request = new ReplyRequest
            {
                FeedbackId = 1,
                ReplyText = "thanks"
            };

            var result = controller.AddReply(request);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            Assert.AreEqual(1, context.FeedbackReplies.Count());

            var saved = context.FeedbackReplies.First();
            Assert.AreEqual(1, saved.FeedbackId);
            Assert.AreEqual("rania", saved.UserName);
            Assert.AreEqual("thanks", saved.ReplyText);
        }
    }

    [TestClass]
    public class FeedbackReplyTests
    {
        [TestMethod]
        public void FeedbackReply_ShouldStorePropertyValuesCorrectly()
        {
            var date = new DateTime(2026, 4, 6);

            var reply = new FeedbackReply
            {
                Id = 1,
                FeedbackId = 10,
                ParentReplyId = 5,
                UserName = "rania",
                ReplyText = "test reply",
                CreatedAt = date
            };

            Assert.AreEqual(1, reply.Id);
            Assert.AreEqual(10, reply.FeedbackId);
            Assert.AreEqual(5, reply.ParentReplyId);
            Assert.AreEqual("rania", reply.UserName);
            Assert.AreEqual("test reply", reply.ReplyText);
            Assert.AreEqual(date, reply.CreatedAt);
        }
    }

    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _sessionStorage = new Dictionary<string, byte[]>();

        public IEnumerable<string> Keys => _sessionStorage.Keys;
        public string Id => Guid.NewGuid().ToString();
        public bool IsAvailable => true;

        public void Clear()
        {
            _sessionStorage.Clear();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _sessionStorage.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            _sessionStorage[key] = value;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _sessionStorage.TryGetValue(key, out value!);
        }
    }

    public static class TestSessionExtensions
    {
        public static void SetString(this ISession session, string key, string value)
        {
            session.Set(key, Encoding.UTF8.GetBytes(value));
        }
    }
}