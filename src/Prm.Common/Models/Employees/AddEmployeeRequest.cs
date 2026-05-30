using System.ComponentModel.DataAnnotations;

namespace Prm.Common.Models.Employees;

public class AddEmployeeRequest
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Designation { get; set; } = string.Empty;
}
