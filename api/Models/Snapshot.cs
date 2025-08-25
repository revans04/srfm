// FamilyBudgetApi/Models/Snapshot.cs
using Google.Cloud.Firestore;
using System.Collections.Generic;

namespace FamilyBudgetApi.Models
{
   [FirestoreData]
    public class Snapshot
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("date")]
        public Timestamp? Date { get; set; }

        [FirestoreProperty("accounts")]
        public List<SnapshotAccount> Accounts { get; set; } = new();

        [FirestoreProperty("netWorth")]
        public double NetWorth { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp? CreatedAt { get; set; }
    }

    [FirestoreData]
    public class SnapshotAccount
    {
        [FirestoreProperty("accountId")]
        public string AccountId { get; set; } = string.Empty;

        [FirestoreProperty("accountName")]
        public string AccountName { get; set; } = string.Empty;

        [FirestoreProperty("value")]
        public double Value { get; set; }

        [FirestoreProperty("type")]
        public string Type { get; set; } = string.Empty;
    }
}