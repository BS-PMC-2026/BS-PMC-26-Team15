using Microsoft.AspNetCore.Mvc;
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
                return Unauthorized();
            }

            feedback.UserName = userName;
            feedback.CreatedAt = DateTime.Now;

            _context.Feedbacks.Add(feedback);
            _context.SaveChanges();

            return Ok();
        }

        [HttpGet]
        public IActionResult GetByShelter(int shelterId)
        {
            var feedbacks = _context.Feedbacks
                .Where(f => f.ShelterId == shelterId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new
                {
                    userName = f.UserName,
                    comment = f.Comment,
                    createdAt = f.CreatedAt
                })
                .ToList();

            return Json(feedbacks);
        }
    }
}