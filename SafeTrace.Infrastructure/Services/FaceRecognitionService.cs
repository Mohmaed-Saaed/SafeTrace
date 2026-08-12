using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.AiMatching.Response;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using System.Net;

namespace SafeTrace.Infrastructure.Services
{
    public class FaceRecognitionService : IFaceRecognitionService
    {
        private readonly IAmazonRekognition _rekognitionClient;
        private readonly ILogger<FaceRecognitionService> _logger;
        private readonly string _collectionId;

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] AllowedImageContentTypes = { "image/jpeg", "image/png", "image/webp", "image/jpg" };
        private const long MaxImageFileSize = 5 * 1024 * 1024; // 5 MB

        public FaceRecognitionService(
            IAmazonRekognition rekognitionClient,
            ILogger<FaceRecognitionService> logger,
            IConfiguration configuration)
        {
            _rekognitionClient = rekognitionClient;
            _logger = logger;
            _collectionId = configuration["AWS:CollectionId"]!;
        }

        public async Task<bool> CreateCollectionAsync()
        {
            try
            {
                var request = new CreateCollectionRequest { CollectionId = _collectionId };
                var response = await _rekognitionClient.CreateCollectionAsync(request);

                _logger.LogInformation("Collection {CollectionId} created successfully.", _collectionId);
                return response.StatusCode == (int)HttpStatusCode.OK;
            }
            catch (ResourceAlreadyExistsException)
            {
                _logger.LogInformation("Collection {CollectionId} already exists.", _collectionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating collection {CollectionId}", _collectionId);
                throw new BadRequestException("حدث خطأ أثناء الاتصال بخدمة التعرف على الوجوه.");
            }
        }

        public async Task ResetCollectionAsync()
        {
            var collectionId = _collectionId;

            try
            {
                var deleteRequest = new DeleteCollectionRequest
                {
                    CollectionId = collectionId
                };
                await _rekognitionClient.DeleteCollectionAsync(deleteRequest);
                _logger.LogInformation("AWS Collection {CollectionId} deleted successfully.", collectionId);
            }
            catch (ResourceNotFoundException)
            {
                _logger.LogWarning("AWS Collection {CollectionId} not found, will be created.", collectionId);
            }

            var createRequest = new CreateCollectionRequest
            {
                CollectionId = collectionId
            };
            await _rekognitionClient.CreateCollectionAsync(createRequest);
            _logger.LogInformation("AWS Collection {CollectionId} created successfully.", collectionId);
        }

        public async Task<string> IndexFaceAsync(IFormFile image)
        {
            ValidateIsImage(image);

            try
            {
                using var memoryStream = await GetImageMemoryStreamAsync(image);

                var request = new IndexFacesRequest
                {
                    CollectionId = _collectionId,
                    Image = new Amazon.Rekognition.Model.Image { Bytes = memoryStream },
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
                    _logger.LogWarning("IndexFace failed: Multiple faces detected. Rolling back.");

                    var faceIdsToDelete = response.FaceRecords.Select(f => f.Face.FaceId).ToList();
                    await _rekognitionClient.DeleteFacesAsync(new DeleteFacesRequest
                    {
                        CollectionId = _collectionId,
                        FaceIds = faceIdsToDelete
                    });

                    throw new BadRequestException("الصورة تحتوي على أكثر من شخص. يرجى رفع صورة تحتوي على شخص واحد فقط.");
                }

                var faceId = response.FaceRecords.First().Face.FaceId;
                _logger.LogInformation("Successfully indexed face {FaceId} in AWS.", faceId);

                return faceId;
            }
            catch (Exception ex) when (!(ex is BadRequestException))
            {
                _logger.LogError(ex, "Error indexing face in collection {CollectionId}", _collectionId);
                throw new BadRequestException("حدث خطأ أثناء معالجة الصورة في خوادم الذكاء الاصطناعي.");
            }
        }

        public async Task<List<FaceMatchResult>> SearchByImageAsync(IFormFile image)
        {
            ValidateIsImage(image);

            using var memoryStream = await GetImageMemoryStreamAsync(image);
            var awsImage = new Amazon.Rekognition.Model.Image { Bytes = memoryStream };

            try
            {
                var detectRequest = new DetectFacesRequest { Image = awsImage };
                var detectResponse = await _rekognitionClient.DetectFacesAsync(detectRequest);

                if (detectResponse.FaceDetails.Count == 0)
                {
                    throw new BadRequestException("عذراً، لم يتم التعرف على أي وجه في الصورة. يرجى رفع صورة واضحة.");
                }

                if (detectResponse.FaceDetails.Count > 1)
                {
                    throw new BadRequestException("عذراً، الصورة تحتوي على أكثر من شخص. يرجى رفع صورة تحتوي على شخص واحد فقط للبحث.");
                }
            }
            catch (Exception ex) when (!(ex is BadRequestException))
            {
                _logger.LogError(ex, "Error detecting faces for validation.");
                throw new BadRequestException("حدث خطأ أثناء تحليل الصورة المرفوعة. حاول مره أخرى بصورة اكثر وضوحا.");
            }

            try
            {
                var searchRequest = new SearchFacesByImageRequest
                {
                    CollectionId = _collectionId,
                    Image = awsImage,
                    FaceMatchThreshold = 75F,
                    MaxFaces = 4096
                };

                var searchResponse = await _rekognitionClient.SearchFacesByImageAsync(searchRequest);

                var matchedFaces = searchResponse.FaceMatches.Select(m => new FaceMatchResult
                {
                    FaceId = m.Face.FaceId,
                    Similarity = m.Similarity
                }).ToList();

                _logger.LogInformation("Search completed. Found {Count} matches.", matchedFaces.Count);

                return matchedFaces;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during face search in AWS.");
                throw new BadRequestException("حدث خطأ أثناء البحث عن الوجوه المشابهة.");
            }
        }

        public async Task<FaceComparisonResult> CompareFacesAsync(IFormFile firstImage, IFormFile secondImage)
        {
            try
            {
                ValidateIsImage(firstImage);
                ValidateIsImage(secondImage);
            }
            catch (BadRequestException ex)
            {
                return new FaceComparisonResult { Success = false, Error = ex.Message };
            }

            try
            {
                using var stream1 = await GetImageMemoryStreamAsync(firstImage);
                using var stream2 = await GetImageMemoryStreamAsync(secondImage);

                var awsImage1 = new Amazon.Rekognition.Model.Image { Bytes = stream1 };
                var awsImage2 = new Amazon.Rekognition.Model.Image { Bytes = stream2 };

                var request = new CompareFacesRequest
                {
                    SourceImage = awsImage1,
                    TargetImage = awsImage2,
                    SimilarityThreshold = 75F
                };

                var response = await _rekognitionClient.CompareFacesAsync(request);

                if (response.FaceMatches.Count > 0)
                {
                    var bestMatch = response.FaceMatches.OrderByDescending(m => m.Similarity).First();
                    return new FaceComparisonResult
                    {
                        Success = true,
                        IsSamePerson = true,
                        Similarity = bestMatch.Similarity ?? 0f
                    };
                }

                if (response.UnmatchedFaces.Count > 0)
                {
                    return new FaceComparisonResult
                    {
                        Success = true,
                        IsSamePerson = false,
                        Similarity = 0
                    };
                }

                return new FaceComparisonResult { Success = false, Error = "لم يتم التعرف على أي وجه في إحدى الصورتين." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during face comparison.");
                return new FaceComparisonResult { Success = false, Error = "حدث خطأ أثناء مقارنة الوجوه. قد لا تحتوي إحدى الصورتين على وجه واضح." };
            }
        }

        public async Task<bool> DeleteFaceAsync(string faceId)
        {
            if (string.IsNullOrWhiteSpace(faceId)) return true;

            return await DeleteFacesAsync(new List<string> { faceId });
        }

        public async Task<bool> DeleteFacesAsync(List<string> faceIds)
        {
            if (faceIds == null || !faceIds.Any()) return true;

            try
            {
                var request = new DeleteFacesRequest
                {
                    CollectionId = _collectionId,
                    FaceIds = faceIds
                };

                var response = await _rekognitionClient.DeleteFacesAsync(request);

                _logger.LogInformation("Successfully deleted {Count} faces from AWS collection.", response.DeletedFaces.Count);

                return response.HttpStatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting faces from collection {CollectionId}", _collectionId);
                throw new BadRequestException("حدث خطأ أثناء محاولة مسح بيانات الوجوه.");
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
                _logger.LogWarning("Attempted to process a non-image file: {FileName}", file.FileName);
                throw new BadRequestException("الملف المرفوع ليس صورة. يرجى التأكد من رفع صور فقط.");
            }

            if (file.Length > MaxImageFileSize)
            {
                throw new BadRequestException("حجم الصورة لا يمكن أن يتجاوز 5 ميجابايت.");
            }
        }

        private async Task<MemoryStream> GetImageMemoryStreamAsync(IFormFile imageFile)
        {
            var outputStream = new MemoryStream();
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            if (extension == ".webp" || imageFile.ContentType == "image/webp")
            {
                using var inputStream = imageFile.OpenReadStream();
                using var image = await SixLabors.ImageSharp.Image.LoadAsync(inputStream);
                await image.SaveAsync(outputStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
            }
            else
            {
                await imageFile.CopyToAsync(outputStream);
            }

            outputStream.Position = 0;
            return outputStream;
        }
    }
}