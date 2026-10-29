using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using SkiaSharp;
using DrawingImage = System.Drawing.Image;

namespace WinAppDtudo.Services;

/// <summary>
/// Utilitário estático para download assíncrono de imagens via HTTP.
/// As imagens são validadas e copiadas antes de serem retornadas.
/// </summary>
public static class ImageLoaderService
{
    private static string MyAnimeListAnimeUrl => $"{AppConfigurationService.ApiMyAnimeListBaseUrl.TrimEnd('/')}/ApiMyAnimeList/";
    private static readonly HttpClient _client;
    private static readonly SemaphoreSlim _downloadSlots = new(4, 4);
    private static readonly SemaphoreSlim _myAnimeListRequestLock = new(1, 1);
    private static readonly Dictionary<int, Task<IReadOnlyList<string>>> _myAnimeListCoverUrls = [];
    private static readonly Lock _myAnimeListCoverUrlsLock = new();
    private const long MaxPixelsPorImagem = 40_000_000;

    static ImageLoaderService()
    {
        var handler = AppConfigurationService.CreateHttpClientHandler();
        _client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    /// <summary>Faz download e valida uma imagem, repetindo falhas transitórias.</summary>
    public static async Task<DrawingImage?> DownloadAsync(string? url, CancellationToken cancellationToken = default)
    {
        await _downloadSlots.WaitAsync(cancellationToken);
        try
        {
            return await DownloadCoreAsync(url, cancellationToken);
        }
        finally
        {
            _downloadSlots.Release();
        }
    }

    private static async Task<DrawingImage?> DownloadCoreAsync(string? url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var imageUri)
            || (imageUri.Scheme != Uri.UriSchemeHttp && imageUri.Scheme != Uri.UriSchemeHttps))
            return null;

        for (var tentativa = 1; tentativa <= 3; tentativa++)
        {
            try
            {
                using var response = await _client.GetAsync(imageUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    if (!EhErroTransitorio(response.StatusCode) || tentativa == 3)
                        return null;

                    await AguardarTentativaAsync(tentativa, response, cancellationToken);
                    continue;
                }

                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                if (bytes.Length == 0)
                    return null;

                return DecodificarImagem(bytes);
            }
            catch (Exception ex) when (tentativa < 3 && EhErroTransitorio(ex, cancellationToken))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500 * tentativa), cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Decodifica os bytes em um <see cref="Bitmap"/> independente do stream de origem.
    /// O SkiaSharp cobre JPEG, PNG, WebP, GIF e BMP (o GDI+ nao le WebP); o GDI+ e o fallback para TIFF e ICO.
    /// </summary>
    internal static DrawingImage? DecodificarImagem(byte[] bytes)
    {
        // Rejeita imagens com dimensoes absurdas antes de alocar memoria (protecao contra decompression bomb).
        var limites = SKBitmap.DecodeBounds(bytes);
        if ((long)limites.Width * limites.Height > MaxPixelsPorImagem)
            return null;

        return DecodificarComSkia(bytes) ?? DecodificarComGdi(bytes);
    }

    private static Bitmap? DecodificarComSkia(byte[] bytes)
    {
        try
        {
            var info = SKBitmap.DecodeBounds(bytes);
            if (info.Width <= 0 || info.Height <= 0)
                return null;

            using var origem = SKBitmap.Decode(bytes, new SKImageInfo(
                info.Width, info.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
            if (origem is null)
                return null;

            var bitmap = new Bitmap(origem.Width, origem.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            var area = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var dados = bitmap.LockBits(area, System.Drawing.Imaging.ImageLockMode.WriteOnly, bitmap.PixelFormat);
            try
            {
                // Copia linha a linha porque o stride do Bitmap pode diferir do stride do SKBitmap.
                var linhaBytes = origem.Width * 4;
                var linha = new byte[linhaBytes];
                var origemPtr = origem.GetPixels();
                for (var y = 0; y < origem.Height; y++)
                {
                    Marshal.Copy(origemPtr + (nint)((long)y * origem.RowBytes), linha, 0, linhaBytes);
                    Marshal.Copy(linha, 0, dados.Scan0 + (nint)((long)y * dados.Stride), linhaBytes);
                }
            }
            finally
            {
                bitmap.UnlockBits(dados);
            }

            return bitmap;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or OutOfMemoryException)
        {
            return null;
        }
    }

    private static Bitmap? DecodificarComGdi(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            using var decodificada = DrawingImage.FromStream(
                stream,
                useEmbeddedColorManagement: false,
                validateImageData: true);

            if ((long)decodificada.Width * decodificada.Height > MaxPixelsPorImagem)
                return null;

            var bitmap = new Bitmap(
                decodificada.Width,
                decodificada.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            using var graphics = Graphics.FromImage(bitmap);
            graphics.DrawImage(decodificada, new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height));
            return bitmap;
        }
        catch (Exception ex) when (ex is ArgumentException or OutOfMemoryException)
        {
            return null;
        }
    }

    /// <summary>Obtém a capa exclusivamente pelas URLs da ApiMyAnimeList.</summary>
    public static async Task<DrawingImage?> DownloadAnimeCoverAsync(string? primaryUrl, int malId, CancellationToken cancellationToken = default)
    {
        var image = await DownloadAsync(primaryUrl, cancellationToken);
        if (image is not null || malId <= 0)
            return image;

        foreach (var fallbackUrl in await GetMyAnimeListCoverUrlsAsync(malId, cancellationToken))
        {
            image = await DownloadAsync(fallbackUrl, cancellationToken);
            if (image is not null)
                return image;
        }

        return null;
    }

    public static async Task<byte[]?> DownloadAnimeCoverJpegAsync(
        string? primaryUrl,
        int malId,
        CancellationToken cancellationToken = default)
    {
        var image = await DownloadAnimeCoverAsync(primaryUrl, malId, cancellationToken);
        if (image is null)
            return null;

        using (image)
        using (var jpeg = new MemoryStream())
        {
            image.Save(jpeg, System.Drawing.Imaging.ImageFormat.Jpeg);
            return jpeg.ToArray();
        }
    }

    /// <summary>Carrega a imagem diretamente num PictureBox, marshaling para a thread UI.</summary>
    public static async Task CarregarEmPictureBoxAsync(PictureBox pbx, string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        var img = await DownloadAsync(url);
        if (img == null || pbx.IsDisposed) return;
        if (pbx.InvokeRequired)
            pbx.Invoke(() => pbx.Image = img);
        else
            pbx.Image = img;
    }

    private static Task<IReadOnlyList<string>> GetMyAnimeListCoverUrlsAsync(int malId, CancellationToken cancellationToken)
    {
        lock (_myAnimeListCoverUrlsLock)
        {
            if (_myAnimeListCoverUrls.TryGetValue(malId, out var task))
                return task;

            task = GetMyAnimeListCoverUrlsCoreAsync(malId, cancellationToken);
            _myAnimeListCoverUrls[malId] = task;
            return task;
        }
    }

    private static async Task<IReadOnlyList<string>> GetMyAnimeListCoverUrlsCoreAsync(int malId, CancellationToken cancellationToken)
    {
        await _myAnimeListRequestLock.WaitAsync(cancellationToken);
        try
        {
            for (var tentativa = 1; tentativa <= 3; tentativa++)
            {
                try
                {
                    using var response = await _client.GetAsync($"{MyAnimeListAnimeUrl}{malId}", cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        if (!EhErroTransitorio(response.StatusCode) || tentativa == 3)
                            return [];

                        await AguardarTentativaAsync(tentativa, response, cancellationToken);
                        continue;
                    }

                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                    if (!document.RootElement.TryGetProperty("images", out var images)
                        || !images.TryGetProperty("jpg", out var jpg))
                        return [];

                    return new[] { "largeImageUrl", "imageUrl", "smallImageUrl", "large_image_url", "image_url", "small_image_url" }
                        .Where(name => jpg.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String)
                        .Select(name => jpg.GetProperty(name).GetString())
                        .OfType<string>()
                        .Distinct(StringComparer.Ordinal)
                        .ToArray();
                }
                catch (Exception ex) when (tentativa < 3 && EhErroTransitorio(ex, cancellationToken))
                {
                    await Task.Delay(TimeSpan.FromSeconds(tentativa), cancellationToken);
                }
                catch
                {
                    return [];
                }
            }

            return [];
        }
        finally
        {
            _myAnimeListRequestLock.Release();
        }
    }

    private static bool EhErroTransitorio(HttpStatusCode statusCode)
        => statusCode == HttpStatusCode.RequestTimeout
           || statusCode == HttpStatusCode.TooManyRequests
           || (int)statusCode >= 500;

    private static bool EhErroTransitorio(Exception exception, CancellationToken cancellationToken)
        => !cancellationToken.IsCancellationRequested
           && (exception is HttpRequestException or TaskCanceledException or InvalidOperationException);

    private static async Task AguardarTentativaAsync(int tentativa, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var retryAfter = response.Headers.RetryAfter?.Delta;
        await Task.Delay(retryAfter ?? TimeSpan.FromMilliseconds(500 * tentativa), cancellationToken);
    }
}
