using System.Net.Http.Headers;
using System.Text;
using Hangfire.Dashboard;
using Microsoft.Extensions.Options;
using Prm.Api.Configuration;

namespace Prm.Api.Infrastructure;

public class HangfireDashboardAuthorizationFilter(IOptions<HangfireOptions> _hangfireOptions) : IDashboardAuthorizationFilter
{
    private const string AuthenticationScheme = "Basic";
    private const string AuthenticationRealm = "Hangfire Dashboard";

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var header = httpContext.Request.Headers.Authorization.ToString();

        if (AuthenticationHeaderValue.TryParse(header, out var authenticationHeader)
            && authenticationHeader.Scheme.Equals(AuthenticationScheme, StringComparison.OrdinalIgnoreCase)
            && TryDecodeCredentials(authenticationHeader.Parameter, out var username, out var password)
            && IsValidCredentials(username, password))
        {
            return true;
        }

        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        httpContext.Response.Headers.WWWAuthenticate = $"{AuthenticationScheme} realm=\"{AuthenticationRealm}\"";
        return false;
    }

    private bool IsValidCredentials(string username, string password)
    {
        var options = _hangfireOptions.Value;
        return username == options.DashboardUsername && password == options.DashboardPassword;
    }

    private static bool TryDecodeCredentials(string? encodedCredentials, out string username, out string password)
    {
        username = string.Empty;
        password = string.Empty;

        if (string.IsNullOrWhiteSpace(encodedCredentials))
        {
            return false;
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var separatorIndex = decoded.IndexOf(':');
            if (separatorIndex <= 0)
            {
                return false;
            }

            username = decoded[..separatorIndex];
            password = decoded[(separatorIndex + 1)..];
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
