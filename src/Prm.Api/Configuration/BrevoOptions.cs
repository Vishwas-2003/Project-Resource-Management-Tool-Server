using System.ComponentModel.DataAnnotations;

namespace Prm.Api.Configuration;

public class BrevoOptions
{
    public bool Enabled { get; set; }
    public string SmtpLogin { get; set; } = string.Empty;
    public string SmtpKey { get; set; } = string.Empty;
    public string SmtpHost { get; set; } = "smtp-relay.brevo.com";
    public int SmtpPort { get; set; } = 587;
    [Required]
    [EmailAddress]
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "PRM System";
}
