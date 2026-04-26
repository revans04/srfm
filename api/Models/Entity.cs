using Google.Cloud.Firestore;
using System.Collections.Generic;

namespace FamilyBudgetApi.Models
{
    // Define the EntityType enum in C#
    public enum EntityType
    {
        Family,
        Business,
        RentalProperty,
        Other
    }

    public class Entity
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Type { get; set; } // Store as string in Firestore

        // Helper property to work with EntityType enum in code
        public EntityType EntityType
        {
            get => Enum.TryParse<EntityType>(Type, true, out var result) ? result : throw new InvalidOperationException($"Invalid EntityType: {Type}");
            set => Type = value.ToString();
        }

        public Timestamp CreatedAt { get; set; }

        public Timestamp UpdatedAt { get; set; }

        public List<UserRef> Members { get; set; } = new();

        /// <summary>
        /// Optional per-entity budget template (categories + targets) used as
        /// the seed for new monthly budgets. Persisted as JSONB on
        /// `entities.template_budget` (added in the 2026-04-21 migration).
        /// </summary>
        public TemplateBudget? TemplateBudget { get; set; }

        /// <summary>
        /// Optional list of opaque tax-form IDs (e.g. "form_1040",
        /// "schedule_e", "ca_form_540") this entity files. Persisted as TEXT[]
        /// on `entities.tax_form_ids` with column default `'{}'` — null and
        /// empty list both round-trip to an empty array.
        /// </summary>
        public List<string>? TaxFormIds { get; set; }
    }
}
