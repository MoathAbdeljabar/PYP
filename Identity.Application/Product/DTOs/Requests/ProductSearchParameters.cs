
using MyApp.Domain.Enums;

namespace MyApp.Application.Product.DTOs.Requests;
public class ProductSearchParameters
{
    public string? Name { get; set; }
    //public string? UserId { get; set; }
    public int? SubcategoryId { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    // Pagination
    public short PageNumber { get; set; } = 1;
    public short PageSize { get; set; } = 10;

    // Sorting
    public string? SortBy { get; set; } // e.g., "Name", "Price", "CreatedDate"
    public bool SortDescending { get; set; } = false;

    // Validation method
    public void validateInput()
    {
        if (PageSize <= 0 || PageSize > 100)
            PageSize = 10;


        if (PageNumber <= 0)
            PageNumber = 1;
        if (CreatedFrom > CreatedTo) 
            (CreatedFrom, CreatedTo) = (CreatedTo, CreatedFrom);

        if (MinPrice > MaxPrice) 
            (MinPrice, MaxPrice) = (MaxPrice, MinPrice);
   
    }
}