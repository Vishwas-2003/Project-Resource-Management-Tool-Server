namespace Prm.Common.Constants;

public static class MilestoneConstants
{
    public const string StatusNotStarted = "NOT_STARTED";
    public const string StatusInProgress = "IN_PROGRESS";
    public const string StatusDone = "DONE";

    public static readonly IReadOnlySet<string> ValidStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        StatusNotStarted,
        StatusInProgress,
        StatusDone,
    };
}
