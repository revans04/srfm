// FamilyBudgetApi/Models/Account.cs
using Google.Cloud.Firestore;
using System;

namespace FamilyBudgetApi.Models
{
    public class Account
    {
        public string Id { get; set; }

        public string? UserId { get; set; } // Optional, for personal accounts

        public string Name { get; set; }

        public string Type { get; set; } // "Bank", "CreditCard", "Investment", "Property", "Loan"

        public string Category { get; set; } // "Asset", "Liability"

        public string? AccountNumber { get; set; }

        public string Institution { get; set; }

        public Timestamp CreatedAt { get; set; }

        public Timestamp UpdatedAt { get; set; }

        public double? Balance { get; set; }

        public AccountDetails Details { get; set; }
    }

    public class AccountDetails
    {
        public double? InterestRate { get; set; }

        public double? AppraisedValue { get; set; }

        public string? MaturityDate { get; set; }

        public string? Address { get; set; }
    }

    public class ImportAccountEntry
    {
        public string AccountName { get; set; }

        public string Type { get; set; }

        public string? AccountNumber { get; set; }

        public string? Institution { get; set; }

        public Timestamp Date { get; set; }

        public double? Balance { get; set; }

        public double? InterestRate { get; set; }

        public double? AppraisedValue { get; set; }

        public string? Address { get; set; }
    }

}
