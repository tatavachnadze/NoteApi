using Microsoft.EntityFrameworkCore;
using NoteApi.Common.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace NoteApi.Infrastructure.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<NoteTag> NoteTags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasColumnName("id");
            entity.Property(u => u.Email).HasColumnName("email").IsRequired();
            entity.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(u => u.FullName).HasColumnName("full_name").IsRequired();
            entity.Property(u => u.CreatedAt).HasColumnName("created_at");
            entity.Property(u => u.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.ToTable("notes");
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Id).HasColumnName("id");
            entity.Property(n => n.UserId).HasColumnName("user_id");
            entity.Property(n => n.Title).HasColumnName("title").IsRequired();
            entity.Property(n => n.Content).HasColumnName("content");
            entity.Property(n => n.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            entity.Property(n => n.CreatedAt).HasColumnName("created_at");
            entity.Property(n => n.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(n => n.User)
                  .WithMany(u => u.Notes)
                  .HasForeignKey(n => n.UserId);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("tags");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasColumnName("id");
            entity.Property(t => t.Name).HasColumnName("name").IsRequired();
            entity.HasIndex(t => t.Name).IsUnique();
        });

        modelBuilder.Entity<NoteTag>(entity =>
        {
            entity.ToTable("note_tags");
            entity.HasKey(nt => new { nt.NoteId, nt.TagId });
            entity.Property(nt => nt.NoteId).HasColumnName("note_id");
            entity.Property(nt => nt.TagId).HasColumnName("tag_id");

            entity.HasOne(nt => nt.Note)
                  .WithMany(n => n.NoteTags)
                  .HasForeignKey(nt => nt.NoteId);

            entity.HasOne(nt => nt.Tag)
                  .WithMany(t => t.NoteTags)
                  .HasForeignKey(nt => nt.TagId);
        });
    }
}