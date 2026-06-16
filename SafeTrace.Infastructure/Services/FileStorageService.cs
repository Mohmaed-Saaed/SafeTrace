using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Interfaces.IServices;

namespace SafeTrace.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;

        public FileStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<ApiResponse<string>> SaveFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "No file was uploaded."
                };
            }

            const long maxFileSize = 5 * 1024 * 1024; // 5 MB

            if (file.Length > maxFileSize)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "File size cannot exceed 5 MB."
                };
            }

            string wwwRootPath = _environment.WebRootPath;
            string contentPath = Path.Combine(wwwRootPath, "Images", folderName);

            if (!Directory.Exists(contentPath))
            {
                Directory.CreateDirectory(contentPath);
            }

            string extension = Path.GetExtension(file.FileName).ToLower();
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
            string[] allowedContentTypes = {"image/jpeg", "image/png", "image/webp", "image/jpg"};

            if (!allowedExtensions.Contains(extension) || !allowedContentTypes.Contains(file.ContentType))
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Only .jpg, .jpeg, .png, and .webp files are allowed.",
                    Data = null
                };
            }

            string uniqueFileName = $"{Guid.NewGuid()}{extension}";
            string fullPath = Path.Combine(contentPath, uniqueFileName);

            using (var fileStream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return new ApiResponse<string>
            {
                Success = true,
                Message = "Image uploaded successfully",
                Data = $"/Images/{folderName}/{uniqueFileName}"
            };
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