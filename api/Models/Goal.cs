using Google.Cloud.Firestore;

namespace FamilyBudgetApi.Models
{
    [FirestoreData]
    public class Goal
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("entityId")]
        public string EntityId { get; set; } = string.Empty;

        [FirestoreProperty("name")]
        public string Name { get; set; } = string.Empty;

        [FirestoreProperty("totalTarget")]
        public double TotalTarget { get; set; }

        [FirestoreProperty("monthlyTarget")]
        public double MonthlyTarget { get; set; }

        [FirestoreProperty("savedToDate")]
        public double SavedToDate { get; set; }

        [FirestoreProperty("spentToDate")]
        public double SpentToDate { get; set; }

        [FirestoreProperty("targetDate")]
        public string? TargetDate { get; set; }

        [FirestoreProperty("archived")]
        public bool Archived { get; set; }
    }
}
