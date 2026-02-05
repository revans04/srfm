
namespace FamilyBudgetApi.Models
{
    public class Statement
    {
        public string Id { get; set; } = string.Empty;

        public string AccountNumber { get; set; } = string.Empty;

        public string StartDate { get; set; } = string.Empty;

        public double StartingBalance { get; set; }

        public string EndDate { get; set; } = string.Empty;

        public double EndingBalance { get; set; }

        public bool Reconciled { get; set; }
    }
}
