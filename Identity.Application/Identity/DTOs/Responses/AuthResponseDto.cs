namespace Identity.Application.Identity.DTOs.Responses;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiry { get; set; }


    public bool requiresTwoFactor { get; set; } = false;
   
    public MFAData? MFAData { get; set; }
}

public  struct MFAData {
 public string UserId {  get; set; }
 public string TempToken { get; set; }

}