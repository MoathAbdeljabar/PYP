namespace MyApp.Domain.Models;
    public class SubCategory : BaseDomainModel
    {
    public int ProductCategoryId { get; set; }
    public ProductCategory ProductCategory { get; set; }
    public string IconFileName { get; set; }


    public List<ProductEntity> Products { get; set; }

    }

