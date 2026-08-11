namespace ApiMusicX.Dtos;

public sealed record HealthResponse(string Status, string Service, string? Database = null);
