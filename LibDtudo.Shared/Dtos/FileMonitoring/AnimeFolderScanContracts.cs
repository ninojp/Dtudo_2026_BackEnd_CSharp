namespace LibDtudo.Shared.Dtos.FileMonitoring;

/// <summary>Estados do indicador de pasta do anime (valores do contrato JSON).</summary>
public static class AnimeFolderState
{
    public const string None = "none";
    public const string Partial = "partial";
    public const string Complete = "complete";

    /// <summary>Normaliza o texto recebido de uma API; qualquer valor desconhecido vira <see cref="None"/>.</summary>
    public static string Normalize(string? state) =>
        state?.Trim().ToLowerInvariant() switch
        {
            Partial => Partial,
            Complete => Complete,
            _ => None
        };

    /// <summary>
    /// <c>none</c>: pasta inexistente/indisponivel ou sem nenhum video de episodio. <c>complete</c>: leitura integral
    /// (sem <paramref name="truncated"/>), total conhecido (&gt; 0) e videos &gt;= total. <c>partial</c>: ha videos, mas menos
    /// que o total, ou o total e desconhecido, ou a leitura foi truncada.
    /// </summary>
    public static string Evaluate(bool exists, int videoCount, int? expectedEpisodes, bool truncated)
    {
        if (!exists || videoCount <= 0) return None;
        return !truncated && expectedEpisodes is > 0 && videoCount >= expectedEpisodes ? Complete : Partial;
    }
}

/// <summary>Pede a contagem sob demanda de videos de episodio na pasta de um anime.</summary>
/// <param name="RootKey">Chave de raiz autorizada (ver <c>GET monitoring/roots</c>).</param>
/// <param name="RelativePath">Pasta do anime relativa a raiz; nao pode ser vazia.</param>
/// <param name="ExpectedEpisodes">Total de episodios do catalogo; nulo ou 0 significa desconhecido.</param>
public sealed record AnimeFolderScanRequest(string RootKey, string RelativePath, int? ExpectedEpisodes = null);

/// <summary>Resultado da contagem; nunca contem caminho fisico.</summary>
/// <param name="Exists">A pasta existe e pode ser lida sem redirecionamentos.</param>
/// <param name="VideoCount">Videos de episodio encontrados (ver <see cref="EpisodeVideoPolicy"/>).</param>
/// <param name="DirectoryCount">Subpastas percorridas (extras, links e itens de sistema nao contam).</param>
/// <param name="State"><c>none</c>, <c>partial</c> ou <c>complete</c> (ver <see cref="AnimeFolderState"/>).</param>
/// <param name="Truncated">Limite atingido ou subpasta ilegivel: contagens podem estar abaixo do real.</param>
public sealed record AnimeFolderScanResponse(bool Exists, int VideoCount, int DirectoryCount, string State, bool Truncated);

/// <summary>Pede a lista das pastas de anime (subpastas de 1o nivel) de uma pasta de colecao, com suas capas numericas.</summary>
/// <param name="RootKey">Chave de raiz autorizada (ver <c>GET monitoring/roots</c>).</param>
/// <param name="RelativePath">Pasta da colecao relativa a raiz; nao pode ser vazia.</param>
public sealed record AnimeFolderDiscoveryRequest(string RootKey, string RelativePath);

/// <summary>Pasta candidata a pasta de anime, com os MalIds das capas <c>&lt;MalId&gt;.jpg</c> do seu 1o nivel.</summary>
/// <param name="RelativePath">Caminho relativo a raiz (separador <c>\</c>); nunca fisico.</param>
/// <param name="CoverIds">MalIds das imagens numericas diretamente dentro da pasta, em ordem crescente.</param>
public sealed record AnimeFolderCandidateDto(string RelativePath, int[] CoverIds);

/// <summary>Resultado da descoberta; nunca contem caminho fisico.</summary>
/// <param name="Exists">A pasta da colecao existe e pode ser lida sem redirecionamentos.</param>
/// <param name="Folders">Subpastas diretas e, quando ha capas diretas, a propria pasta da colecao.</param>
/// <param name="Truncated">Limite atingido ou pasta ilegivel: a lista pode estar incompleta.</param>
public sealed record AnimeFolderDiscoveryResponse(bool Exists, AnimeFolderCandidateDto[] Folders, bool Truncated);
