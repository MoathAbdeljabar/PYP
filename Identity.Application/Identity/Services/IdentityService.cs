using Identity.Application.Identity.DTOs.Requests;
using Identity.Application.Identity.DTOs.Responses;
using Identity.Application.Identity.Interfaces;
using Identity.Data;
using Identity.Data.Models;
using Identity.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MyApp.Application.Shared;
using QRCoder;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using static QRCoder.PayloadGenerator;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Identity.Application.IdentityService;
public partial class IdentityService : IIdentityService
{


    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ApplicationDbContext _context;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        ApplicationDbContext context) {
        _userManager = userManager;
        _emailService = emailService;
        _signInManager = signInManager;
        _configuration = configuration;
        _context = context;
    }




    public async Task<ServiceResult<CreatedUserDto>> SignUpAsync(SignupRequestDto userInfo, EnRoles userRole = EnRoles.User)
    {

        if (await _userManager.FindByEmailAsync(userInfo.Email) != null)
        {

            return ServiceResult<CreatedUserDto>.Failure(BusinessErrorType.EmailAlreadyExists, "This email address is already associated with an existing account. " +
            "Please use a different email address or sign in to your existing account.");

        }


        if (!IsOver18(userInfo.BirthDate))
        {
            return ServiceResult<CreatedUserDto>.Failure(BusinessErrorType.CanNotCreateUser, "You must be at least 18 years old");
        }


        var user = new ApplicationUser
        {
            UserName = userInfo.Email,
            Email = userInfo.Email,
            FirstName = userInfo.FirstName,
            LastName = userInfo.LastName,
            BirthDate = userInfo.BirthDate,
            Gender = userInfo.Gender,

        };




        await using var transaction = await _context.Database.BeginTransactionAsync();

        IdentityResult result = await _userManager.CreateAsync(user, userInfo.Password);

        if (result.Succeeded)
        {

            // Assign default role 
            var addToRoleResult = await _userManager.AddToRoleAsync(user, userRole.ToString());

            if (!addToRoleResult.Succeeded) {

                await transaction.RollbackAsync();

                var errors = addToRoleResult.Errors.Select(e => e.Description);
                ;
                return ServiceResult<CreatedUserDto>.Failure(
                    errorType: BusinessErrorType.CanNotCreateUser,
                    message: "Role Error " + string.Join(", ", errors)
                );
            }

            /* -- Later One user may have multiple roles,

            // If additional roles are provided and user has permission, assign them
            if (model.AdditionalRoles != null && User.IsInRole("Admin"))
            //User.IsInRole("Admin") refers to the currently authenticated user (the user making the API request)
            {
                foreach (var role in model.AdditionalRoles)
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
            }

            */



            var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = HttpUtility.UrlEncode(emailConfirmationToken);

            var confirmationLink = $"https://localhost:7244/api/Auth/confirm-email?userId={user.Id}&token={encodedToken}";
            /*
            Option 1: Direct Link to Backend Endpoint (Recommended for Simple Apps)
            The user clicks a link that goes directly to your backend API:
            Email contains: https://yourapi.com/api/auth/confirm-email?userId=123&token=abc123

            Backend uses [HttpGet] and redirects to frontend after processing.

            Option 2: Link to Frontend + API Call (Recommended for SPAs)  
            Email contains: https://yourapp.com/confirm-email?userId=123&token=abc123
            Frontend extracts parameters from URL and makes POST request to backend API:
            [HttpPost("confirm-email")]
            public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
            */
            bool isSent = await _emailService.SendEmailAsync(
                    user.Email,
                    "Confirm Your Email",
                    $"Please confirm your email by clicking <a href='{confirmationLink}'>here</a>");

            if (!isSent)
            {
                await transaction.RollbackAsync();

                return ServiceResult<CreatedUserDto>.Failure(
                    errorType: BusinessErrorType.CanNotCreateUser,
                    message: "Can Not Send Email "
                );
            }
            await transaction.CommitAsync();

            return ServiceResult<CreatedUserDto>.Success(
                data: new CreatedUserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.UserName,

                },
              message: "User registered successfully. Please check your email to confirm your account."
            );
        }
        else
        {


            var errors = result.Errors.Select(e => e.Description).ToList();
            await transaction.RollbackAsync();
            return ServiceResult<CreatedUserDto>.Failure(
                errorType: BusinessErrorType.CanNotCreateUser,
                message: "User creation failed: " + string.Join(", ", errors)
            );
        }
    }


    public async Task<ServiceResult<object>> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return ServiceResult<object>.Failure(BusinessErrorType.UserNotFound, "User Not Found");
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);

        if (result.Succeeded)
        {
            return ServiceResult<object>.Success(null, "Email confirmed successfully");
        }
        else
        {
            return ServiceResult<object>.Failure(BusinessErrorType.InvalidToken, "Email confirmation failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }


    //-------------------------------------
    //Login 
    //-------------------------------------


    public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        // 1. Find user
        var user = await _userManager.FindByEmailAsync(request.Email);

        // 2. Generic error for user not found - prevent enumeration
        if (user == null)
        {
            return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.InvalidCredentials, "Invalid Credentials");
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            // This will increment the failed login count
            await _userManager.AccessFailedAsync(user);
            return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.InvalidCredentials, "Invalid Credentials");

        }

        // 3. **CRITICAL: Account Status Checks BEFORE 2FA**
        if (!user.EmailConfirmed)
        {
            return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.EmailNotConfirmed, "Please confirm your email address");
        }

        if (user.LockoutEnabled && await _userManager.IsLockedOutAsync(user))
        {
            return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.AccountLocked, "Account is locked. Please try again later");
        }


        // 4. Check if 2FA is enabled for this user
        var has2fa = await _userManager.GetTwoFactorEnabledAsync(user);
        if (has2fa)


        {
            var tempToken = GenerateTempToken(user.Id, "2fa_verification");// Generate secure JWT token for 2FA

            var result = new AuthResponseDto
            { requiresTwoFactor = true,
                MFAData = new MFAData
                {
                    UserId = user.Id,
                    TempToken = tempToken
                }
            };
            return ServiceResult<AuthResponseDto>.Success(result, "Please Enter Your 2FA Code");



            //username, password, and a second-factor code in one request
            // if (!(request.ValidationToken != null && await _userManager.VerifyTwoFactorTokenAsync(
            //user,
            //_userManager.Options.Tokens.AuthenticatorTokenProvider,

            //request.ValidationToken))){
            //     return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.InvalidCredentials, "Invalid Credentials");
            // }        
        }

        var loginTokens = await GenerateLoginTokens(user);
        return ServiceResult<AuthResponseDto>.Success(loginTokens, "Login successful");
    }


    public async Task<ServiceResult<AuthResponseDto>> VerifyTwoFactorAsync(VerifyTwoFactorRequestDto verifyTwoFactorRequestDto)
    {

        bool isValidTempToken = ValidateTempToken(verifyTwoFactorRequestDto.TempToken, out string userId);
        if (!isValidTempToken)
        {
            return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.InvalidToken, "Invalid Token");

        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.UserNotFound, "User Not Found");
        }


        var isVerifyCodeValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
                verifyTwoFactorRequestDto.VerifyCode);
        if (!isVerifyCodeValid) {
            return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.InvalidCredentials, "Invalid 2FA Code");

        }

        var loginTokens = await GenerateLoginTokens(user);
        return ServiceResult<AuthResponseDto>.Success(loginTokens, "Login successful");

    }

    private async Task<AuthResponseDto> GenerateLoginTokens(ApplicationUser user)
    {
        // Reset failed count on successful password verification
        await _userManager.ResetAccessFailedCountAsync(user);

        // Generate tokens
        var accessToken = await _GenerateJwtTokenAsync(user);
        var refreshToken = GenerateRefreshToken();

        // Store refresh token in database
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7); // Refresh token valid for 7 days
        await _userManager.UpdateAsync(user);


        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes())
        };
    }




    //--------------------------------------
    //Refersh Token
    //--------------------------------------

    public async Task<ServiceResult<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        try
        {
            // Get user from expired token
            var principal = GetPrincipalFromExpiredToken(request.AccessToken);
            //Principal = The authenticated user's identity + claims (like a user passport)
            //Principal = User's identity extracted from the expired token
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //FindFirst(ClaimTypes.NameIdentifier) - Gets the User ID from the token claims


            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.InvalidToken, "Invalid token");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.InvalidToken, "User not found");
            }

            // Validate refresh token
            if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiry <= DateTime.UtcNow)
            {
                return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.InvalidToken, "Invalid or expired refresh token");
            }

            // Generate new access token
            var newAccessToken = await _GenerateJwtTokenAsync(user);

            // Generate new refresh token (rotation) but keep original expiry
            var newRefreshToken = GenerateRefreshToken();

            // Update refresh token in database - KEEP ORIGINAL EXPIRY
            user.RefreshToken = newRefreshToken;
            // RefreshTokenExpiry stays the same (don't extend it)
            await _userManager.UpdateAsync(user);

            var response = new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes())
            };

            return ServiceResult<AuthResponseDto>.Success(response, "Token refreshed successfully");
        }
        catch (SecurityTokenException)
        {
            return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.InvalidToken, "Invalid token");
        }
    }

    //--------------------------------------
    //Logout
    //--------------------------------------

    public async Task<ServiceResult<bool>> LogoutAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            // Revoke refresh token
            user.RefreshToken = null;
            user.RefreshTokenExpiry = DateTime.MinValue;
            await _userManager.UpdateAsync(user);
        }
        else
        {
            return ServiceResult<bool>.Failure(BusinessErrorType.InvalidToken, "Can not logout");
        }

        return ServiceResult<bool>.Success(true, "Logged out successfully");
    }



    //--------------------------------------
    //MFA Setup
    //--------------------------------------
    public async Task<ServiceResult<string>> MFASetupAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<string>.Failure(BusinessErrorType.InvalidToken, "Invalid Token");
        }

        // Check if MFA is already enabled
        if (await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return ServiceResult<string>.Failure(BusinessErrorType.OperationNotAllowed, "Multi-factor authentication is already enabled for your account");

        }
        var setupKey = await _userManager.ResetAuthenticatorKeyAsync(user); //ensures a key exists (generates if missing)
        //If key already exists: Invalidates the old key and generates a new one

        if (setupKey.Succeeded) {
            var authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user); // returns null if no key has been generated yet

            // QR code
            var qrCodeBase64 = GenerateQrCodeBase64(authenticatorKey, user.Email);

            //return ServiceResult<string>.Success(authenticatorKey, "successes");
            return ServiceResult<string>.Success(qrCodeBase64, "QR code generated");

        }
        else
        {
            var errors = setupKey.Errors.Select(e => e.Description);

            return ServiceResult<string>.Failure(
                    errorType: BusinessErrorType.AuthenticatorSetupFailed,
                    message: string.Join(", ", errors)
                );
        }

    }



    public async Task<ServiceResult<bool>> MFASetupVerifyAsync(string userId, string validationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<bool>.Failure(BusinessErrorType.InvalidToken, "Invalid Token");
        }

        // Check if MFA is already enabled
        if (await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return ServiceResult<bool>.Failure(BusinessErrorType.OperationNotAllowed, "Multi-factor authentication is already enabled for your account");

        }

        if (await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider, // which type of two-factor authentication you're trying to verify against
                                                                    //_userManager.Options.Tokens.AuthenticatorTokenProvider -- Typically resolves to: "Authenticator"
                                                                    // _userManager.Options.Tokens.PhoneTokenProvider -- SMS/Phone Codes  
            validationToken))
        {
            await _userManager.SetTwoFactorEnabledAsync(user, true);
            //await _userManager.SetTwoFactorEnabledAsync(user, false);//Disable 2FA

            return ServiceResult<bool>.Success(true, "2FA Successfully Configured");

        }
        else
        {
            return ServiceResult<bool>.Failure(BusinessErrorType.ValidationFailed, "Invalid MFA Verification Token");
        }


    }







    //----------------------------------
    //Rest Password 
    //----------------------------------


    public async Task<ServiceResult<object>> ForgotPasswordAsync(string emailAddress){


        var user = await _userManager.FindByEmailAsync(emailAddress);
        if (user == null)
        {
            return ServiceResult<object>.Failure(BusinessErrorType.UserNotFound, "User Not Found");
        }
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        //Link to Frontend + API Call (ONLY VALID APPROACH)

        //var resetLink = $"{_configuration["ClientApp:Url"]}/reset-password?token={WebUtility.UrlEncode(resetToken)}&userId={user.Id}";
        var resetLink = $"https://myapp..etc/ResettPassword?userId={user.Id}&token={WebUtility.UrlEncode(resetToken)}";

 

        bool isSent = await _emailService.SendEmailAsync(
            user.Email,
            "Rest Your Password",
            $"Please reset your password by clicking here: <a href='{resetLink}'>here</a>");

                if (!isSent)
                

                return ServiceResult<object>.Failure(
                    errorType: BusinessErrorType.CanNotCreateUser,
                    message: "Can Not Send Email "
                );


        return ServiceResult<object>.Success("Email Sent Successfully");


        }



    public async Task<ServiceResult<object>> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        // Validate request
        if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.UserId))
        {
            return ServiceResult<object>.Failure(BusinessErrorType.ValidationFailed,"Token and User ID are required");
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            return ServiceResult<object>.Failure(BusinessErrorType.ValidationFailed, "Passwords do not match");
        }

        // Find user
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return ServiceResult<object>.Failure(BusinessErrorType.UserNotFound, "Invalid Data");
        }

        // URL decode the token
        var decodedToken = WebUtility.UrlDecode(request.Token);

        // Reset password
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

        if (result.Succeeded)
        {
            return ServiceResult<object>.Success(null,"Password has been reset successfully");
        }

        return ServiceResult<object>.Failure(BusinessErrorType.Unknown,"Failed to reset password" + result.Errors.Select(e => e.Description));
    }





}



