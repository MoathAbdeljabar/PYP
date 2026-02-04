using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MyApp.Application.Shared.Interfaces;


namespace MyApp.Application.Shared.Services;

public class FileUrlService : IFileUrlService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public FileUrlService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public string GetFullUrl(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return string.Empty;

        var request = _httpContextAccessor.HttpContext?.Request;

        if (request != null)
        {
            var baseUrl = $"{request.Scheme}://{request.Host}";
            return $"{baseUrl}/{filePath.Replace("\\", "/")}";
        }

        // Fallback to app settings if HttpContext is not available
        var apiUrl = _configuration["ApiBaseUrl"];
        return $"{apiUrl}/{filePath.Replace("\\", "/")}";
    }
}
