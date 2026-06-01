using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SamiSpot.Data;
using SamiSpot.Models;
using SamiSpot.Services;

namespace SamiSpot.Controllers
{
    public class AiChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly OpenAiService _ai;

        public AiChatController(ApplicationDbContext context, OpenAiService ai)
        {
            _context = context;
            _ai = ai;
        }
        // US34: allow AI to use current location (BSPMT15-212)
        private int? GetCurrentUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (int.TryParse(userIdStr, out int userId))
                return userId;

            return null;
        }

        public IActionResult Index(int? sessionId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var sessions = _context.ChatSessions
                .AsNoTracking()
                .Where(s => s.UserId == userId.Value)
                .OrderByDescending(s => s.UpdatedAt)
                .ToList();

            ChatSession? currentSession = null;
            List<ChatMessage> messages = new List<ChatMessage>();

            if (sessionId.HasValue)
            {
                currentSession = _context.ChatSessions
                    .AsNoTracking()
                    .FirstOrDefault(s => s.Id == sessionId.Value && s.UserId == userId.Value);

                if (currentSession != null)
                {
                    messages = _context.ChatMessages
                        .AsNoTracking()
                        .Where(m => m.ChatSessionId == currentSession.Id)
                        .OrderBy(m => m.SentAt)
                        .ToList();
                }
            }

            var vm = new AiChatViewModel
            {
                Sessions = sessions,
                CurrentSession = currentSession,
                Messages = messages,
                CurrentSessionId = currentSession?.Id
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult NewChat()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var newSession = new ChatSession
            {
                UserId = userId.Value,
                Title = "New Chat",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.ChatSessions.Add(newSession);
            _context.SaveChanges();

            return RedirectToAction("Index", new { sessionId = newSession.Id });
        }

        [HttpPost]
        public async Task<IActionResult> SendMessageAjax(int sessionId, string message, double? latitude, double? longitude)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(message))
                return BadRequest("Empty message");

            message = message.Trim();

            var session = _context.ChatSessions
                .FirstOrDefault(s => s.Id == sessionId && s.UserId == userId.Value);

            if (session == null)
                return NotFound();

            var userMsg = new ChatMessage
            {
                ChatSessionId = sessionId,
                Role = "user",
                Content = message,
                SentAt = DateTime.Now
            };

            _context.ChatMessages.Add(userMsg);

            if (session.Title == "New Chat")
                session.Title = message.Length > 35 ? message.Substring(0, 35) + "..." : message;

            string aiReplyText;

            try
            {
                aiReplyText = await _ai.GetReply(message, latitude, longitude);
            }
            catch (Exception ex)
            {
                aiReplyText = ex.Message;
            }

            var assistantMsg = new ChatMessage
            {
                ChatSessionId = sessionId,
                Role = "assistant",
                Content = aiReplyText,
                SentAt = DateTime.Now
            };

            _context.ChatMessages.Add(assistantMsg);
            session.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new
            {
                userMessage = message,
                assistantMessage = aiReplyText,
                sessionTitle = session.Title
            });
        }

        [HttpPost]
        public IActionResult DeleteChat(int sessionId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var session = _context.ChatSessions
                .FirstOrDefault(s => s.Id == sessionId && s.UserId == userId.Value);

            if (session == null)
                return RedirectToAction("Index");

            var messages = _context.ChatMessages
                .Where(m => m.ChatSessionId == sessionId)
                .ToList();

            _context.ChatMessages.RemoveRange(messages);
            _context.ChatSessions.Remove(session);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}