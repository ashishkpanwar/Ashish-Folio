using Ashish_Backend_Folio.Models;
using Microsoft.AspNetCore.Identity;

namespace Ashish_Backend_Folio.Data
{
    public class SeedData
    {
        public static async Task SeedRolesAndAdminAsync(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            string[] roles = new[] { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Create admin if not exists
            var adminEmail = "admin@yourfolio.local";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    DisplayName = "Admin"
                };

                var create = await userManager.CreateAsync(admin, "Admin@12345"); // change in prod
                if (create.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }
    }
}
