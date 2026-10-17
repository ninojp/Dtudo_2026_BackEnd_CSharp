namespace WinAppDtudo.Controls;

/// <summary>Intervalo de caracteres de uma linha produzida por <see cref="TextLineBreaker"/>.</summary>
internal readonly record struct TextLine(int Start, int Length)
{
    public int End => Start + Length;
}

/// <summary>
/// Quebra um texto em linhas pelos espaços, partindo apenas palavras maiores que a largura.
/// A medição é injetada para que controles com várias cores ou fontes usem o mesmo algoritmo
/// de medição, desenho e seleção.
/// </summary>
internal static class TextLineBreaker
{
    /// <param name="text">Texto completo; quebras explícitas (\r, \n) sempre encerram a linha.</param>
    /// <param name="maxWidth">Largura máxima de uma linha.</param>
    /// <param name="measure">Mede a largura do trecho (início, comprimento) do texto.</param>
    /// <param name="isUnbreakableSpace">Indica espaços (pelo índice) que não podem iniciar uma nova linha.</param>
    public static List<TextLine> Break(
        string text,
        int maxWidth,
        Func<int, int, int> measure,
        Func<int, bool>? isUnbreakableSpace = null)
    {
        var lines = new List<TextLine>();
        var length = text.Length;
        maxWidth = Math.Max(1, maxWidth);
        var position = 0;

        while (position < length)
        {
            var hardEnd = FindLineBreak(text, position);
            if (hardEnd == position)
            {
                lines.Add(new TextLine(position, 0));
                position = SkipLineBreak(text, position);
                continue;
            }

            var lineEnd = position;
            var wordStart = position;
            while (wordStart < hardEnd)
            {
                var wordEnd = FindWordEnd(text, wordStart, hardEnd, isUnbreakableSpace);
                var width = measure(position, wordEnd - position);
                if (width <= maxWidth)
                {
                    lineEnd = wordEnd;
                    wordStart = wordEnd;
                    continue;
                }

                if (lineEnd == position)
                    lineEnd = position + FitCharacters(text, position, wordEnd, maxWidth, measure);
                break;
            }

            lines.Add(new TextLine(position, lineEnd - position));
            position = lineEnd;
            while (position < length && text[position] == ' ')
                position++;
            if (position < length && (text[position] == '\r' || text[position] == '\n'))
                position = SkipLineBreak(text, position);
        }

        return lines;
    }

    private static int FindLineBreak(string text, int from)
    {
        var index = text.AsSpan(from).IndexOfAny('\r', '\n');
        return index < 0 ? text.Length : from + index;
    }

    private static int SkipLineBreak(string text, int position)
    {
        if (position < text.Length && text[position] == '\r')
            position++;
        if (position < text.Length && text[position] == '\n')
            position++;
        return position;
    }

    private static int FindWordEnd(string text, int from, int limit, Func<int, bool>? isUnbreakableSpace)
    {
        bool IsBreakableSpace(int index) => text[index] == ' ' && isUnbreakableSpace?.Invoke(index) != true;

        var index = from;
        while (index < limit && IsBreakableSpace(index))
            index++;
        while (index < limit && !IsBreakableSpace(index))
            index++;
        return index;
    }

    /// <summary>Quantidade de caracteres (no mínimo um) que cabem na largura, sem separar pares substitutos.</summary>
    private static int FitCharacters(string text, int start, int end, int maxWidth, Func<int, int, int> measure)
    {
        var low = 1;
        var high = end - start;
        while (low < high)
        {
            var middle = (low + high + 1) / 2;
            if (measure(start, middle) <= maxWidth)
                low = middle;
            else
                high = middle - 1;
        }

        var cut = start + low;
        if (cut < end && char.IsLowSurrogate(text[cut]) && char.IsHighSurrogate(text[cut - 1]))
            cut = cut - start > 1 ? cut - 1 : cut + 1;
        return cut - start;
    }
}
