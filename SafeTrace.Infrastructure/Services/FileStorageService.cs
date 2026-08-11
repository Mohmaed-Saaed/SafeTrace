using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using SafeTrace.Application.Exceptions;

namespace SafeTrace.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] AllowedImageContentTypes = { "image/jpeg", "image/png", "image/webp", "image/jpg" };
        private const long MaxImageFileSize = 5 * 1024 * 1024; // 5 MB

        private static readonly string[] AllowedVideoExtensions = { ".mp4", ".mov", ".webm" };
        private static readonly string[] AllowedVideoContentTypes = { "video/mp4", "video/quicktime", "video/webm" };
        private const long MaxVideoFileSize = 50 * 1024 * 1024; // 50 MB

        public FileStorageService(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _bucketName = configuration["AWS:FilesBucketName"] ?? throw new ArgumentNullException("AWS:FilesBucketName is missing in configuration.");
        }

        public async Task<List<string>> SaveFilesAsync(IEnumerable<IFormFile> files, string folderName)
        {
            if (files == null || !files.Any())
                throw new BadRequestException("لم يتم رفع أي ملفات.");

            var fileList = files.ToList();

            if (fileList.Count > 5)
                throw new BadRequestException("الحد الأقصى للملفات المرفوعة هو 5 ملفات في المرة الواحدة.");

            int videoCount = fileList.Count(f =>
            {
                var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
                return AllowedVideoExtensions.Contains(ext) || f.ContentType.StartsWith("video/");
            });

            if (videoCount > 1)
                throw new BadRequestException("الحد الأقصى للفيديوهات المرفوعة هو فيديو واحد فقط.");

            var savedFileUrls = new List<string>();

            try
            {
                foreach (var file in fileList)
                {
                    var fileUrl = await SaveFileAsync(file, folderName);
                    savedFileUrls.Add(fileUrl);
                }
            }
            catch (Exception)
            {
                foreach (var url in savedFileUrls)
                {
                    DeleteFile(url);
                }
                throw;
            }

            return savedFileUrls;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                throw new BadRequestException("لم يتم رفع أي ملف أو أن الملف فارغ.");

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            bool isImage = AllowedImageExtensions.Contains(extension) && AllowedImageContentTypes.Contains(file.ContentType);
            bool isVideo = AllowedVideoExtensions.Contains(extension) && AllowedVideoContentTypes.Contains(file.ContentType);

            if (!isImage && !isVideo)
                throw new BadRequestException("صيغة الملف غير مدعومة. الصور المسموحة (jpg, jpeg, png, webp) والفيديوهات المسموحة (mp4, mov, webm).");

            if (isImage && file.Length > MaxImageFileSize)
                throw new BadRequestException("حجم الصورة لا يمكن أن يتجاوز 5 ميجابايت.");

            if (isVideo && file.Length > MaxVideoFileSize)
                throw new BadRequestException("حجم الفيديو لا يمكن أن يتجاوز 50 ميجابايت.");

            string baseFolder = isVideo ? "Videos" : "Images";
            string uniqueFileName = $"{Guid.NewGuid()}{extension}";

            string objectKey = $"{baseFolder}/{folderName}/{uniqueFileName}";

            try
            {
                using var newMemoryStream = new MemoryStream();
                await file.CopyToAsync(newMemoryStream);
                newMemoryStream.Position = 0;

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = newMemoryStream,
                    Key = objectKey,
                    BucketName = _bucketName,
                    ContentType = file.ContentType
                };

                using var fileTransferUtility = new TransferUtility(_s3Client);
                await fileTransferUtility.UploadAsync(uploadRequest);
            }
            catch (Exception)
            {
                throw new BadRequestException("حدث خطأ أثناء رفع الملف إلى خوادم التخزين.");
            }

            return objectKey;
        }

        public bool DeleteFile(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return false;

            try
            {
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileUrl
                };

                _s3Client.DeleteObjectAsync(deleteObjectRequest).GetAwaiter().GetResult();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}