using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Domain.Models;

namespace MyApp.Data.Configurations;

public class CartProductConfiguration : IEntityTypeConfiguration<CartProduct>
{
    public void Configure(EntityTypeBuilder<CartProduct> builder)
    {
        // Configure table name
        builder.ToTable("CartProducts");

        // Configure primary key
        builder.HasKey(cp => cp.Id);
        builder.Property(cp => cp.Id).ValueGeneratedOnAdd();

        // Configure properties
        builder.Property(cp => cp.Quantity)
            .IsRequired();

        builder.HasCheckConstraint("CK_ProductCart_Stock", "Quantity > 0");

        builder.Property(cp => cp.CartId)
            .IsRequired();

        builder.Property(cp => cp.ProductId)
            .IsRequired();


        builder.Property(cp => cp.RowVersion)
        .IsRowVersion();


        //// Relationship to Cart (no navigation) 
        //builder.HasOne<CartEntity>()          // No navigation property specified
        //    .WithMany(c => c.Products)  // Cart has navigation to CartProducts
        //    .HasForeignKey(cp => cp.CartId)
        //    .OnDelete(DeleteBehavior.Cascade);


        // Many-to-One relationship with ProductEntity
        builder.HasOne(cp => cp.Product)
            .WithMany() // ProductEntity doesn't have CartProducts navigation property
            .HasForeignKey(cp => cp.ProductId)
            .OnDelete(DeleteBehavior.ClientCascade); 

    }
}