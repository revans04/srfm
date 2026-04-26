using System.Collections.Generic;

namespace FamilyBudgetApi.Models
{
    /// <summary>
    /// Body of POST /api/onboarding/seed. Captures the minimum a brand-new
    /// user needs to land on a populated Budget page: a family, an entity,
    /// optionally a starter budget for the current month, and optionally a
    /// few accounts. The endpoint executes everything in a single Postgres
    /// transaction — see <c>OnboardingService.SeedAsync</c>.
    /// </summary>
    public class OnboardingSeedRequest
    {
        public string FamilyName { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;

        /// <summary>
        /// Lowercase / titlecase string matching <see cref="EntityType"/>
        /// (e.g. "Family", "Business", "RentalProperty"). Validated by the
        /// controller; service throws on invalid value.
        /// </summary>
        public string EntityType { get; set; } = "Family";

        /// <summary>
        /// When true (default), seed the first budget with the categories
        /// from <c>DEFAULT_BUDGET_TEMPLATES[entityType]</c> on the frontend.
        /// The frontend resolves the template and sends the categories
        /// inline via <see cref="TemplateCategories"/>.
        /// When false, the entity is created with no template categories
        /// and the budget is created empty (matching today's "Create Default
        /// Budget" minimal fallback).
        /// </summary>
        public bool UseTemplate { get; set; } = true;

        /// <summary>
        /// YYYY-MM month to seed. Empty/null → caller intends to skip budget
        /// creation entirely (entity is still created and an Income group
        /// is still seeded so the next manual budget save is fast).
        /// </summary>
        public string? Month { get; set; }

        /// <summary>
        /// Optional category seed for the first budget. Each item carries
        /// `name`, `groupName`, `target`, `isFund`. Resolved against
        /// `budget_groups` via <c>BudgetService.EnsureGroupAsync</c>.
        /// </summary>
        public List<OnboardingSeedCategory>? TemplateCategories { get; set; }

        public List<string>? TaxFormIds { get; set; }
        public List<OnboardingSeedAccount>? Accounts { get; set; }
    }

    public class OnboardingSeedCategory
    {
        public string Name { get; set; } = string.Empty;
        public string? GroupName { get; set; }
        public double Target { get; set; }
        public bool IsFund { get; set; }
    }

    public class OnboardingSeedAccount
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>One of the <see cref="AccountType"/> string values: Bank, CreditCard, Investment, Property, Loan.</summary>
        public string Type { get; set; } = "Bank";

        /// <summary>"Asset" or "Liability". Caller can omit; service infers from Type.</summary>
        public string? Category { get; set; }

        public string? Institution { get; set; }
        public string? AccountNumber { get; set; }
        public decimal? Balance { get; set; }
    }

    public class OnboardingSeedResponse
    {
        public string FamilyId { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;

        /// <summary>Null when no budget was seeded (e.g. caller passed empty Month).</summary>
        public string? BudgetId { get; set; }

        public List<string> AccountIds { get; set; } = new();

        /// <summary>
        /// True when the request was served on a fresh user with no prior
        /// family. False when the user already had a family (controller
        /// returns 409 with the existing IDs in that case — present here so
        /// the frontend can disambiguate "I just created you" vs. "you were
        /// already onboarded").
        /// </summary>
        public bool Created { get; set; }
    }
}
