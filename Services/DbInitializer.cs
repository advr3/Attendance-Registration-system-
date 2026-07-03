using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using personal_attendanse_system.Data.Models;
using personal_attendanse_system.Data;
using Microsoft.EntityFrameworkCore;

namespace personal_attendanse_system.Services
{
    // Define a class to hold your DefaultAdmin settings (remains the same)
    public class DefaultAdminSettings
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        //public string FirstName { get; set; } = string.Empty;
        //public string LastName { get; set; } = string.Empty;
        //public string EmployeeId { get; set; } = string.Empty;
    }

    public class DbInitializer : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DefaultAdminSettings _adminSettings;
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(
            IServiceProvider serviceProvider,
            IOptions<DefaultAdminSettings> adminSettings,
            ILogger<DbInitializer> logger)
        {
            _serviceProvider = serviceProvider;
            _adminSettings = adminSettings.Value;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Database initialization started.");

            using (var scope = _serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                // RoleManager<IdentityRole> is NO LONGER needed and will be removed.
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Ensure the database is migrated
                try
                {
                    await dbContext.Database.MigrateAsync(cancellationToken);
                    _logger.LogInformation("Database migration completed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while migrating the database.");
                }

                // 1. Create Default Admin User
                // default id 1
                //var adminUser = await userManager.FindByEmailAsync(_adminSettings.Email);
                var adminUser = await userManager.FindByIdAsync("1");
                if (adminUser == null)
                {
                    adminUser = new User
                    {
                        Id = "1",
                        UserName = _adminSettings.Email,
                        Email = _adminSettings.Email,
                        //FirstName = _adminSettings.FirstName,
                        //LastName = _adminSettings.LastName,
                        EmailConfirmed = true // Confirm email by default for admin
                    };

                    var result = await userManager.CreateAsync(adminUser, _adminSettings.Password);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"Default admin user '{adminUser.Email}' created.");

                        // 2. Create Entry in your custom 'Admin' table for this user
                        var adminEntry = new Admin
                        {
                            UserId = adminUser.Id,
                            assignDate = DateTime.UtcNow
                        };
                        await dbContext.Admins.AddAsync(adminEntry);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation($"Entry created in 'Admin' table for user '{adminUser.Email}'.");
                    }
                    else
                    {
                        _logger.LogError($"Failed to create default admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    _logger.LogInformation($"Default admin user '{adminUser.Email}' already exists.");
                    // Ensure the existing admin user has an entry in your custom 'Admin' table
                    if (!await dbContext.Admins.AnyAsync(a => a.UserId == adminUser.Id, cancellationToken))
                    {
                        var adminEntry = new Admin
                        {
                            UserId = adminUser.Id,
                            assignDate = DateTime.UtcNow
                        };
                        await dbContext.Admins.AddAsync(adminEntry);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation($"Entry created in 'Admin' table for existing user '{adminUser.Email}'.");
                    }
                }
            }

            _logger.LogInformation("Database initialization finished.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Database initialization service stopped.");
            return Task.CompletedTask;
        }
    }
}
