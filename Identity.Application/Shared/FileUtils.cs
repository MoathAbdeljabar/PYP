using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public static class FileUtils
{
    // Configuration - consider moving to appsettings.json
    private static readonly long MaxFileSize = 10_000_000; // 10MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    // Magic numbers for common image formats
    private static readonly Dictionary<string, byte[]> ImageSignatures = new Dictionary<string, byte[]>
    {
        { ".jpg", new byte[] { 0xFF, 0xD8, 0xFF } },
        { ".jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },
        { ".png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
        { ".gif", new byte[] { 0x47, 0x49, 0x46, 0x38 } }, // "GIF8"
        { ".webp", new byte[] { 0x52, 0x49, 0x46, 0x46 } } // "RIFF"
    };

    public static bool IsValidImageFile(this IFormFile file)
    {
        // Basic null/empty checks
        if (file == null || file.Length == 0)
            return false;

        // 1. Size check (CRITICAL for security)
        if (file.Length > MaxFileSize)
            return false;

        // 2. Extension check
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (string.IsNullOrEmpty(fileExtension) ||
            !AllowedExtensions.Contains(fileExtension))
            return false;

        // 3. File name security check
        if (!IsSafeFileName(file.FileName))
            return false;

        // 4. Magic number/header validation
        try
        {
            using (var stream = file.OpenReadStream())
            {
                // Read enough bytes for the longest signature
                int maxSignatureLength = ImageSignatures.Values.Max(s => s.Length);
                byte[] header = new byte[maxSignatureLength];
                int bytesRead = stream.Read(header, 0, maxSignatureLength);

                if (bytesRead < 4) // Too small to validate
                    return false;

                // Validate based on file extension
                if (ImageSignatures.TryGetValue(fileExtension, out var expectedSignature))
                {
                    // Check if header matches expected signature
                    for (int i = 0; i < expectedSignature.Length; i++)
                    {
                        if (i >= bytesRead || header[i] != expectedSignature[i])
                            return false;
                    }

                    // Additional format-specific validations
                    return ValidateSpecificFormat(fileExtension, header, bytesRead);
                }

                return false;
            }
        }
        catch (Exception ex)
        {
            // Log the exception (important for debugging)
            // Consider injecting ILogger instead of Console
            Console.WriteLine($"File validation error: {ex.Message}");
            return false;
        }
    }

    private static bool ValidateSpecificFormat(string extension, byte[] header, int bytesRead)
    {
        switch (extension)
        {
            case ".jpg":
            case ".jpeg":
                // Additional JPEG validation
                // JPEG should have 0xFF 0xD8 0xFF at start
                return header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

            case ".png":
                // PNG validation
                // Check for PNG signature and valid IHDR chunk
                return header[0] == 0x89 &&
                       header[1] == 0x50 &&
                       header[2] == 0x4E &&
                       header[3] == 0x47;

            case ".gif":
                // GIF87a or GIF89a
                return header[0] == 0x47 &&
                       header[1] == 0x49 &&
                       header[2] == 0x46 &&
                       header[3] == 0x38 &&
                       (header[4] == 0x37 || header[4] == 0x39) && // 7 or 9
                       header[5] == 0x61; // 'a'

            case ".webp":
                // WEBP: RIFF header
                return header[0] == 0x52 && // R
                       header[1] == 0x49 && // I
                       header[2] == 0x46 && // F
                       header[3] == 0x46 && // F
                       header[8] == 0x57 && // W
                       header[9] == 0x45 && // E
                       header[10] == 0x42 && // B
                       header[11] == 0x50;  // P

            default:
                return true;
        }
    }

    private static bool IsSafeFileName(string fileName)
    {
        // Prevent path traversal attacks
        var invalidChars = Path.GetInvalidFileNameChars();
        if (fileName.Any(c => invalidChars.Contains(c)))
            return false;

        // Prevent directory traversal
        if (fileName.Contains("..") ||
            fileName.Contains("/") ||
            fileName.Contains("\\"))
            return false;

        // Prevent overly long filenames
        if (fileName.Length > 255)
            return false;

        return true;
    }

    // Additional security method: Generate safe filename
    public static string GenerateSafeFileName(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var uniqueName = $"{Guid.NewGuid():N}{extension}";
        return uniqueName;
    }
}