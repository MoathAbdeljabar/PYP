
using Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyApp.Application.Product.DTOs.Requests;
using MyApp.Application.Product.DTOs.Responses;
using MyApp.Application.Product.Interfaces;
using MyApp.Application.Shared;
using MyApp.Application.Shared.Interfaces;
using MyApp.Domain.Models;
using Org.BouncyCastle.Crypto.Macs;


namespace MyApp.Application.Product.Services;
public class ProudctCategoryService : IProudctCategoryService
{

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileUrlService _fileUrlService;
    public ProudctCategoryService(ApplicationDbContext context, IFileStorageService fileStorageService, IFileUrlService fileUrlService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _fileUrlService = fileUrlService;
    }

    public async Task<ServiceResult<ProductCategoryDto>> CreateNewCategoryAsync(CreateProductTypeDto newProductCategory)
    {

        if (!newProductCategory.Icon.IsValidImageFile())
        {
                return ServiceResult<ProductCategoryDto>
                    .Failure(BusinessErrorType.InvalidImage, "Invalid Icon");
        }

        var productCategory = new ProductCategory()
        {
            Name = newProductCategory.Name,
            IconFileName = $"{Guid.NewGuid()}{Path.GetExtension(newProductCategory.Icon.FileName)}",
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var filePath = await _fileStorageService.UploadFileAsync(newProductCategory.Icon, "icons/ProductCategory", productCategory.IconFileName);

            if(filePath.IsNullOrEmpty())
            {
                return ServiceResult<ProductCategoryDto>.Failure(BusinessErrorType.Unknown, "Can Not Upload Icon");
            }

            await _context.AddAsync(productCategory);


            await _context.SaveChangesAsync();

            ProductCategoryDto productCategoryDto = new ProductCategoryDto()
            {
                Id = productCategory.Id,
                Name = productCategory.Name,
                IconUrl = _fileUrlService.GetFullUrl("icons/ProductCategory/" + productCategory.IconFileName)
            };


            await transaction.CommitAsync();
            return ServiceResult<ProductCategoryDto>.Success(productCategoryDto, "Product Category Created Successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            await _fileStorageService.DeleteFileAsync("icons/ProductCategory/" + productCategory.IconFileName);
            return ServiceResult<ProductCategoryDto>.Failure(BusinessErrorType.Unknown, ex.Message);
            // throw; // Re-throw to handle the exception upstream
        }

    }


    public async Task<ServiceResult<List<ProductCategoryDto>>> GetAllCategoriesAsync()
    {

        var categoryDtos = await _context.ProductCategories
          .Select(category => new ProductCategoryDto()
          {
              Id = category.Id,
              Name = category.Name,
              IconUrl = _fileUrlService.GetFullUrl("icons/ProductCategory/" + category.IconFileName)
          })
          .ToListAsync();


        return ServiceResult<List<ProductCategoryDto>>.Success(categoryDtos);
    }


    public async Task<ServiceResult<ProductCategoryDto>> GetCategoryByIdAsync(int id)
    {

        var category = await _context.ProductCategories.FindAsync(id);
        if(category == null)
        {
            return ServiceResult<ProductCategoryDto>.Failure(BusinessErrorType.ResourceNotFound, "Product Category Not Found");

        }

        var productCategoryDto = new ProductCategoryDto()
        {
            Id = category.Id,
            Name = category.Name,
            IconUrl = _fileUrlService.GetFullUrl("icons/ProductCategory/" + category.IconFileName)

        };

        return ServiceResult<ProductCategoryDto>.Success(productCategoryDto);
    }


    public async Task<ServiceResult<ProductCategoryDto>> UpdateCategoryAsync(int id, UpdateProductTypeDto updateProductCategoryDto)
    {
        if(updateProductCategoryDto.Name == null && updateProductCategoryDto.Icon == null)
        {
            return ServiceResult<ProductCategoryDto>.Failure(BusinessErrorType.OperationNotAllowed, "Empty Changes");
        }

        var category = await _context.ProductCategories.FindAsync(id);
        if (category == null)
        {
            return ServiceResult<ProductCategoryDto>.Failure(
                BusinessErrorType.ResourceNotFound,
                "Product Category Not Found");
        }

        string currentIcon = category.IconFileName;
        string newIcon = null;
        bool newIconUploaded = false;


        await using var transaction = await _context.Database.BeginTransactionAsync();


        try
        {

            if (updateProductCategoryDto.Icon != null)
            {
                if (!updateProductCategoryDto.Icon.IsValidImageFile())
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<ProductCategoryDto>.Failure(
                        BusinessErrorType.InvalidImage,
                        "Invalid Icon");
                }

                newIcon = $"{Guid.NewGuid()}{Path.GetExtension(updateProductCategoryDto.Icon.FileName)}";
                var filePath = await _fileStorageService.UploadFileAsync(
                   updateProductCategoryDto.Icon,
                   "icons/ProductCategory",
                   newIcon);
                if (filePath.IsNullOrEmpty())
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<ProductCategoryDto>.Failure(
                    BusinessErrorType.Unknown,
                    "Can Not Upload Icon");
                }

                newIconUploaded = true;
                category.IconFileName = newIcon;
            }

            if (updateProductCategoryDto.Name != null)
            {
                category.Name = updateProductCategoryDto.Name;
            }

            await _context.SaveChangesAsync();

            ProductCategoryDto productCategoryDto = new ProductCategoryDto()
            {
                Id = category.Id,
                Name = category.Name,
                IconUrl = _fileUrlService.GetFullUrl("icons/ProductCategory/" + category.IconFileName)
            };
            await transaction.CommitAsync();


            //-----------------
            //Delete Old Icon
            //-----------------

            try
            {

                if (newIconUploaded && !string.IsNullOrEmpty(currentIcon))
                {
                    await _fileStorageService.DeleteFileAsync($"icons/ProductCategory/{currentIcon}");
                }
              
            }
            catch (Exception ex) { 
            
                //log old icon name
            }



            return ServiceResult<ProductCategoryDto>.Success(
        productCategoryDto,
        "Product Category Updated Successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            if(newIcon != null)
            await _fileStorageService.DeleteFileAsync("icons/ProductCategory/" + newIcon);
            return ServiceResult<ProductCategoryDto>.Failure(BusinessErrorType.Unknown, ex.Message);
            // throw; // Re-throw to handle the exception upstream
        }


    }


    public async Task<ServiceResult<object>> DeleteCategoryAsync(int id)
    {
        var category = await _context.ProductCategories.FindAsync(id);
        if (category is null)
        {
            return ServiceResult<object>.Failure(
                BusinessErrorType.ResourceNotFound,
                "Product Category Not Found");
        }

        string? iconFileName = category.IconFileName;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Delete from database first
            _context.ProductCategories.Remove(category);
            await _context.SaveChangesAsync();

            // 2. Then delete file
            if (!string.IsNullOrEmpty(iconFileName))
            {
                await _fileStorageService.DeleteFileAsync($"icons/ProductCategory/{iconFileName}");
            }

            await transaction.CommitAsync();
            return ServiceResult<object>.Success(null, "Product category deleted successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Failure(BusinessErrorType.Unknown, ex.Message);
        }
    }
}


