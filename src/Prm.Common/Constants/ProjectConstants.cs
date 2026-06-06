namespace Prm.Common.Constants;

public static class ProjectConstants
{
    public const string StatusPlanned = "PLANNED";
    public const string StatusActive = "ACTIVE";
    public const string StatusOnHold = "ON_HOLD";
    public const string StatusCompleted = "COMPLETED";

    public static readonly IReadOnlySet<string> ValidStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        StatusPlanned,
        StatusActive,
        StatusOnHold,
        StatusCompleted
    };
}
