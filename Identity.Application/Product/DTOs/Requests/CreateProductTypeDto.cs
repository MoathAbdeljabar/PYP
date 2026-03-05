using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;


namespace MyApp.Application.Product.DTOs.Requests;
    public class CreateProductTypeDto
    {


    [StringLength(25)]
    [Required]
    public string Name { get; set; }


    [Required]
    public IFormFile Icon { get; set; } 

    }
