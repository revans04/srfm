// FamilyBudgetApi/Models/Snapshot.cs
using System.Collections.Generic;

namespace FamilyBudgetApi.Models
{
    public class Snapshot
    {
        public string Id { get; set; }

        public string? Date { get; set; }

        public List<SnapshotAccount> Accounts { get; set; } = new();

        public double NetWorth { get; set; }

        public string? CreatedAt { get; set; }
    }

    public class SnapshotAccount
    {
        public string AccountId { get; set; } = string.Empty;

        public string AccountName { get; set; } = string.Empty;

        public double Value { get; set; }

        public string Type { get; set; } = string.Empty;
    }
}
