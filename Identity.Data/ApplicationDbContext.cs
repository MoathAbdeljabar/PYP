using Identity.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using MyApp.Data.Configurations;
using MyApp.Domain.Models;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace Identity.Data;
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{

        public ApplicationDbContext(DbContextOptions option) : base(option)
        {


        }


    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<SubCategory> SubCategories { get; set; } 
    public DbSet<ProductEntity> Products { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<CartEntity> Carts { get; set; }
    public DbSet<CartProduct> CartProducts { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<IdentityRole>().HasData(
            new IdentityRole { Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole { Name = "Store", NormalizedName = "STORE" },
            new IdentityRole { Name = "User", NormalizedName = "USER" }
        );

        //builder.ApplyConfiguration(new ProductConfiguration());
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());



    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseDomainModel>().Where(entry => entry.State == EntityState.Modified
        || entry.State == EntityState.Added);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.Now;
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.Now;
            }
        }



        var ProductEntries = ChangeTracker.Entries<ProductEntity>().Where(entry => entry.State == EntityState.Modified
       || entry.State == EntityState.Added);

        foreach (var entry in ProductEntries)
        {
            entry.Entity.UpdatedAt = DateTime.Now;
        }
        return base.SaveChangesAsync(cancellationToken);
    }

}



public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {

        var basePath = Path.GetFullPath(Path.Combine(
              Directory.GetCurrentDirectory(),
              "..", "Identity.API"));


        // Build configuration manually
        IConfiguration configuration = new ConfigurationBuilder()
            // Set the path to the ASP.NET Core project (adjust as needed)
            .SetBasePath(basePath).AddJsonFile("appsettings.json").Build(); 

        // Read the connection string by name
        var connectionString = configuration.GetConnectionString("Default");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }


}