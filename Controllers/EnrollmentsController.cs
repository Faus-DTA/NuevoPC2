using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuevoPC2.Data;
using NuevoPC2.Models;

namespace NuevoPC2.Controllers;

[Authorize]
public class EnrollmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public EnrollmentsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int courseId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge(); // Fuerza el login si por alguna razón no hay usuario válido
        }

        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        
        if (course == null || !course.IsActive)
        {
            TempData["ErrorMessage"] = "El curso no existe o no está activo.";
            return RedirectToAction("Index", "Courses");
        }

        // 1. Validar que no esté matriculado ya
        var existingEnrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.CourseId == courseId && e.UserId == user.Id);

        if (existingEnrollment != null)
        {
            if (existingEnrollment.Status == EnrollmentStatus.Cancelada)
            {
                // Si estaba cancelada, podríamos reactivarla, pero para el examen crearemos o actualizaremos.
                // Como hay un índice único (CourseId, UserId), actualizamos el estado.
                existingEnrollment.Status = EnrollmentStatus.Pendiente;
                existingEnrollment.RegistrationDate = DateTime.UtcNow;
                _context.Update(existingEnrollment);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Tu matrícula ha sido reactivada y está pendiente de confirmación.";
                return RedirectToAction("Details", "Courses", new { id = courseId });
            }

            TempData["ErrorMessage"] = "Ya te encuentras matriculado en este curso.";
            return RedirectToAction("Details", "Courses", new { id = courseId });
        }

        // 2. Validación de Cupos
        var currentEnrollmentsCount = await _context.Enrollments
            .CountAsync(e => e.CourseId == courseId && e.Status != EnrollmentStatus.Cancelada);

        if (currentEnrollmentsCount >= course.MaxCapacity)
        {
            TempData["ErrorMessage"] = "El curso ha alcanzado su cupo máximo.";
            return RedirectToAction("Details", "Courses", new { id = courseId });
        }

        // 3. Validación de Cruce de Horarios
        // Obtener matrículas activas/pendientes del usuario con los datos del curso incluido
        var userActiveEnrollments = await _context.Enrollments
            .Include(e => e.Course)
            .Where(e => e.UserId == user.Id && e.Status != EnrollmentStatus.Cancelada)
            .ToListAsync();

        // Fórmula de solapamiento: (InicioA < FinB) && (FinA > InicioB)
        var overlappingEnrollment = userActiveEnrollments.FirstOrDefault(e => 
            course.StartTime < e.Course!.EndTime && course.EndTime > e.Course!.StartTime);

        if (overlappingEnrollment != null)
        {
            TempData["ErrorMessage"] = $"Cruce de horarios detectado con el curso '{overlappingEnrollment.Course!.Name}' ({overlappingEnrollment.Course.StartTime:hh\\:mm} - {overlappingEnrollment.Course.EndTime:hh\\:mm}).";
            return RedirectToAction("Details", "Courses", new { id = courseId });
        }

        // Si pasa todas las validaciones, creamos la matrícula
        var newEnrollment = new Enrollment
        {
            CourseId = courseId,
            UserId = user.Id,
            RegistrationDate = DateTime.UtcNow,
            Status = EnrollmentStatus.Pendiente
        };

        _context.Enrollments.Add(newEnrollment);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Inscripción realizada con éxito. Estado: Pendiente.";
        return RedirectToAction("Details", "Courses", new { id = courseId });
    }
}
