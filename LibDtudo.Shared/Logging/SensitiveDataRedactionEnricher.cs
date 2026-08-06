using Serilog.Core;
using Serilog.Events;

namespace LibDtudo.Shared.Logging;

public sealed class SensitiveDataRedactionEnricher : ILogEventEnricher
{
    private const string RedactedValue = "[REDACTED]";

    private static readonly string[] SensitivePropertyTokens =
    [
        "password",
        "passwd",
        "secret",
        "token",
        "authorization",
        "cookie",
        "apikey",
        "api-key",
        "clientid",
        "connectionstring",
        "accesskey",
        "privatekey",
        "requestbody",
        "responsebody",
        "body"
    ];

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var property in logEvent.Properties.ToArray())
        {
            var redactedValue = RedactProperty(property.Key, property.Value);
            if (!ReferenceEquals(redactedValue, property.Value))
            {
                logEvent.AddOrUpdateProperty(new LogEventProperty(property.Key, redactedValue));
            }
        }
    }

    private static LogEventPropertyValue RedactProperty(string propertyName, LogEventPropertyValue value)
    {
        if (IsSensitiveProperty(propertyName)) return new ScalarValue(RedactedValue);
        return RedactValue(value);
    }

    private static LogEventPropertyValue RedactValue(LogEventPropertyValue value)
        => value switch
        {
            StructureValue structure => new StructureValue(
                structure.Properties
                    .Select(property => new LogEventProperty(
                        property.Name,
                        RedactProperty(property.Name, property.Value)))
                    .ToList(),
                structure.TypeTag),
            SequenceValue sequence => new SequenceValue(sequence.Elements.Select(RedactValue).ToList()),
            DictionaryValue dictionary => new DictionaryValue(
                dictionary.Elements
                    .Select(element => new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                        element.Key,
                        RedactValue(element.Value)))
                    .ToList()),
            _ => value
        };

    private static bool IsSensitiveProperty(string propertyName)
        => SensitivePropertyTokens.Any(token => propertyName.Contains(token, StringComparison.OrdinalIgnoreCase));
}
