using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NuevoPC2.Models;

namespace NuevoPC2.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Course> Courses { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configuración para Course
        builder.Entity<Course>(entity =>
        {
            // Código único
            entity.HasIndex(c => c.Code).IsUnique();

            // Restricción: Créditos > 0 y HorarioInicio < HorarioFin
            // Note: EF Core con SQLite sí soporta Check Constraints desde EF Core 3.0+
            entity.ToTable(t => 
            {
                t.HasCheckConstraint("CK_Course_Credits", "\"Credits\" > 0");
                // SQLite usa string en el formato 'HH:MM:SS' para TimeSpan por lo que podemos compararlos alfabéticamente
                t.HasCheckConstraint("CK_Course_Time", "\"StartTime\" < \"EndTime\"");
            });
        });

        // Configuración para Enrollment
        builder.Entity<Enrollment>(entity =>
        {
            // Un usuario no puede estar matriculado más de una vez en el mismo curso
            entity.HasIndex(e => new { e.CourseId, e.UserId }).IsUnique();

            entity.HasOne(e => e.Course)
                  .WithMany(c => c.Enrollments)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
