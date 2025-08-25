// FamilyBudgetApi/Models/Snapshot.cs
using Google.Cloud.Firestore;
using System.Collections.Generic;

namespace FamilyBudgetApi.Models
{
   [FirestoreData]
    public class Snapshot
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty("Date", ConverterType = typeof(FirestoreDateStringConverter))]
        public string? Date { get; set; }

        [FirestoreProperty("Accounts")]
        public List<SnapshotAccount> Accounts { get; set; } = new();

        [FirestoreProperty]
        public double NetWorth { get; set; }

        [FirestoreProperty("CreatedAt", ConverterType = typeof(FirestoreDateStringConverter))]
        public string? CreatedAt { get; set; }
    }

    [FirestoreData]
    public class SnapshotAccount
    {
        [FirestoreProperty]
        public string AccountId { get; set; } = string.Empty;

        [FirestoreProperty]
        public string AccountName { get; set; } = string.Empty;

        [FirestoreProperty]
        public double Value { get; set; }

        [FirestoreProperty]
        public string Type { get; set; } = string.Empty;
    }
}