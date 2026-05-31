namespace Prm.Api.Models.SystemConfigurations;

public class SystemConfigurationResponse
{
    public int Id { get; set; }
    public string ConfigurationType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
