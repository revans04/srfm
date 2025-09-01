using Microsoft.AspNetCore.Mvc;
using FamilyBudgetApi.Models;
using FamilyBudgetApi.Services;
using System;
using System.Threading.Tasks;

namespace FamilyBudgetApi.Controllers
{
    [ApiController]
    [Route("api/goals")]
    public class GoalController : ControllerBase
    {
        private readonly GoalService _goalService;

        public GoalController(GoalService goalService)
        {
            _goalService = goalService;
        }

        [HttpPost]
        [AuthorizeFirebase]
        public async Task<IActionResult> SaveGoal([FromBody] Goal goal)
        {
            try
            {
                await _goalService.SaveGoal(goal);
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SaveGoal: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}
