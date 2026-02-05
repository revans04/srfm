
namespace FamilyBudgetApi.Models
{
    public class Goal
    {
        public string Id { get; set; } = string.Empty;

        public string EntityId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public double TotalTarget { get; set; }

        public double MonthlyTarget { get; set; }

        public double SavedToDate { get; set; }

        public double SpentToDate { get; set; }

        public string? TargetDate { get; set; }

        public bool Archived { get; set; }
    }
}
