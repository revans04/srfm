using System.Collections.Generic;

namespace FamilyBudgetApi.Models
{
    public class GoalContribution
    {
        public string? Month { get; set; }
        public double Amount { get; set; }
    }

    public class GoalSpend
    {
        public string? TxId { get; set; }
        public string? TxDate { get; set; }
        public string? Merchant { get; set; }
        public double Amount { get; set; }
    }

    public class GoalDetails
    {
        public List<GoalContribution> Contributions { get; set; } = new();
        public List<GoalSpend> Spend { get; set; } = new();
    }
}
