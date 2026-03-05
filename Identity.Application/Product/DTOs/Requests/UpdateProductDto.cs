using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Application.Product.DTOs.Requests;
    public class UpdateProductDto
    {
    [Length(0, 25)]
    public string? Name { get; set; }


    public string? Description { get; set; }


    public decimal? Price { get; set; }

    public int? SubCategoryId { get; set; }

    public int? StockQuantity { get; set; }


}
