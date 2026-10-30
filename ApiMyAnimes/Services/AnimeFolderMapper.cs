using System.Text.RegularExpressions;
using LibDtudo.Shared.Dtos.FileMonitoring;

namespace ApiMyAnimes.Services;

/// <summary>Como a pasta de um anime foi resolvida.</summary>
public static class AnimeFolderMatchMethods
{
    /// <summary>Capa <c>&lt;MalId&gt;.jpg</c> no 1o nivel de uma subpasta da colecao.</summary>
    public const string Cover = "cover";

    /// <summary>Capa na propria pasta da colecao (so vale para colecao de um unico anime).</summary>
    public const string CollectionCover = "collection-cover";

    /// <summary>Convencao de nome <c>&lt;ano&gt; &lt;titulo&gt; - &lt;tipo&gt;</c>.</summary>
    public const string Name = "name";
}

/// <summary>Dados minimos de um anime para o mapeamento de pasta.</summary>
public sealed record AnimeFolderMappingAnime(int MalId, int? Year, string? Type, IReadOnlyList<string> Titles);

/// <summary>Pasta resolvida para um anime (caminho relativo a raiz, nunca fisico).</summary>
public sealed record AnimeFolderMatch(string RelativePath, string Method);

/// <summary>
/// Resolve a subpasta de cada anime de uma colecao a partir da descoberta da ApiFileStorage. Ordem: (1) subpasta cuja capa
/// <c>&lt;MalId&gt;.jpg</c> pertence ao anime; (2) convencao de nome <c>&lt;ano&gt; &lt;titulo&gt; - &lt;tipo&gt;</c> (exata e depois
/// pelas primeiras palavras, com ano igual, reaproveitando <see cref="FolderNameMatcher"/>); (3) capa na propria pasta da
/// colecao, somente se a colecao tem um unico anime. Pares ambiguos nao sao resolvidos: o anime fica sem pasta.
/// </summary>
public static partial class AnimeFolderMapper
{
    public static IReadOnlyDictionary<int, AnimeFolderMatch> Map(
        string collectionRelativePath,
        IReadOnlyList<AnimeFolderMappingAnime> animes,
        IReadOnlyList<AnimeFolderCandidateDto> candidates)
    {
        var result = new Dictionary<int, AnimeFolderMatch>();
        var subfolders = candidates.Where(candidate => !SamePath(candidate.RelativePath, collectionRelativePath)).ToList();
        var root = candidates.FirstOrDefault(candidate => SamePath(candidate.RelativePath, collectionRelativePath));
        var ordered = animes.OrderBy(anime => anime.MalId).ToList();
        var usedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var anime in ordered)
        {
            var matches = subfolders.Where(candidate => candidate.CoverIds.Contains(anime.MalId)).ToList();
            if (matches.Count == 0) continue;
            var chosen = matches.Count == 1
                ? matches[0]
                : PickByName(anime, matches) ?? matches.OrderBy(candidate => candidate.RelativePath, StringComparer.OrdinalIgnoreCase).First();
            result[anime.MalId] = new AnimeFolderMatch(chosen.RelativePath, AnimeFolderMatchMethods.Cover);
            usedFolders.Add(chosen.RelativePath);
        }

        var remaining = ordered.Where(anime => !result.ContainsKey(anime.MalId)).ToList();
        var freeFolders = subfolders.Where(candidate => !usedFolders.Contains(candidate.RelativePath)).ToList();
        foreach (var predicate in new Func<AnimeFolderMappingAnime, string, bool>[] { MatchesExactName, MatchesLooseName })
        {
            foreach (var (anime, folder) in UniquePairs(remaining, freeFolders, predicate))
            {
                result[anime.MalId] = new AnimeFolderMatch(folder.RelativePath, AnimeFolderMatchMethods.Name);
            }
            remaining = remaining.Where(anime => !result.ContainsKey(anime.MalId)).ToList();
            freeFolders = freeFolders.Where(folder => !result.Values.Any(match => SamePath(match.RelativePath, folder.RelativePath))).ToList();
        }

        if (root is not null && ordered.Count == 1 && !result.ContainsKey(ordered[0].MalId) && root.CoverIds.Contains(ordered[0].MalId))
            result[ordered[0].MalId] = new AnimeFolderMatch(root.RelativePath, AnimeFolderMatchMethods.CollectionCover);

        return result;
    }

    private static AnimeFolderCandidateDto? PickByName(AnimeFolderMappingAnime anime, List<AnimeFolderCandidateDto> matches)
    {
        var byName = matches.Where(candidate => MatchesExactName(anime, candidate.RelativePath)).ToList();
        if (byName.Count == 0) byName = matches.Where(candidate => MatchesLooseName(anime, candidate.RelativePath)).ToList();
        return byName.Count == 1 ? byName[0] : null;
    }

    // Um par so e aceito quando o anime casa com uma unica pasta e a pasta com um unico anime (sem chute em empates).
    private static IEnumerable<(AnimeFolderMappingAnime Anime, AnimeFolderCandidateDto Folder)> UniquePairs(
        List<AnimeFolderMappingAnime> animes,
        List<AnimeFolderCandidateDto> folders,
        Func<AnimeFolderMappingAnime, string, bool> predicate)
    {
        var pairs = (from anime in animes
                     from folder in folders
                     where predicate(anime, folder.RelativePath)
                     select (anime, folder)).ToList();
        return pairs.Where(pair => pairs.Count(other => other.anime.MalId == pair.anime.MalId) == 1
            && pairs.Count(other => SamePath(other.folder.RelativePath, pair.folder.RelativePath)) == 1);
    }

    private static bool MatchesExactName(AnimeFolderMappingAnime anime, string folderRelativePath)
    {
        var (year, title) = ParseFolderName(anime, folderRelativePath);
        if (year is not null && anime.Year is not null && year != anime.Year) return false;
        return anime.Titles.Any(candidate => title.SequenceEqual(FolderNameMatcher.GetWords(candidate), StringComparer.Ordinal));
    }

    private static bool MatchesLooseName(AnimeFolderMappingAnime anime, string folderRelativePath)
    {
        var (year, title) = ParseFolderName(anime, folderRelativePath);
        if (year is null || anime.Year is null || year != anime.Year) return false;
        return anime.Titles.Any(candidate => FolderNameMatcher.ShareFirstWords(title, FolderNameMatcher.GetWords(candidate)));
    }

    // Nome da pasta -> (ano, palavras do titulo): remove o sufixo " (n)" de duplicatas, o ano inicial e o tipo final.
    private static (int? Year, IReadOnlyList<string> Title) ParseFolderName(AnimeFolderMappingAnime anime, string folderRelativePath)
    {
        var leaf = folderRelativePath[(folderRelativePath.LastIndexOf('\\') + 1)..];
        leaf = DuplicateSuffix().Replace(leaf, string.Empty);
        var words = FolderNameMatcher.GetWords(leaf).ToList();
        int? year = null;
        if (words.Count > 1 && words[0].Length == 4 && words[0].All(char.IsAsciiDigit))
        {
            year = int.Parse(words[0], System.Globalization.CultureInfo.InvariantCulture);
            words.RemoveAt(0);
        }
        if (!string.IsNullOrWhiteSpace(anime.Type))
        {
            var typeWords = FolderNameMatcher.GetWords(anime.Type);
            if (typeWords.Count > 0 && words.Count > typeWords.Count && words.TakeLast(typeWords.Count).SequenceEqual(typeWords, StringComparer.Ordinal))
                words.RemoveRange(words.Count - typeWords.Count, typeWords.Count);
        }
        return (year, words);
    }

    private static bool SamePath(string left, string right) =>
        string.Equals(left.TrimEnd('\\'), right.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"\s*\(\d+\)\s*$")]
    private static partial Regex DuplicateSuffix();
}
