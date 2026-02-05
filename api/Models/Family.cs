using Google.Cloud.Firestore;
using System.Collections.Generic;

namespace FamilyBudgetApi.Models
{
    public class Family
    {
        public required string Id { get; set; }

        public required string Name { get; set; }

        public required string OwnerUid { get; set; }

        public List<UserRef> Members { get; set; } = new();

        public List<string> MemberUids { get; set; } = new();

        public List<Account> Accounts { get; set; } = new();

        public List<Snapshot> Snapshots { get; set; } = new();

        public List<Entity> Entities { get; set; } = new();

        public Timestamp CreatedAt { get; set; }

        public Timestamp UpdatedAt { get; set; }
    }

    public class CreateFamilyRequest
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
    }

}
