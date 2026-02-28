using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SocialMediaAPI.Models;

namespace SocialMediaAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── Post ───────────────────────────────────────────
            builder.Entity<Post>(e =>
            {
                e.HasOne(p => p.User)
                 .WithMany(u => u.Posts)
                 .HasForeignKey(p => p.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasQueryFilter(p => !p.IsDeleted);
                e.HasIndex(p => p.UserId);
                e.HasIndex(p => p.CreatedAt);
            });

            // ── Comment ────────────────────────────────────────
            builder.Entity<Comment>(e =>
            {
                e.HasOne(c => c.Post)
                 .WithMany(p => p.Comments)
                 .HasForeignKey(c => c.PostId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(c => c.User)
                 .WithMany(u => u.Comments)
                 .HasForeignKey(c => c.UserId)
                 .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(c => c.ParentComment)
                 .WithMany(c => c.Replies)
                 .HasForeignKey(c => c.ParentCommentId)
                 .OnDelete(DeleteBehavior.NoAction);

                e.HasQueryFilter(c => !c.IsDeleted);
            });

            // ── Like ───────────────────────────────────────────
            builder.Entity<Like>(e =>
            {
                e.HasOne(l => l.Post)
                 .WithMany(p => p.Likes)
                 .HasForeignKey(l => l.PostId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(l => l.User)
                 .WithMany(u => u.Likes)
                 .HasForeignKey(l => l.UserId)
                 .OnDelete(DeleteBehavior.NoAction);

                // Unique: one like per user per post
                e.HasIndex(l => new { l.UserId, l.PostId }).IsUnique();
            });

            // ── Follow ─────────────────────────────────────────
            builder.Entity<Follow>(e =>
            {
                e.HasOne(f => f.Follower)
                 .WithMany(u => u.Following)
                 .HasForeignKey(f => f.FollowerId)
                 .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(f => f.FollowingUser)
                 .WithMany(u => u.Followers)
                 .HasForeignKey(f => f.FollowingId)
                 .OnDelete(DeleteBehavior.NoAction);

                // Unique: can't follow same person twice
                e.HasIndex(f => new { f.FollowerId, f.FollowingId }).IsUnique();
            });

            // ── RefreshToken ───────────────────────────────────
            builder.Entity<RefreshToken>(e =>
            {
                e.HasOne(r => r.User)
                 .WithMany(u => u.RefreshTokens)
                 .HasForeignKey(r => r.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(r => r.Token);
            });
        }
    }
}
