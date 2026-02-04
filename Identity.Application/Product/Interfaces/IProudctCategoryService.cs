using MyApp.Application.Product.DTOs.Requests;
using MyApp.Application.Product.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Application.Product.Interfaces;
public interface IProudctCategoryService
    {
    Task<ServiceResult<ProductCategoryDto>> CreateNewCategoryAsync(CreateProductTypeDto newProductCategory);
    Task<ServiceResult<List<ProductCategoryDto>>> GetAllCategoriesAsync();

    Task<ServiceResult<ProductCategoryDto>> UpdateCategoryAsync(int id, UpdateProductTypeDto updateProductCategoryDto);
    Task<ServiceResult<ProductCategoryDto>> GetCategoryByIdAsync(int id);
    Task<ServiceResult<object>> DeleteCategoryAsync(int id);
    }
