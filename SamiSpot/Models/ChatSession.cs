using System;
using System.Collections.Generic;

namespace SamiSpot.Models
{
    public class ChatSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public User User { get; set; }
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}