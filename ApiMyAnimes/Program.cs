using ApiMyAnimes.Data;
using ApiMyAnimes.Configuration;
using ApiMyAnimes.Infrastructure;
using ApiMyAnimes.Services;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using LibDtudo.Shared.Logging;
using Serilog;
using Serilog.Events;
using System.Reflection;

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

builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.LocalDbConnection), "ConnectionStrings:LocalDbConnection nao configurada.")
    .ValidateOnStart();

// Add services to the container.
//===============================
// Configuração do Entity.Framework.Core para MyAnimeContext usando SQL Server
builder.Services.AddDbContext<MyAnimesContext>((serviceProvider, options) =>
    options.UseSqlServer(serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value.LocalDbConnection));

builder.Services.AddControllers().AddNewtonsoftJson();

builder.Services.AddMemoryCache();
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
builder.Services.AddScoped<AnimeBuscaLocalService>();
builder.Services.AddScoped<AnimeTitleConflictService>();
builder.Services.AddScoped<ISecurityAuditWriter, SecurityAuditWriter>();
builder.Services.AddSingleton<LocalAuthService>();
builder.Services.AddOptions<AuthOptions>()
    .Bind(builder.Configuration.GetSection(AuthOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.UsersFilePath), "Auth:UsersFilePath nao configurado.")
    .ValidateOnStart();

builder.Services.AddOptions<ApiMyAnimeListOptions>()
    .Bind(builder.Configuration.GetSection(ApiMyAnimeListOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps, "ApiMyAnimeList:BaseUrl deve ser uma URL HTTPS absoluta.")
    .ValidateOnStart();

builder.Services.AddHttpClient<MyAnimeListImportClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<ApiMyAnimeListOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

builder.Services.AddEndpointsApiExplorer();

// Configuração do Swagger para documentação da API
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Api Local MyAnimes",
        Version = "v1",
        Description = "Esta é uma Api Local que manipula (CRUD completo) um Banco de dados Relacional local que contém informações relacionadas as minhas coleções de animes. MyAnime (DBtabela) representa coleções nomeadas que agrupam APENAS os IDs dos animes, e Anime (DBtabela) contém informações detalhadas sobre cada anime."
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
//=======================================================================
// Configuração de CORS para permitir acesso apenas do frontend DtudoSite
var dtudoSiteOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(dtudoSiteOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
//=======================================================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Api Local MyAnimes v1");
        options.RoutePrefix = "swagger";
    });
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
