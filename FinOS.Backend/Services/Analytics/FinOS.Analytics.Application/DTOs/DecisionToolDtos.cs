namespace FinOS.Analytics.Application.DTOs;

public record CalculatorRequest(
    string Calculator,
    decimal Principal,
    decimal MonthlyAmount,
    decimal AnnualRate,
    int Months,
    decimal TargetAmount = 0,
    decimal CurrentAmount = 0,
    decimal EndingAmount = 0);

public record CalculatorResultDto(
    string Calculator,
    decimal PrimaryResult,
    decimal SecondaryResult,
    decimal TotalContribution,
    string ResultUnit,
    string Formula,
    IReadOnlyDictionary<string, decimal> Assumptions);

public record XirrCashFlowDto(DateTime Date, decimal Amount);
public record XirrRequest(IReadOnlyList<XirrCashFlowDto> CashFlows);

public record ScenarioRequest(
    string ScenarioType,
    decimal CurrentNetWorth,
    decimal MonthlyIncome,
    decimal MonthlyExpenses,
    decimal MonthlyDebtPayments,
    decimal LiquidAssets,
    decimal OneTimeCost,
    decimal MonthlyIncomeChange,
    decimal MonthlyExpenseChange,
    decimal NewMonthlyDebtPayment,
    int HorizonMonths);

public record ScenarioResultDto(
    string ScenarioType,
    decimal CurrentNetWorth,
    decimal ScenarioNetWorth,
    decimal CurrentMonthlySurplus,
    decimal ScenarioMonthlySurplus,
    decimal CurrentSavingsRatePct,
    decimal ScenarioSavingsRatePct,
    decimal CurrentDtiPct,
    decimal ScenarioDtiPct,
    decimal CurrentEmergencyFundMonths,
    decimal ScenarioEmergencyFundMonths,
    string Verdict,
    IReadOnlyList<string> Reasons,
    IReadOnlyDictionary<string, decimal> Assumptions);

public record SaveScenarioRequest(string Name, ScenarioRequest Scenario);
public record SavedScenarioDto(long Id, string Name, string ScenarioType, string Verdict,
    ScenarioRequest Input, ScenarioResultDto Result, DateTime CreatedAt);
