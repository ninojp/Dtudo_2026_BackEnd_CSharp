namespace ApiMyAnimes.Data;

/// <summary>
/// Ultimo resultado persistido da verificacao da pasta de um anime no disco (indicador de pasta dos cards).
/// Calculado sob demanda pelo servico de atualizacao; o site apenas le. Sem chave estrangeira para <c>Animes</c>
/// (o anime pode ser reimportado sem perder o resultado), como em <see cref="AnimeEpisodeWatch"/>.
/// </summary>
public sealed class AnimeFolderStatus
{
    public const int RootKeyMaxLength = 100;
    public const int StateMaxLength = 10;

    /// <summary>Id do anime no MyAnimeList (chave; nao auto-incremento).</summary>
    public int MalId { get; set; }

    /// <summary>Colecao (<c>MyAnime.Id</c>) a que o anime pertencia na verificacao.</summary>
    public int MyAnimeId { get; set; }

    /// <summary>Raiz de monitoramento da pasta encontrada; vazio quando nenhuma pasta foi encontrada.</summary>
    public string RootKey { get; set; } = string.Empty;

    /// <summary>Pasta do anime relativa a raiz; vazio quando nenhuma pasta foi encontrada. Nunca e caminho fisico.</summary>
    public string RelativePath { get; set; } = string.Empty;

    public int VideoCount { get; set; }

    /// <summary>Total de episodios do catalogo na verificacao; <c>null</c> se desconhecido.</summary>
    public int? ExpectedEpisodes { get; set; }

    /// <summary><c>none</c>, <c>partial</c> ou <c>complete</c> (ver <see cref="Services.AnimeStatusStates"/>).</summary>
    public string State { get; set; } = Services.AnimeStatusStates.None;

    /// <summary>A leitura bateu em um limite: a contagem pode estar abaixo do real.</summary>
    public bool Truncated { get; set; }

    public DateTime CheckedAtUtc { get; set; }
}

/// <summary>Execucao (persistida e consultavel) de uma atualizacao de status de pasta.</summary>
public sealed class AnimeFolderRefreshRun
{
    public const int RequestedByMaxLength = 256;
    public const int ErrorMaxLength = 2000;

    public Guid Id { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }

    /// <summary>Ver <see cref="LibDtudo.Shared.Dtos.AnimeFolderRefreshStatuses"/>.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Colecao atualizada; <c>null</c> quando a execucao cobre todas.</summary>
    public int? MyAnimeId { get; set; }

    public string RequestedBy { get; set; } = string.Empty;
    public int TotalAnimes { get; set; }
    public int ProcessedAnimes { get; set; }
    public int Updated { get; set; }
    public int NotFound { get; set; }
    public int Unavailable { get; set; }
    public int Failed { get; set; }
    public string? Error { get; set; }
}

/// <summary>Resultado de um anime dentro de uma execucao (relatorio por anime).</summary>
public sealed class AnimeFolderRefreshItem
{
    public const int DetailMaxLength = 500;

    public long Id { get; set; }
    public Guid RunId { get; set; }
    public int MalId { get; set; }
    public int MyAnimeId { get; set; }

    /// <summary>Ver <see cref="LibDtudo.Shared.Dtos.AnimeFolderRefreshOutcomes"/>.</summary>
    public string Outcome { get; set; } = string.Empty;

    public string? State { get; set; }
    public int? VideoCount { get; set; }
    public string Detail { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
}
