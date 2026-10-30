using System.Globalization;
using System.Text;

namespace ApiMyAnimes.Services;

/// <summary>
/// Normalizacao e comparacao de nomes de pasta com titulos do catalogo. Compartilhada pelo sincronizador de colecoes
/// (nome da pasta da colecao) e pelo mapeamento de pastas de anime (<c>&lt;ano&gt; &lt;titulo&gt; - &lt;tipo&gt;</c>).
/// </summary>
public static class FolderNameMatcher
{
    /// <summary>Quantidade de palavras iniciais comparadas por <see cref="ShareFirstWords(string, string)"/>.</summary>
    public const int FirstWordsCount = 4;

    /// <summary>
    /// Palavras alfanumericas em maiusculas, sem acentos nem pontuacao (ex.: "Sen to Chihiro - TV" vira SEN, TO, CHIHIRO, TV).
    /// </summary>
    public static IReadOnlyList<string> GetWords(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var words = new List<string>();
        var current = new StringBuilder();
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(character))
            {
                current.Append(char.ToUpperInvariant(character));
                continue;
            }
            if (current.Length > 0) { words.Add(current.ToString()); current.Clear(); }
        }
        if (current.Length > 0) words.Add(current.ToString());
        return words;
    }

    /// <summary>Verdadeiro quando as primeiras (ate quatro) palavras do nome e do titulo sao iguais.</summary>
    public static bool ShareFirstWords(string folderName, string catalogTitle) =>
        ShareFirstWords(GetWords(folderName), GetWords(catalogTitle));

    /// <inheritdoc cref="ShareFirstWords(string, string)"/>
    public static bool ShareFirstWords(IReadOnlyList<string> folderWords, IReadOnlyList<string> titleWords)
    {
        var count = Math.Min(FirstWordsCount, Math.Min(folderWords.Count, titleWords.Count));
        return count > 0 && folderWords.Take(count).SequenceEqual(titleWords.Take(count), StringComparer.OrdinalIgnoreCase);
    }
}
