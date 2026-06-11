namespace Prm.Common.Auth;

public static class PasswordChangeRules
{
    public static bool IsRequired(DateTime? passwordExpiryTime) => passwordExpiryTime.HasValue;
}
