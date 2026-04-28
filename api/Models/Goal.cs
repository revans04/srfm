
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

        /// <summary>
        /// Money the user already had toward this goal at goal-creation time
        /// (e.g. "I have $5,000 in my house fund already"). Added to the
        /// saved-to-date rollup. Has no transaction counterpart — it's a
        /// starting balance, not an event.
        /// </summary>
        public double OpeningBalance { get; set; }
    }
}
