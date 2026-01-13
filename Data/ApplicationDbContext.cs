using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SecureApi.Models;

namespace SecureApi.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
//    Key rule to remember

//Configure relationships from the dependent side
//(the side with the foreign key)
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Customize Identity table names if needed
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
        });
        builder.Entity<Category>().HasIndex(p=>p.Name).IsUnique();
        builder.Entity<Product>().HasOne(p => p.Category)
            .WithMany(p=>p.Products).HasForeignKey(p=>p.CategoryId).IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
            ;
        builder.Entity<BasketItems>().HasOne(p=>p.Basket)
            .WithMany(p=>p.BasketItems).HasForeignKey(p=>p.BasketId)
            .IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Entity<BasketItems>().HasOne(p=>p.Product).
            WithMany(p=>p.basketItems).IsRequired().HasForeignKey(p=>p.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<BasketItems>()
    .HasIndex(bi => new { bi.BasketId, bi.ProductId })
    .IsUnique();
        builder.Entity<OrderItems>().HasOne(p=>p.Order)
            .WithMany(p=>p.OrderItems).HasForeignKey(p=>p.OrderId)
            .IsRequired().OnDelete(DeleteBehavior.Cascade);
    }

    //public class ProductConfiguration : IEntityTypeConfiguration<Product>
    //{
    //    public void Configure(EntityTypeBuilder<Product> builder)
    //    {
    //        builder
    //            .HasOne(p => p.Category)
    //            .WithMany(c => c.Products)
    //            .HasForeignKey(p => p.CategoryId)
    //            .IsRequired();
    //    }
    //}

    public DbSet<Category> Category { get; set; }
    public DbSet<Product> Product { get; set; }
    public DbSet<Basket> Basket { get; set; }
    public DbSet<BasketItems> BasketItems { get; set; }
    public DbSet<Order> Order { get; set; }
    public DbSet<OrderItems> OrderItems { get; set; }

}
