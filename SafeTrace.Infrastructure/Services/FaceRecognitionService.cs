using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using System.Net;

namespace SafeTrace.Infrastructure.Services
{
    public class FaceRecognitionService : IFaceRecognitionService
    {
        private readonly IAmazonRekognition _rekognitionClient;
        private readonly ILogger<FaceRecognitionService> _logger;

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] AllowedImageContentTypes = { "image/jpeg", "image/png", "image/webp", "image/jpg" };

        public FaceRecognitionService(IAmazonRekognition rekognitionClient, ILogger<FaceRecognitionService> logger)
        {
            _rekognitionClient = rekognitionClient;
            _logger = logger;
        }

        public async Task<bool> CreateCollectionAsync(string collectionId)
        {
            try
            {
                var request = new CreateCollectionRequest { CollectionId = collectionId };
                var response = await _rekognitionClient.CreateCollectionAsync(request);

                _logger.LogInformation("Collection {CollectionId} created successfully.", collectionId);
                return response.StatusCode == (int)HttpStatusCode.OK;
            }
            catch (ResourceAlreadyExistsException)
            {
                _logger.LogInformation("Collection {CollectionId} already exists.", collectionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating collection {CollectionId}", collectionId);
                throw new BadRequestException("حدث خطأ أثناء الاتصال بخدمة التعرف على الوجوه.");
            }
        }

        public async Task<string> IndexFaceAsync(IFormFile image, string collectionId)
        {
            ValidateIsImage(image);

            try
            {
                using var memoryStream = new MemoryStream();
                await image.CopyToAsync(memoryStream);

                var request = new IndexFacesRequest
                {
                    CollectionId = collectionId,
                    Image = new Image { Bytes = memoryStream },
                    DetectionAttributes = new List<string> { "DEFAULT" },
                    MaxFaces = 2
                };

                var response = await _rekognitionClient.IndexFacesAsync(request);

                if (response.FaceRecords.Count == 0)
                {
                    _logger.LogWarning("IndexFace failed: No faces detected in the uploaded image.");
                    throw new BadRequestException("عذراً، لم يتم التعرف على أي وجه في الصورة.");
                }

                if (response.FaceRecords.Count > 1)
                {
                    _logger.LogWarning("IndexFace failed: Multiple faces detected. Rolling back indexed faces from AWS.");

                    var faceIdsToDelete = response.FaceRecords.Select(f => f.Face.FaceId).ToList();
                    await _rekognitionClient.DeleteFacesAsync(new DeleteFacesRequest
                    {
                        CollectionId = collectionId,
                        FaceIds = faceIdsToDelete
                    });

                    throw new BadRequestException("الصورة تحتوي على أكثر من شخص. يرجى رفع صورة تحتوي على شخص واحد فقط.");
                }

                var faceId = response.FaceRecords.First().Face.FaceId;

                _logger.LogInformation("Successfully indexed face {FaceId} in AWS for collection {CollectionId}.", faceId, collectionId);

                return faceId;
            }
            catch (Exception ex) when (!(ex is BadRequestException))
            {
                _logger.LogError(ex, "Error indexing face in collection {CollectionId}", collectionId);
                throw new BadRequestException("حدث خطأ أثناء معالجة الصورة في خوادم الذكاء الاصطناعي.");
            }
        }

        public async Task<List<string>> SearchByImageAsync(IFormFile image, string collectionId)
        {
            ValidateIsImage(image);

            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            var awsImage = new Image { Bytes = memoryStream };

            try
            {
                var detectRequest = new DetectFacesRequest { Image = awsImage };
                var detectResponse = await _rekognitionClient.DetectFacesAsync(detectRequest);

                if (detectResponse.FaceDetails.Count == 0)
                {
                    _logger.LogWarning("Search attempt failed: No faces detected in the uploaded image.");
                    throw new BadRequestException("عذراً، لم يتم التعرف على أي وجه في الصورة. يرجى رفع صورة واضحة.");
                }

                if (detectResponse.FaceDetails.Count > 1)
                {
                    _logger.LogWarning("Search attempt failed: Multiple faces ({Count}) detected in the uploaded image.", detectResponse.FaceDetails.Count);
                    throw new BadRequestException("عذراً، الصورة تحتوي على أكثر من شخص. يرجى رفع صورة تحتوي على شخص واحد فقط للبحث.");
                }
            }
            catch (Exception ex) when (!(ex is BadRequestException))
            {
                _logger.LogError(ex, "Error detecting faces for validation.");
                throw new BadRequestException("حدث خطأ أثناء تحليل الصورة المرفوعة.");
            }

            try
            {
                var searchRequest = new SearchFacesByImageRequest
                {
                    CollectionId = collectionId,
                    Image = awsImage,
                    FaceMatchThreshold = 85F,
                    MaxFaces = 4096
                };

                var searchResponse = await _rekognitionClient.SearchFacesByImageAsync(searchRequest);
                var matchedFaceIds = searchResponse.FaceMatches.Select(m => m.Face.FaceId).ToList();

                _logger.LogInformation("Search completed. Found {Count} matching faces.", matchedFaceIds.Count);

                return matchedFaceIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during face search in AWS.");
                throw new BadRequestException("حدث خطأ أثناء البحث عن الوجوه المشابهة.");
            }
        }

        private void ValidateIsImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new BadRequestException("لم يتم رفع أي ملف أو أن الملف فارغ.");

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            bool isImage = AllowedImageExtensions.Contains(extension) && AllowedImageContentTypes.Contains(file.ContentType);

            if (!isImage)
            {
                _logger.LogWarning("Attempted to process a non-image file: {FileName} with ContentType: {ContentType}", file.FileName, file.ContentType);
                throw new BadRequestException("الملف المرفوع ليس صورة. يرجى التأكد من رفع صور فقط.");
            }
        }
    }
}