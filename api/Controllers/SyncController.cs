// FamilyBudgetApi/Controllers/SyncController.cs
using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.Threading.Tasks;
using FamilyBudgetApi.Services;
using Microsoft.Extensions.Logging;

namespace FamilyBudgetApi.Controllers
{
    /// <summary>
    /// API controller that exposes endpoints to trigger synchronization between Firestore and Supabase.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        private readonly SyncService _syncService;
        private readonly ILogger<SyncController> _logger;

        public SyncController(SyncService syncService, ILogger<SyncController> logger)
        {
            _syncService = syncService;
            _logger = logger;
        }

        /// <summary>
        /// Perform a full synchronization from Firestore to Supabase.
        /// This will upsert families, users, accounts, snapshots,
        /// budgets, transactions, and imported transactions.
        /// </summary>
        [HttpPost("full")]
        public async Task<IActionResult> FullSync()
        {
            await _syncService.FullSyncFirestoreToSupabaseAsync();
            return Ok(new { message = "Full sync completed" });
        }

        /// <summary>
        /// Perform an incremental synchronization from Firestore to Supabase.
        /// Requires a 'since' query parameter representing an ISO-8601 timestamp.
        /// </summary>
        [HttpPost("incremental")]
        public async Task<IActionResult> IncrementalSync([FromQuery] string since)
        {
            if (string.IsNullOrEmpty(since))
            {
                return BadRequest("Query parameter 'since' is required.");
            }
            // Prefer strict ISO-8601 parsing; fall back to Assume/Adjust to UTC
            DateTime sinceUtc;
            if (DateTimeOffset.TryParse(since, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
            {
                sinceUtc = dto.UtcDateTime;
            }
            else if (DateTime.TryParse(since, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            {
                sinceUtc = dt.ToUniversalTime();
            }
            else
            {
                return BadRequest("Query parameter 'since' must be a valid ISO-8601 timestamp.");
            }
            await _syncService.IncrementalSyncFirestoreToSupabaseAsync(sinceUtc);
            return Ok(new { message = $"Incremental sync completed since {sinceUtc:o}" });
        }

        /// <summary>
        /// Sync only user records from Firestore to Supabase.
        /// </summary>
        [HttpPost("users")]
        public async Task<IActionResult> SyncUsers()
        {
            await _syncService.SyncUsersAsync(null);
            return Ok(new { message = "User sync completed" });
        }

        /// <summary>
        /// Sync only family records from Firestore to Supabase.
        /// </summary>
        [HttpPost("families")]
        public async Task<IActionResult> SyncFamilies()
        {
            await _syncService.SyncFamiliesAsync(null);
            return Ok(new { message = "Family sync completed" });
        }

        /// <summary>
        /// Sync only account records from Firestore to Supabase.
        /// </summary>
        [HttpPost("accounts")]
        public async Task<IActionResult> SyncAccounts()
        {
            await _syncService.SyncAccountsAsync(null);
            return Ok(new { message = "Account sync completed" });
        }

        /// <summary>
        /// Sync only snapshot records from Firestore to Supabase.
        /// </summary>
        [HttpPost("snapshots")]
        public async Task<IActionResult> SyncSnapshots()
        {
            await _syncService.SyncSnapshotsAsync(null);
            return Ok(new { message = "Snapshot sync completed" });
        }
    }
}
