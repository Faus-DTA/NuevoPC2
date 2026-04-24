using System.ComponentModel.DataAnnotations;
using NuevoPC2.Models;

namespace NuevoPC2.ViewModels;

public class CourseFilterViewModel : IValidatableObject
{
    [Display(Name = "Nombre del curso")]
    public string? SearchName { get; set; }

    [Display(Name = "Créditos Mínimos")]
    [Range(0, int.MaxValue, ErrorMessage = "No se aceptan créditos negativos.")]
    public int? MinCredits { get; set; }

    [Display(Name = "Créditos Máximos")]
    [Range(0, int.MaxValue, ErrorMessage = "No se aceptan créditos negativos.")]
    public int? MaxCredits { get; set; }

    [Display(Name = "Horario Inicio")]
    public TimeSpan? FilterStartTime { get; set; }

    [Display(Name = "Horario Fin")]
    public TimeSpan? FilterEndTime { get; set; }

    public List<Course> Courses { get; set; } = new List<Course>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FilterStartTime.HasValue && FilterEndTime.HasValue)
        {
            if (FilterEndTime.Value <= FilterStartTime.Value)
            {
                yield return new ValidationResult(
                    "No se permite HorarioFin anterior o igual a HorarioInicio.",
                    new[] { nameof(FilterEndTime) }
                );
            }
        }
        
        if (MinCredits.HasValue && MaxCredits.HasValue)
        {
            if (MaxCredits.Value < MinCredits.Value)
            {
                yield return new ValidationResult(
                    "Los créditos máximos no pueden ser menores a los mínimos.",
                    new[] { nameof(MaxCredits) }
                );
            }
        }
    }
}
