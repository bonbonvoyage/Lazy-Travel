using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace LazyTravel.Services;

// VlogPosts 專用的圖床上傳：新增的圖片（封面大圖、景點圖片）丟到 Cloudflare R2（S3 相容 API），
// 不影響舊圖——舊圖還是本機 wwwroot/uploads/vlog/ 底下的路徑，維持原狀。
// Key 統一放在 R2 bucket 裡既有的 VlogPosts/ 資料夾下，跟 Members/Reports/TravelGroups 分開。
public class VlogPostImageUploadService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _publicUrl;

    public VlogPostImageUploadService(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["CloudflareR2:BucketName"]
            ?? throw new InvalidOperationException("appsettings 缺少 CloudflareR2:BucketName 設定。");
        _publicUrl = configuration["CloudflareR2:PublicUrl"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("appsettings 缺少 CloudflareR2:PublicUrl 設定。");
    }

    // subFolder 是 "cover" 或 "itinerary"，回傳可以直接放進 <img src> 的公開網址。
    public async Task<string> UploadAsync(IFormFile file, string subFolder)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var key = $"VlogPosts/{subFolder}/{Guid.NewGuid():N}{ext}";

        await using var stream = file.OpenReadStream();
        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = file.ContentType,
            AutoCloseStream = true,
            DisablePayloadSigning = true,
        });

        return $"{_publicUrl}/{key}";
    }
}
