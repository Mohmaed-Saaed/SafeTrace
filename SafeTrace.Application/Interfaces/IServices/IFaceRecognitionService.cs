using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IFaceRecognitionService
    {
        Task<bool> CreateCollectionAsync();
        Task<string> IndexFaceAsync(IFormFile image);
        Task<List<string>> SearchByImageAsync(IFormFile image);
        Task<bool> DeleteFaceAsync(string faceId);
        Task<bool> DeleteFacesAsync(List<string> faceIds);
    }
}