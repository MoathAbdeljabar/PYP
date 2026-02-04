using System.Security.Claims;
using Identity.API.Helpers;
using Identity.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyApp.Application.Product.DTOs.Requests;
using MyApp.Application.Product.Interfaces;
using MyApp.Domain.Enums;

namespace MyApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("PublicPerIp")] // default for public endpoints
    public class ProductController : ControllerBase
    {

        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }


        [Authorize(Roles = "Store,Admin")]
        [EnableRateLimiting("PerUser")]
        [HttpPost]
        public async Task<IActionResult> CreateNewProduct([FromForm] CreateProductDto newProductDto)
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

            if (userId == null) {
                return BadRequest("User Not Found");
            }


            //---------------------------
            var result = await _productService.CreateNewProductAsync(userId, newProductDto);
            return result.ToActionResult();


        }

        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("PerUser")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingProducts([FromQuery] string? user, [FromQuery] short pageNumber = 1, [FromQuery] short pageSize = 10)
        {
            var result = await _productService.GetPendingProductsAsync(user, pageNumber, pageSize);
            return result.ToActionResult();
        }




        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("PerUser")]
        [HttpPatch("{productId}/state")]
        public async Task<IActionResult> UpdateProductSate(int productId, [FromBody] EnProductState newState)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var result = await _productService.UpdateProductSateAsync(productId, newState, adminId);
           
            return result.ToActionResult();
        }


        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] ProductSearchParameters parameters, [FromQuery] string? userId)
        {
            var result = await _productService.SearchProductsAsync(parameters, userId);
            return result.ToActionResult();
        }


        [Authorize]
        [EnableRateLimiting("PerUser")]
        [HttpDelete("{productId}")]
        public async Task<IActionResult> DeleteProduct(int productId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isAdmin = User.IsInRole(EnRoles.Admin.ToString());

            var result = await _productService.DeleteProductAsync(productId, userId, isAdmin);


            if (!result.IsSuccess)
                return result.ToActionResult();
            else
                return NoContent();

        }


        [Authorize(Roles = "Store")]
        [EnableRateLimiting("PerUser")]
        [HttpGet("my-products")]
        public async Task<IActionResult> GetMyProducts([FromQuery] ProductSearchParameters parameters)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _productService.SearchProductsAsync(parameters, userId, null);
            return result.ToActionResult();

        }

        [HttpGet]
        public async Task<IActionResult> GetProductById(int productId)
        {
            bool isAdmin = false;
            if (User.IsInRole("Admin"))
                isAdmin = true;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            

            var result = await _productService.GetProductByIdAsync(productId, isAdmin,  userId);
            return result.ToActionResult();

        }


        // 1. PATCH: Update product data
        [Authorize(Roles = "Store")]
        [EnableRateLimiting("PerUser")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto updateProductDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _productService.UpdateProductDetailsAsync(id, updateProductDto, userId);

            if (!result.IsSuccess)
                return result.ToActionResult();
            else
                return NoContent();

        }

        //// 2. POST: Upload new images
        [Authorize(Roles = "Store")]
        [EnableRateLimiting("PerUser")]
        [HttpPost("{productId}/images")]
        public async Task<IActionResult> AddProductImages(int productId, [FromForm] List<IFormFile> newImages)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _productService.AddProductImagesAsync(productId, userId, newImages);

            if (!result.IsSuccess)
                return result.ToActionResult();
            else
                return NoContent();
        }

        //// 3. DELETE: Remove specific image
        [Authorize(Roles = "Store")]
        [EnableRateLimiting("PerUser")]
        [HttpDelete("{productId}/images/{imageId}")]
        public async Task<IActionResult> DeleteProductImage(int productId, string imageId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            var result = await _productService.DeleteProductImageAsync(productId, userId, imageId);
            if (!result.IsSuccess)
                return result.ToActionResult();
            else
                return NoContent();

        }
    }
}
