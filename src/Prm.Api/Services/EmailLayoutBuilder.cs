using System.Net;
using System.Text;
using Prm.Common.Constants;

namespace Prm.Api.Services;

internal static class EmailLayoutBuilder
{
    private const string ColorPrimary = "#1e3a5f";
    private const string ColorInfo = "#2563eb";
    private const string ColorWarning = "#d97706";
    private const string ColorDanger = "#dc2626";
    private const string ColorSurface = "#f8fafc";
    private const string ColorBorder = "#e2e8f0";
    private const string ColorMuted = "#64748b";

    public static string AccentInfo => ColorInfo;
    public static string AccentWarning => ColorWarning;
    public static string AccentDanger => ColorDanger;
    public static string AccentPrimary => ColorPrimary;

    public static string BuildHtml(
        string title,
        string accentColor,
        string bodyHtml,
        string? disclaimer = null)
    {
        var builder = new StringBuilder();
        builder.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\" />");
        builder.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" /></head>");
        builder.Append("<body style=\"margin:0;padding:0;background-color:#eef2f7;font-family:Segoe UI,Arial,sans-serif;color:#1e293b;\">");
        builder.Append("<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"background-color:#eef2f7;padding:24px 12px;\">");
        builder.Append("<tr><td align=\"center\">");
        builder.Append($"<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"max-width:640px;background:#ffffff;border:1px solid {ColorBorder};border-radius:12px;overflow:hidden;box-shadow:0 8px 24px rgba(15,23,42,0.08);\">");
        builder.Append($"<tr><td style=\"background:{accentColor};padding:20px 28px;color:#ffffff;\">");
        builder.Append($"<div style=\"font-size:12px;letter-spacing:0.08em;text-transform:uppercase;opacity:0.9;\">{Encode(AppConstants.Email.BrandName)}</div>");
        builder.Append($"<div style=\"font-size:22px;font-weight:600;margin-top:6px;\">{Encode(title)}</div>");
        builder.Append("</td></tr>");
        builder.Append("<tr><td style=\"padding:28px;font-size:15px;line-height:1.6;\">");
        builder.Append(bodyHtml);
        builder.Append("</td></tr>");

        if (!string.IsNullOrWhiteSpace(disclaimer))
        {
            builder.Append("<tr><td style=\"padding:0 28px 20px;\">");
            builder.Append($"<div style=\"background:{ColorSurface};border-left:4px solid {accentColor};padding:12px 16px;border-radius:8px;font-size:13px;color:{ColorMuted};\">");
            builder.Append(Encode(disclaimer));
            builder.Append("</div></td></tr>");
        }

        builder.Append($"<tr><td style=\"background:{ColorSurface};border-top:1px solid {ColorBorder};padding:18px 28px;text-align:center;\">");
        builder.Append($"<div style=\"font-size:13px;font-weight:600;color:{ColorPrimary};margin-bottom:6px;\">{Encode(AppConstants.Email.FooterSignature)}</div>");
        builder.Append($"<div style=\"font-size:12px;color:{ColorMuted};line-height:1.5;\">{Encode(AppConstants.Email.FooterAutomatedNotice)}</div>");
        builder.Append("</td></tr></table></td></tr></table></body></html>");
        return builder.ToString();
    }

    public static string BuildText(string bodyText, string? disclaimer = null)
    {
        var builder = new StringBuilder();
        builder.AppendLine(bodyText.Trim());
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(disclaimer))
        {
            builder.AppendLine(disclaimer);
            builder.AppendLine();
        }

        builder.AppendLine("--");
        builder.AppendLine(AppConstants.Email.FooterSignature);
        builder.AppendLine(AppConstants.Email.FooterAutomatedNotice);
        return builder.ToString().TrimEnd();
    }

    public static string Paragraph(string htmlContent) =>
        $"<p style=\"margin:0 0 16px;\">{htmlContent}</p>";

    public static string SectionHeading(string title) =>
        $"<h3 style=\"margin:24px 0 12px;font-size:16px;color:{ColorPrimary};\">{Encode(title)}</h3>";

    public static string InfoPanel(string accentColor, string htmlContent) =>
        $"<div style=\"background:{ColorSurface};border:1px solid {ColorBorder};border-left:4px solid {accentColor};border-radius:8px;padding:14px 16px;margin:0 0 16px;\">{htmlContent}</div>";

    public static string Encode(string value) => WebUtility.HtmlEncode(value);
}
