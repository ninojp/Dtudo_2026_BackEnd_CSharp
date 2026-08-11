using System.Reflection;
using ApiMusicX.Configuration;
using ApiMusicX.Data;
using ApiMusicX.Infrastructure;
using ApiMusicX.Services;
using LibDtudo.Shared.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
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
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<MusicExceptionHandler>();
builder.Services.AddScoped<IMusicCollectionService, MusicCollectionService>();
builder.Services.AddScoped<IMusicCollectionImportService, MusicCollectionImportService>();

builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.LocalDbConnection),
        "ConnectionStrings:LocalDbConnection nao configurada.")
    .ValidateOnStart();

builder.Services.AddDbContext<MusicContext>((serviceProvider, options) =>
    options.UseSqlServer(
        serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value.LocalDbConnection,
        sql => sql.MigrationsAssembly(typeof(MusicContext).Assembly.FullName)));

builder.Services.AddOptions<ApiAuthorizationOptions>()
    .Bind(builder.Configuration.GetSection(ApiAuthorizationOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.Issuer, UriKind.Absolute, out var issuer)
        && issuer.Scheme == Uri.UriSchemeHttps,
        "Authentication:Issuer deve ser uma URL HTTPS absoluta.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Audience),
        "Authentication:Audience nao configurado.")
    .ValidateOnStart();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var authorizationOptions = builder.Configuration
        .GetSection(ApiAuthorizationOptions.SectionName)
        .Get<ApiAuthorizationOptions>() ?? new ApiAuthorizationOptions();

    options.Authority = authorizationOptions.Issuer;
    options.Audience = authorizationOptions.Audience;
    options.RequireHttpsMetadata = true;
    options.MapInboundClaims = false;
    options.SaveToken = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = authorizationOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = authorizationOptions.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    ApiAuthorizationPolicies.AddPermissionPolicy(
        options,
        ApiAuthorizationPolicies.CatalogReadPolicy,
        "catalog.read",
        "catalog.read");
    ApiAuthorizationPolicies.AddPermissionPolicy(
        options,
        ApiAuthorizationPolicies.CatalogWritePolicy,
        "catalog.write",
        "catalog.write");
    ApiAuthorizationPolicies.AddPermissionPolicy(
        options,
        ApiAuthorizationPolicies.CatalogDeletePolicy,
        "catalog.delete",
        "catalog.delete");
    ApiAuthorizationPolicies.AddPermissionPolicy(
        options,
        ApiAuthorizationPolicies.HealthReadPolicy,
        "health.read",
        "health.read");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ApiMusicX",
        Version = "v1",
        Description = "API local para a Colecao de musicas, com persistencia relacional SQL Server e Entity Framework Core."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Informe um token JWT no formato Bearer {token}.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddOptions<ApiCorsOptions>()
    .Bind(builder.Configuration.GetSection(ApiCorsOptions.SectionName))
    .Validate(options => options.AllowedOrigins is { Length: > 0 }
        && options.AllowedOrigins.Distinct(StringComparer.OrdinalIgnoreCase).Count() == options.AllowedOrigins.Length
        && options.AllowedOrigins.All(origin =>
            ApiCorsOptions.IsValidOrigin(origin, !builder.Environment.IsDevelopment())),
        "Cors:AllowedOrigins deve conter origins HTTP(S) unicos; em producao, use somente HTTPS e nunca o wildcard.")
    .ValidateOnStart();

var configuredCorsOrigins = builder.Configuration
    .GetSection(ApiCorsOptions.SectionName)
    .Get<ApiCorsOptions>()?.AllowedOrigins ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        policy.WithOrigins(configuredCorsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

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
app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseCors("AllowConfiguredOrigins");
app.UseAuthentication();

if (app.Environment.IsDevelopment())
{
    app.UseWhen(
        context => context.Request.Path.StartsWithSegments("/swagger"),
        branch => branch.Use(async (context, next) =>
        {
            if (!(context.User.Identity?.IsAuthenticated ?? false))
            {
                await context.ChallengeAsync();
                return;
            }

            if (!ApiAuthorizationPolicies.HasPermissionAndScope(context.User, "health.read", "health.read"))
            {
                await context.ForbidAsync();
                return;
            }

            await next(context);
        }));

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiMusicX v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
