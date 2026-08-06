using System.Diagnostics;

namespace LibDtudo.Shared.Logging;

public sealed class CorrelationIdDelegatingHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var existingHeader = request.Headers.TryGetValues(CorrelationContext.HeaderName, out var values)
            ? CorrelationContext.Normalize(values.FirstOrDefault())
            : null;

        var correlationId = CorrelationContext.Current
            ?? existingHeader
            ?? Activity.Current?.TraceId.ToHexString()
            ?? Guid.NewGuid().ToString("N");

        request.Headers.Remove(CorrelationContext.HeaderName);
        request.Headers.TryAddWithoutValidation(CorrelationContext.HeaderName, correlationId);

        return base.SendAsync(request, cancellationToken);
    }
}
