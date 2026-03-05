using MyApp.Application.Product.DTOs.Requests;
using MyApp.Application.Product.DTOs.Responses;

namespace MyApp.Application.Product.Interfaces;

public interface ISubCategoryService
{
    Task<ServiceResult<ProductCategoryDto>> CreateNewSubCategoryAsync(int categoryId, CreateProductTypeDto newSubCategoryDto);

    Task<ServiceResult<List<ProductCategoryDto>>> GetAllSubCategoriesAsync(int id);
    Task<ServiceResult<ProductCategoryDto>> GetSubCategoryByIdAsync(int id);
    Task<ServiceResult<ProductCategoryDto>> UpdateSubCategoryAsync(int id, UpdateProductTypeDto updateSubCategoryDto);
    Task<ServiceResult<object>> DeleteSubCategoryAsync(int id);
}