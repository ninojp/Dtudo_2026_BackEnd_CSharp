using System.Drawing;
using System.Windows.Forms;
using WinAppDtudo.Forms;
using WinAppDtudo.Services;

namespace WinAppDtudo.Tests;

public sealed class WinAppHealthDashboardTests
{
    [Fact]
    public void DashboardUsesDpiScalingAndStableMinimumSize()
    {
        var tokenPath = Path.Combine(
            Path.GetTempPath(),
            "Dtudo2026-WinAppTests",
            Guid.NewGuid().ToString("N"),
            "session.bin");
        using var authenticationService = new WinAppAuthenticationService(new ProtectedTokenStore(tokenPath));
        using var httpClient = new HttpClient(new NoOpHandler());
        using var healthService = new WinAppHealthMonitoringService(
            authenticationService,
            httpClient,
            new WinAppHealthMonitoringOptions
            {
                IdentityBaseUrl = new Uri("https://identity.test/"),
                ApiMyAnimesBaseUrl = new Uri("https://animes.test/"),
                ApiMyAnimeListBaseUrl = new Uri("https://mal.test/"),
                ApiFileStorageBaseUrl = new Uri("https://storage.test/"),
                CertificateTargets = [],
                BackupRoot = null,
                ProbeTimeout = TimeSpan.FromSeconds(1)
            });
        using var form = new Frm_HealthDashboard(healthService);

        Assert.Equal(AutoScaleMode.Dpi, form.AutoScaleMode);
        Assert.True(form.MinimumSize.Width >= 960);
        Assert.True(form.MinimumSize.Height >= 560);

        form.ClientSize = new Size(1440, 855);
        form.PerformAutoScale();

        Assert.True(form.ClientSize.Width >= form.MinimumSize.Width);
        Assert.True(form.ClientSize.Height >= form.MinimumSize.Height);
    }

    private sealed class NoOpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
    }
}
