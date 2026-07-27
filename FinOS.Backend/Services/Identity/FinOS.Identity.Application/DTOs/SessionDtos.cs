namespace FinOS.Identity.Application.DTOs;

public sealed record SessionDto(
    long Id,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool IsCurrent);
