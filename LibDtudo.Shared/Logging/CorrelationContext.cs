namespace LibDtudo.Shared.Logging;

public static class CorrelationContext
{
    public const string HeaderName = "X-Correlation-ID";

    private static readonly AsyncLocal<string?> CurrentValue = new();

    public static string? Current => CurrentValue.Value;

    public static string GetOrCreate(string? requestedValue)
        => Normalize(requestedValue) ?? Guid.NewGuid().ToString("N");

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var normalized = value.Trim();
        if (normalized.Length is 0 or > 64) return null;

        foreach (var character in normalized)
        {
            var isAsciiLetter = character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
            var isAsciiDigit = character is >= '0' and <= '9';
            if (!isAsciiLetter && !isAsciiDigit && character is not ('.' or '-' or '_')) return null;
        }

        return normalized;
    }

    public static IDisposable Push(string correlationId)
    {
        var previousValue = CurrentValue.Value;
        CurrentValue.Value = correlationId;
        return new Scope(previousValue);
    }

    private sealed class Scope(string? previousValue) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;

            CurrentValue.Value = previousValue;
            _disposed = true;
        }
    }
}
