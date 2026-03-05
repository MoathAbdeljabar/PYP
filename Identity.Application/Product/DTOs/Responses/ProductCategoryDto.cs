using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Application.Product.DTOs.Responses;
    public class ProductCategoryDto
    {
    public int Id { get; set; }
    public string Name { get; set; }
    //public string IconFileName { get; set; }
    public string IconUrl { get; set; } // Add this property
}

