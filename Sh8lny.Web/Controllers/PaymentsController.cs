using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sh8lny.Abstraction.Services;
using Sh8lny.Abstraction.Repositories;
using Sh8lny.Domain.Models;
using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.Payments;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sh8lny.Web.Controllers;

/// <summary>
/// Controller for payment operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentsController(IPaymentService paymentService, IUnitOfWork unitOfWork)
    {
        _paymentService = paymentService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Processes a payment from a Company to a Student for a completed job.
    /// </summary>
    /// <param name="dto">The payment processing details.</param>
    /// <returns>Payment receipt on success.</returns>
    [HttpPost("pay")]
    [Authorize(Roles = "Company")]
    public async Task<ActionResult<ServiceResponse<PaymentReceiptDto>>> ProcessPayment([FromBody] ProcessPaymentDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<PaymentReceiptDto>.Failure("Invalid or missing user token."));
        }

        var result = await _paymentService.ProcessPaymentAsync(userId.Value, dto);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets all payment transactions associated with the current user.
    /// </summary>
    [HttpGet("history")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<IEnumerable<object>>>> GetPaymentHistory()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<IEnumerable<object>>.Failure("Invalid or missing user token."));
        }

        var transactions = await _unitOfWork.Transactions.FindAsync(t => t.PayeeId == userId.Value || t.PayerId == userId.Value);
        
        var appIds = transactions.Select(t => t.ApplicationId).Distinct().ToList();
        var appsDict = new Dictionary<int, Application>();
        foreach (var appId in appIds)
        {
            var app = await _unitOfWork.Applications.GetByIdAsync(appId);
            if (app != null) appsDict[appId] = app;
        }

        var projectIds = appsDict.Values.Select(a => a.ProjectID).Distinct().ToList();
        var projectsDict = new Dictionary<int, Project>();
        foreach (var projectId in projectIds)
        {
            var proj = await _unitOfWork.Projects.GetByIdAsync(projectId);
            if (proj != null) projectsDict[projectId] = proj;
        }

        var resultList = new List<object>();
        foreach (var t in transactions.OrderByDescending(t => t.TransactionDate))
        {
            string projectName = "Unknown Project";
            if (appsDict.TryGetValue(t.ApplicationId, out var app) && projectsDict.TryGetValue(app.ProjectID, out var proj))
            {
                projectName = proj.ProjectName;
            }

            resultList.Add(new
            {
                id = t.Id,
                description = $"Payment for {projectName}",
                projectName = projectName,
                amount = t.Amount,
                paidAt = t.TransactionDate,
                createdAt = t.TransactionDate,
                paymentMethod = t.PaymentMethod,
                status = t.Status.ToString()
            });
        }

        return Ok(ServiceResponse<IEnumerable<object>>.Success(resultList, "Payment history retrieved successfully."));
    }

    /// <summary>
    /// Gets a specific transaction by ID.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<object>>> GetPaymentById(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ServiceResponse<object>.Failure("Invalid or missing user token."));
        }

        var t = await _unitOfWork.Transactions.GetByIdAsync(id);
        if (t is null)
        {
            return NotFound(ServiceResponse<object>.Failure("Transaction not found."));
        }

        if (t.PayeeId != userId.Value && t.PayerId != userId.Value)
        {
            return Forbid();
        }

        var app = await _unitOfWork.Applications.GetByIdAsync(t.ApplicationId);
        string projectName = "Unknown Project";
        if (app != null)
        {
            var proj = await _unitOfWork.Projects.GetByIdAsync(app.ProjectID);
            if (proj != null)
            {
                projectName = proj.ProjectName;
            }
        }

        var result = new
        {
            id = t.Id,
            description = $"Payment for {projectName}",
            projectName = projectName,
            amount = t.Amount,
            paidAt = t.TransactionDate,
            createdAt = t.TransactionDate,
            paymentMethod = t.PaymentMethod,
            status = t.Status.ToString()
        };

        return Ok(ServiceResponse<object>.Success(result, "Transaction retrieved successfully."));
    }

    /// <summary>
    /// Extracts the current user ID from JWT claims.
    /// </summary>
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }
        return userId;
    }
}
