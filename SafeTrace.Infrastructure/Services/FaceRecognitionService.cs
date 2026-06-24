using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.Interfaces.IServices;
using System.Net;

namespace SafeTrace.Infrastructure.Services
{
    public class FaceRecognitionService : IFaceRecognitionService
    {
        private readonly IAmazonRekognition _rekognitionClient;
        private readonly ILogger<FaceRecognitionService> _logger;

        public FaceRecognitionService(IAmazonRekognition rekognitionClient, ILogger<FaceRecognitionService> logger)
        {
            _rekognitionClient = rekognitionClient;
            _logger = logger;
        }

        public async Task<bool> CreateCollectionAsync(string collectionId)
        {
            try
            {
                var request = new CreateCollectionRequest
                {
                    CollectionId = collectionId
                };

                var response = await _rekognitionClient.CreateCollectionAsync(request);

                if (response.StatusCode == (int)HttpStatusCode.OK)
                    _logger.LogInformation($"AWS {collectionId} Collection Created Successfully");

                return response.StatusCode == (int)HttpStatusCode.OK;
            }
            catch (ResourceAlreadyExistsException)
            {
                _logger.LogInformation("AWS Collection already exists");
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"AWS Rekognition Error: {ex.Message}", ex);
            }
        }


    }
}