using Microsoft.AspNetCore.Http;
using MyApp.Application.Product.DTOs.Requests;
using MyApp.Application.Product.DTOs.Responses;
using MyApp.Domain.Enums;

namespace MyApp.Application.Product.Interfaces;
    public interface IProductService
    {
    Task<ServiceResult<ProductResponseDto>> CreateNewProductAsync(string userId, CreateProductDto newProductDto);
    Task<ServiceResult<ProductsResponseDto>> GetPendingProductsAsync(string? userId, short pageNumber = 1, short pageSize = 10);
    Task<ServiceResult<ProductResponseDto>> UpdateProductSateAsync(int productId, EnProductState newState, string adminId);
    Task<ServiceResult<ProductsResponseDto>> SearchProductsAsync(ProductSearchParameters parameters, string? userId, EnProductState? productState = EnProductState.Approved);
    Task<ServiceResult<object>> DeleteProductAsync(int productId, string userId, bool isAdmin);
    Task<ServiceResult<ProductResponseDto>> GetProductByIdAsync(int productId, bool isAdmin, string? userId);
    Task<ServiceResult<object>> UpdateProductDetailsAsync(int productId, UpdateProductDto updateProductDto, string userId);
    Task<ServiceResult<object>> AddProductImagesAsync(int productId, string userId, List<IFormFile> newImages);
    Task<ServiceResult<object>> DeleteProductImageAsync(int productId, string userId, string imageId);
    }

