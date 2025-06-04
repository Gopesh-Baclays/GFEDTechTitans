namespace RECAP.Models
{
    public class DashboardViewModel
    {
        public decimal TotalAmount { get; set; }
        public decimal MatchedBalanceRuleBased { get; set; }
        public int UnmatchedBalance { get; set; }
        public int MatchedBalanceAiPercent { get; set; }
        // Add more properties as needed for charts, projects, etc.
    }
}