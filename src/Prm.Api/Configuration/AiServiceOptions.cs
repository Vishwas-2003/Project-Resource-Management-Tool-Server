using System.ComponentModel.DataAnnotations;

namespace Prm.Api.Configuration;

public class AiServiceOptions
{
    public const string Section = "Ai";
    [Required]
    public string BaseUrl { get; set; } = "http://127.0.0.1:8080";
}
