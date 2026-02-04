using Identity.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyApp.Application.Product.DTOs.Requests;
using MyApp.Application.Product.DTOs.Responses;
using MyApp.Application.Product.Interfaces;
using MyApp.Domain.Models;
using static System.Net.Mime.MediaTypeNames;

namespace MyApp.API.Controllers
{
    [Route("api/[controller]")]
    [EnableRateLimiting("PublicPerIp")] // default for public endpoints
    [ApiController]
    public class ProductCategoryController : ControllerBase
    {
        private readonly IProudctCategoryService _proudctCategoryService;

        public ProductCategoryController(IProudctCategoryService proudctCategoryService) {
            _proudctCategoryService = proudctCategoryService;
        }



        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("PerUser")]
        [HttpPost]
        public async Task<IActionResult> CreateNewCategory([FromForm] CreateProductTypeDto productCategoryDto)
        {
            if (!ModelState.IsValid)
            {
                // Extract validation errors
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                // Return 400 with structured error response
                return BadRequest(new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = "Validation failed " + string.Join(", ", errors),
                    Data = null

                });
            }


            var result = await _proudctCategoryService.CreateNewCategoryAsync(productCategoryDto);

            if (!result.IsSuccess)
                return result.ToActionResult();
            else
                return CreatedAtAction(
               nameof(GetCategoryById),        // Action name to generate URL
               new { id = result.Data.Id },   // Route parameters
               result.Data                 // Response body
           );

            /*
            The CreatedAtAction method automatically adds a Location header to the response:
            Location: https://yourapi.com/api/categories/123
            This header tells the client where to find the newly created resource.
            */
        }



        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            var result = await _proudctCategoryService.GetAllCategoriesAsync();
            return result.ToActionResult();

        }

        [HttpGet("Categories/{id}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var result = await _proudctCategoryService.GetCategoryByIdAsync(id);
            return result.ToActionResult();

        }



        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("PerUser")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromForm] UpdateProductTypeDto productCategoryDto)
        {

            var result = await _proudctCategoryService.UpdateCategoryAsync(id, productCategoryDto);
            return result.ToActionResult();

        }


        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("PerUser")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {

            var result = await _proudctCategoryService.DeleteCategoryAsync(id);

            if (!result.IsSuccess)
                return result.ToActionResult();
            else
                return NoContent();
               
        }

    }
}
