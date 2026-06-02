using CarRentalSystem.Models;
using Microsoft.AspNetCore.Identity;

namespace CarRentalSystem.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            context.Database.EnsureCreated();

            foreach (var role in new[] { "Admin", "Customer" })
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            if (await userManager.FindByNameAsync("admin") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@carrental.com",
                    FullName = "System Administrator",
                    Role = "Admin",
                    EmailConfirmed = true,
                    IsEmailVerified = true
                };
                var r = await userManager.CreateAsync(admin, "Admin@123");
                if (r.Succeeded) await userManager.AddToRoleAsync(admin, "Admin");
            }

            if (!context.Cars.Any())
            {
                context.Cars.AddRange(
                    new Car { Brand = "Toyota", Model = "Vios", Year = 2022, PlateNumber = "ABC-1234", Color = "White", DailyRate = 2500, Status = "Available", Description = "Fuel efficient sedan" },
                    new Car { Brand = "Honda", Model = "City", Year = 2023, PlateNumber = "DEF-5678", Color = "Silver", DailyRate = 2800, Status = "Available", Description = "Compact sedan" },
                    new Car { Brand = "Mitsubishi", Model = "Mirage", Year = 2021, PlateNumber = "GHI-9012", Color = "Red", DailyRate = 2200, Status = "Available", Description = "Economy hatchback" },
                    new Car { Brand = "Toyota", Model = "Innova", Year = 2022, PlateNumber = "JKL-3456", Color = "Black", DailyRate = 4500, Status = "Available", Description = "7-seater MPV" },
                    new Car { Brand = "Ford", Model = "Ranger", Year = 2023, PlateNumber = "MNO-7890", Color = "Blue", DailyRate = 5000, Status = "Available", Description = "4x4 pickup truck" }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}