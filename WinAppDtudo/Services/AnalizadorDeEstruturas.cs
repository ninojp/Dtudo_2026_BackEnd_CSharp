using System.Globalization;
using LibDtudo.Shared.Dtos;

namespace WinAppDtudo.Services;

public class AnalizadorDeEstruturas
{
    private static readonly HashSet<string> ExtensoesDeImagemAceitas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp"
    };

    public AnaliseEstruturas AnalisarDiretorio(string diretorioRaiz, IProgress<ProgressoAnalise>? progresso = null)
    {
        if (string.IsNullOrWhiteSpace(diretorioRaiz))
            throw new ArgumentException("O diretório raiz deve ser informado.", nameof(diretorioRaiz));

        if (!Directory.Exists(diretorioRaiz))
            throw new DirectoryNotFoundException($"Diretório não encontrado: {diretorioRaiz}");

        var analise = new AnaliseEstruturas
        {
            DiretorioRaiz = diretorioRaiz,
            DataAnalise = DateTime.Now
        };

        var pastasMyAnime = Directory.GetDirectories(diretorioRaiz, "*", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        progresso?.Report(new ProgressoAnalise
        {
            PercentualConcluido = 0,
            Mensagem = "Preparando análise das estruturas..."
        });

        if (pastasMyAnime.Count == 0)
        {
            progresso?.Report(new ProgressoAnalise
            {
                PercentualConcluido = 100,
                Mensagem = "Nenhuma pasta encontrada para análise."
            });
            return analise;
        }

        for (var indice = 0; indice < pastasMyAnime.Count; indice++)
        {
            var pastaMyAnime = pastasMyAnime[indice];
            var item = new MyAnimeEstruturaAnalise
            {
                Titulo = Path.GetFileName(pastaMyAnime).Trim(),
                Caminho = pastaMyAnime
            };

            analise.Itens.Add(item);
            AnalisarSubPastasDoMyAnime(item, analise.Erros);

            var idsUnicos = item.Animes
                .SelectMany(a => a.IdsNumericos)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (string.IsNullOrWhiteSpace(item.Titulo))
            {
                item.Avisos.Add("Pasta ignorada porque o título ficou vazio após o tratamento do nome.");
                continue;
            }

            if (idsUnicos.Count == 0)
            {
                item.Avisos.Add("Nenhum arquivo de imagem com nome numérico foi encontrado nas subpastas de primeiro nível.");
                continue;
            }

            item.MyAnimeDto = new AdicionaMyAnimeDto
            {
                Titulo = item.Titulo,
                AnimesMalId = idsUnicos
            };

            var percentual = (int)Math.Round(((indice + 1) / (double)pastasMyAnime.Count) * 100, MidpointRounding.AwayFromZero);
            progresso?.Report(new ProgressoAnalise
            {
                PercentualConcluido = Math.Clamp(percentual, 0, 100),
                Mensagem = $"Analisando {indice + 1}/{pastasMyAnime.Count}: {item.Titulo}"
            });
        }

        progresso?.Report(new ProgressoAnalise
        {
            PercentualConcluido = 100,
            Mensagem = "Análise concluída."
        });

        return analise;
    }

    private static void AnalisarSubPastasDoMyAnime(MyAnimeEstruturaAnalise item, List<string> erros)
    {
        string[] subPastas;
        try
        {
            subPastas = Directory.GetDirectories(item.Caminho, "*", SearchOption.TopDirectoryOnly);
        }
        catch (Exception ex)
        {
            erros.Add($"Falha ao listar subpastas de '{item.Caminho}': {ex.Message}");
            return;
        }

        foreach (var subPasta in subPastas.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            var animeAnalise = new AnimeEstruturaAnalise
            {
                NomePasta = Path.GetFileName(subPasta),
                Caminho = subPasta
            };

            item.Animes.Add(animeAnalise);

            string[] arquivos;
            try
            {
                arquivos = Directory.GetFiles(subPasta, "*", SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex)
            {
                erros.Add($"Falha ao listar arquivos de '{subPasta}': {ex.Message}");
                continue;
            }

            animeAnalise.TotalArquivosAnalisados = arquivos.Length;

            foreach (var arquivo in arquivos)
            {
                var extensao = Path.GetExtension(arquivo);
                if (!ExtensoesDeImagemAceitas.Contains(extensao))
                    continue;

                var nomeSemExtensao = Path.GetFileNameWithoutExtension(arquivo).Trim();

                if (int.TryParse(nomeSemExtensao, NumberStyles.None, CultureInfo.InvariantCulture, out var malId))
                {
                    animeAnalise.IdsNumericos.Add(malId);
                }
                else
                {
                    animeAnalise.ArquivosDeImagemIgnorados.Add(Path.GetFileName(arquivo));
                }
            }

            animeAnalise.IdsNumericos = animeAnalise.IdsNumericos
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }
    }
}

public class AnaliseEstruturas
{
    public string DiretorioRaiz { get; init; } = string.Empty;
    public DateTime DataAnalise { get; init; }
    public List<MyAnimeEstruturaAnalise> Itens { get; } = [];
    public List<string> Erros { get; } = [];

    public List<AdicionaMyAnimeDto> MyAnimesParaPersistir => Itens
        .Where(i => i.MyAnimeDto is not null)
        .Select(i => i.MyAnimeDto!)
        .ToList();

    public string CriarResumo()
    {
        var totalMyAnimes = Itens.Count;
        var validos = MyAnimesParaPersistir.Count;
        var totalPastasAnimes = Itens.Sum(i => i.Animes.Count);
        var totalIds = MyAnimesParaPersistir.Sum(i => i.AnimesMalId.Count);

        var resumo =
            $"Raiz analisada: {DiretorioRaiz}\n" +
            $"Pastas MyAnime encontradas: {totalMyAnimes}\n" +
            $"Subpastas de Anime analisadas: {totalPastasAnimes}\n" +
            $"MyAnimes válidos para persistir: {validos}\n" +
            $"Total de IDs numéricos encontrados: {totalIds}";

        if (Erros.Count > 0)
            resumo += $"\nErros de acesso/processamento: {Erros.Count}";

        return resumo;
    }
}

public class MyAnimeEstruturaAnalise
{
    public string Titulo { get; init; } = string.Empty;
    public string Caminho { get; init; } = string.Empty;
    public List<AnimeEstruturaAnalise> Animes { get; } = [];
    public List<string> Avisos { get; } = [];
    public AdicionaMyAnimeDto? MyAnimeDto { get; set; }
}

public class AnimeEstruturaAnalise
{
    public string NomePasta { get; init; } = string.Empty;
    public string Caminho { get; init; } = string.Empty;
    public int TotalArquivosAnalisados { get; set; }
    public List<int> IdsNumericos { get; set; } = [];
    public List<string> ArquivosDeImagemIgnorados { get; } = [];
}

public class ProgressoAnalise
{
    public int PercentualConcluido { get; init; }
    public string Mensagem { get; init; } = string.Empty;
}
