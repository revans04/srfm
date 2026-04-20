using FamilyBudgetApi.Models;
using FamilyBudgetApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FamilyBudgetApi.Controllers
{
    [ApiController]
    public class GroupController : ControllerBase
    {
        private readonly GroupService _groupService;
        private readonly ILogger<GroupController> _logger;

        public GroupController(GroupService groupService, ILogger<GroupController> logger)
        {
            _groupService = groupService;
            _logger = logger;
        }

        [HttpGet("api/entities/{entityId}/groups")]
        [AuthorizeFirebase]
        public async Task<IActionResult> GetGroups(string entityId)
        {
            try
            {
                var groups = await _groupService.GetGroups(entityId);
                return Ok(groups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch groups for entity {EntityId}", entityId);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("api/entities/{entityId}/groups")]
        [AuthorizeFirebase]
        public async Task<IActionResult> CreateGroup(string entityId, [FromBody] BudgetGroup payload)
        {
            try
            {
                var created = await _groupService.CreateGroup(entityId, payload);
                return Ok(created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create group for entity {EntityId}", entityId);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("api/entities/{entityId}/groups/{groupId}")]
        [AuthorizeFirebase]
        public async Task<IActionResult> UpdateGroup(string entityId, string groupId, [FromBody] BudgetGroup payload)
        {
            try
            {
                var updated = await _groupService.UpdateGroup(entityId, groupId, payload);
                if (updated == null) return NotFound();
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update group {GroupId}", groupId);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("api/entities/{entityId}/groups/order")]
        [AuthorizeFirebase]
        public async Task<IActionResult> ReorderGroups(string entityId, [FromBody] GroupReorderRequest payload)
        {
            try
            {
                await _groupService.ReorderGroups(entityId, payload?.GroupIds ?? new());
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reorder groups for entity {EntityId}", entityId);
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("api/entities/{entityId}/groups/{groupId}")]
        [AuthorizeFirebase]
        public async Task<IActionResult> DeleteGroup(string entityId, string groupId)
        {
            try
            {
                var deleted = await _groupService.DeleteGroup(entityId, groupId);
                if (!deleted) return NotFound();
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                // Group still has categories — surface as 409 Conflict.
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete group {GroupId}", groupId);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("api/budget/{budgetId}/categories/order")]
        [AuthorizeFirebase]
        public async Task<IActionResult> ReorderCategories(string budgetId, [FromBody] CategoryReorderRequest payload)
        {
            try
            {
                await _groupService.ReorderCategories(budgetId, payload);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reorder categories for budget {BudgetId}", budgetId);
                return BadRequest(ex.Message);
            }
        }
    }
}
