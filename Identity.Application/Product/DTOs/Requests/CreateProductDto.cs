using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.Application.Product.DTOs.Requests;
public class CreateProductDto
{

    [Length(0, 25)]
    [Required]
    public string Name { get; set; }


    [Required]
    public string Description { get; set; }

    [Required]
  
    public decimal Price { get; set; }


    [Required]
    public List<IFormFile> ImagesList { get; set; }


    [Required]
    public int SubCategoryId { get; set; }

    [Required]
    public int StockQuantity { get; set; }


}

