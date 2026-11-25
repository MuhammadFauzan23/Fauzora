using SkiaSharp;

namespace Fauzora.Services;

public sealed class ProductImageStorageService(IWebHostEnvironment environment, ILogger<ProductImageStorageService> logger)
{
    public const int MaxImages = 15;
    public const long MaxFileBytes = 5 * 1024 * 1024;
    public const long MaxRequestFilesBytes = 50 * 1024 * 1024;
    private const long MaxPixels = 24_000_000;
    private string UploadDirectory => Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads", "products");
    private string UploadUrlPrefix => "/uploads/products/";

    public string? ValidateFiles(IReadOnlyCollection<IFormFile> files, int retainedCount)
    {
        if (retainedCount < 0 || retainedCount + files.Count > MaxImages)
            return $"Maksimal {MaxImages} gambar per produk, termasuk gambar yang dipertahankan.";

        long totalBytes = 0;
        foreach (var file in files)
        {
            if (file.Length <= 0) return "Salah satu file gambar kosong.";
            if (file.Length > MaxFileBytes) return "Ukuran setiap gambar maksimal 5 MB.";
            totalBytes += file.Length;
            if (totalBytes > MaxRequestFilesBytes) return "Total ukuran gambar dalam satu pengiriman maksimal 50 MB.";
        }
        return null;
    }

    public async Task<(IReadOnlyList<string> Urls, string? Error)> StoreAsync(IReadOnlyCollection<IFormFile> files, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateFiles(files, 0);
        if (validationError is not null) return ([], validationError);
        if (files.Count == 0) return ([], null);

        var storedUrls = new List<string>(files.Count);
        try
        {
            Directory.CreateDirectory(UploadDirectory);
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await using var input = file.OpenReadStream();
                using var codec = SKCodec.Create(input);
                if (codec is null || codec.Info.Width <= 0 || codec.Info.Height <= 0 || (long)codec.Info.Width * codec.Info.Height > MaxPixels)
                    throw new InvalidProductImageException($"File '{Path.GetFileName(file.FileName)}' bukan gambar valid atau dimensinya terlalu besar.");

                if (!IsSupportedFormat(codec.EncodedFormat))
                    throw new InvalidProductImageException($"Format isi file '{Path.GetFileName(file.FileName)}' tidak didukung. Gunakan gambar JPG, PNG, atau WEBP.");

                using var bitmap = SKBitmap.Decode(codec);
                if (bitmap is null || bitmap.Width != codec.Info.Width || bitmap.Height != codec.Info.Height)
                    throw new InvalidProductImageException($"Isi file '{Path.GetFileName(file.FileName)}' tidak dapat dibaca sebagai gambar yang valid.");

                var fileName = $"{Guid.NewGuid():N}.webp";
                var destination = Path.Combine(UploadDirectory, fileName);
                storedUrls.Add(UploadUrlPrefix + fileName);
                using var image = SKImage.FromBitmap(bitmap);
                using var encoded = image.Encode(SKEncodedImageFormat.Webp, 82);
                if (encoded is null) throw new InvalidProductImageException("Gambar tidak dapat diproses menjadi format WEBP.");
                await using (var output = File.Create(destination))
                    encoded.SaveTo(output);
            }
            return (storedUrls, null);
        }
        catch (InvalidProductImageException ex)
        {
            await DeleteManagedAsync(storedUrls);
            return ([], ex.Message);
        }
        catch (OperationCanceledException)
        {
            await DeleteManagedAsync(storedUrls);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not decode or store uploaded product image files");
            await DeleteManagedAsync(storedUrls);
            return ([], "Gambar tidak dapat diproses atau penyimpanan lokal tidak tersedia. Coba file lain atau hubungi admin.");
        }
    }

    public async Task DeleteManagedAsync(IEnumerable<string> urls)
    {
        foreach (var url in urls.Distinct(StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith(UploadUrlPrefix, StringComparison.Ordinal)) continue;
            var fileName = url[UploadUrlPrefix.Length..];
            if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName)) continue;

            var path = Path.GetFullPath(Path.Combine(UploadDirectory, fileName));
            var root = Path.GetFullPath(UploadDirectory) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;

            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not remove managed product image file {FileName}", fileName);
            }
        }
        await Task.CompletedTask;
    }

    private static bool IsSupportedFormat(SKEncodedImageFormat format) =>
        format is SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp;

    private sealed class InvalidProductImageException(string message) : Exception(message);
}
