using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyApp.Application.Cart.DTOs.Requests;
using MyApp.Application.Cart.DTOs.Responses;
using MyApp.Application.Shared.DTOs;

namespace MyApp.Application.Cart.Interfaces;
    public interface ICartService
    {
    Task<ServiceResult<CartProductResponse>> AddToCartAsync(string userId, AddToCartRequest addToCartRequest);
    Task<ServiceResult<CartProductResponse>> UpdateCartItemAsync(string userId, UpdateCartItemRequest request);

    Task<ServiceResult<CartItemsResponse>> GetCartItemsAsync(string userId, PaginatedRequest request);
}

