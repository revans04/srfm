//Controllers/AuthController.cs
using FirebaseAdmin.Auth;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using FamilyBudgetApi.Models;
using FamilyBudgetApi.Services;

namespace FamilyBudgetApi.Controllers
{

  [ApiController]
  [Route("api/auth")]
  public class AuthController : ControllerBase
  {
    private readonly UserService _userService;
    private readonly FamilyService _familyService;
    private readonly BrevoService _brevoService;
    private readonly string _baseUrl;

    public AuthController(
      UserService userService,
      FamilyService familyService,
      BrevoService brevoService,
      IConfiguration configuration)
    {
      _userService = userService;
      _familyService = familyService;
      _brevoService = brevoService;
      _baseUrl = configuration["BaseUrl"] ?? "http://family-budget.local:8080";
    }

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
      try
      {
        var payload = await GoogleJsonWebSignature.ValidateAsync(request.GoogleIdToken);
        if (payload == null)
        {
          return BadRequest(new { Error = "Invalid Google ID token" });
        }

        var additionalClaims = new Dictionary<string, object>
      {
        { "email", payload.Email },
        { "name", payload.Name ?? "" },
        { "given_name", payload.GivenName ?? "" },
        { "family_name", payload.FamilyName ?? "" },
        { "picture", payload.Picture ?? "" }
      };
        string customToken = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(payload.Subject, additionalClaims);

        UserRecord userRecord;
        try
        {
          userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(payload.Subject);

          await _userService.SaveUser(payload.Subject, payload.Email);

          // For existing users, ensure verification token exists if not verified
          if (!userRecord.EmailVerified)
          {
            var existingToken = await _userService.GetVerificationToken(payload.Subject);
            if (string.IsNullOrWhiteSpace(existingToken))
            {
              var verificationToken = await _userService.EnsureVerificationToken(payload.Subject);
              await SendVerificationEmail(payload.Email, verificationToken);
            }
          }
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
          var userRecordArgs = new UserRecordArgs
          {
            Uid = payload.Subject,
            Email = payload.Email,
            DisplayName = payload.Name ?? "",
            PhotoUrl = payload.Picture ?? "",
            EmailVerified = false
          };
          userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(userRecordArgs);

          await _userService.SaveUser(payload.Subject, payload.Email);
          var verificationToken = await _userService.EnsureVerificationToken(payload.Subject);
          await SendVerificationEmail(payload.Email, verificationToken);
        }

        return Ok(new { Token = customToken });
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Authentication error: {ex.Message}");
        return BadRequest(new { Error = $"Failed to authenticate: {ex.Message}" });
      }
    }

    [HttpPost("create-family")]
    [AuthorizeFirebase]
    public async Task<IActionResult> CreateFamily([FromBody] CreateFamilyRequest request)
    {
      var userId = HttpContext.Items["UserId"]?.ToString() ?? throw new Exception("User ID not found");

      if (string.IsNullOrEmpty(request.Name))
        return BadRequest(new { Error = "Family name is required" });

      var familyId = Guid.NewGuid().ToString();
      var family = new Family
      {
        Id = familyId,
        Name = request.Name,
        OwnerUid = userId,
        Members = new List<UserRef> { new UserRef { Uid = userId, Email = request.Email } }
      };
      await _familyService.CreateFamily(familyId, family);
      return Ok(new { FamilyId = familyId, Name = family.Name });
    }

    [AuthorizeFirebase]
    [HttpPost("ensure-profile")]
    public async Task<IActionResult> EnsureUserProfile()
    {
      try
      {
        var userId = HttpContext.Items["UserId"]?.ToString() ?? throw new Exception("User ID not found");
        var email = HttpContext.Items["Email"]?.ToString() ?? string.Empty;
        await _userService.SaveUser(userId, email);

        return Ok();
      }
      catch (Exception ex)
      {
        return Unauthorized($"Error: {ex.Message}");
      }
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail(string token)
    {
      try
      {
        var uid = await _userService.GetUserIdByVerificationToken(token);
        if (string.IsNullOrWhiteSpace(uid))
        {
          return BadRequest(new { Error = "Invalid or expired verification token" });
        }

        await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
        {
          Uid = uid,
          EmailVerified = true
        });

        await _userService.ClearVerificationToken(uid);

        return Ok(new { Message = "Email verified successfully" });
      }
      catch (Exception ex)
      {
        return BadRequest(new { Error = $"Failed to verify email: {ex.Message}" });
      }
    }

    [AuthorizeFirebase]
    [HttpPost("resend-verification-email")]
    public async Task<IActionResult> ResendVerificationEmail()
    {
      try
      {
        var userId = HttpContext.Items["UserId"]?.ToString() ?? throw new Exception("User ID not found");
        var userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(userId);
        if (userRecord.EmailVerified)
        {
          return BadRequest(new { Error = "Email is already verified" });
        }
        if (string.IsNullOrWhiteSpace(userRecord.Email))
        {
          return BadRequest(new { Error = "User does not have an email address on record" });
        }

        await _userService.SaveUser(userId, userRecord.Email);
        var verificationToken = await _userService.EnsureVerificationToken(userId);

        await SendVerificationEmail(userRecord.Email, verificationToken);
        return Ok(new { Message = "Verification email resent successfully" });
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error resending verification email: {ex.Message}");
        return BadRequest(new { Error = $"Failed to resend verification email: {ex.Message}" });
      }
    }

    private Task SendVerificationEmail(string email, string token)
    {
      var baseUrl = string.IsNullOrWhiteSpace(_baseUrl) ? "http://family-budget.local:8080" : _baseUrl;
      var verificationLink = $"{baseUrl.TrimEnd('/')}/verify-email?token={token}";
      return _brevoService.SendVerificationEmail(email, verificationLink);
    }
  }

  public class GoogleLoginRequest
  {
    public string? GoogleIdToken { get; set; }
  }
}
