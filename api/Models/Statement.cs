using Google.Cloud.Firestore;

namespace FamilyBudgetApi.Models
{
    [FirestoreData]
    public class Statement
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("accountNumber")]
        public string AccountNumber { get; set; } = string.Empty;

        [FirestoreProperty("startDate")]
        public string StartDate { get; set; } = string.Empty;

        [FirestoreProperty("startingBalance")]
        public double StartingBalance { get; set; }

        [FirestoreProperty("endDate")]
        public string EndDate { get; set; } = string.Empty;

        [FirestoreProperty("endingBalance")]
        public double EndingBalance { get; set; }

        [FirestoreProperty("reconciled")]
        public bool Reconciled { get; set; }
    }
}