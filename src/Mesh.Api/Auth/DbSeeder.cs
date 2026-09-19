using Mesh.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Mesh.Api.Auth;

/// <summary>
/// Runs once at startup. Only seeds the demo user account itself for now -
/// demo entries/tasks/journal data gets seeded per-module as each module's
/// backend gets built (it needs a real UserId to belong to).
/// </summary>
public static class DbSeeder
{
    public const string DemoEmail = "demo@meshapp.local";
    public const string DemoPassword = "Demo@12345";

    public static async Task SeedDemoUserAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.FindByEmailAsync(DemoEmail) is not null)
            return; // already seeded, nothing to do

        var demoUser = new ApplicationUser
        {
            UserName = DemoEmail,
            Email = DemoEmail,
            EmailConfirmed = true,
            DisplayName = "Demo User",
        };

        await userManager.CreateAsync(demoUser, DemoPassword);
    }
}
