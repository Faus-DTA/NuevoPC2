using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using NuevoPC2.Data;
using NuevoPC2.Models;
using NuevoPC2.ViewModels;

namespace NuevoPC2.Controllers;

public class CoursesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private const string CacheKey = "ActiveCoursesList";

    public CoursesController(ApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // GET: Courses
    public async Task<IActionResult> Index(CourseFilterViewModel filter)
    {
        List<Course> activeCourses;

        // 1. Obtener de caché o base de datos
        var cachedCourses = await _cache.GetStringAsync(CacheKey);
        if (string.IsNullOrEmpty(cachedCourses))
        {
            activeCourses = await _context.Courses.Where(c => c.IsActive).ToListAsync();
            
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
            };
            await _cache.SetStringAsync(CacheKey, JsonSerializer.Serialize(activeCourses), cacheOptions);
        }
        else
        {
            activeCourses = JsonSerializer.Deserialize<List<Course>>(cachedCourses) ?? new List<Course>();
        }

        // 2. Si el modelo es inválido, devolver la lista completa
        if (!ModelState.IsValid)
        {
            filter.Courses = activeCourses;
            return View(filter);
        }

        // 3. Aplicar filtros en memoria
        var query = activeCourses.AsEnumerable();

        if (!string.IsNullOrEmpty(filter.SearchName))
        {
            query = query.Where(c => c.Name.Contains(filter.SearchName, StringComparison.OrdinalIgnoreCase));
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
        filter.Courses = query.ToList();

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

        // Guardar el último curso visitado en la sesión
        HttpContext.Session.SetInt32("LastCourseId", course.Id);
        HttpContext.Session.SetString("LastCourseName", course.Name);

        return View(course);
    }
}
