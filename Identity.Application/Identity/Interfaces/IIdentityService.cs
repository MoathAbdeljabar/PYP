using Identity.Application.Identity.DTOs.Requests;
using Identity.Application.Identity.DTOs.Responses;
using Identity.Domain.Enums;

namespace Identity.Application.Identity.Interfaces;
    public interface IIdentityService
    {
    Task<ServiceResult<CreatedUserDto>> SignUpAsync(SignupRequestDto userInfo, EnRoles userRole = EnRoles.User);
    Task<ServiceResult<object>> ConfirmEmailAsync(ConfirmEmailRequest request);

    Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequestDto request);

    Task<ServiceResult<bool>> LogoutAsync(string userId);
    Task<ServiceResult<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request);

    Task<ServiceResult<string>> MFASetupAsync(string userId);
    Task<ServiceResult<bool>> MFASetupVerifyAsync(string userId, string validationToken);

    Task<ServiceResult<AuthResponseDto>> VerifyTwoFactorAsync(VerifyTwoFactorRequestDto verifyTwoFactorRequestDto);

    Task<ServiceResult<object>> ForgotPasswordAsync(string emailAddress);
    Task<ServiceResult<object>> ResetPasswordAsync(ResetPasswordRequestDto request);
}

