using Identity.API.Helpers;
using Identity.Application.Identity.DTOs.Requests;
using Identity.Application.Identity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Identity.Domain.Enums;

namespace Identity.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly IIdentityService _identityService;

        public AuthController(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("store-registration")]
        public async Task<IActionResult> StoreRegistration(SignupRequestDto userInfo)
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


            var result = await _identityService.SignUpAsync(userInfo, EnRoles.Store);
            return result.ToActionResult();


        }


        [HttpPost("signup")]
        public async Task<IActionResult> Signup(SignupRequestDto userInfo)
        {

            if (!ModelState.IsValid) {
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


            var result = await _identityService.SignUpAsync(userInfo, EnRoles.Admin);
            return result.ToActionResult();


        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return BadRequest(new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = "User ID and token are required",
                    Data = null
                });
            }

            var request = new ConfirmEmailRequest
            {
                UserId = userId,
                Token = token
            };

            var result = await _identityService.ConfirmEmailAsync(request);
            return result.ToActionResult();
        }



        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
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

            var result = await _identityService.LoginAsync(request);
            return result.ToActionResult();
        }


        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = "Invalid request",
                    Data = null
                });
            }

            var result = await _identityService.RefreshTokenAsync(request);
            return result.ToActionResult();
        }


        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = "Invalid user",
                    Data = null
                });
            }

            var result = await _identityService.LogoutAsync(userId);
            return result.ToActionResult();
        }


        [HttpGet("setup-mfa")]
        [Authorize]

        public async Task<IActionResult> MFASetup()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = "Invalid user",
                    Data = null
                });
            }

            var result = await _identityService.MFASetupAsync(userId);
            return result.ToActionResult();
        }

        [HttpPost("setup-mfa-verify")]
        [Authorize]

        public async Task<IActionResult> MFASetupVerify([FromBody] string validationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = "Invalid user",
                    Data = null
                });
            }

            var result = await _identityService.MFASetupVerifyAsync(userId, validationToken);
            return result.ToActionResult();
        }


        [HttpPost("verify-2fa")]
        public async Task<IActionResult> VerifyTwoFactor(VerifyTwoFactorRequestDto verifyTwoFactorRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = "Invalid request",
                    Data = null
                });
            }

            var result = await _identityService.VerifyTwoFactorAsync(verifyTwoFactorRequestDto);
            return result.ToActionResult();
        }



        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgotPasswor([FromBody] string emailAddress)
        {
            
            var result = await _identityService.ForgotPasswordAsync(emailAddress);
            return result.ToActionResult();
        }

  

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            var result = await _identityService.ResetPasswordAsync(request);
            return result.ToActionResult();
        }
    }
}
