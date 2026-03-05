namespace Identity.Application.Identity.DTOs.Requests;

public class ResetPasswordRequestDto
{
    public string Token { get; set; }
    public string UserId { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmPassword { get; set; }
}