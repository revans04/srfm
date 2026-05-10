using System.Collections.Generic;

namespace FamilyBudgetApi.Models
{
    public class PayeeSpendingRow
    {
        public string Payee { get; set; } = string.Empty;

        public double Total { get; set; }

        public List<PayeeCategoryBreakdown> Categories { get; set; } = new();
    }

    public class PayeeCategoryBreakdown
    {
        public string Name { get; set; } = string.Empty;

        public double Amount { get; set; }
    }
}
