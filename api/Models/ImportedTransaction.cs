using Google.Cloud.Firestore;

namespace FamilyBudgetApi.Models
{

    public class ImportedTransaction
    {
        public string? Id { get; set; }

        public string AccountId { get; set; }

        public string? AccountNumber { get; set; }

        public string? AccountSource { get; set; }

        public string? Payee { get; set; }

        public string? TransactionDate { get; set; }

        public string? PostedDate { get; set; }

        public double? Amount { get; set; }

        public string? Status { get; set; }

        public bool Matched { get; set; }

        public bool Ignored { get; set; }

        public double? DebitAmount { get; set; }
        
        public double? CreditAmount { get; set; }

        public string? CheckNumber { get; set; }

        public bool? Deleted { get; set; }
    }

    public class ImportedTransactionDoc
    {
        public string Id { get; set; } // Maps to the Firestore "id" field (e.g., "c2cf43c9-08a5-4f40-bdbc-195797a91bc1")

        public string FamilyId { get; set; }

        [FirestoreProperty("userId")] // Who imported it
        public string UserId { get; set; }

        public ImportedTransaction[] importedTransactions { get; set; }
    }
}
