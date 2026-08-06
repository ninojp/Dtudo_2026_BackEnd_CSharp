using ApiMyAnimes.Data;
using ApiMyAnimes.Services;
using Microsoft.EntityFrameworkCore;

namespace ApiMyAnimes.Tests;

public sealed class SecurityAuditTests
{
    [Fact]
    public async Task RecordAsync_PersistsRequiredFieldsAndTwelveMonthRetention()
    {
        await using var context = CriarContexto();
        var writer = new SecurityAuditWriter(context);
        var beforeUtc = DateTimeOffset.UtcNow;

        var eventId = await writer.RecordAsync(CriarEntry());

        var afterUtc = DateTimeOffset.UtcNow;
        var persistedEvent = await context.SecurityAuditEvents.SingleAsync();

        Assert.Equal(eventId, persistedEvent.Id);
        Assert.Equal("service:tests", persistedEvent.Actor);
        Assert.Equal("catalog.read", persistedEvent.Action);
        Assert.Equal("anime:123", persistedEvent.Target);
        Assert.Equal("success", persistedEvent.Result);
        Assert.Equal("device:test", persistedEvent.DeviceId);
        Assert.Equal("corr-stage05", persistedEvent.CorrelationId);
        Assert.Equal("validacao de persistencia", persistedEvent.Reason);
        Assert.Equal(TimeSpan.Zero, persistedEvent.OccurredAtUtc.Offset);
        Assert.InRange(persistedEvent.OccurredAtUtc, beforeUtc, afterUtc);
        Assert.Equal(
            persistedEvent.OccurredAtUtc.AddMonths(12),
            persistedEvent.RetentionUntilUtc);
    }

    [Fact]
    public async Task SaveChangesAsync_RejectsAuditEventModification()
    {
        await using var context = CriarContexto();
        var writer = new SecurityAuditWriter(context);
        await writer.RecordAsync(CriarEntry());
        var persistedEvent = await context.SecurityAuditEvents.SingleAsync();

        context.Update(persistedEvent);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());

        Assert.Contains("append-only", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveChangesAsync_RejectsAuditEventDeletion()
    {
        await using var context = CriarContexto();
        var writer = new SecurityAuditWriter(context);
        await writer.RecordAsync(CriarEntry());
        var persistedEvent = await context.SecurityAuditEvents.SingleAsync();

        context.SecurityAuditEvents.Remove(persistedEvent);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());

        Assert.Contains("append-only", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static SecurityAuditEntry CriarEntry() => new(
        Actor: "service:tests",
        Action: "catalog.read",
        Target: "anime:123",
        Result: "success",
        DeviceId: "device:test",
        CorrelationId: "corr-stage05",
        Reason: "validacao de persistencia");

    private static MyAnimesContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<MyAnimesContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new MyAnimesContext(options);
    }
}
