namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IFaceRecognitionService
    {
        Task<bool> CreateCollectionAsync(string collectionId);
    }
}