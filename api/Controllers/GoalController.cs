using Microsoft.AspNetCore.Mvc;
using FamilyBudgetApi.Models;
using FamilyBudgetApi.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FamilyBudgetApi.Controllers
{
    [ApiController]
    [Route("api/goals")]
    public class GoalController : ControllerBase
    {
        private readonly GoalService _goalService;
        private readonly ILogger<GoalController> _logger;

        public GoalController(GoalService goalService, ILogger<GoalController> logger)
        {
            _goalService = goalService;
            _logger = logger;
        }

        [HttpPost]
        [AuthorizeFirebase]
        public async Task<IActionResult> SaveGoal([FromBody] Goal goal)
        {
            _logger.LogInformation("Received request to save goal {GoalId}", goal?.Id);
            try
            {
                await _goalService.SaveGoal(goal);
                _logger.LogInformation("Goal {GoalId} saved successfully", goal?.Id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving goal {GoalId}", goal?.Id);
                return BadRequest(ex.Message);
            }
        }
    }
}
