namespace FamilyBudgetApi.Models
{
    /// <summary>
    /// Entity-scoped category group taxonomy. Replaces the legacy per-category
    /// "group" text column and the per-budget `group_order` array. A group
    /// belongs to an entity, not a single monthly budget — renaming or
    /// reordering applies to all months automatically.
    /// </summary>
    public class BudgetGroup
    {
        public string Id { get; set; } = string.Empty;          // uuid
        public string EntityId { get; set; } = string.Empty;    // uuid
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool Archived { get; set; }
        // 'income' | 'expense' | 'savings'. Replaces string-equality
        // checks against the literal "Income"/"Savings" group names.
        public string Kind { get; set; } = "expense";
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public bool CollapsedDefault { get; set; }
    }

    public class GroupReorderRequest
    {
        public List<string> GroupIds { get; set; } = new();
    }

    public class CategoryReorderItem
    {
        public long Id { get; set; }
        public string GroupId { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class CategoryReorderRequest
    {
        public List<CategoryReorderItem> Categories { get; set; } = new();
    }
}
