using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using FamilyBudgetApi.Services;
using FamilyBudgetApi.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FamilyBudgetApi.Controllers
{
    [ApiController]
    [Route("api/families/{familyId}/accounts/{accountNumber}")]
    public class StatementController : ControllerBase
    {
        private readonly StatementService _statementService;
        private readonly AccountService _accountService;

        public StatementController(StatementService statementService, AccountService accountService)
        {
            _statementService = statementService;
            _accountService = accountService;
        }

        [HttpGet("statements")]
        [AuthorizeFirebase]
        public async Task<IActionResult> GetStatements(string familyId, string accountNumber)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            if (!await _accountService.IsFamilyMember(familyId, userId))
                return Unauthorized("Not a member of this family");

            var statements = await _statementService.GetStatements(familyId, accountNumber);
            return Ok(statements);
        }

        [HttpPut("statements/{statementId}")]
        [AuthorizeFirebase]
         public async Task<IActionResult> SaveStatement(string familyId, string accountNumber, string statementId, [FromBody] SaveStatementRequest request)
        {
            if (request.Statement.Id != statementId)
                return BadRequest("Statement ID mismatch");

            var userId = HttpContext.Items["UserId"]?.ToString() ?? string.Empty;
            if (!await _accountService.IsFamilyMember(familyId, userId))
                return Unauthorized("Not a member of this family");

            var userEmail = (await FirebaseAuth.DefaultInstance.GetUserAsync(userId)).Email ?? string.Empty;

            var txRefs = request.Transactions?.Select(t => (t.BudgetId, t.TransactionId)).ToList() ?? new List<(string, string)>();

            await _statementService.SaveStatement(familyId, accountNumber, request.Statement, txRefs, userId, userEmail);
            return Ok();
        }
    }

    public class SaveStatementRequest
    {
        public required Statement Statement { get; set; }
        public List<TransactionRef>? Transactions { get; set; }
    }

    public class TransactionRef
    {
        public required string BudgetId { get; set; }
        public required string TransactionId { get; set; }
    }
}