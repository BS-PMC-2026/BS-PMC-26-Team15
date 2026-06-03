using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamiSpot.Controllers;
using SamiSpot.Data;
using SamiSpot.Models;
using SamiSpot.Services;

namespace SamiSpot1.Tests
{
    [TestClass]
    public class AiChatTests
    {
        private ApplicationDbContext CreateFakeDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private AiChatController CreateController(ApplicationDbContext db, int? userId = 1)
        {
            // we pass null for OpenAiService because we do not want github actions
            // to call real openai api. controller catches the error and saves reply text.
            var controller = new AiChatController(db, null!);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new FakeSession();

            if (userId != null)
                httpContext.Session.SetString("UserId", userId.Value.ToString());

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        [TestMethod]
        public void Unit_Index_NoUserLoggedIn_RedirectsToLogin()
        {
            var db = CreateFakeDb();
            var controller = CreateController(db, null);

            var result = controller.Index(null);

            var redirect = result as RedirectToActionResult;

            Assert.IsNotNull(redirect);
            Assert.AreEqual("Login", redirect.ActionName);
            Assert.AreEqual("Account", redirect.ControllerName);
        }

        [TestMethod]
        public void Unit_NewChat_UserLoggedIn_CreatesNewChat()
        {
            var db = CreateFakeDb();
            var controller = CreateController(db, 1);

            var result = controller.NewChat();

            Assert.AreEqual(1, db.ChatSessions.Count());
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        }

        [TestMethod]
        public async Task Unit_SendMessage_NoUserLoggedIn_ReturnsUnauthorized()
        {
            var db = CreateFakeDb();
            var controller = CreateController(db, null);

            var result = await controller.SendMessageAjax(1, "hello", null, null);

            Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
        }

        [TestMethod]
        public async Task Unit_SendMessage_EmptyMessage_ReturnsBadRequest()
        {
            var db = CreateFakeDb();

            db.ChatSessions.Add(new ChatSession
            {
                Id = 1,
                UserId = 1,
                Title = "New Chat",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });

            db.SaveChanges();

            var controller = CreateController(db, 1);

            var result = await controller.SendMessageAjax(1, "   ", null, null);

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            Assert.AreEqual(0, db.ChatMessages.Count());
        }

        [TestMethod]
        public async Task Unit_SendMessage_WrongSession_ReturnsNotFound()
        {
            var db = CreateFakeDb();
            var controller = CreateController(db, 1);

            var result = await controller.SendMessageAjax(999, "hello", null, null);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public void Unit_DeleteChat_NoUserLoggedIn_RedirectsToLogin()
        {
            var db = CreateFakeDb();
            var controller = CreateController(db, null);

            var result = controller.DeleteChat(1);

            var redirect = result as RedirectToActionResult;

            Assert.IsNotNull(redirect);
            Assert.AreEqual("Login", redirect.ActionName);
            Assert.AreEqual("Account", redirect.ControllerName);
        }

        [TestMethod]
        public async Task Integration_LoginThenSendMessage_ReplyNotEmpty_AndSavedInHistory()
        {
            var db = CreateFakeDb();

            db.ChatSessions.Add(new ChatSession
            {
                Id = 1,
                UserId = 1,
                Title = "New Chat",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });

            db.SaveChanges();

            var controller = CreateController(db, 1);

            var result = await controller.SendMessageAjax(
                sessionId: 1,
                message: "where is the nearest shelter?",
                latitude: 31.2518,
                longitude: 34.7913
            );

            Assert.IsInstanceOfType(result, typeof(JsonResult));

            var messages = db.ChatMessages.OrderBy(m => m.SentAt).ToList();

            Assert.AreEqual(2, messages.Count);
            Assert.AreEqual("user", messages[0].Role);
            Assert.AreEqual("assistant", messages[1].Role);
            Assert.IsFalse(string.IsNullOrWhiteSpace(messages[1].Content));
        }

        [TestMethod]
        public void Integration_LoginThenDeleteChat_DeletesChatAndMessages()
        {
            var db = CreateFakeDb();

            db.ChatSessions.Add(new ChatSession
            {
                Id = 1,
                UserId = 1,
                Title = "Chat To Delete",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });

            db.ChatMessages.Add(new ChatMessage
            {
                ChatSessionId = 1,
                Role = "user",
                Content = "hello",
                SentAt = DateTime.Now
            });

            db.SaveChanges();

            var controller = CreateController(db, 1);

            var result = controller.DeleteChat(1);

            Assert.AreEqual(0, db.ChatSessions.Count());
            Assert.AreEqual(0, db.ChatMessages.Count());
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        }
    }

    public class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _data = new();

        public bool IsAvailable => true;
        public string Id => Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _data.Keys;

        public void Clear()
        {
            _data.Clear();
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
            _data.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            _data[key] = value;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _data.TryGetValue(key, out value!);
        }
    }
}