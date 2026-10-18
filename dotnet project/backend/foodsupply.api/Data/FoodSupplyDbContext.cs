using FoodSupply.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodSupply.Api.Data;

public sealed class FoodSupplyDbContext : DbContext
{
    public FoodSupplyDbContext(DbContextOptions<FoodSupplyDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");

            entity.HasKey(category => category.Id);

            entity.Property(category => category.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(category => category.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(category => category.Description)
                .HasColumnName("description")
                .HasMaxLength(300);

            entity.Property(category => category.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();

            entity.HasIndex(category => category.Name)
                .IsUnique();
        });
    }
}
