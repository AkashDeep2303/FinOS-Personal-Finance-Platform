namespace FinOS.EventBus.Events;

public class TransactionCreatedEvent : IntegrationEvent
{
    public long UserId { get; set; }
    public long TransactionId { get; set; }
    public long AccountId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Currency { get; set; } = "INR";
}

public class BudgetThresholdExceededEvent : IntegrationEvent
{
    public long UserId { get; set; }
    public long BudgetCategoryId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal ThresholdPct { get; set; }
}

public class EMIReminderEvent : IntegrationEvent
{
    public long UserId { get; set; }
    public long LoanId { get; set; }
    public string LenderName { get; set; } = string.Empty;
    public decimal EmiAmount { get; set; }
    public DateTime DueDate { get; set; }
}

public class SIPInstallmentDueEvent : IntegrationEvent
{
    public long UserId { get; set; }
    public long SIPId { get; set; }
    public string FundName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExecutionDate { get; set; }
}

public class GoalMilestoneReachedEvent : IntegrationEvent
{
    public long UserId { get; set; }
    public long GoalId { get; set; }
    public string GoalName { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal ProgressPct { get; set; }
}

public class NetWorthCalculatedEvent : IntegrationEvent
{
    public long UserId { get; set; }
    public decimal NetWorth { get; set; }
    public decimal ChangeFromPrevious { get; set; }
    public DateTime SnapshotDate { get; set; }
}

public class UserRegisteredEvent : IntegrationEvent
{
    public long UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
