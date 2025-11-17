using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Ashish_Backend_Folio.Models;

namespace Ashish_Backend_Folio.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // Add DbSets for Projects, BlogPosts, etc.
        // public DbSet<Project> Projects { get; set; }
    }
}
