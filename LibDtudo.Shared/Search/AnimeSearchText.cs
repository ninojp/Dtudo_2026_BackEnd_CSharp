namespace LibDtudo.Shared.Search;

public sealed record AnimeSearchText(string Value, string CompactValue, IReadOnlyList<string> Tokens)
{
    private const int MinimumFuzzyTokenLength = 4;

    public static AnimeSearchText Empty { get; } = new(string.Empty, string.Empty, []);

    public bool IsEmpty => Value.Length == 0;

    /// <summary>
    /// Verifica correspondência exata, por trecho ou com pequenos erros de digitação.
    /// A tolerância é limitada por palavra para evitar resultados irrelevantes.
    /// </summary>
    public bool Matches(AnimeSearchText query)
    {
        if (query.IsEmpty || IsEmpty) return false;

        if (Value.Contains(query.Value, StringComparison.Ordinal)
            || CompactValue.Contains(query.CompactValue, StringComparison.Ordinal))
        {
            return true;
        }

        return query.Tokens.All(queryToken =>
            Tokens.Any(candidateToken => TokenMatches(candidateToken, queryToken)));
    }

    private static bool TokenMatches(string candidateToken, string queryToken)
    {
        if (candidateToken.Equals(queryToken, StringComparison.Ordinal)
            || candidateToken.Contains(queryToken, StringComparison.Ordinal)
            || queryToken.Contains(candidateToken, StringComparison.Ordinal))
        {
            return true;
        }

        var shorterLength = Math.Min(candidateToken.Length, queryToken.Length);
        if (shorterLength < MinimumFuzzyTokenLength) return false;

        var maximumDistance = shorterLength >= 8 ? 2 : 1;
        return LevenshteinDistance(candidateToken, queryToken, maximumDistance) <= maximumDistance;
    }

    private static int LevenshteinDistance(string left, string right, int maximumDistance)
    {
        if (Math.Abs(left.Length - right.Length) > maximumDistance) return maximumDistance + 1;

        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];
        for (var index = 0; index <= right.Length; index++) previous[index] = index;

        for (var leftIndex = 1; leftIndex <= left.Length; leftIndex++)
        {
            current[0] = leftIndex;
            var rowMinimum = current[0];

            for (var rightIndex = 1; rightIndex <= right.Length; rightIndex++)
            {
                current[rightIndex] = Math.Min(
                    Math.Min(current[rightIndex - 1] + 1, previous[rightIndex] + 1),
                    previous[rightIndex - 1] + (left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1));
                rowMinimum = Math.Min(rowMinimum, current[rightIndex]);
            }

            if (rowMinimum > maximumDistance) return maximumDistance + 1;
            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }
}
