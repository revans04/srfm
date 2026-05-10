using Microsoft.AspNetCore.Mvc;
using FamilyBudgetApi.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace FamilyBudgetApi.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private static readonly Regex MonthPattern = new(@"^\d{4}-\d{2}$", RegexOptions.Compiled);

        private readonly ReportsService _reportsService;

        public ReportsController(ReportsService reportsService)
        {
            _reportsService = reportsService;
        }

        [HttpGet("by-payee")]
        [AuthorizeFirebase]
        public async Task<IActionResult> GetSpendingByPayee(
            [FromQuery] string entityId,
            [FromQuery] string from,
            [FromQuery] string to,
            [FromQuery] string? excludeGroupIds = null,
            [FromQuery] string? excludeCategoryNames = null,
            [FromQuery] string? excludeMerchants = null)
        {
            try
            {
                var userId = HttpContext.Items["UserId"]?.ToString() ?? throw new Exception("User ID not found");

                if (string.IsNullOrWhiteSpace(entityId))
                    return BadRequest("entityId is required");
                if (!MonthPattern.IsMatch(from ?? string.Empty) || !MonthPattern.IsMatch(to ?? string.Empty))
                    return BadRequest("from and to must be YYYY-MM");
                if (string.CompareOrdinal(from, to) > 0)
                    return BadRequest("from must be <= to");

                var groupIds = (excludeGroupIds ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

                var catNames = (excludeCategoryNames ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

                var merchants = (excludeMerchants ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

                var rows = await _reportsService.GetSpendingByPayee(entityId, userId, from, to, groupIds, catNames, merchants);
                return Ok(rows);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetSpendingByPayee: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, ex.Message);
            }
        }
    }
}
