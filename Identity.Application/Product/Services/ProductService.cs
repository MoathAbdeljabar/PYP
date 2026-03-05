using Identity.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyApp.Application.Product.DTOs.Requests;
using MyApp.Application.Product.DTOs.Responses;
using MyApp.Application.Product.Interfaces;
using MyApp.Application.Shared;
using MyApp.Application.Shared.Interfaces;
using MyApp.Domain.Enums;
using MyApp.Domain.Models;


namespace MyApp.Application.Product.Services;
    public class ProductService : IProductService {
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileUrlService _fileUrlService;
    public ProductService(ApplicationDbContext context, IFileStorageService fileStorageService, IFileUrlService fileUrlService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _fileUrlService = fileUrlService;
    }

    public async Task<ServiceResult<ProductResponseDto>> CreateNewProductAsync(string userId, CreateProductDto newProductDto)
     {
        if (!await _context.Users.AnyAsync(user => user.Id == userId))
        {
            return ServiceResult<ProductResponseDto>.Failure(BusinessErrorType.UserNotFound, "User Not Found");
        }

        var subCategory = await _context.SubCategories.FindAsync(newProductDto.SubCategoryId);
        if(subCategory is null)
        {
            return ServiceResult<ProductResponseDto>.Failure(BusinessErrorType.ResourceNotFound, "Subcategory Not Found");
        }

        foreach (var image in newProductDto.ImagesList)
        {
            if (!image.IsValidImageFile())
            {
                return ServiceResult<ProductResponseDto>
                    .Failure(BusinessErrorType.InvalidImage, "Invalid Image(s)");

            }
        }

        var imagesInfo = new Dictionary<string, IFormFile>();

        foreach (var image in newProductDto.ImagesList)
        {
            imagesInfo.Add($"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}", image);
        }

        var productImages = new List<ProductImage>();

        foreach (var key in imagesInfo.Keys) {
            productImages.Add(new ProductImage()
            {
                StoredName = key,
            });
        }

        ProductEntity product = new ProductEntity()
        {

            Name = newProductDto.Name,
            Description = newProductDto.Description,
            Price = newProductDto.Price,
            SubCategory = subCategory,
            SubCategoryId = subCategory.Id,
            ApplicationUserId = userId,
            ProductState = EnProductState.Pending,
            ProductImages = productImages,
            StockQuantity = newProductDto.StockQuantity, 
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var image in imagesInfo)
            {
                var filePath = await _fileStorageService.UploadFileAsync(image.Value, "uploads/products", image.Key);

                if (filePath.IsNullOrEmpty())
                {
                    return ServiceResult<ProductResponseDto>.Failure(BusinessErrorType.Unknown, "Can Not Upload Image");
                }
            }


            await _context.AddAsync(product);


            await _context.SaveChangesAsync();


            var ImageUrls = new List<string>();
            foreach(var imageObject in product.ProductImages)
            {
                ImageUrls.Add("uploads/products/" + imageObject.StoredName);
            }


            var createdProduct = new ProductResponseDto()
            {
              Id = product.Id,
              Name = product.Name,
              Description = product.Description,
              Price = product.Price,
              SubCategoryId = product.SubCategory.Id,
              SubCategoryName = product.SubCategory.Name,
              ApplicationUserId = product.ApplicationUserId,
              ProductState = product.ProductState,
              UpdatedAt = product.UpdatedAt,
              ProductImageUrls = ImageUrls
            };


            await transaction.CommitAsync();
            return ServiceResult<ProductResponseDto>.Success(createdProduct, "Product Created Successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            foreach (var image in imagesInfo)
            {
                var filePath = await _fileStorageService.UploadFileAsync(image.Value, "uploads/products", image.Key);

                await _fileStorageService.DeleteFileAsync("uploads/products" + image.Key);
            }
            return ServiceResult<ProductResponseDto>.Failure(BusinessErrorType.Unknown, ex.Message);
           
        }
    }


    public async Task<ServiceResult<ProductsResponseDto>> GetPendingProductsAsync(string? userId, short pageNumber = 1, short pageSize = 10)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var baseQuery = _context.Products.IgnoreQueryFilters()
            .Where(p => p.ProductState == EnProductState.Pending);

    
        if (!string.IsNullOrEmpty(userId))
        {
            baseQuery = baseQuery.Where(p => p.ApplicationUserId == userId!);
        }

        // Apply ordering last
        baseQuery = baseQuery.OrderByDescending(p => p.UpdatedAt);


        var totalCount = await baseQuery.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var products = await baseQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductResponseDto()
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                SubCategoryId = p.SubCategory.Id,
                SubCategoryName = p.SubCategory.Name,
                ApplicationUserId = p.ApplicationUserId,
                ProductState = p.ProductState,
                UpdatedAt = p.UpdatedAt,
                // Load images in the same query -  EAGER LOADING via PROJECTION
                ProductImageUrls = p.ProductImages
                    .Select(pi => "uploads/products/" + pi.StoredName)
                    .ToList()
            })
            .ToListAsync();

        return ServiceResult<ProductsResponseDto>.Success(new ProductsResponseDto
        {
            Products = products,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }


    public async Task<ServiceResult<ProductResponseDto>> UpdateProductSateAsync(int productId, EnProductState newState, string adminId)
    {
        var product = await _context.Products
         .IgnoreQueryFilters()
         .Include(p => p.SubCategory) // Eager load the SubCategory
         .Include(p => p.ProductImages)
         .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
        {
            return ServiceResult<ProductResponseDto>.Failure(BusinessErrorType.ResourceNotFound, "Product Not Found");
        }

        product.ProductState = newState;
        product.StateChangedBy = adminId;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return ServiceResult<ProductResponseDto>.Failure(BusinessErrorType.ConcurrencyConflict, "The resource has been modified by another user.");
        }



        //await _context.Entry(product) //used to get an EntityEntry object that represents the
        //                            //  state and metadata of the specified entity within the DbContext.
        //.Collection(l => l.SubCategory)
        //.LoadAsync();

        var productDto = new ProductResponseDto()
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            SubCategoryId = product.SubCategory.Id,
            SubCategoryName = product.SubCategory.Name,
            ApplicationUserId = product.ApplicationUserId,
            ProductState = product.ProductState,
            UpdatedAt = product.UpdatedAt,
            StockQuantity = product.StockQuantity,
            
            ProductImageUrls = product.ProductImages
                    .Select(pi => "uploads/products/" + pi.StoredName)
                    .ToList()
        };
        return ServiceResult<ProductResponseDto>.Success(productDto);

    }

    public async Task<ServiceResult<ProductsResponseDto>> SearchProductsAsync(ProductSearchParameters parameters, string? userId, EnProductState? productState = EnProductState.Approved)
    {
       
        parameters.validateInput();


        // Build query
        var query = _context.Products.AsQueryable();

        if(productState is not null)
        {
            query = query.Where(p => p.ProductState == productState);
        }

        // Apply filters (only if provided)
        if (!string.IsNullOrWhiteSpace(parameters.Name))
        {
            query = query.Where(p => p.Name.Contains(parameters.Name) ||
                                     p.Description.Contains(parameters.Name));
        }

        if (userId is not null)
        {
            query = query.Where(p => p.ApplicationUserId == userId);
        }

        if (parameters.SubcategoryId.HasValue)
        {
            query = query.Where(p => p.SubCategoryId == parameters.SubcategoryId);
        }
        if (parameters.CreatedFrom.HasValue)
        {
            query = query.Where(p => p.UpdatedAt >= parameters.CreatedFrom.Value);
        }
        if (parameters.CreatedTo.HasValue)
        {
            query = query.Where(p => p.UpdatedAt <= parameters.CreatedTo.Value);
        }
        if (parameters.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= parameters.MinPrice.Value);
        }

        if (parameters.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= parameters.MaxPrice.Value);
        }

        // Apply sorting
        query = parameters.SortBy?.ToLower() switch
        {
            "name" => parameters.SortDescending
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name),
            "price" => parameters.SortDescending
                ? query.OrderByDescending(p => p.Price)
                : query.OrderBy(p => p.Price),
            "createddate" => parameters.SortDescending
                ? query.OrderByDescending(p => p.UpdatedAt)
                : query.OrderBy(p => p.UpdatedAt),
            _ => query.OrderByDescending(p => p.UpdatedAt) // Default sorting
        };

        // Get total count for pagination metadata
        var totalCount = await query.CountAsync();

        // Apply pagination
        var products = await query
                     .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .Select(p => new ProductResponseDto()
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        SubCategoryId = p.SubCategory.Id,
                        SubCategoryName = p.SubCategory.Name,
                        ApplicationUserId = p.ApplicationUserId,
                        ProductState = p.ProductState,
                        UpdatedAt = p.UpdatedAt,
                        // Load images in the same query 
                        ProductImageUrls = p.ProductImages
                            .Select(pi => "uploads/products/" + pi.StoredName)
                            .ToList()
                    })
                    .ToListAsync();


        var productsResponseDto = new ProductsResponseDto()
        {
            TotalCount = totalCount,
            Products = products,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / parameters.PageSize)
        };

         return ServiceResult<ProductsResponseDto>.Success(productsResponseDto);
    }

    public async Task<ServiceResult<object>> DeleteProductAsync(int productId, string userId, bool isAdmin)
    {
        var product = await _context.Products
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null)
        {
            return ServiceResult<object>.Failure(
                BusinessErrorType.ResourceNotFound,
                "Product Not Found");
        }

        if (!isAdmin)
        {
            if (product.ApplicationUserId != userId)
            {
                return ServiceResult<object>.Failure(BusinessErrorType.InsufficientPermissions, "Not authorized to delete this product");
            }
        }

        //Start Delete

        await using var transaction = await _context.Database.BeginTransactionAsync();
        List<string> productImages = product.ProductImages.Select(i => i.StoredName).ToList();

        try
        {
            // 1. Delete from database first
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            // 2. Then delete files
            foreach (var image in productImages) { 
                await _fileStorageService.DeleteFileAsync($"uploads/products/{image}");

            }

            await transaction.CommitAsync();
            return ServiceResult<object>.Success(null, "Product deleted successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Failure(BusinessErrorType.Unknown, ex.Message);
        }
    }

    public async Task<ServiceResult<ProductResponseDto>> GetProductByIdAsync(int productId, bool isAdmin, string? userId)
    {
        var product = await _context.Products.IgnoreQueryFilters()
            .Include(p=> p.SubCategory)
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id ==  productId);

        if (product == null) {

            return ServiceResult<ProductResponseDto>.Failure(BusinessErrorType.ResourceNotFound, "Product Not Found");
        }



        if (product.ProductState == EnProductState.Approved || isAdmin|| product.ApplicationUserId == userId)
        {
            var result =
            new ProductResponseDto()
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                SubCategoryId = product.SubCategory.Id,
                SubCategoryName = product.SubCategory.Name,
                ApplicationUserId = product.ApplicationUserId,
                ProductState = product.ProductState,
                UpdatedAt = product.UpdatedAt,
                // Load images in the same query 
                ProductImageUrls = product.ProductImages
                                .Select(pi => "uploads/products/" + pi.StoredName)
                                .ToList()
            };
            return ServiceResult<ProductResponseDto>.Success(result);
        }
        else
        {
            return ServiceResult<ProductResponseDto>.Failure(BusinessErrorType.InsufficientPermissions, "Not authorized to view this product");
        }


    }

    public async Task<ServiceResult<object>> UpdateProductDetailsAsync(int productId, UpdateProductDto updateProductDto, string userId)
    {

        if(updateProductDto is null)
        {
            return ServiceResult<object>.Failure(BusinessErrorType.ValidationFailed, "Empty Changes");
        }

        var product = await _context.Products.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            return ServiceResult<object>.Failure(BusinessErrorType.ResourceNotFound, "Product Not Found");
        }

        if (product.ApplicationUserId != userId) {
            return ServiceResult<object>.Failure(BusinessErrorType.InsufficientPermissions, "Not authorized to update this product");
        }


        bool stateChanged = false;

        if(updateProductDto.Name is not null)
        {
            product.Name = updateProductDto.Name;
            stateChanged = true;
        }

        if(updateProductDto.Description is not null)
        {
            product.Description = updateProductDto.Description;
            stateChanged = true;
        }
        if(updateProductDto.SubCategoryId is not null)
        {
            var subCategory = await _context.SubCategories.FindAsync(updateProductDto.SubCategoryId);
            if(subCategory is null)
            {
                return ServiceResult<object>.Failure(BusinessErrorType.ResourceNotFound, "Subcategory Not Found");
            }
            product.SubCategory = subCategory;
            product.SubCategoryId = subCategory.Id;
            stateChanged = true;
        }

        if (updateProductDto.Price is not null) {
            product.Price = updateProductDto.Price.Value;
            stateChanged = true;
        }

        if (updateProductDto.StockQuantity is not null)
        {
            product.StockQuantity = updateProductDto.StockQuantity.Value;
            stateChanged = true;
        }



        if (stateChanged)
        {
            product.ProductState = EnProductState.Pending;
            try
            {
            await _context.SaveChangesAsync();

            }
            catch (DbUpdateConcurrencyException ex)
            {
                return ServiceResult<object>.Failure(BusinessErrorType.ConcurrencyConflict, "The resource has been modified by another user.");
            }
        }

            return ServiceResult<object>.Success(null, "Product Updated Successfully");

    }


    public async Task<ServiceResult<object>> AddProductImagesAsync(int productId, string userId , List<IFormFile> newImages)
    {

        if(newImages.Count == 0)
        {
            return ServiceResult<object>.Failure(BusinessErrorType.ValidationFailed, "Please select at least one image to upload");
        }

        var product = await _context.Products.IgnoreQueryFilters().Include(p => p.ProductImages).FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null) {
            return ServiceResult<object>.Failure(BusinessErrorType.ResourceNotFound, "Product Not Found");
        }

        if(product.ApplicationUserId != userId)
        {
            return ServiceResult<object>.Failure(BusinessErrorType.InsufficientPermissions, "Not authorized to update this product");
        }

        foreach (var image in newImages)
        {
            if (!image.IsValidImageFile())
            {
                return ServiceResult<object>
                    .Failure(BusinessErrorType.InvalidImage, "Invalid Image(s)");

            }
        }


     
        var imagesInfo = new Dictionary<string, IFormFile>();

        foreach (var image in newImages)
        {
            imagesInfo.Add($"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}", image);
        }


        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var image in imagesInfo)
            {
                var filePath = await _fileStorageService.UploadFileAsync(image.Value, "uploads/products", image.Key);

                if (filePath.IsNullOrEmpty())
                {
                    return ServiceResult<object>.Failure(BusinessErrorType.Unknown, "Can Not Upload Image");
                }
            }


            foreach (var key in imagesInfo.Keys)
            {
                product.ProductImages.Add(new ProductImage()
                {
                    StoredName = key,
                });
            }


            await _context.SaveChangesAsync();


            await transaction.CommitAsync();
            return ServiceResult<object>.Success(null, "Product images uploaded successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            foreach (var image in imagesInfo)
            {
                var filePath = await _fileStorageService.UploadFileAsync(image.Value, "uploads/products", image.Key);

                await _fileStorageService.DeleteFileAsync("uploads/products" + image.Key);
            }
            return ServiceResult<object>.Failure(BusinessErrorType.Unknown, ex.Message);
           
        }
    }

    public async Task<ServiceResult<object>> DeleteProductImageAsync(int productId, string userId, string imageId)
    {
        var product = await _context.Products.IgnoreQueryFilters()
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null)
        {
            return ServiceResult<object>.Failure(
                BusinessErrorType.ResourceNotFound,
                "Product Not Found");
        }

            if (product.ApplicationUserId != userId)
            {
                return ServiceResult<object>.Failure(BusinessErrorType.InsufficientPermissions, "Not authorized to update this product");
            }


        var targetImage = product.ProductImages.FirstOrDefault(image => image.StoredName == imageId);
        if (targetImage is null) { 
                return ServiceResult<object>.Failure(BusinessErrorType.ResourceNotFound, "Image not found");

        }

        //Start Delete

        await using var transaction = await _context.Database.BeginTransactionAsync();
        List<string> productImages = product.ProductImages.Select(i => i.StoredName).ToList();

        try
        {
            // 1. Delete from database first
            product.ProductImages.Remove(targetImage);
            await _context.SaveChangesAsync();

            // 2. Then delete file
                await _fileStorageService.DeleteFileAsync($"uploads/products/{targetImage.StoredName}");

            

            await transaction.CommitAsync();
            return ServiceResult<object>.Success(null, "Product image deleted successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ServiceResult<object>.Failure(BusinessErrorType.Unknown, ex.Message);
        }
    }
}

