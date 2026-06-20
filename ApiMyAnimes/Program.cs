using ApiMyAnimes.Data;
using ApiMyAnimes.Services;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

var localDbConnection = builder.Configuration.GetConnectionString("LocalDbConnection");

// Add services to the container.
//===============================
// Configuração do Entity.Framework.Core para MyAnimeContext usando SQL Server
builder.Services.AddDbContext<MyAnimesContext>(opts => opts.UseSqlServer(localDbConnection));

builder.Services.AddControllers().AddNewtonsoftJson();

builder.Services.AddMemoryCache();

var apiJikanBaseUrl = builder.Configuration["ApiJikan:BaseUrl"] ?? "http://localhost:63983/";
builder.Services.AddHttpClient<ApiJikanClient>(client =>
{
    client.BaseAddress = new Uri(apiJikanBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Api Local MyAnimes v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
