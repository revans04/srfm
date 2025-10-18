using System.Threading.Tasks;
using FamilyBudgetApi.Models;
using Microsoft.Extensions.Options;
using sib_api_v3_sdk.Api; // Brevo (Sendinblue) SDK
using BrevoModel = sib_api_v3_sdk.Model;
using sib_api_v3_sdk.Client;

namespace FamilyBudgetApi.Services
{
  public class BrevoService
  {
    private readonly BrevoSettings _brevoSettings;

    public BrevoService(IOptions<BrevoSettings> brevoSettings)
    {
       _brevoSettings = brevoSettings.Value ?? throw new ArgumentNullException(nameof(brevoSettings));
        if (string.IsNullOrEmpty(_brevoSettings.ApiKey))
            throw new InvalidOperationException("Brevo API key is not configured.");
    }

    public async Task SendInviteEmail(string toEmail, string familyName, string inviteLink)
    {
      var subject = $"You’re Invited to Join {familyName} on Steady Rise";
      var htmlContent = $@"
                    <h1>Join {familyName} on Steady Rise</h1>
                    <p>You’ve been invited to collaborate on budget management. Click below to accept:</p>
                    <a href='{inviteLink}'>Accept Invite</a>
                    <p>This link expires in 7 days.</p>";

      await SendEmail(toEmail, subject, htmlContent);
    }

    public async Task SendVerificationEmail(string toEmail, string verificationLink)
    {
      var subject = "Verify Your Steady Rise Email";
      var htmlContent = $@"
                    <h1>Verify your email</h1>
                    <p>Thanks for signing up for Steady Rise. Please confirm your email address by clicking the button below:</p>
                    <a href='{verificationLink}'>Verify Email</a>
                    <p>If you did not create an account, you can safely ignore this email.</p>";

      await SendEmail(toEmail, subject, htmlContent);
    }

    public async Task SendEmail(string email, string subject, string htmlContent)
    {
      Configuration.Default.ApiKey["api-key"] = _brevoSettings.ApiKey;
      var apiInstance = new TransactionalEmailsApi();

      var sendSmtpEmail = new BrevoModel.SendSmtpEmail(
        sender: new BrevoModel.SendSmtpEmailSender(_brevoSettings.SenderName, _brevoSettings.SenderEmail),
        to: new List<BrevoModel.SendSmtpEmailTo> { new BrevoModel.SendSmtpEmailTo(email) },
        subject: subject,
        htmlContent: htmlContent
      );

      try
      {
        await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
        Console.WriteLine($"Email '{subject}' sent to {email}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to send email '{subject}' to {email}: {ex.Message}");
        throw;
      }
    }
  }
}
