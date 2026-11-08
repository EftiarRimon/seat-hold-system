using Microsoft.EntityFrameworkCore;
using SeatHold.Api.Models;

namespace SeatHold.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<Hold> Holds => Set<Hold>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<SchoolClass>(e =>
        {
            e.ToTable("classes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });

        b.Entity<Hold>(e =>
        {
            e.ToTable("holds");
            e.HasKey(x => x.Id);
            e.Property(x => x.StudentId).IsRequired().HasMaxLength(17);
            e.Property(x => x.Status).IsRequired().HasMaxLength(20);
            e.HasIndex(x => new { x.ClassId, x.Status });
            e.HasIndex(x => x.StudentId);
            e.HasOne<SchoolClass>().WithMany().HasForeignKey(x => x.ClassId);
        });
    }
}