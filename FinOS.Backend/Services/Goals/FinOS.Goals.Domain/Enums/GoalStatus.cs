namespace FinOS.Goals.Domain.Enums;

public enum GoalStatus
{
    Active = 0,
    InProgress = Active,
    Paused = 1,
    Completed = 2,
    Cancelled = 3
}
