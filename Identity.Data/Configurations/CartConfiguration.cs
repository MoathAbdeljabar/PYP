using Identity.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Domain.Models;

namespace MyApp.Data.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<CartEntity>
{

    public void Configure(EntityTypeBuilder<CartEntity> builder)
    {
        // Configure table name
        builder.ToTable("Carts");

        // Configure primary key
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();




        // Configure ApplicationUserId as string
        builder.Property(c => c.ApplicationUserId)
            .IsRequired()
            .HasMaxLength(450); // Standard IdentityUser Id length


        builder.Property(c => c.RowVersion).IsRowVersion();


        builder.HasIndex(c => c.ApplicationUserId).IsUnique();


        builder.HasOne<ApplicationUser>()           // No navigation property in Cart
               .WithOne()                           // No navigation property in ApplicationUser
               .HasForeignKey<CartEntity>(c => c.ApplicationUserId)  // Foreign key column in Cart table
               .OnDelete(DeleteBehavior.Cascade)
               .IsRequired();

        // One-to-Many relationship with CartProduct
        builder.HasMany(c => c.Products)
            .WithOne(cp => cp.Cart)
            .HasForeignKey(cp => cp.CartId)
            .OnDelete(DeleteBehavior.Cascade);




    }
}
