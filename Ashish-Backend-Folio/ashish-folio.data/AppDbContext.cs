using Ashish_Backend_Folio.data.Models;
using Ashish_Backend_Folio.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ashish_Backend_Folio.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // Add DbSets for Projects, BlogPosts, etc.
        // public DbSet<Project> Projects { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<ProcessedMessage> ProcessedMessages { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // customizations if any
            builder.Entity<RefreshToken>().HasIndex(rt => rt.Token).IsUnique();
            builder.Entity<RefreshToken>().HasIndex(rt => rt.UserId);

            // ensure MessageId is unique to help DB-level dedupe
            builder.Entity<ProcessedMessage>()
                   .HasIndex(pm => pm.MessageId)
                   .IsUnique();

            // ensure business OrderId is unique (idempotent inserts)
            builder.Entity<Order>()
                   .HasIndex(o => o.OrderId)
                   .IsUnique();


        }
    }
}
