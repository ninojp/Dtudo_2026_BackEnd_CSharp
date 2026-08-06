using ApiMyAnimeList.Configuration;
using ApiMyAnimeList.Infrastructure;
using ApiMyAnimeList.Services;
using LibDtudo.Shared.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.With<SensitiveDataRedactionEnricher>();

    var seqUrl = context.Configuration["Seq:Url"];
    if (Uri.TryCreate(seqUrl, UriKind.Absolute, out var seqUri)
        && seqUri.Scheme is "http" or "https")
    {
        loggerConfiguration.WriteTo.Seq(seqUri.ToString());
    }
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
builder.Services.AddOptions<MyAnimeListOptions>()
    .Bind(builder.Configuration.GetSection(MyAnimeListOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps, "MyAnimeList:BaseUrl deve ser uma URL HTTPS absoluta.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), "MyAnimeList:ClientId não configurado.")
    .Validate(options => options.TimeoutSeconds is >= 1 and <= 300, "MyAnimeList:TimeoutSeconds deve estar entre 1 e 300.")
    .Validate(options => options.MaxRetries is >= 0 and <= 10, "MyAnimeList:MaxRetries deve estar entre 0 e 10.")
    .Validate(options => options.CacheMinutes is >= 1 and <= 1440, "MyAnimeList:CacheMinutes deve estar entre 1 e 1440.")
    .ValidateOnStart();

builder.Services.AddHttpClient<MyAnimeListClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<MyAnimeListOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    client.DefaultRequestHeaders.Add("X-MAL-CLIENT-ID", options.ClientId);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Dtudo-ApiMyAnimeList/1.0");
}).AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

var dtudoSiteOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options => options.AddPolicy("AllowFrontend", policy =>
    policy.WithOrigins(dtudoSiteOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<RequestCorrelationMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, _, exception) =>
        exception is not null || httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode >= 400
                ? LogEventLevel.Warning
                : LogEventLevel.Information;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
        diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value ?? "/");
        diagnosticContext.Set("ResponseStatusCode", httpContext.Response.StatusCode);
    };
});

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
