using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NuevoPC2.Models;

namespace NuevoPC2.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync();

        // 1. Seed Roles
        string[] roleNames = { "Coordinador", "Estudiante" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 2. Seed Coordinator User
        string coordEmail = "coordinador@universidad.edu";
        var coordUser = await userManager.FindByEmailAsync(coordEmail);
        if (coordUser == null)
        {
            coordUser = new IdentityUser
            {
                UserName = coordEmail,
                Email = coordEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(coordUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(coordUser, "Coordinador");
            }
        }

        // 3. Seed Courses (Al menos 3 activos)
        if (!context.Courses.Any())
        {
            var courses = new Course[]
            {
                new Course
                {
                    Code = "CS101",
                    Name = "Introducción a la Programación",
                    Credits = 4,
                    MaxCapacity = 30,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(10, 0, 0),
                    IsActive = true
                },
                new Course
                {
                    Code = "DB201",
                    Name = "Base de Datos I",
                    Credits = 3,
                    MaxCapacity = 25,
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0),
                    IsActive = true
                },
                new Course
                {
                    Code = "WEB301",
                    Name = "Desarrollo Web MVC",
                    Credits = 5,
                    MaxCapacity = 20,
                    StartTime = new TimeSpan(14, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    IsActive = true
                }
            };

            context.Courses.AddRange(courses);
            await context.SaveChangesAsync();
        }
    }
}
