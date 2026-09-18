namespace LibDtudo.Shared.Dtos.FileMonitoring;

public static class MonitoringPathPolicy
{
    public static IReadOnlyList<MonitoringRootDto> CollectionRoots() =>
        new[] { "#Dots" }.Concat("ABCDEFGHIJKLMNOPQRU".Select(letter => letter.ToString()))
            .Select(letter => new MonitoringRootDto("H_" + letter, @"H:\" + letter))
            .Concat("SVWXYZ".Select(letter => new MonitoringRootDto("G_" + letter, @"G:\" + letter)))
            .Append(new MonitoringRootDto("J_T", @"J:\T"))
            .Concat(new[] { "#Dots" }.Concat("ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(letter => letter.ToString()))
                .Select(letter => new MonitoringRootDto("X_" + letter, @"G:\AnimeX\" + letter))).ToArray();

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
