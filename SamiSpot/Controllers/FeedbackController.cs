using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SamiSpot.Data;
using SamiSpot.Models;

namespace SamiSpot.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FeedbackController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult Add([FromBody] Feedback feedback)
        {
            var userName = HttpContext.Session.GetString("UserName");

            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized(new { message = "user not logged in" });
            }

            if (feedback == null || string.IsNullOrWhiteSpace(feedback.Comment))
            {
                return BadRequest(new { message = "comment is empty" });
            }

            feedback.UserName = userName;
            feedback.CreatedAt = DateTime.Now;

            _context.Feedbacks.Add(feedback);
            _context.SaveChanges();

            return Ok(new { message = "feedback saved" });
        }

        [HttpPost]
        public IActionResult AddReply([FromBody] ReplyRequest request)
        {
            var userName = HttpContext.Session.GetString("UserName");

            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized(new { message = "You must log in first." });
            }

            if (string.IsNullOrWhiteSpace(request.ReplyText))
            {
                return BadRequest(new { message = "Reply is empty." });
            }

            var feedbackExists = _context.Feedbacks.Any(f => f.Id == request.FeedbackId);
            if (!feedbackExists)
            {
                return NotFound(new { message = "Feedback not found." });
            }

            if (request.ParentReplyId.HasValue)
            {
                var parentReplyExists = _context.FeedbackReplies.Any(r => r.Id == request.ParentReplyId.Value);
                if (!parentReplyExists)
                {
                    return NotFound(new { message = "Parent reply not found." });
                }
            }

            var reply = new FeedbackReply
            {
                FeedbackId = request.FeedbackId,
                ParentReplyId = request.ParentReplyId,
                UserName = userName,
                ReplyText = request.ReplyText,
                CreatedAt = DateTime.Now
            };

            _context.FeedbackReplies.Add(reply);
            _context.SaveChanges();

            return Ok(new { message = "reply added" });
        }

        [HttpGet]
        public IActionResult GetReplies(int feedbackId)
        {
            var replies = _context.FeedbackReplies
                .Where(r => r.FeedbackId == feedbackId)
                .OrderBy(r => r.CreatedAt)
                .Select(r => new
                {
                    id = r.Id,
                    feedbackId = r.FeedbackId,
                    parentReplyId = r.ParentReplyId,

                    userName = r.UserName,
                    role = _context.Users
        .Where(u => u.UserName == r.UserName)
        .Select(u => u.RoleType)
        .FirstOrDefault(),

                    replyText = r.ReplyText,
                    createdAt = r.CreatedAt
                })
                .ToList();

            return Json(replies);
        }

        [HttpGet]
        public IActionResult GetByShelter(int shelterId)
        {
            var feedbacks = _context.Feedbacks
                .Where(f => f.ShelterId == shelterId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new
                {
                    id = f.Id,
                    userName = f.UserName,

                    // 🔥 THIS IS THE IMPORTANT PART
                    role = _context.Users
                        .Where(u => u.UserName == f.UserName)
                        .Select(u => u.RoleType)
                        .FirstOrDefault(),

                    comment = f.Comment,
                    createdAt = f.CreatedAt
                })
                .ToList();

            return Json(feedbacks);
        }
    }

    public class ReplyRequest
    {
        public int FeedbackId { get; set; }
        public int? ParentReplyId { get; set; }
        public string ReplyText { get; set; }
    }
}