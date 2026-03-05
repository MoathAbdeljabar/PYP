using Microsoft.AspNetCore.Http;

namespace MyApp.Application.Shared.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string storagePath, string fileName);
    Task<bool> DeleteFileAsync(string filePath);
}
