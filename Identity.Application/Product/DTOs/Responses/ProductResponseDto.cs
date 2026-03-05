using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyApp.Domain.Enums;
using MyApp.Domain.Models;

namespace MyApp.Application.Product.DTOs.Responses;
    public class ProductResponseDto
    {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }

    // Category information (flattened for easy consumption)
    public int SubCategoryId { get; set; }
    public string SubCategoryName { get; set; } 

    // Owner information
    public string ApplicationUserId { get; set; }

    // Product state
    public EnProductState ProductState { get; set; }

    // Timestamps
    public DateTime UpdatedAt { get; set; }

    // Images - converted to List<string>
    public List<string> ProductImageUrls { get; set; } = new List<string>();

    public int StockQuantity { get; set; }


}

