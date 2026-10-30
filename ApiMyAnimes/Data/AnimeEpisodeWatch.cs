namespace ApiMyAnimes.Data;

/// <summary>
/// Episodio de um anime marcado como visto por um usuario. A chave primaria e (AccountId, MalId, EpisodeNumber):
/// marcar o mesmo episodio duas vezes nao cria uma segunda linha.
/// </summary>
public class AnimeEpisodeWatch
{
    /// <summary>Tamanho maximo do identificador da conta (claim <c>sub</c> do token).</summary>
    public const int AccountIdMaxLength = 128;

    /// <summary>Identificador da conta: claim <c>sub</c> do token, nunca um dado enviado pelo cliente.</summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>Id do anime no MyAnimeList (<c>Animes.MalId</c>). Sem chave estrangeira: o progresso nao e apagado se o anime for recriado.</summary>
    public int MalId { get; set; }

    /// <summary>Numero do episodio visto (a partir de 1).</summary>
    public int EpisodeNumber { get; set; }

    /// <summary>Momento (UTC) em que o episodio foi marcado como visto.</summary>
    public DateTime WatchedAtUtc { get; set; }
}
