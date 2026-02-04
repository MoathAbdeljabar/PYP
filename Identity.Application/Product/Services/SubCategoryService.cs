using Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyApp.Application.Product.DTOs.Requests;
using MyApp.Application.Product.DTOs.Responses;
using MyApp.Application.Product.Interfaces;
using MyApp.Application.Shared;
using MyApp.Application.Shared.Interfaces;
using MyApp.Domain.Models;

namespace MyApp.Application.Product.Services;
    public class SubCategoryService : ISubCategoryService
    {
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileUrlService _fileUrlService;
    public SubCategoryService(ApplicationDbContext context, IFileStorageService fileStorageService, IFileUrlService fileUrlService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _fileUrlService = fileUrlService;
    }

    public async Task<ServiceResult<ProductCategoryDto>> CreateNewSubCategoryAsync(int categoryId, CreateProductTypeDto newSubCategoryDto)
    {

        if (!newSubCategoryDto.Icon.IsValidImageFile())
        {
            return new ServiceResult<ProductCategoryDto>()
            {
                IsSuccess = false,
                ErrorType = BusinessErrorType.InvalidImage,
                Message = "Invalid Icon"
            };
        }

        var mainCategory = await _context.ProductCategories.FindAsync(categoryId);
        if (mainCategory is null)
        {
            return ServiceResult<ProductCategoryDto>.Failure(BusinessErrorType.ResourceNotFound, "Product Category Not Found");
        }

        var subCategory = new SubCategory()
        {
            Name = newSubCategoryDto.Name,
            IconFileName = $"{Guid.NewGuid()}{Path.GetExtension(newSubCategoryDto.Icon.FileName)}",
            ProductCategoryId = mainCategory.Id,
            ProductCategory = mainCategory
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var filePath = await _fileStorageService.UploadFileAsync(newSubCategoryDto.Icon, "icons/SubCategory", subCategory.IconFileName);

            if (filePath.IsNullOrEmpty())
            {
                return ServiceResult<ProductCategoryDto>.Failure(BusinessErrorType.Unknown, "Can Not Upload Icon");
            }

            await _context.AddAsync(subCategory);


            await _context.SaveChangesAsync();

            ProductCategoryDto productCategoryDto = new ProductCategoryDto()
            {
                Id = subCategory.Id,
                Name = subCategory.Name,
                IconUrl = _fileUrlService.GetFullUrl("icons/SubCategory/" + subCategory.IconFileName)
            };


            await transaction.CommitAsync();
            return ServiceResult<ProductCategoryDto>.Success(productCategoryDto, "Product Category Created Successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            await _fileStorageService.DeleteFileAsync("icons/SubCategory/" + subCategory.IconFileName);
            return ServiceResult<ProductCategoryDto>.Failure(BusinessErrorType.Unknown, ex.Message);
            // throw; // Re-throw to handle the exception upstream
        }
    }


    public async Task<ServiceResult<List<ProductCategoryDto>>> GetAllSubCategoriesAsync(int id)
    {

        if(! _context.ProductCategories.Any(ct => ct.Id == id))
        {
            return ServiceResult<List<ProductCategoryDto>>.Failure(BusinessErrorType.ResourceNotFound, "Product Category Not Found");
        }
        var subCategoryDtos = await _context.SubCategories.Where(sc => sc.ProductCategoryId == id)
         .Select(category => new ProductCategoryDto()
         {
             Id = category.Id,
             Name = category.Name,
             IconUrl = _fileUrlService.GetFullUrl("icons/SubCategory/" + category.IconFileName)
         })
         .ToListAsync();


        return ServiceResult<List<ProductCategoryDto>>.Success(subCategoryDtos);
    }


    public async Task<ServiceResult<ProductCategoryDto>> GetSubCategoryByIdAsync(int id)
    {

        var subCategory = await _context.SubCategories.FindAsync(id);
        if (subCategory == null)
        {
            return ServiceResult<ProductCategoryDto>.Failure(BusinessErrorType.ResourceNotFound, "Subcategory Not Found");

        }

        var productCategoryDto = new ProductCategoryDto()
        {
            Id = subCategory.Id,
            Name = subCategory.Name,
            IconUrl = _fileUrlService.GetFullUrl("icons/SubCategory/" + subCategory.IconFileName)

        };

        return ServiceResult<ProductCategoryDto>.Success(productCategoryDto);
    }


    //------------------------
    public async Task<ServiceResult<ProductCategoryDto>> UpdateSubCategoryAsync(int id, UpdateProductTypeDto updateSubCategoryDto)
    {
        if (updateSubCategoryDto.Name == null && updateSubCategoryDto.Icon == null)
        {
            return ServiceResult<ProductCategoryDto>.Failure(BusinessErrorType.OperationNotAllowed, "Empty Changes");
        }

        var subCategory = await _context.SubCategories.FindAsync(id);
        if (subCategory == null)
        {
            return ServiceResult<ProductCategoryDto>.Failure(
                BusinessErrorType.ResourceNotFound,
                "Subcategory Not Found");
        }

        string currentIcon = subCategory.IconFileName;
        string newIcon = null;
        bool newIconUploaded = false;


        await using var transaction = await _context.Database.BeginTransactionAsync();


        try
        {

            if (updateSubCategoryDto.Icon != null)
            {
                if (!updateSubCategoryDto.Icon.IsValidImageFile())
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<ProductCategoryDto>.Failure(
                        BusinessErrorType.InvalidImage,
                        "Invalid Icon");
                }

                newIcon = $"{Guid.NewGuid()}{Path.GetExtension(updateSubCategoryDto.Icon.FileName)}";
                var filePath = await _fileStorageService.UploadFileAsync(
                   updateSubCategoryDto.Icon,
                   "icons/SubCategory",
                   newIcon);
                if (filePath.IsNullOrEmpty())
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<ProductCategoryDto>.Failure(
                    BusinessErrorType.Unknown,
                    "Can Not Upload Icon");
                }

                newIconUploaded = true;
                subCategory.IconFileName = newIcon;
            }

            if (updateSubCategoryDto.Name != null)
            {
                subCategory.Name = updateSubCategoryDto.Name;
            }

            await _context.SaveChangesAsync();

          
            ProductCategoryDto subCategoryDto = new ProductCategoryDto()
            {
                Id = subCategory.Id,
                Name = subCategory.Name,
                IconUrl = _fileUrlService.GetFullUrl("icons/SubCategory/" + subCategory.IconFileName)
            };
            await transaction.CommitAsync();


            //-----------------
            //Delete Old Icon
            //-----------------

            try
            {
                if (newIconUploaded && !string.IsNullOrEmpty(currentIcon))
                {
                    await _fileStorageService.DeleteFileAsync(
                         $"icons/SubCategory/{currentIcon}");
                }
            }
            catch (Exception ex)
            {

                //log old icon name
            }



            return ServiceResult<ProductCategoryDto>.Success(
        subCategoryDto,
        "Subcategory Updated Successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            if(newIcon != null)
            await _fileStorageService.DeleteFileAsync("icons/SubCategory/" + newIcon);
            return ServiceResult<ProductCategoryDto>.Failure(BusinessErrorType.Unknown, ex.Message);
            // throw; // Re-throw to handle the exception upstream
        }


    }


    public async Task<ServiceResult<object>> DeleteSubCategoryAsync(int id)
    {
        var category = await _context.SubCategories.FindAsync(id);
        if (category is null)
        {
            return ServiceResult<object>.Failure(
                BusinessErrorType.ResourceNotFound,
                "Subcategory Not Found");
        }

        string? iconFileName = category.IconFileName;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Delete from database first
            _context.SubCategories.Remove(category);
            await _context.SaveChangesAsync();

            // 2. Then delete file
            if (!string.IsNullOrEmpty(iconFileName))
            {
                await _fileStorageService.DeleteFileAsync($"icons/SubCategory/{iconFileName}");
            }

            await transaction.CommitAsync();
            return ServiceResult<object>.Success(null, "Subcategory deleted successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Failure(BusinessErrorType.Unknown, ex.Message);
        }
    }

}
    


