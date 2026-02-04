using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using MyApp.Application.Shared.Interfaces;

namespace MyApp.Application.Shared.Services;
public class LocalFileStorageService : IFileStorageService
{
    private readonly IHostEnvironment _environment;

    public LocalFileStorageService(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string storagePath, string fileName)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty");

        //var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var wwwrootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(wwwrootPath, storagePath, fileName);

        var directory = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory!);

        using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return Path.Combine(storagePath, fileName).Replace("\\", "/");
    }

    public Task<bool> DeleteFileAsync(string filePath)
    {
        var wwwrootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(wwwrootPath, filePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}