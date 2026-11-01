using System;
using System.Linq;
using ASI.Basecode.Data;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Manager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ASI.Basecode.WebApp
{
    public static class DbSeeder
    {
        public static void Seed(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TiaCContext>();
                var logger = scope.ServiceProvider.GetService<ILogger<TiaCContext>>();

                try
                {
                    context.Database.EnsureCreated();

                    // 1. Seed Categories if missing
                    if (!context.EquipmentCategories.Any())
                    {
                        context.EquipmentCategories.AddRange(
                            new EquipmentCategory { CategoryCode = "AV-MEDIA", CategoryName = "Audio-Visual & Multimedia", IsActive = true, CreatedTime = DateTime.Now, UpdatedTime = DateTime.Now },
                            new EquipmentCategory { CategoryCode = "IT-TECH", CategoryName = "IT & Computing Devices", IsActive = true, CreatedTime = DateTime.Now, UpdatedTime = DateTime.Now },
                            new EquipmentCategory { CategoryCode = "EVENT-SUP", CategoryName = "Event & Stage Supplies", IsActive = true, CreatedTime = DateTime.Now, UpdatedTime = DateTime.Now },
                            new EquipmentCategory { CategoryCode = "LAB-SCI", CategoryName = "Science & Lab Apparatus", IsActive = true, CreatedTime = DateTime.Now, UpdatedTime = DateTime.Now },
                            new EquipmentCategory { CategoryCode = "SPORTS-REC", CategoryName = "Sports & Athletics", IsActive = true, CreatedTime = DateTime.Now, UpdatedTime = DateTime.Now },
                            new EquipmentCategory { CategoryCode = "GEN-TOOLS", CategoryName = "Facilities & General Tools", IsActive = true, CreatedTime = DateTime.Now, UpdatedTime = DateTime.Now }
                        );
                        context.SaveChanges();
                    }

                    // 2. Seed Default Admin User with encrypted password
                    if (!context.Users.Any(u => u.Role == "Admin"))
                    {
                        var adminUser = new User
                        {
                            UserId = "ADMIN-001",
                            FirstName = "System",
                            LastName = "Administrator",
                            Email = "admin@tiac.edu.ph",
                            Role = "Admin",
                            PasswordHash = PasswordManager.EncryptPassword("Admin@123"),
                            BorrowerStatus = "eligible",
                            IsActive = true,
                            CreatedBy = "System",
                            UpdatedBy = "System",
                            CreatedTime = DateTime.Now,
                            UpdatedTime = DateTime.Now
                        };

                        context.Users.Add(adminUser);
                        context.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "An error occurred while seeding default database records.");
                }
            }
        }
    }
}