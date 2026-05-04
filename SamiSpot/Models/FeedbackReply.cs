namespace SamiSpot.Models
{
    public class FeedbackReply
    {

        public int Id { get; set; }
        public int FeedbackId { get; set; }
        public int? ParentReplyId { get; set; }
        public string UserName { get; set; }
        public string ReplyText { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}