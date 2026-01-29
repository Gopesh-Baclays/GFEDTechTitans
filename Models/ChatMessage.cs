using System;

namespace RECAP.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int ProspectId { get; set; }
        public string SenderType { get; set; } // "User", "Bot", "Prospect", "Guardian", "Referee"
        public string SenderName { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string Channel { get; set; } // "whatsapp", "socialmedia", "message"
    }

    public class ChatRequest
    {
        public int ProspectId { get; set; }
        public string PartyType { get; set; } // "Prospect", "Guardian", "Referee"
        public string Message { get; set; }
        public string Channel { get; set; }
    }
}