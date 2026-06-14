using ApiCSharp.Services;
using ApiCSharp.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var localDbConnection = builder.Configuration.GetConnectionString("LocalDbConnection");

// Add services to the container.
//===============================
// Configuração do Entity.Framework.Core para MyAnimeContext usando SQL Server
builder.Services.AddDbContext<MyAnimesContext>(opts => opts.UseSqlServer(localDbConnection));
// Configuração do Entity.Framework.Core para AnimeContext usando SQL Server
//builder.Services.AddDbContext<AnimeContext>(opts => opts.UseSqlServer(localDbConnection));

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "ApiJikanCSharp",
        Version = "v1",
        Description = "Documentação da API Jikan em ASP.NET Core."
    });
});
//==========================================
// Configuração do HttpClient para Jikan API
builder.Services.AddHttpClient<IJikanService, JikanService>(client =>
{
    client.BaseAddress = new Uri("https://api.jikan.moe/v4/");
    client.DefaultRequestHeaders.Add("User-Agent", "ApiCSharp-JikanClient/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
});
//=======================================================================
// Configuração de CORS para permitir acesso apenas do frontend DtudoSite
var dtudoSiteOrigin = "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(dtudoSiteOrigin)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
//=======================================================================
var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiJikanCSharp v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
