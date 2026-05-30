namespace Prm.Common.Constants;

public static class SkillConstants
{
    public const string ProficiencyBeginner = "Beginner";
    public const string ProficiencyIntermediate = "Intermediate";
    public const string ProficiencyAdvanced = "Advanced";

    public static readonly IReadOnlySet<string> ValidProficiencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ProficiencyBeginner,
        ProficiencyIntermediate,
        ProficiencyAdvanced,
    };

    public static readonly IReadOnlySet<string> ValidCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Backend",
        "Frontend",
        "DevOps",
        "QA",
        "Other",
    };
}
