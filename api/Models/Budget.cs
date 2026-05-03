
namespace FamilyBudgetApi.Models
{
    public class BudgetInfo : Budget
    {
        public bool IsOwner { get; set; }

        public int TransactionCount { get; set; }
    }

    public class Budget
    {
        public string BudgetId { get; set; } = string.Empty;

        public required string FamilyId { get; set; }

        public string? EntityId { get; set; } // New field

        public required string Month { get; set; }

        public string? Label { get; set; }

        public double IncomeTarget { get; set; }

        public List<BudgetCategory> Categories { get; set; } = new();

        public List<Transaction> Transactions { get; set; } = new();

        public string? OriginalBudgetId { get; set; }

        public List<Merchant> Merchants { get; set; } = new();

        // Snapshot of the entity-level group taxonomy this budget belongs to.
        // Returned on every Budget GET so the frontend has both per-category
        // membership (`BudgetCategory.GroupId`) and the entity's groups.
        public List<BudgetGroup> Groups { get; set; } = new();
    }

    public class BudgetCategory
    {
        // bigint PK on budget_categories (nullable for new/in-flight rows).
        public long? Id { get; set; }
        public string? Name { get; set; }
        public double Target { get; set; }
        public bool IsFund { get; set; }
        // FK to budget_groups.id. Required server-side once a row is persisted;
        // nullable here so newly-typed groups can be resolved from `GroupName`.
        public string? GroupId { get; set; }
        // Display name hydrated on read (also accepted on write to upsert a
        // new group when `GroupId` is unknown).
        public string? GroupName { get; set; }
        public int SortOrder { get; set; }
        public double? Carryover { get; set; }
        public bool? Favorite { get; set; }

        // Name of another category that by default funds expenses in this
        // category. When set, the transaction form will offer to source the
        // expense from this category (creating a transfer transaction instead
        // of a standard expense).
        public string? FundingSourceCategory { get; set; }

        // UUID of a savings goal that by default funds expenses in this
        // category. Same auto-transfer behavior as FundingSourceCategory but
        // sourced from the goal's auto-synced fund category. The DB enforces
        // mutual exclusion with FundingSourceCategory.
        public string? FundingSourceGoalId { get; set; }
    }

    public class Transaction
    {
        public string? Id { get; set; }

        public string? BudgetId { get; set; }

        public string? Date { get; set; }

        public string? BudgetMonth { get; set; }

        public string? Merchant { get; set; }

        public List<TransactionCategory>? Categories { get; set; }

        public double Amount { get; set; }

        public string? Notes { get; set; }

        public bool Recurring { get; set; }

        public string? RecurringInterval { get; set; }

        public string? UserId { get; set; }

        public bool IsIncome { get; set; }

        public string? AccountNumber { get; set; }

        public string? AccountSource { get; set; }

        public string? TransactionDate { get; set; }

        public string? PostedDate { get; set; }

        public string? ImportedMerchant { get; set; }

        public string? Status { get; set; }

        public string? CheckNumber { get; set; }

        public bool? Deleted { get; set; }

        public string? EntityId { get; set; } // New field

        public string? TransactionType { get; set; } // 'standard' or 'transfer'

        // UUID of the savings goal that funded this expense. Persisted on
        // standard expense rows; goal computation reads it to mark the goal's
        // savedToDate / spentToDate. Null for income, transfers, and self-
        // funded expenses.
        public string? FundedByGoalId { get; set; }
    }

    public class TransactionCategory
    {
        public string? Category { get; set; }
        public double Amount { get; set; }
    }


    public class TransactionWithBudgetId
    {
        public string BudgetId { get; set; }
        // Id of the transaction as currently stored in the budget. This allows
        // updates where the client changes the transaction id to resolve
        // duplicates.
        public string? OldId { get; set; }
        public Transaction Transaction { get; set; }
    }

    public class SharedBudget
    {
        public string? OwnerUid { get; set; }
        public string? UserId { get; set; }
        public List<string> BudgetIds { get; set; } = new();
    }

    public class Merchant
    {
        public string? Name { get; set; }
        public int UsageCount { get; set; }
    }

    public class ReconcileRequest
    {
        public string BudgetTransactionId { get; set; }
        public string ImportedTransactionId { get; set; }
        public bool Match { get; set; }
        public bool Ignore { get; set; }
    }

    public class BatchReconcileRequest
    {
        public string BudgetId { get; set; }
        public List<ReconcileRequest> Reconciliations { get; set; }
    }

    public class MergeBudgetsRequest
    {
        public string TargetBudgetId { get; set; } = string.Empty;
        public string SourceBudgetId { get; set; } = string.Empty;
    }

    public class TemplateBudget
    {
        public List<BudgetCategory> Categories { get; set; } = new();
    }
}
