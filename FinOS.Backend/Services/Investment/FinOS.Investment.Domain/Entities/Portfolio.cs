using FinOS.Common.Interfaces;

namespace FinOS.Investment.Domain.Entities;

public class Portfolio : IAuditableEntity, ISoftDeletable
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Currency { get; set; } = "INR";
    public bool IsDefault { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public List<Holding> Holdings { get; set; } = new();
}
