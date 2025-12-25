using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Models;
using IMS.Api.Application.Queries.Audit;
using IMS.Api.Domain.Enums;
using IMS.Api.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Api.Presentation.Controllers;

/// <summary>
/// Controller for audit trail and compliance operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AuditController : BaseController
{
    public AuditController(IMediator mediator) : base(mediator)
    {
    }

    /// <summary>
    /// Gets audit logs with filtering and pagination
    /// </summary>
    /// <param name="action">Filter by action type</param>
    /// <param name="entityType">Filter by entity type</param>
    /// <param name="entityId">Filter by entity ID</param>
    /// <param name="actorId">Filter by actor (user) ID</param>
    /// <param name="warehouseId">Filter by warehouse ID</param>
    /// <param name="variantId">Filter by variant ID</param>
    /// <param name="startDate">Filter by start date</param>
    /// <param name="endDate">Filter by end date</param>
    /// <param name="searchTerm">Search term for description or reason</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortAscending">Sort direction</param>
    /// <returns>Paginated list of audit logs</returns>
    [HttpGet]
    [AuthorizePermission("view_audit_logs")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAuditLogs(
        [FromQuery] AuditAction? action = null,
        [FromQuery] string entityType = null,
        [FromQuery] string entityId = null,
        [FromQuery] Guid? actorId = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? variantId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string sortBy = "TimestampUtc",
        [FromQuery] bool sortAscending = false)
    {
        var query = new GetAuditLogsQuery
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            ActorId = actorId,
            WarehouseId = warehouseId,
            VariantId = variantId,
            StartDate = startDate,
            EndDate = endDate,
            SearchTerm = searchTerm,
            PageNumber = pageNumber,
            PageSize = Math.Min(pageSize, 100), // Limit page size
            SortBy = sortBy,
            SortAscending = sortAscending
        };

        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets audit history for a specific entity
    /// </summary>
    /// <param name="entityType">The entity type</param>
    /// <param name="entityId">The entity ID</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of audit logs for the entity</returns>
    [HttpGet("entity/{entityType}/{entityId}")]
    [AuthorizePermission("view_audit_logs")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetEntityAuditHistory(
        string entityType,
        string entityId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(entityId))
        {
            return BadRequest("EntityType and EntityId are required");
        }

        var query = new GetEntityAuditHistoryQuery(entityType, entityId)
        {
            PageNumber = pageNumber,
            PageSize = Math.Min(pageSize, 100)
        };

        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets audit summary information
    /// </summary>
    /// <param name="startDate">Start date for the summary period</param>
    /// <param name="endDate">End date for the summary period</param>
    /// <returns>Audit summary information</returns>
    [HttpGet("summary")]
    [AuthorizePermission("view_audit_logs")]
    [ProducesResponseType(typeof(AuditSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuditSummaryDto>> GetAuditSummary(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = new GetAuditSummaryQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Generates a compliance report for the specified period
    /// </summary>
    /// <param name="startDate">Start date for the report period</param>
    /// <param name="endDate">End date for the report period</param>
    /// <param name="includeViolationAnalysis">Whether to include violation analysis</param>
    /// <returns>Compliance report</returns>
    [HttpGet("compliance-report")]
    [AuthorizePermission("view_audit_logs")]
    [ProducesResponseType(typeof(ComplianceReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ComplianceReportDto>> GetComplianceReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] bool includeViolationAnalysis = true)
    {
        if (startDate >= endDate)
        {
            return BadRequest("Start date must be before end date");
        }

        if ((endDate - startDate).TotalDays > 365)
        {
            return BadRequest("Report period cannot exceed 365 days");
        }

        var query = new GetComplianceReportQuery(startDate, endDate)
        {
            IncludeViolationAnalysis = includeViolationAnalysis
        };

        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of audit logs for the user</returns>
    [HttpGet("user/{userId}")]
    [AuthorizePermission("view_audit_logs")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetUserAuditHistory(
        Guid userId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = new GetAuditLogsQuery
        {
            ActorId = userId,
            PageNumber = pageNumber,
            PageSize = Math.Min(pageSize, 100),
            SortBy = "TimestampUtc",
            SortAscending = false
        };

        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets audit logs for a specific warehouse
    /// </summary>
    /// <param name="warehouseId">The warehouse ID</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of audit logs for the warehouse</returns>
    [HttpGet("warehouse/{warehouseId}")]
    [AuthorizePermission("view_audit_logs")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetWarehouseAuditHistory(
        Guid warehouseId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = new GetAuditLogsQuery
        {
            WarehouseId = warehouseId,
            PageNumber = pageNumber,
            PageSize = Math.Min(pageSize, 100),
            SortBy = "TimestampUtc",
            SortAscending = false
        };

        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets audit logs for a specific variant
    /// </summary>
    /// <param name="variantId">The variant ID</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of audit logs for the variant</returns>
    [HttpGet("variant/{variantId}")]
    [AuthorizePermission("view_audit_logs")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetVariantAuditHistory(
        Guid variantId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = new GetAuditLogsQuery
        {
            VariantId = variantId,
            PageNumber = pageNumber,
            PageSize = Math.Min(pageSize, 100),
            SortBy = "TimestampUtc",
            SortAscending = false
        };

        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Exports audit logs to CSV format
    /// </summary>
    /// <param name="action">Filter by action type</param>
    /// <param name="entityType">Filter by entity type</param>
    /// <param name="startDate">Filter by start date</param>
    /// <param name="endDate">Filter by end date</param>
    /// <returns>CSV file with audit logs</returns>
    [HttpGet("export")]
    [AuthorizePermission("view_audit_logs")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] AuditAction? action = null,
        [FromQuery] string entityType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        // Limit export to reasonable time periods
        if (startDate.HasValue && endDate.HasValue && (endDate.Value - startDate.Value).TotalDays > 90)
        {
            return BadRequest("Export period cannot exceed 90 days");
        }

        var query = new GetAuditLogsQuery
        {
            Action = action,
            EntityType = entityType,
            StartDate = startDate,
            EndDate = endDate,
            PageNumber = 1,
            PageSize = 10000, // Large page size for export
            SortBy = "TimestampUtc",
            SortAscending = false
        };

        var result = await Mediator.Send(query);

        // Generate CSV content
        var csv = GenerateCsv(result.Items);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

        var fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv", fileName);
    }

    private string GenerateCsv(IEnumerable<AuditLogDto> auditLogs)
    {
        var csv = new System.Text.StringBuilder();
        
        // Header
        csv.AppendLine("Timestamp,Action,EntityType,EntityId,ActorId,Description,IpAddress,Reason");

        // Data
        foreach (var log in auditLogs)
        {
            csv.AppendLine($"{log.TimestampUtc:yyyy-MM-dd HH:mm:ss}," +
                          $"{EscapeCsvField(log.Action)}," +
                          $"{EscapeCsvField(log.EntityType)}," +
                          $"{EscapeCsvField(log.EntityId)}," +
                          $"{log.ActorId}," +
                          $"{EscapeCsvField(log.Description)}," +
                          $"{EscapeCsvField(log.IpAddress)}," +
                          $"{EscapeCsvField(log.Reason)}");
        }

        return csv.ToString();
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }

        return field;
    }
}