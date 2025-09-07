using Microsoft.AspNetCore.Mvc;
using FamilyBudgetApi.Models;
using FamilyBudgetApi.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        public async Task<IActionResult> InsertGoal([FromBody] Goal goal)
        {
            _logger.LogInformation("Received request to insert goal {GoalId}", goal?.Id);
            try
            {
                await _goalService.InsertGoal(goal);
                _logger.LogInformation("Goal {GoalId} inserted successfully", goal?.Id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting goal {GoalId}", goal?.Id);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [AuthorizeFirebase]
        public async Task<IActionResult> UpdateGoal([FromBody] Goal goal)
        {
            _logger.LogInformation("Received request to update goal {GoalId}", goal?.Id);
            try
            {
                await _goalService.UpdateGoal(goal);
                _logger.LogInformation("Goal {GoalId} updated successfully", goal?.Id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating goal {GoalId}", goal?.Id);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [AuthorizeFirebase]
        public async Task<IActionResult> GetGoals([FromQuery] string entityId)
        {
            _logger.LogInformation("Received request to get goals for entity {EntityId}", entityId);
            try
            {
                var goals = await _goalService.GetGoals(entityId);
                return Ok(goals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching goals for entity {EntityId}", entityId);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{goalId}/details")]
        [AuthorizeFirebase]
        public async Task<IActionResult> GetGoalDetails(string goalId)
        {
            _logger.LogInformation("Received request to get goal details for {GoalId}", goalId);
            try
            {
                var details = await _goalService.GetGoalDetails(goalId);
                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching goal details for {GoalId}", goalId);
                return BadRequest(ex.Message);
            }
        }
    }
}
