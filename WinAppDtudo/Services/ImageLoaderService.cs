namespace WinAppDtudo.Services;

/// <summary>
/// Utilitário estático para download assíncrono de imagens via HTTP.
/// O MemoryStream não é descartado intencionalmente: Image.FromStream requer
/// que o stream permaneça aberto durante a vida da imagem.
/// </summary>
public static class ImageLoaderService
{
    private static readonly HttpClient _client;

    static ImageLoaderService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
    }

    /// <summary>Faz download de uma imagem da URL e retorna null em caso de falha.</summary>
    public static async Task<Image?> DownloadAsync(string url)
    {
        try
        {
            var bytes = await _client.GetByteArrayAsync(url);
            var ms = new MemoryStream(bytes); // não descartado: necessário para o Image
            return Image.FromStream(ms);
        }
        catch
        {
            return null;
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
}
