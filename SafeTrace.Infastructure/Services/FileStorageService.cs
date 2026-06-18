using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;

namespace SafeTrace.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp", "image/jpg" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public FileStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                throw new BadRequestException("No file was uploaded.");

            if (file.Length > MaxFileSize)
                throw new BadRequestException("File size cannot exceed 5 MB.");

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(file.ContentType))
                throw new BadRequestException("Only .jpg, .jpeg, .png, and .webp files are allowed.");

            string wwwRootPath = _environment.WebRootPath;
            string contentPath = Path.Combine(wwwRootPath, "Images", folderName);

            if (!Directory.Exists(contentPath))
                Directory.CreateDirectory(contentPath);

            string uniqueFileName = $"{Guid.NewGuid()}{extension}";
            string fullPath = Path.Combine(contentPath, uniqueFileName);

            try
            {
                using var fileStream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(fileStream);
            }
            catch (Exception ex)
            {
                // Don't leak a half-written file if something goes wrong mid-copy
                if (File.Exists(fullPath))
                    File.Delete(fullPath);

                throw new BadRequestException($"Failed to save the uploaded file: {ex.Message}");
            }

            return $"/Images/{folderName}/{uniqueFileName}";
        }

        public bool DeleteFile(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return false;

            string wwwRootPath = _environment.WebRootPath;

            string cleanedPath = fileUrl.TrimStart('/');
            string fullPath = Path.Combine(wwwRootPath, cleanedPath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }

            return false;
        }
    }
}
