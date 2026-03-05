using Identity.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Domain.Enums;
using MyApp.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Data.Configurations;
public class ProductConfiguration : IEntityTypeConfiguration<ProductEntity>
{
    public void Configure(EntityTypeBuilder<ProductEntity> builder)
    {
        // Configure table name
        builder.ToTable("Products");

        // Configure primary key
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        // Configure properties
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(25);

        builder.Property(p => p.Description)
            .IsRequired();

        builder.Property(p => p.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.HasCheckConstraint("CK_Product_Price", "Price > 0");

        builder.Property(p => p.RowVersion)
            .IsRowVersion();


        builder.Property(p => p.StockQuantity)
        .IsRequired();

        builder.HasCheckConstraint("CK_Product_Stock", "StockQuantity >= 0");


        // Configure ApplicationUserId as string
        builder.Property(p => p.ApplicationUserId)
            .IsRequired()
            .HasMaxLength(450); // Standard IdentityUser Id length


  

         

        // ====================================================
        // 1:M Relation between Product and ProductImage
        // (Product has many ProductImages)
        // ====================================================
        builder.HasMany(p => p.ProductImages)
            .WithOne(pi => pi.Product) // Assuming ProductImage has Product property
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade) // Cascade delete images
            .IsRequired();

        // ====================================================
        // M:1 Relation between Product and SubCategory
        // (Product belongs to one SubCategory)
        // ====================================================
        builder.HasOne(p => p.SubCategory)
            .WithMany(sc => sc.Products) // Assuming SubCategory has Products collection
            .HasForeignKey(p => p.SubCategoryId)
            .OnDelete(DeleteBehavior.Cascade) 
            .IsRequired();

        // ====================================================
        // M:1 Relation between Product and ApplicationUser
        // (Product belongs to one User)
        // ====================================================
        // Option 1: Without navigation property in Product
        builder.HasOne<ApplicationUser>() // Type parameter only, no navigation property
            .WithMany() // ApplicationUser doesn't have Products collection
            .HasForeignKey(p => p.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade) 
            .IsRequired();


        // Indexes for performance
        builder.HasIndex(p => p.ApplicationUserId);
        builder.HasIndex(p => p.SubCategoryId);
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.Price);

        // Optional: Query filter for soft delete if you implement it
        // builder.HasQueryFilter(p => !p.IsDeleted);



        builder.HasQueryFilter(product => product.ProductState == EnProductState.Approved);
    }
}
