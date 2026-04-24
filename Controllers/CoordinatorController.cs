using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using NuevoPC2.Data;
using NuevoPC2.Models;

namespace NuevoPC2.Controllers;

[Authorize(Roles = "Coordinador")]
public class CoordinatorController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private const string CacheKey = "ActiveCoursesList";

    public CoordinatorController(ApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // GET: Coordinator
    public async Task<IActionResult> Index()
    {
        var courses = await _context.Courses.ToListAsync();
        return View(courses);
    }

    // GET: Coordinator/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Coordinator/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Code,Name,Credits,MaxCapacity,StartTime,EndTime,IsActive")] Course course)
    {
        if (ModelState.IsValid)
        {
            if (await _context.Courses.AnyAsync(c => c.Code == course.Code))
            {
                ModelState.AddModelError("Code", "El código del curso ya existe.");
                return View(course);
            }

            _context.Add(course);
            await _context.SaveChangesAsync();
            
            // Invalidar caché
            await _cache.RemoveAsync(CacheKey);
            
            TempData["SuccessMessage"] = "Curso creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        return View(course);
    }

    // GET: Coordinator/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound();
        }
        return View(course);
    }

    // POST: Coordinator/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Code,Name,Credits,MaxCapacity,StartTime,EndTime,IsActive")] Course course)
    {
        if (id != course.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingCourse = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
                if (existingCourse != null && existingCourse.Code != course.Code)
                {
                    if (await _context.Courses.AnyAsync(c => c.Code == course.Code && c.Id != id))
                    {
                        ModelState.AddModelError("Code", "El código del curso ya existe en otro curso.");
                        return View(course);
                    }
                }

                _context.Update(course);
                await _context.SaveChangesAsync();
                
                // Invalidar caché
                await _cache.RemoveAsync(CacheKey);
                
                TempData["SuccessMessage"] = "Curso actualizado exitosamente.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseExists(course.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(course);
    }

    // POST: Coordinator/ToggleStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound();
        }

        course.IsActive = !course.IsActive;
        _context.Update(course);
        await _context.SaveChangesAsync();
        
        // Invalidar caché
        await _cache.RemoveAsync(CacheKey);
        
        TempData["SuccessMessage"] = $"Curso {(course.IsActive ? "activado" : "desactivado")} exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Coordinator/Enrollments/5
    public async Task<IActionResult> Enrollments(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound();
        }

        ViewBag.CourseName = course.Name;
        ViewBag.CourseId = course.Id;

        var enrollments = await _context.Enrollments
            .Include(e => e.User)
            .Where(e => e.CourseId == id)
            .OrderByDescending(e => e.RegistrationDate)
            .ToListAsync();

        return View(enrollments);
    }

    // POST: Coordinator/UpdateEnrollmentStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEnrollmentStatus(int id, EnrollmentStatus status)
    {
        var enrollment = await _context.Enrollments.FindAsync(id);
        if (enrollment == null)
        {
            return NotFound();
        }

        enrollment.Status = status;
        _context.Update(enrollment);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Estado de la matrícula actualizado a {status}.";
        return RedirectToAction(nameof(Enrollments), new { id = enrollment.CourseId });
    }

    private bool CourseExists(int id)
    {
        return _context.Courses.Any(e => e.Id == id);
    }
}
