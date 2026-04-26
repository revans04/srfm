using FamilyBudgetApi.Models;
using FamilyBudgetApi.Services;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FamilyBudgetApi.Controllers
{
    /// <summary>
    /// Atomic onboarding endpoint. Currently exposes a single POST that
    /// performs first-run setup (family + entity + first budget + accounts)
    /// in one Postgres transaction. Lives in its own controller (rather than
    /// hanging off FamilyController) so the seed-vs-CRUD boundary stays
    /// obvious.
    /// </summary>
    [ApiController]
    [Route("api/onboarding")]
    public class OnboardingController : ControllerBase
    {
        private readonly OnboardingService _onboardingService;
        private readonly ILogger<OnboardingController> _logger;

        public OnboardingController(OnboardingService onboardingService, ILogger<OnboardingController> logger)
        {
            _onboardingService = onboardingService;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/onboarding/seed
        /// Returns 200 when a fresh family is created.
        /// Returns 409 (with the existing FamilyId / EntityId / BudgetId in
        /// the response body) when the user already belongs to a family —
        /// frontend treats this as success and navigates to /budget.
        /// Returns 400 on validation failure (bad EntityType, blank names).
        /// Returns 500 on transaction failure.
        /// </summary>
        [HttpPost("seed")]
        [AuthorizeFirebase]
        public async Task<IActionResult> Seed([FromBody] OnboardingSeedRequest request)
        {
            if (request == null) return BadRequest("Request body is required");

            var userId = HttpContext.Items["UserId"]?.ToString();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in request context");

            string userEmail = string.Empty;
            try
            {
                userEmail = (await FirebaseAuth.DefaultInstance.GetUserAsync(userId))?.Email ?? string.Empty;
            }
            catch (Exception ex)
            {
                // Email is informational only (logged with the seed) — don't
                // fail the seed because Firebase metadata fetch hiccuped.
                _logger.LogWarning(ex, "Could not fetch Firebase email for user {Uid}; continuing with seed", userId);
            }

            try
            {
                var response = await _onboardingService.SeedAsync(userId, userEmail, request);
                return response.Created ? Ok(response) : Conflict(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Onboarding seed validation failed for user {Uid}", userId);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onboarding seed failed for user {Uid}", userId);
                return StatusCode(500, "Failed to complete onboarding seed");
            }
        }
    }
}
