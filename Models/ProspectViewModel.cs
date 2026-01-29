namespace RECAP.Models
{
    public class ProspectViewModel
    {
        // Essential Information
        public string? CandidateName { get; set; }
        public string? MobileNumber { get; set; }
        public int? Age { get; set; }
        public DateOnly? DOB { get; set; }
        public string? AADHAR { get; set; }
        public string? GuardianName { get; set; }
        public string? Address { get; set; }
        public string? EmailId { get; set; }

        // Education & Background
        public string? FamilyIncome { get; set; }
        public string? EducationLevel { get; set; }
        public string? CurrentStage { get; set; }
        public string? SmartDevAvailable { get; set; }
        public string? InternetAccess { get; set; }

        // Preferences & Details
        public string? PreferredLanguage { get; set; }
        public string? PreferredContactMode { get; set; }
        public string? PreferredTimeForContact { get; set; }

        // Health & Personal Information
        public string? HealthIssues { get; set; }
        public bool? PastWorkExperience { get; set; }
        public string? PastWorkDetails { get; set; }
        public string? FamilySupport { get; set; }
        public int? FamilyMembers { get; set; }

        // Lifestyle Habits
        public bool? SmokingHabits { get; set; }
        public bool? DrinkingHabits { get; set; }
        public bool? Reference { get; set; }

        // Additional Information
        public string? ReferenceSourceOf { get; set; }
        public string? InstallmentDetails { get; set; }
        public string? PreviousProgram { get; set; }
        public string? ReasonForPlanning { get; set; }

        // Scoring
        public int? Score { get; set; }
    }
}
