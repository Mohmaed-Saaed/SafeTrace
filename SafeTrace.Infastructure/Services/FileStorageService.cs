using Microsoft.AspNetCore.Http;
using SafeTrace.Application.Interfaces.IServices;

namespace SafeTrace.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            var rootPath = Directory.GetCurrentDirectory();

            var uploadsPath = Path.Combine(
                rootPath,
                "wwwroot",
                "Uploads",
                folderName);

            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var extension = Path.GetExtension(file.FileName).ToLower();

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            if (!allowedExtensions.Contains(extension))
            {
                throw new Exception("Only .jpg, .jpeg, .png and .webp files are allowed.");
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/Uploads/{folderName}/{fileName}";
        }

        public bool DeleteFile(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return false;

            var rootPath = Directory.GetCurrentDirectory();

            var filePath = Path.Combine(
                rootPath,
                "wwwroot",
                fileUrl.TrimStart('/'));

            if (!File.Exists(filePath))
                return false;

            File.Delete(filePath);
            return true;
        }
    }
}