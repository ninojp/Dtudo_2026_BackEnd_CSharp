using WinAppDtudo.Controls;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

/// <summary>
/// Conteudo da aba "Inicio" do WinAppDtudo: fundo binario sobre preto, titulo e atalhos (imagem + texto)
/// para DtudoSite, MyAnimes, MyMusicX e Nino T.I. O layout usa as coordenadas do desenho original
/// (<see cref="DesignWidth"/> x <see cref="DesignHeight"/>) e e escalado de forma uniforme e centralizado
/// ao redimensionar a janela ou mudar o DPI. Enquanto o usuario nao esta conectado, os atalhos ficam
/// em preto e branco e bloqueados (<see cref="DefinirAcessoLiberado"/>).
/// </summary>
public sealed class FUC_Inicio : UserControl
{
    private const float DesignWidth = 1272F;
    private const float DesignHeight = 652F;
    private const float MinimumFontSize = 6F;

    private readonly Label _titulo;
    private readonly NavigationImageButton _btnDtudoSite;
    private readonly Label _lblDtudoSite;
    private readonly NavigationImageButton _btnMyAnimes;
    private readonly Label _lblMyAnimes;
    private readonly NavigationImageButton _btnMyMusicX;
    private readonly Label _lblMyMusicX;
    private readonly NavigationImageButton _btnNinoTI;
    private readonly Label _lblNinoTI;
    private readonly List<(Label Label, FontFamily Family, float Size, FontStyle Style)> _scalableLabels = [];
    private float _appliedFontScale;
    private bool _isApplyingLayout;
    private bool _acessoLiberado;
    private bool _dtudoSiteOcupado;

    public FUC_Inicio()
    {
        Name = "FUC_Inicio";
        Dock = DockStyle.Fill;
        DoubleBuffered = true;
        BackColor = Color.Black;
        ForeColor = DarkModeColors.TextColor;

        _titulo = CreateLabel("Lbl_Titulo", "WinAppDtudo", "Arial Black", 21F, FontStyle.Bold | FontStyle.Italic,
            ContentAlignment.MiddleCenter);
        _btnDtudoSite = CreateButton("Btn_DtudoSite", Properties.Resources.DtudoSiteShortcut, "DtudoSite");
        _lblDtudoSite = CreateLabel("Lbl_DtudoSite", "DtudoSite", "Segoe Fluent Icons", 20F, FontStyle.Bold);
        _btnMyAnimes = CreateButton("Btn_MyAnimes", Properties.Resources.onePieceGroup, "MyAnimes");
        _lblMyAnimes = CreateLabel("Lbl_MyAnimes", "MyAnimes", "Segoe Fluent Icons", 20F, FontStyle.Bold);
        _btnMyMusicX = CreateButton("Btn_MyMusicX", Properties.Resources.NotaMusica, "MyMusicX");
        _lblMyMusicX = CreateLabel("Lbl_MyMusicX", "MyMusicX", "Segoe Fluent Icons", 20F, FontStyle.Bold);
        _btnNinoTI = CreateButton("Btn_NinoTI", Properties.Resources.TI_link, "Nino T.I");
        _lblNinoTI = CreateLabel("Lbl_NinoTI", "Nino T.I", "Segoe Fluent Icons", 17F, FontStyle.Bold);

        _btnDtudoSite.Click += (_, _) => DtudoSiteSolicitado?.Invoke(this, EventArgs.Empty);
        _btnMyAnimes.Click += (_, _) => MyAnimesSolicitado?.Invoke(this, EventArgs.Empty);
        _btnMyMusicX.Click += (_, _) => MyMusicXSolicitado?.Invoke(this, EventArgs.Empty);
        _btnNinoTI.Click += (_, _) => NinoTISolicitado?.Invoke(this, EventArgs.Empty);

        Controls.AddRange([
            _lblNinoTI, _lblDtudoSite, _lblMyMusicX, _lblMyAnimes,
            _btnNinoTI, _btnMyMusicX, _btnMyAnimes, _btnDtudoSite, _titulo]);

        ApplyLayout();
        AtualizarAtalhos();
    }

    public event EventHandler? DtudoSiteSolicitado;
    public event EventHandler? MyAnimesSolicitado;
    public event EventHandler? MyMusicXSolicitado;
    public event EventHandler? NinoTISolicitado;

    /// <summary>Bloqueia o atalho do DtudoSite enquanto a abertura do site esta em andamento.</summary>
    public void DefinirDtudoSiteOcupado(bool ocupado)
    {
        _dtudoSiteOcupado = ocupado;
        AtualizarAtalhos();
    }

    /// <summary>
    /// Libera (colorido e clicavel) ou bloqueia (preto e branco e desabilitado) os atalhos, conforme o usuario
    /// esteja conectado ou nao.
    /// </summary>
    public void DefinirAcessoLiberado(bool liberado)
    {
        _acessoLiberado = liberado;
        AtualizarAtalhos();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // O fundo binario tem transparencia: ele e desenhado sobre preto, independentemente do BackColor/tema.
        e.Graphics.Clear(Color.Black);
        e.Graphics.DrawImage(Properties.Resources.code01_background, ClientRectangle);
    }

    private void AtualizarAtalhos()
    {
        foreach (var button in new[] { _btnDtudoSite, _btnMyAnimes, _btnMyMusicX, _btnNinoTI })
        {
            button.Grayscale = !_acessoLiberado;
            button.Enabled = _acessoLiberado && (button != _btnDtudoSite || !_dtudoSiteOcupado);
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ApplyLayout();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ApplyLayout();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var (label, _, _, _) in _scalableLabels)
                label.Font.Dispose();
            _scalableLabels.Clear();
        }

        base.Dispose(disposing);
    }

    private Label CreateLabel(
        string name,
        string text,
        string fontFamily,
        float fontSize,
        FontStyle style,
        ContentAlignment alignment = ContentAlignment.MiddleCenter)
    {
        var font = new Font(fontFamily, fontSize, style, GraphicsUnit.Point);
        var label = new Label
        {
            Name = name,
            Text = text,
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = DarkModeColors.TextColor,
            Font = font,
            TextAlign = alignment,
            UseMnemonic = false
        };
        _scalableLabels.Add((label, font.FontFamily, fontSize, style));
        return label;
    }

    private static NavigationImageButton CreateButton(string name, Image image, string accessibleName) => new()
    {
        Name = name,
        Image = image,
        SizeMode = PictureBoxSizeMode.StretchImage,
        AccessibleName = accessibleName,
        AccessibleRole = AccessibleRole.PushButton
    };

    private void ApplyLayout()
    {
        if (_isApplyingLayout || IsDisposed || ClientSize.Width <= 0 || ClientSize.Height <= 0)
            return;

        _isApplyingLayout = true;
        SuspendLayout();
        try
        {
            var scale = Math.Min(ClientSize.Width / DesignWidth, ClientSize.Height / DesignHeight);
            var offsetX = (ClientSize.Width - DesignWidth * scale) / 2F;
            var offsetY = (ClientSize.Height - DesignHeight * scale) / 2F;

            SetScaledBounds(_titulo, 801F, 12F, 296F, 90F, scale, offsetX, offsetY);
            SetScaledBounds(_btnDtudoSite, 280F, 85F, 242F, 86F, scale, offsetX, offsetY);
            SetScaledBounds(_lblDtudoSite, 333F, 51F, 157F, 42F, scale, offsetX, offsetY);
            SetScaledBounds(_btnNinoTI, 1044F, 233F, 144F, 120F, scale, offsetX, offsetY);
            SetScaledBounds(_lblNinoTI, 1058F, 204F, 111F, 37F, scale, offsetX, offsetY);
            SetScaledBounds(_btnMyMusicX, 86F, 322F, 85F, 213F, scale, offsetX, offsetY);
            SetScaledBounds(_lblMyMusicX, 42F, 537F, 160F, 53F, scale, offsetX, offsetY);
            SetScaledBounds(_btnMyAnimes, 532F, 420F, 272F, 232F, scale, offsetX, offsetY);
            SetScaledBounds(_lblMyAnimes, 588F, 385F, 157F, 46F, scale, offsetX, offsetY);
            ApplyFontScale(scale);
        }
        finally
        {
            ResumeLayout(true);
            _isApplyingLayout = false;
        }
    }

    // As fontes acompanham a escala do layout em pixels reais (compensando o DPI do monitor), para que os
    // textos mantenham a proporcao com as imagens e nao sejam cortados.
    private void ApplyFontScale(float layoutScale)
    {
        var fontScale = MathF.Round(layoutScale * 96F / Math.Max(1, DeviceDpi) * 16F) / 16F;
        if (MathF.Abs(fontScale - _appliedFontScale) < 0.001F)
            return;

        _appliedFontScale = fontScale;
        for (var index = 0; index < _scalableLabels.Count; index++)
        {
            var (label, family, size, style) = _scalableLabels[index];
            var previousFont = label.Font;
            label.Font = new Font(family, Math.Max(MinimumFontSize, size * fontScale), style, GraphicsUnit.Point);
            previousFont.Dispose();
        }
    }

    private static void SetScaledBounds(
        Control control,
        float x,
        float y,
        float width,
        float height,
        float scale,
        float offsetX,
        float offsetY)
    {
        var bounds = new Rectangle(
            (int)Math.Round(offsetX + x * scale),
            (int)Math.Round(offsetY + y * scale),
            Math.Max(1, (int)Math.Round(width * scale)),
            Math.Max(1, (int)Math.Round(height * scale)));

        if (control.Bounds != bounds)
            control.Bounds = bounds;
    }
}
