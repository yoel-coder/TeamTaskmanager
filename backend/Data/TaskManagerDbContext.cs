using Microsoft.EntityFrameworkCore;
using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Data;

public sealed class TaskManagerDbContext(DbContextOptions<TaskManagerDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<UserSession> Sessions => Set<UserSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(task => task.Id);
            entity.Property(task => task.Title).HasMaxLength(200).IsRequired();
            entity.Property(task => task.Description).HasMaxLength(2000).IsRequired();
            entity.Property(task => task.Status).HasMaxLength(20).IsRequired();
            entity.Property(task => task.CreatedBy).HasMaxLength(100).IsRequired();
            entity.Property(task => task.AssignedTo).HasMaxLength(100);
            entity.Property(task => task.CompletedBy).HasMaxLength(100);
            entity.HasIndex(task => new { task.Status, task.CreatedAt });
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.UserName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(32).IsRequired();
            entity.Property(user => user.PasswordSalt).HasMaxLength(32).IsRequired();
            entity.HasIndex(user => user.UserName).IsUnique();
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("Sessions");
            entity.HasKey(session => session.Token);
            entity.Property(session => session.Token).HasMaxLength(64).IsFixedLength();
            entity.Property(session => session.UserName).HasMaxLength(100).IsRequired();
            entity.HasIndex(session => session.ExpiresAt);
        });
    }
}
