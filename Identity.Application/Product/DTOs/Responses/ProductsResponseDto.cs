using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Application.Product.DTOs.Responses;
    public class ProductsResponseDto
    {

    public List<ProductResponseDto> Products { get; set; }

    public int TotalCount {  get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public int TotalPages {  get; set; }



    }
