using FinOS.Notification.Domain.Enums;

namespace FinOS.Notification.Domain.Entities;

/// <summary>
/// Defines a type/category of notification (e.g., BudgetAlert, LoginAlert, EMIReminder).
/// NotificationType is a reference/lookup entity with an integer primary key.
/// </summary>
public class NotificationType
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Logical grouping: Security, Budget, Loan, Investment, Goal, System.
    /// Stored as string in DB for readability; mapped via EF Core conversion.
    /// </summary>
    public NotificationCategory Category { get; set; }

    public bool IsEnabled { get; set; } = true;
}
