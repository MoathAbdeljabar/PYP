using Microsoft.AspNetCore.Http;


namespace MyApp.Application.Product.DTOs.Requests;

public class UpdateProductTypeDto
{
    public string? Name { get; set; }
    public IFormFile? Icon { get; set; }


}