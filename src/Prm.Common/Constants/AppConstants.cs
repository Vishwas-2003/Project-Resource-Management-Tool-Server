namespace Prm.Common.Constants;

public static class AppConstants
{
    public static class Configuration
    {
        public const string DefaultConnection = "DefaultConnection";
        public const string JwtSection = "Jwt";
        public const string BootstrapAdminSection = "BootstrapAdmin";
    }

    public static class ErrorCodes
    {
        public const string SessionExpired = "SESSION_EXPIRED";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string BadRequest = "BAD_REQUEST";
        public const string NotFound = "NOT_FOUND";
        public const string Conflict = "CONFLICT";
        public const string InternalError = "INTERNAL_ERROR";
    }

    public static class Messages
    {
        public const string SessionExpired = "Your session has expired. Please login again.";
        public const string InternalError = "An unexpected error occurred. Please try again later.";
        public const string DatabaseError = "A database error occurred while processing your request.";
        public const string ConcurrencyConflict =
            "The record was modified by another process. Please refresh and try again.";
        public const string JwtSecretMissing = "Jwt:Secret is missing from configuration.";
        public const string JwtConfigurationInvalid = "Jwt configuration is missing required values.";
    }

    public static class Auth
    {
        public const string InvalidCredentials = "Invalid username or password.";
        public const string RefreshTokenInvalidOrExpired = "Refresh token is invalid or expired.";
    }

    public static class Http
    {
        public const string JsonContentType = "application/json";
        public const string BearerScheme = "Bearer";
    }
}
