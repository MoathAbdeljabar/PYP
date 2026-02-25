using Identity.Data;
using Microsoft.EntityFrameworkCore;
using MyApp.Application.Cart.DTOs.Requests;
using MyApp.Application.Cart.DTOs.Responses;
using MyApp.Application.Cart.Interfaces;
using MyApp.Application.Product.DTOs.Responses;
using MyApp.Application.Shared;
using MyApp.Application.Shared.DTOs;
using MyApp.Domain.Enums;
using MyApp.Domain.Models;


namespace MyApp.Application.Cart.Services;
public class CartService : ICartService
{
    private readonly ApplicationDbContext _context;

    public CartService(ApplicationDbContext context)
    {
        _context = context;
    }

    private async Task<CartEntity> _GetCartAsync(string userId)
    {
        var cart = await _context.Carts.FirstOrDefaultAsync(cart => cart.ApplicationUserId == userId);
        if(cart != null)
        {
            return cart;
        }


        if (!await _context.Users.AnyAsync(user => user.Id == userId))
        {
            throw new Exception("User Does Not Exist");
        }


        cart = new CartEntity()
        {
            ApplicationUserId = userId,
            Products = new List<CartProduct>()
        };

        await _context.Carts.AddAsync(cart);
        await _context.SaveChangesAsync();
        return cart;
    }
    public async Task<ServiceResult<CartProductResponse>> AddToCartAsync(string userId, AddToCartRequest addToCartRequest)
    {
        //check if current user exist
        if (!await _context.Users.AnyAsync(user => user.Id == userId))
        {
            return ServiceResult<CartProductResponse>.Failure(BusinessErrorType.UserNotFound, "User Not Found");
        }

        var cart = await _context.Carts.FirstOrDefaultAsync(cart => cart.ApplicationUserId == userId);
        if (cart == null) {

            try
            {
                cart = await _GetCartAsync(userId);

            }
            catch (DbUpdateConcurrencyException ex)
            {
                return ServiceResult< CartProductResponse>.Failure(BusinessErrorType.ConcurrencyConflict, "The cart state has been changed, try again.");
            }
            catch (Exception ex) {
                return ServiceResult<CartProductResponse>.Failure(BusinessErrorType.Unknown, ex.Message);

            }
        }


        var product = await _context.Products.FirstOrDefaultAsync(product => product.Id == addToCartRequest.ProductId);

        if (product == null)
        {
            return ServiceResult<CartProductResponse>.Failure(BusinessErrorType.ResourceNotFound, "Product Not Found");
        }


        CartProduct cartProduct = new CartProduct()
        {
            Quantity = addToCartRequest.Quantity,
            CartId = cart.Id,
            ProductId = addToCartRequest.ProductId,
            Product = product,
            Cart = cart,
        };

        if(product.StockQuantity < cartProduct.Quantity)
        {
            return ServiceResult<CartProductResponse>.Failure(BusinessErrorType.OperationNotAllowed, $"Only {product.StockQuantity} items left in stock");

        }

        var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.CartProducts.AddAsync(cartProduct);
            await _context.SaveChangesAsync();

           await transaction.CommitAsync();

            var cartProductResponse = new CartProductResponse()
            {
                Id = cartProduct.Id,
                Quantity = cartProduct.Quantity,
                Product = new ProductResponseDto
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
                }
            };
            return ServiceResult<CartProductResponse>.Success(cartProductResponse, "Added to cart!");
        }
        catch (Exception ex)
        {
            try
            {
                await transaction.RollbackAsync();
              
            }
            catch (Exception exL2)
            {

                // Log rollback failure, but return original error
            }
            return ServiceResult<CartProductResponse>.Failure(BusinessErrorType.Unknown, ex.Message);

        }
    }



    public async Task<ServiceResult<CartProductResponse>> UpdateCartItemAsync(string userId, UpdateCartItemRequest request)
    {
        //Check if cart item exist
        var cartItem = await _context.CartProducts.FindAsync(request.CartItemId);

        if(cartItem is null)
        {
            return ServiceResult<CartProductResponse>.Failure(BusinessErrorType.ResourceNotFound, "Cart Item Not Found");
        }

        var cart = await _GetCartAsync(userId);

        //Check that cart item belong to the cart of the current user
        if (cart is null || cartItem.CartId != cart.Id) { 
            return ServiceResult<CartProductResponse>.Failure(BusinessErrorType.ResourceNotFound, "Cart Item Not Found");
        }

        //Check if the product still exist in 
        var product = await _context.Products.FirstOrDefaultAsync(product => product.Id == cartItem.ProductId);

        if (product == null)
        {
            return ServiceResult<CartProductResponse>.Failure(BusinessErrorType.ResourceNotFound, "Product Not Found");
        }

        //Update The Quantity
        cartItem.Quantity = request.Quantity;   


        if (product.StockQuantity < request.Quantity)
        {
            return ServiceResult<CartProductResponse>.Failure(BusinessErrorType.OperationNotAllowed, $"Only {product.StockQuantity} items left in stock");

        }

        var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await transaction.CommitAsync();
            await _context.SaveChangesAsync();
            var cartProductResponse = new CartProductResponse()
            {
                Id = cartItem.Id,
                Quantity = cartItem.Quantity,
                Product = new ProductResponseDto
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
                }
            };

            return ServiceResult<CartProductResponse>.Success(cartProductResponse, "Added to cart!");
        }
        catch (Exception ex)
        {
            try
            {
                await transaction.RollbackAsync();

            }
            catch (Exception exL2)
            {

                // Log rollback failure, but return original error
            }
            return ServiceResult<CartProductResponse>.Failure(BusinessErrorType.Unknown, ex.Message);

        }
    }


   public async Task<ServiceResult<CartItemsResponse>> GetCartItemsAsync(string userId, PaginatedRequest request)
    {
        request.validateInput();

        var cart = await _GetCartAsync(userId);

        // Build query
        var query = _context.CartProducts.AsQueryable().Where(c => c.CartId == cart.Id);

   
        // Get total count for pagination metadata
        var totalCount = await query.CountAsync();

        // Apply pagination
        var cartItems = await query
            .Include(cp => cp.Product) // Explicitly include product
            .Where(cp => cp.Product.ProductState == EnProductState.Approved)
                     .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(cartItem => new CartProductResponse()
                    {
                         Id = cartItem.Id,
                         Quantity = cartItem.Quantity,
                        Product = new ProductResponseDto
                        {
                            Id = cartItem.Product.Id,
                            Name = cartItem.Product.Name,
                            Description = cartItem.Product.Description,
                            Price = cartItem.Product.Price,
                            SubCategoryId = cartItem.Product.SubCategory.Id,
                            SubCategoryName = cartItem.Product.SubCategory.Name,
                            ApplicationUserId = cartItem.Product.ApplicationUserId,
                            ProductState = cartItem.Product.ProductState,
                            UpdatedAt = cartItem.Product.UpdatedAt,
                            // Load images in the same query 
                            ProductImageUrls = cartItem.Product.ProductImages
                             .Select(pi => "uploads/products/" + pi.StoredName)
                             .ToList()
                        }
                    })
                    .ToListAsync();

 

        var removedItemsCount = await  _context.CartProducts
            .Where(c => c.CartId == cart.Id)
            .Where(c => c.CartId == cart.Id)
            .Where(cp => cp.Product.ProductState != EnProductState.Approved).CountAsync(); 

        var cartItemsResponse = new CartItemsResponse()
        {
            CartProducts = cartItems,
            RemovedItemsCount = removedItemsCount,
            TotalCount =  totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };
     

        return ServiceResult<CartItemsResponse>.Success(cartItemsResponse);
    }
}

/*
 * For Orders
============================= 
Initial stock: 10 units
============================= 

Before payment: 
  - Reserve: Stock = 9 (temporarily), ReservedCount = 1

On payment success:
  - Keep stock at 9, clear reservation flag

On payment failure:
  - Release: Stock = 10, ReservedCount = 0
*/
