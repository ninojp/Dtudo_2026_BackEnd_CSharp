using System.ComponentModel.DataAnnotations;

namespace LibDtudo.Shared.Dtos;

/// <summary>
/// Contrato interno (servico-a-servico) entre a ApiIdentity e a ApiMyAnimes para exportacao e exclusao
/// de dados pessoais por conta (LGPD). Nunca contem caminhos fisicos nem dados de outras contas.
/// </summary>
public static class MyAnimesPrivacyContract
{
    /// <summary>Tamanho maximo do identificador da conta (igual ao limite do progresso de episodios).</summary>
    public const int AccountIdMaxLength = 128;

    /// <summary>Versao do formato da secao exportada pela ApiMyAnimes.</summary>
    public const string ExportSchemaVersion = "1";

    /// <summary>Rota de exportacao (POST; a conta vai no corpo para nao aparecer em URL/log de requisicao).</summary>
    public const string ExportRoute = "apiLocal/internal/privacy/export";

    /// <summary>Rota de exclusao idempotente (POST; a conta vai no corpo).</summary>
    public const string EraseRoute = "apiLocal/internal/privacy/erase";

    /// <summary>Permissao e escopo de servico exigidos nas duas rotas.</summary>
    public const string ServicePermission = "service.privacy.myanimes";

    /// <summary>Audiencia (recurso) do token de servico.</summary>
    public const string ServiceAudience = "urn:dtudo:api-my-animes";

    /// <summary>Regra de formato do identificador: nao vazio, ate o limite, sem espacos nem caracteres de controle.</summary>
    public static bool IsValidAccountId(string? accountId) =>
        !string.IsNullOrEmpty(accountId)
        && accountId.Length <= AccountIdMaxLength
        && !accountId.Any(character => char.IsWhiteSpace(character) || char.IsControl(character));
}

/// <summary>Corpo das chamadas internas de privacidade.</summary>
public sealed class MyAnimesPrivacyAccountRequest
{
    [Required]
    [StringLength(MyAnimesPrivacyContract.AccountIdMaxLength, MinimumLength = 1)]
    public string AccountId { get; set; } = string.Empty;
}

/// <summary>Episodio visto exportado.</summary>
public sealed record MyAnimesPrivacyEpisodeDto(int EpisodeNumber, DateTimeOffset WatchedAtUtc);

/// <summary>Progresso de episodios da conta em um anime.</summary>
public sealed record MyAnimesPrivacyAnimeProgressDto(int MalId, IReadOnlyList<MyAnimesPrivacyEpisodeDto> Episodes);

/// <summary>Execucao de atualizacao de status de pasta iniciada pela conta (sem caminhos nem relatorio por anime).</summary>
public sealed record MyAnimesPrivacyFolderRefreshRunDto(
    Guid Id,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    string Status,
    int? MyAnimeId,
    int TotalAnimes,
    int ProcessedAnimes);

/// <summary>Dados pessoais da conta guardados pela ApiMyAnimes.</summary>
public sealed record MyAnimesPrivacyExportDto(
    string SchemaVersion,
    IReadOnlyList<MyAnimesPrivacyAnimeProgressDto> EpisodeProgress,
    IReadOnlyList<MyAnimesPrivacyFolderRefreshRunDto> FolderRefreshRuns);

/// <summary>Resultado da exclusao (contagens; zero quando nao havia dado, sem erro).</summary>
public sealed record MyAnimesPrivacyEraseResultDto(int DeletedEpisodeRecords, int AnonymizedFolderRefreshRuns);
