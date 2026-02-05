using Google.Cloud.Firestore;

namespace FamilyBudgetApi.Models
{
  public class PendingInvite
  {
    public string InviterUid { get; set; }

    public string InviterEmail { get; set; }

    public string InviteeEmail { get; set; }

    public string Token { get; set; } // Unique token for the invite link

    public Timestamp CreatedAt { get; set; }

    public Timestamp ExpiresAt { get; set; } // Expiration for security
  }


  public class InviteRequest
  {
    public string? InviterUid { get; set; }
    public string? InviterEmail { get; set; }
    public string? InviteeEmail { get; set; }
  }

  public class AcceptInviteRequest
  {
    public string Token { get; set; }
  }
}
