using System.Collections.Generic;
using OpenAI.Chat;

namespace SamiSpot.Models
{
    public class AiChatViewModel
    {
        public List<ChatSession> Sessions { get; set; } = new List<ChatSession>();
        public ChatSession CurrentSession { get; set; }
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public int? CurrentSessionId { get; set; }
    }
}