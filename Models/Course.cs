using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NuevoPC2.Models;

public class Course
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Los créditos deben ser mayores a 0.")]
    public int Credits { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El cupo máximo debe ser mayor a 0.")]
    public int MaxCapacity { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation property
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
