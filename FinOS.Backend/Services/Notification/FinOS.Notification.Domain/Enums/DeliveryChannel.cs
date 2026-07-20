namespace FinOS.Notification.Domain.Enums;

/// <summary>
/// The channel through which a notification is delivered.
/// </summary>
public enum DeliveryChannel
{
    InApp = 0,
    Email = 1,
    Push = 2,
    SMS = 3
}
