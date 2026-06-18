using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IFileStorageService
    {
        // Returns the saved relative path directly. Throws BadRequestException on validation/IO failure
        // so the GlobalExceptionHandler middleware turns it into a proper 400 response.
        Task<string> SaveFileAsync(IFormFile file, string folderName);

        bool DeleteFile(string fileUrl);
    }
}
