using FinOS.Investment.Domain.Enums;

namespace FinOS.Investment.Application.DTOs;

public class PortfolioDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Currency { get; set; } = "INR";
    public bool IsDefault { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPct { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<HoldingDto> Holdings { get; set; } = new();
}

public class CreatePortfolioRequest
{
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Currency { get; set; } = "INR";
    public bool IsDefault { get; set; }
}

public class PortfolioListDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal TotalReturnPct { get; set; }
    public int HoldingCount { get; set; }
    public bool IsDefault { get; set; }
}
