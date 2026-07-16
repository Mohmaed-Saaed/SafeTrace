using Microsoft.AspNetCore.Http;
using SafeTrace.Application.DTOs.AiMatching.Response;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IFaceRecognitionService
    {
        Task<bool> CreateCollectionAsync();
        Task ResetCollectionAsync();
        Task<string> IndexFaceAsync(IFormFile image);
        Task<List<FaceMatchResult>> SearchByImageAsync(IFormFile image);
        Task<bool> DeleteFaceAsync(string faceId);
        Task<bool> DeleteFacesAsync(List<string> faceIds);
    }
}