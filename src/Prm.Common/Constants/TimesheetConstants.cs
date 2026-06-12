namespace Prm.Common.Constants;

public static class TimesheetConstants
{
    public const string StatusSubmitted = "SUBMITTED";
    public const string StatusMissed = "MISSED";

    public const string AccessAllowed = "ALLOWED";
    public const string AccessBlocked = "BLOCKED";

    public const string AllocationStatusActive = "ACTIVE";
    public const string AllocationStatusEnded = "ENDED";

    public const int HistoryWeeksCount = 8;

    public static readonly IReadOnlyList<string> StandardActivityTagNames =
    [
        "Backend API Development",
        "Microservices / Architecture",
        "Database Design & Queries",
        "WebSocket / Real-time Features",
        "Frontend Development",
        "Code Review / Mentoring",
        "Bug Fixing",
        "DevOps / Deployment",
        "Testing & QA",
        "Documentation",
        "Other (type manually)",
    ];
}
