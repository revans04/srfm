// FamilyBudgetApi/Controllers/SyncController.cs
using Microsoft.AspNetCore.Mvc;
using System;
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
        /// This will upsert all budgets and transactions.
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
            if (!DateTime.TryParse(since, out var parsedSince))
            {
                return BadRequest("Query parameter 'since' must be a valid ISO-8601 timestamp.");
            }

            await _syncService.IncrementalSyncFirestoreToSupabaseAsync(parsedSince);
            return Ok(new { message = $"Incremental sync completed since {parsedSince:o}" });
        }
    }
}