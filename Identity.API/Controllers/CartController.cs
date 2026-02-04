using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyApp.Application.Cart.Interfaces;
using MyApp.Application.Cart.DTOs.Requests;
using Identity.API.Helpers;
using MyApp.Application.Product.DTOs.Requests;
using MyApp.Application.Shared.DTOs;

namespace MyApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [Authorize(Roles = "User,Admin")]
        [EnableRateLimiting("PerUser")]
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest addToCartRequest)
      
        {

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            //------------------------

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = "Validation failed " + string.Join(", ", errors),
                    Data = null

                });
            }

            if (userId == null)
            {
                return BadRequest("User Not Found");
            }

            //---------------------------
            var result = await _cartService.AddToCartAsync(userId, addToCartRequest);
            return result.ToActionResult();

        }


        [Authorize(Roles = "User,Admin")]
        [EnableRateLimiting("PerUser")]
        [HttpPatch("items/{cartItemId}")]
        public async Task<IActionResult> UpdateCartItem(UpdateCartItemRequest updateCartItemRequest)
        {

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            //------------------------

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = "Validation failed " + string.Join(", ", errors),
                    Data = null

                });
            }

            if (userId == null)
            {
                return BadRequest("User Not Found");
            }

            //---------------------------
            var result = await _cartService.UpdateCartItemAsync(userId, updateCartItemRequest);
            return result.ToActionResult();

        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetCartItems([FromQuery] PaginatedRequest  request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _cartService.GetCartItemsAsync(userId, request);
            return result.ToActionResult();
        }

    }
}



