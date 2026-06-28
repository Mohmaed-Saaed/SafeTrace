using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IFileStorageService
    {
        Task<List<string>> SaveFilesAsync(IEnumerable<IFormFile> files, string folderName);
        Task<string> SaveFileAsync(IFormFile file, string folderName);
        bool DeleteFile(string fileUrl);
    }
}
