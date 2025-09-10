namespace Mma.Enums;

public enum NotificationTypes : int
{
    None = 0,
    Email,
    SMS,
    Push
}

public enum NotificationStatuses : int
{
    None = 0,
    Pending,
    Sent,
    Fail
}

public enum NotificationPeriorities : int
{
    None = 0,
    Low,
    Normal,
    Medium,
    High
}