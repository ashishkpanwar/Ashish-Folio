using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Ashish_Backend_Folio.Data.Models;

namespace Ashish_Backend_Folio.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // Add DbSets for Projects, BlogPosts, etc.
        // public DbSet<Project> Projects { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // customizations if any
            builder.Entity<RefreshToken>().HasIndex(rt => rt.Token).IsUnique();
            builder.Entity<RefreshToken>().HasIndex(rt => rt.UserId);


        }
    }
}
