using aspnet.Models;
using Microsoft.AspNetCore.Identity;

namespace aspnet.Data;

public static class IdentitySeed
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

        string[] roles = { "Admin", "Manager" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        const string adminEmail = "admin@casino.local";

        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                OIB = "12345678901",
                JMBG = "1234567890123"
            };

            var result = await userManager.CreateAsync(admin, "Admin123$");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        const string managerEmail = "manager@casino.local";

        if (await userManager.FindByEmailAsync(managerEmail) is null)
        {
            var manager = new AppUser
            {
                UserName = managerEmail,
                Email = managerEmail,
                EmailConfirmed = true,
                OIB = "10987654321",
                JMBG = "3210987654321"
            };

            var result = await userManager.CreateAsync(manager, "Manager123$");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(manager, "Manager");
            }
        }
    }
}
