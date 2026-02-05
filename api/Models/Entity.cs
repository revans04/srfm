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

        public TemplateBudget? TemplateBudget { get; set; }

        public List<string>? TaxFormIds { get; set; }
    }
}
