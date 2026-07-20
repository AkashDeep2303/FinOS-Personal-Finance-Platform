using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Identity.Domain.Entities;

namespace FinOS.Identity.Domain.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    new Task<AuditLog> AddAsync(AuditLog auditLog, CancellationToken ct = default);
    Task<PagedResult<AuditLog>> GetFilteredAsync(
        long? userId,
        string? actionType,
        string? entityType,
        DateTime? fromDate,
        DateTime? toDate,
        string? searchTerm,
        PagedQuery pagination,
        CancellationToken ct = default);
}
