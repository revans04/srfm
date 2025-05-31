using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using FamilyBudgetApi.Services;
using FamilyBudgetApi.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FamilyBudgetApi.Controllers
{
    [ApiController]
    [Route("api/accounts/{accountNumber}")]
    public class StatementController : ControllerBase
    {
        private readonly StatementService _statementService;

        public StatementController(StatementService statementService)
        {
            _statementService = statementService;
        }

        [HttpGet("statements")]
        [AuthorizeFirebase]
        public async Task<IActionResult> GetStatements(string accountNumber)
        {
            var statements = await _statementService.GetStatements(accountNumber);
            return Ok(statements);
        }

        [HttpPut("statements/{statementId}")]
        [AuthorizeFirebase]
        public async Task<IActionResult> SaveStatement(string accountNumber, string statementId, [FromBody] SaveStatementRequest request)
        {
            if (request.Statement.Id != statementId)
                return BadRequest("Statement ID mismatch");

            var userId = HttpContext.Items["UserId"]?.ToString() ?? string.Empty;
            var userEmail = (await FirebaseAuth.DefaultInstance.GetUserAsync(userId)).Email ?? string.Empty;

            var txRefs = request.Transactions?.Select(t => (t.BudgetId, t.TransactionId)).ToList() ?? new List<(string, string)>();

            await _statementService.SaveStatement(accountNumber, request.Statement, txRefs, userId, userEmail);
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