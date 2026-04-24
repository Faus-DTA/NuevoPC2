using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuevoPC2.Data;
using NuevoPC2.ViewModels;

namespace NuevoPC2.Controllers;

public class CoursesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CoursesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Courses
    public async Task<IActionResult> Index(CourseFilterViewModel filter)
    {
        // 1. Obtener la consulta base: Solo cursos activos
        var query = _context.Courses.Where(c => c.IsActive).AsQueryable();

        // 2. Si el modelo es inválido (por las validaciones de créditos negativos o fechas),
        // devolvemos la vista inmediatamente sin aplicar los filtros problemáticos.
        if (!ModelState.IsValid)
        {
            // Solo devolvemos la lista de cursos sin filtrar para que el usuario vea el error
            filter.Courses = await query.ToListAsync();
            return View(filter);
        }

        // 3. Aplicar filtros
        if (!string.IsNullOrEmpty(filter.SearchName))
        {
            query = query.Where(c => c.Name.ToLower().Contains(filter.SearchName.ToLower()));
        }

        if (filter.MinCredits.HasValue)
        {
            query = query.Where(c => c.Credits >= filter.MinCredits.Value);
        }

        if (filter.MaxCredits.HasValue)
        {
            query = query.Where(c => c.Credits <= filter.MaxCredits.Value);
        }

        if (filter.FilterStartTime.HasValue)
        {
            query = query.Where(c => c.StartTime >= filter.FilterStartTime.Value);
        }

        if (filter.FilterEndTime.HasValue)
        {
            query = query.Where(c => c.EndTime <= filter.FilterEndTime.Value);
        }

        // 4. Asignar resultados al ViewModel
        filter.Courses = await query.ToListAsync();

        return View(filter);
    }

    // GET: Courses/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var course = await _context.Courses
            .FirstOrDefaultAsync(m => m.Id == id);
            
        if (course == null || !course.IsActive)
        {
            return NotFound();
        }

        return View(course);
    }
}
