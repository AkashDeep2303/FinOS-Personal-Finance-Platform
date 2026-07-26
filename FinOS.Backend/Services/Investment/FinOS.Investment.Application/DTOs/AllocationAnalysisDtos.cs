namespace FinOS.Investment.Application.DTOs;

public record AllocationAnalysisRequest(long PortfolioId, IReadOnlyList<TargetAllocationInput> Targets);
public record TargetAllocationInput(string AssetClass, decimal TargetPct);
public record AllocationAnalysisDto(
    long PortfolioId,
    decimal TotalCurrentValue,
    IReadOnlyList<AllocationDeviationDto> Allocations,
    bool RebalancingSuggested);
public record AllocationDeviationDto(
    string AssetClass,
    decimal CurrentValue,
    decimal ActualPct,
    decimal TargetPct,
    decimal DeviationPct,
    string Status);
