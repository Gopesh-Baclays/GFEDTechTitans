using System;

namespace RECAP.Models
{
    public class EngagementViewModel
    {
        public int ProspectId { get; set; }
        public string SelectedStage { get; set; }

        // Prospect Information
        public string ProspectName { get; set; }
        public string ProspectPhoneNumber { get; set; }
        public decimal PredictabilityScore { get; set; }

        // Guardian Information
        public string GuardianName { get; set; }
        public string GuardianPhoneNumber { get; set; }

        // Referee Information
        public string RefereeName { get; set; }
        public string RefereePhoneNumber { get; set; }
    }

    /// <summary>
    /// Engagement request model for handling engagement actions
    /// </summary>
    public class EngagementRequest
    {
        public int ProspectId { get; set; }
        public string Stage { get; set; }
        public string PartyType { get; set; } // "Prospect", "Guardian", "Referee"
        public string EngagementChannel { get; set; } // "whatsapp", "socialmedia", "message"
        public string PhoneNumber { get; set; }
        public string Name { get; set; }
    }
}