
namespace FamilyBudgetApi.Models
{

    public class UserRef
    {
        public string? Uid { get; set; }

        public string? Email { get; set; }
        
        public string? Role { get; set; }
    }

    public class UserData
    {
        public string? Uid { get; set; }
        public string? Email { get; set; }
    }

    public class SharedAccess
    {
        public bool CanEdit { get; set; }
    }
}
