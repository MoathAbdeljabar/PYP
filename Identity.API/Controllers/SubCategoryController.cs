using Identity.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyApp.Application.Product.DTOs.Requests;
using MyApp.Application.Product.Interfaces;
using MyApp.Application.Product.Services;

namespace MyApp.API.Controllers
{
    [Route("api/[controller]")]
    [EnableRateLimiting("PublicPerIp")] // default for public endpoints
    [ApiController]
    public class SubCategoryController : ControllerBase
    {
        private readonly ISubCategoryService _subCategoryService;
        public SubCategoryController(ISubCategoryService subCategoryService) { 

            _subCategoryService = subCategoryService;

        }

        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("PerUser")]
        [HttpPost("~/api/Category/{categoryId}/SubCategory")]
        public async Task<IActionResult> CreateNewSubCategory(int categoryId,[FromForm] CreateProductTypeDto newSubCategoryDto)
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
            var result = await _subCategoryService.CreateNewSubCategoryAsync(categoryId, newSubCategoryDto);


            //return result.ToActionResult();

            if (!result.IsSuccess)
                return result.ToActionResult();
            else
                return CreatedAtAction(
               nameof(GetSubCategoryById),        // Action name to generate URL
               new { subCategoryId = result.Data.Id },   // Route parameters
               result.Data                 // Response body
           );
        }


        [HttpGet]
        public async Task<IActionResult> GetAllSubCategories([FromQuery] int categoryId)
        {
            var result = await _subCategoryService.GetAllSubCategoriesAsync(categoryId);
            return result.ToActionResult();
        }


        [HttpGet("{subCategoryId}")]
        public async Task<IActionResult> GetSubCategoryById(int subCategoryId)
        {
            var result = await _subCategoryService.GetSubCategoryByIdAsync(subCategoryId);
            return result.ToActionResult();
        }



        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("PerUser")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateSubCategory(int id, [FromForm] UpdateProductTypeDto updateSubCategoryDto)
        {

            var result = await _subCategoryService.UpdateSubCategoryAsync(id, updateSubCategoryDto);
            return result.ToActionResult();

        }


        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("PerUser")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubCategory(int id)
        {

            var result = await _subCategoryService.DeleteSubCategoryAsync(id);

            if (!result.IsSuccess)
                return result.ToActionResult();
            else
                return NoContent();

        }




    }
}
