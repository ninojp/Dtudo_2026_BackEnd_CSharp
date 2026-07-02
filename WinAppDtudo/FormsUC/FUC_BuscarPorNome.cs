using WinAppDtudo.Controls;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

/// <summary>
/// UserControl responsável por buscar animes por nome via ApiJikan,
/// exibir os resultados como cards clicáveis e gerenciar a paginação.
/// </summary>
public partial class FUC_BuscarPorNome : UserControl
{
    /// <summary>Disparado quando o usuário clica num card. O argumento é o MalId do anime.</summary>
    public event EventHandler<int>? CardClicado;

    private readonly JikanApiService _jikanService = new();
    private int _paginaAtual = 1;
    private string _consultaAtual = string.Empty;
    private bool _carregando;

    public FUC_BuscarPorNome()
    {
        InitializeComponent();
        Btn_BuscarPorNome.Click += Btn_Buscar_Click;
        Btn_PaginaAnterior.Click += Btn_PaginaAnterior_Click;
        Btn_ProximaPagina.Click += Btn_ProximaPagina_Click;
        Txb_InputBuscarPorNome.KeyDown += Txb_Input_KeyDown;
    }

    // ===================================================================

    private async void Btn_Buscar_Click(object? sender, EventArgs e)
    {
        var query = Txb_InputBuscarPorNome.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            MessageBox.Show("Por favor, digite o nome do anime.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Txb_InputBuscarPorNome.Focus();
            return;
        }
        _consultaAtual = query;
        _paginaAtual = 1;
        await ExecutarBuscaAsync();
    }

    private async void Btn_PaginaAnterior_Click(object? sender, EventArgs e)
    {
        if (_paginaAtual > 1)
        {
            _paginaAtual--;
            await ExecutarBuscaAsync();
        }
    }

    private async void Btn_ProximaPagina_Click(object? sender, EventArgs e)
    {
        _paginaAtual++;
        await ExecutarBuscaAsync();
    }

    private async void Txb_Input_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            Btn_BuscarPorNome.PerformClick();
        }
    }

    // ===================================================================

    private async Task ExecutarBuscaAsync()
    {
        if (_carregando) return;
        _carregando = true;
        SetControlesHabilitados(false);
        LimparCards();
        Lbl_Status.Text = "⏳ Buscando...";
        Lbl_Pagina.Text = "Carregando...";
        Btn_PaginaAnterior.Enabled = false;
        Btn_ProximaPagina.Enabled = false;

        try
        {
            var resultado = await _jikanService.BuscarPorNomeAsync(_consultaAtual, _paginaAtual);

            if (resultado.Results.Count == 0)
            {
                Lbl_Status.Text = "Nenhum anime encontrado para esta busca.";
                Lbl_Pagina.Text = "—";
                return;
            }

            Flp_Cards.SuspendLayout();
            foreach (var anime in resultado.Results)
            {
                var card = new UC_AnimeCard();
                card.CarregarDados(anime);
                card.CardClicado += (s, malId) => CardClicado?.Invoke(this, malId);
                Flp_Cards.Controls.Add(card);
            }
            Flp_Cards.ResumeLayout();

            Lbl_Status.Text = $"✅ {resultado.TotalResults:N0} resultado(s).";
            Lbl_Pagina.Text =
                $"Página {resultado.CurrentPage} de {resultado.TotalPages}  |  {resultado.TotalResults:N0} resultado(s)";
            Btn_PaginaAnterior.Enabled = resultado.CurrentPage > 1;
            Btn_ProximaPagina.Enabled = resultado.HasNextPage;
        }
        catch (HttpRequestException ex)
        {
            Lbl_Status.Text = "❌ Erro de conexão.";
            Lbl_Pagina.Text = "—";
            MessageBox.Show(
                $"Não foi possível conectar à API Jikan.\n\n" +
                $"Verifique se o ApiJikan está em execução em:\n{JikanApiService.ApiBase}\n\n" +
                $"Detalhes: {ex.Message}",
                "Erro de Conexão", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            Lbl_Status.Text = "❌ Erro ao buscar.";
            Lbl_Pagina.Text = "—";
            MessageBox.Show($"Erro ao realizar a busca:\n\n{ex.Message}", "Erro",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _carregando = false;
            SetControlesHabilitados(true);
        }
    }

    private void LimparCards()
    {
        var cards = Flp_Cards.Controls.OfType<UC_AnimeCard>().ToList();
        Flp_Cards.Controls.Clear();
        foreach (var c in cards)
            c.Dispose();
    }

    private void SetControlesHabilitados(bool habilitado)
    {
        Btn_BuscarPorNome.Enabled = habilitado;
        Txb_InputBuscarPorNome.Enabled = habilitado;
    }
}
