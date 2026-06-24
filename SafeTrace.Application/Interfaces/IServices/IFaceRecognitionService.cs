using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IFaceRecognitionService
    {
        Task<bool> CreateCollectionAsync(string collectionId);
        Task<string> IndexFaceAsync(IFormFile image, string collectionId);
        Task<List<string>> SearchByImageAsync(IFormFile image, string collectionId);
    }
}