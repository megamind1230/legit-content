using LegitContent.Models;
using Microsoft.EntityFrameworkCore;

namespace LegitContent.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
		public DbSet<User> Users => Set<User>();

		public DbSet<Link> Links => Set<Link>();

		public DbSet<Upvote> Upvotes => Set<Upvote>();

		public DbSet<Tag> Tags => Set<Tag>();

		public DbSet<LinkTag> LinkTags => Set<LinkTag>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
				modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

				modelBuilder.Entity<Link>(entity =>
				{
						entity.HasIndex(l => l.NormalizedUrl);
						entity.Property(l => l.Type).HasConversion<string>();
						entity.HasOne(l => l.Submitter).WithMany().HasForeignKey(l => l.SubmitterId);
				});

				modelBuilder.Entity<Upvote>(entity =>
				{
						entity.HasIndex(v => new { v.UserId, v.LinkId }).IsUnique();
						entity.HasOne(v => v.Link).WithMany(l => l.Upvotes).HasForeignKey(v => v.LinkId);
				});

				modelBuilder.Entity<Tag>(entity =>
				{
						entity.HasIndex(t => new { t.ParentId, t.Name }).IsUnique();
						entity.HasOne(t => t.Parent).WithMany().HasForeignKey(t => t.ParentId);
				});

				modelBuilder.Entity<LinkTag>(entity =>
				{
						entity.HasKey(lt => new { lt.LinkId, lt.TagId });
						entity.HasOne(lt => lt.Link).WithMany(l => l.Tags).HasForeignKey(lt => lt.LinkId);
						entity.HasOne(lt => lt.Tag).WithMany(t => t.Links).HasForeignKey(lt => lt.TagId);
				});
		}
}
