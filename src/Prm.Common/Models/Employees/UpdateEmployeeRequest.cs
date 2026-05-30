using System.ComponentModel.DataAnnotations;

namespace Prm.Common.Models.Employees;

public class UpdateEmployeeRequest
{
    [Required]
    [MaxLength(100)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Designation { get; set; } = string.Empty;
}
