namespace Prm.Common.Constants;

public static class ManagerConstants
{
    public const string HealthAtRisk = "AT_RISK";
    public const string HealthOnTrack = "ON_TRACK";
    public const string HealthAttention = "ATTENTION";

    public const string AvailabilityFull = "FULL";
    public const string AvailabilityOnBench = "fully on bench";

    public const string RiskFlagFail = "FAIL";
    public const string RiskFlagPass = "PASS";

    public const string ResourcesCorrectlyAllocated = "Resources are correctly allocated";
    public const string ProjectResourcesNeedAttention = "Project resource allocation needs attention";

    public const int ActivityTagsLookbackWeeks = 4;
    public const int PastAllocationsDisplayCount = 5;
    public const int DefaultMaxWeeklyHours = 40;

    public const int RiskFlagCountForProjectUnderRisk = 2;
    public const int RiskFlagCountForProjectNeedAttention = 1;
}
