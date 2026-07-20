namespace FinOS.Notification.Domain.Enums;

/// <summary>
/// The current delivery status of a notification.
/// </summary>
public enum DeliveryStatus
{
    Pending = 0,
    Sent = 1,
    Delivered = 2,
    Failed = 3
}
