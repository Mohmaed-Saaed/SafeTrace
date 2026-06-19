using Microsoft.AspNetCore.Http;
using SafeTrace.Application.DTOs.Responses;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IFileStorageService
    {
        public Task<string> SaveFileAsync(IFormFile file, string folderName);
        public bool DeleteFile(string fileUrl);
    }
}