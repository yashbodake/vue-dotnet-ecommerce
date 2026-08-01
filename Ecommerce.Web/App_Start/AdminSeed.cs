using System.Linq;
using Ecommerce.Web.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Ecommerce.Web
{
    /// <summary>
    /// Spec 09 — seed Admin role + admin@legacy.local (password: Admin123!).
    /// Idempotent; safe to call on every app start.
    /// </summary>
    public static class AdminSeed
    {
        public const string AdminRoleName = "Admin";
        public const string AdminEmail = "admin@legacy.local";
        public const string AdminPassword = "Admin123!";

        public static void EnsureAdminUser()
        {
            using (var context = ApplicationDbContext.Create())
            {
                var roleStore = new RoleStore<IdentityRole>(context);
                var roleManager = new RoleManager<IdentityRole>(roleStore);

                if (!roleManager.RoleExists(AdminRoleName))
                {
                    roleManager.Create(new IdentityRole(AdminRoleName));
                }

                var userStore = new UserStore<ApplicationUser>(context);
                var userManager = new UserManager<ApplicationUser>(userStore);
                userManager.UserValidator = new UserValidator<ApplicationUser>(userManager)
                {
                    AllowOnlyAlphanumericUserNames = false,
                    RequireUniqueEmail = true
                };

                var user = userManager.FindByEmail(AdminEmail);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = AdminEmail,
                        Email = AdminEmail,
                        EmailConfirmed = true
                    };

                    var create = userManager.Create(user, AdminPassword);
                    if (!create.Succeeded)
                    {
                        throw new System.InvalidOperationException(
                            "Failed to seed admin user: " + string.Join("; ", create.Errors));
                    }
                }

                if (!userManager.IsInRole(user.Id, AdminRoleName))
                {
                    userManager.AddToRole(user.Id, AdminRoleName);
                }
            }
        }
    }
}
