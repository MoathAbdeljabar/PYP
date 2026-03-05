using Identity.Data.Models;
using Microsoft.IdentityModel.Tokens;
using QRCoder;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Application.IdentityService;

public partial class IdentityService 
{

    private bool IsOver18(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - date.Year;

        // Subtract one year if birthday hasn't occurred yet this year
        if (date > today.AddYears(-age))
        {
            age--;
        }

        return age >= 18;
    }






    //--------------------------------------
    //Temp Token Helpers
    //--------------------------------------


    private string GenerateTempToken(string userId, string purpose)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret2"]);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("purpose", purpose),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique token ID
        }),
            Expires = DateTime.UtcNow.AddMinutes(1),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private bool ValidateTempToken(string token, out string userId)
    {
        userId = null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret2"]);

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // Strict 1-minute expiry
            }, out _);

            // Extra security: verify this is specifically a 2FA token
            if (principal.FindFirst("purpose")?.Value != "2fa_verification")
                return false;

            userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(userId);
        }
        catch
        {
            return false;
        }
    }








    //--------------------------------------
    //Token
    //--------------------------------------


    //-----------------------------------
    //ClaimTypes.NameIdentifier should be user.Id (the GUID)
    private async Task<string> _GenerateJwtTokenAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id), //user.userName
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.Id)
    };

        // Get roles for the user
        var roles = await _userManager.GetRolesAsync(user);

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:SecretKey"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expireMinutes = Convert.ToDouble(_configuration["Jwt:ExpireMinutes"]);
        var expires = DateTime.Now.AddMinutes(expireMinutes);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }


    // NEW: Helper method to generate refresh token
    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    // NEW: Helper method to read expired token
    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        /*
         GetPrincipalFromExpiredToken() - Reads the EXPIRED JWT token and extracts user information from it (even though it's expired)
           principal - Contains the decoded user information from the token:
           -- User ID
           -- Username
           -- Roles
           -- Other claims

         */

        if (string.IsNullOrEmpty(token) || token.Split('.').Length != 3)
        // token is malformed/invalid - it doesn't have the proper 3-part JWT structure (header.payload.signature)
        {
            throw new SecurityTokenException("Invalid token format - must have 3 parts");
        }

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidAudience = _configuration["Jwt:Audience"],
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"])),
            ValidateLifetime = false // Important: we want to read expired tokens
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
        //The signature validation happens automatically here

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }



    // NEW: Helper to get expiry minutes
    private double GetAccessTokenExpiryMinutes()
    {
        return Convert.ToDouble(_configuration["Jwt:ExpireMinutes"] ?? "15");
    }




    private string GenerateQrCodeBase64(string authenticatorKey, string userEmail)
    {
        // Use your application name that users will recognize
        var appName = _configuration["TOTP:AppName"]; //should not be hardcoded , read it 
        var accountName = userEmail; // or username

        var encodedIssuer = Uri.EscapeDataString(appName);
        var otpauthUri = $"otpauth://totp/{encodedIssuer}:{accountName}?secret={authenticatorKey}&issuer={encodedIssuer}&digits=6&period=30";

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(otpauthUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);

        return Convert.ToBase64String(qrCode.GetGraphic(20));
    }

}

