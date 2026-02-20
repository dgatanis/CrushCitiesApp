using Microsoft.AspNetCore.Identity;

public sealed class IdentitySeeder(
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration,
    ILogger<IdentitySeeder> logger)
{
    public async Task SeedAsync()
    {
        var roles = configuration.GetSection("Seed:Roles").Get<string[]>() ?? ["Admin"];
        foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                if (!roleResult.Succeeded)
                {
                    logger.LogWarning("Failed to create role {Role}: {Errors}", role, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                }
            }
        }

        var adminUsername = configuration["Seed:Admin:Username"];
        var adminEmail = configuration["Seed:Admin:Email"];
        var adminPassword = configuration["Seed:Admin:Password"];

        if (string.IsNullOrWhiteSpace(adminUsername) ||
            string.IsNullOrWhiteSpace(adminEmail) ||
            string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogInformation("Identity seeding skipped admin creation. Set Seed:Admin:Username, Seed:Admin:Email, and Seed:Admin:Password to enable it.");
            return;
        }

        var adminUser = await userManager.FindByNameAsync(adminUsername) ?? await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new IdentityUser
            {
                UserName = adminUsername,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var userResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!userResult.Succeeded)
            {
                logger.LogWarning("Failed to create admin user: {Errors}", string.Join("; ", userResult.Errors.Select(e => e.Description)));
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            var roleAssignResult = await userManager.AddToRoleAsync(adminUser, "Admin");
            if (!roleAssignResult.Succeeded)
            {
                logger.LogWarning("Failed to assign Admin role: {Errors}", string.Join("; ", roleAssignResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
