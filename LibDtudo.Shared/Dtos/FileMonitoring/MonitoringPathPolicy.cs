namespace LibDtudo.Shared.Dtos.FileMonitoring;

public static class MonitoringPathPolicy
{
    public static IReadOnlyList<MonitoringRootDto> CollectionRoots() =>
        new[] { ".Dots" }.Concat("ABCDEFGHIJKLMNOPQ".Select(letter => letter.ToString()))
            .Select(letter => new MonitoringRootDto("E_" + letter, @"E:\" + letter))
            .Concat("RSTUVWXYZ".Select(letter => new MonitoringRootDto("H_" + letter, @"H:\" + letter)))
            .Concat(new[] { ".Dots" }.Concat("ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(letter => letter.ToString()))
                .Select(letter => new MonitoringRootDto("X_" + letter, @"H:\AnimeX\" + letter))).ToArray();

    public static string NormalizeRootKey(string rootKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootKey);
        if (rootKey.Equals("H_#Dots", StringComparison.OrdinalIgnoreCase)) return "E_.Dots";
        if (rootKey.Equals("E_R", StringComparison.OrdinalIgnoreCase)) return "H_R";
        if (rootKey.Equals("J_T", StringComparison.OrdinalIgnoreCase)) return "H_T";
        if (rootKey.Equals("X_#Dots", StringComparison.OrdinalIgnoreCase)) return "X_.Dots";
        if (rootKey.Length == 3 && rootKey.StartsWith("H_", StringComparison.OrdinalIgnoreCase)
            && "ABCDEFGHIJKLMNOPQ".Contains(char.ToUpperInvariant(rootKey[2])))
            return "E_" + char.ToUpperInvariant(rootKey[2]);
        if (rootKey.Length == 3 && rootKey.StartsWith("G_", StringComparison.OrdinalIgnoreCase)
            && "SVWXYZ".Contains(char.ToUpperInvariant(rootKey[2])))
            return "H_" + char.ToUpperInvariant(rootKey[2]);
        return rootKey;
    }

    public static bool IsCurrentRootKey(string? rootKey) => rootKey is not null
        && CollectionRoots().Any(root => root.Key.Equals(rootKey, StringComparison.Ordinal));

    public static bool IsExcluded(string name) => name.Equals(".ImportanteX", StringComparison.OrdinalIgnoreCase)
        || name.Equals("$RECYCLE.BIN", StringComparison.OrdinalIgnoreCase)
        || name.Equals("System Volume Information", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith(".dtudo-", StringComparison.OrdinalIgnoreCase);

    public static bool IsValidRelativePath(string? path, bool allowEmpty = false)
    {
        if (path is null) return false;
        if (path.Length == 0) return allowEmpty;
        if (path.Length > 32000 || Path.IsPathRooted(path) || path.Contains('/') || path.Contains(':') || path.Any(char.IsControl)) return false;
        foreach (var segment in path.Split('\\'))
        {
            if (string.IsNullOrWhiteSpace(segment) || segment.Length > 255 || segment is "." or ".."
                || segment.EndsWith('.') || segment.EndsWith(' ') || IsExcluded(segment)
                || segment.IndexOfAny(['"', '<', '>', '|', '*', '?']) >= 0) return false;
            var name = segment.Split('.')[0];
            if (name.Equals("CON", StringComparison.OrdinalIgnoreCase) || name.Equals("PRN", StringComparison.OrdinalIgnoreCase)
                || name.Equals("AUX", StringComparison.OrdinalIgnoreCase) || name.Equals("NUL", StringComparison.OrdinalIgnoreCase)
                || (name.Length == 4 && (name.StartsWith("COM", StringComparison.OrdinalIgnoreCase) || name.StartsWith("LPT", StringComparison.OrdinalIgnoreCase))
                    && name[3] is >= '1' and <= '9')) return false;
        }
        return true;
    }
}
