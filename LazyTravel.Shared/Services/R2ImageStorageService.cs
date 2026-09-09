using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace LazyTravel.Shared.Services
{
    // 用 AWS S3 SDK 連 Cloudflare R2(R2 相容 S3 協定,同一套 SDK 可以直接用)。
    // 上傳完回傳的是 PublicUrl + 檔案路徑組出來的公開網址,前端直接用這個網址顯示圖片。
    public class R2ImageStorageService : IImageStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly string _publicUrl;

        public R2ImageStorageService(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _bucketName = configuration["CloudflareR2:BucketName"]
                ?? throw new InvalidOperationException("缺少設定 CloudflareR2:BucketName");
            _publicUrl = configuration["CloudflareR2:PublicUrl"]?.TrimEnd('/')
                ?? throw new InvalidOperationException("缺少設定 CloudflareR2:PublicUrl");
        }

        public async Task<string> UploadAsync(IFormFile file, string folder)
        {
            var fileName = $"{folder}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            using var stream = file.OpenReadStream();
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                InputStream = stream,
                ContentType = file.ContentType,
                DisablePayloadSigning = true // R2 需要這個設定,不然簽章會對不起來
            };

            await _s3Client.PutObjectAsync(request);

            return $"{_publicUrl}/{fileName}";
        }
    }
}
