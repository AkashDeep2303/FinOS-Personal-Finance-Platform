using FinOS.Common.Models;
using FinOS.Identity.Application.DTOs;
using FinOS.Identity.Domain.Interfaces;
using MediatR;

namespace FinOS.Identity.Application.Queries;

public class GetAuditLogsQuery : IRequest<PagedResult<AuditLogDto>>
{
    public long? UserId { get; set; }
    public string? ActionType { get; set; }
    public string? EntityType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public PagedQuery Pagination { get; set; } = new();
}

public class AuditLogDto
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly IAuditLogRepository _auditLogRepository;

    public GetAuditLogsQueryHandler(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery query, CancellationToken ct)
    {
        var result = await _auditLogRepository.GetFilteredAsync(
            query.UserId,
            query.ActionType,
            query.EntityType,
            query.FromDate,
            query.ToDate,
            query.Pagination.SearchTerm,
            query.Pagination,
            ct);

        return new PagedResult<AuditLogDto>
        {
            Items = result.Items.Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                ActionType = a.ActionType,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                CreatedAt = a.CreatedAt
            }).ToList(),
            TotalCount = result.TotalCount,
            Page = result.PageNumber,
            PageSize = result.PageSize
        };
    }
}
