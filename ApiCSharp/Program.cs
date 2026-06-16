using ApiCSharp.Data;
using ApiCSharp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

var localDbConnection = builder.Configuration.GetConnectionString("LocalDbConnection");

// Add services to the container.
//===============================
// Configuração do Entity.Framework.Core para MyAnimeContext usando SQL Server
builder.Services.AddDbContext<MyAnimesContext>(opts => opts.UseSqlServer(localDbConnection));
// Configuração do Entity.Framework.Core para AnimeContext usando SQL Server
//builder.Services.AddDbContext<AnimeContext>(opts => opts.UseSqlServer(localDbConnection));

builder.Services.AddControllers().AddNewtonsoftJson();

builder.Services.AddMemoryCache();

builder.Services.AddEndpointsApiExplorer();

// Configuração do Swagger para documentação da API
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Api Local MyAnimes",
        Version = "v1",
        Description = "Esta é uma Api Local que manipula (CRUD, completo) um Banco de dados Relacional local que contém informações relacionadas as minhas coleções de animes, MyAnime (DBtabela) Api Local MyAnimes, engloba todos os endpoints relacionados a MyAnimes (coleções nomeadas, que agrupam APENAS os IDs dos animes) e Anime (DBtabela) que contém informações detalhadas sobre cada anime. Api Jikan, tem apenas 2 EndPoints (consulta por Nome ou ID) de consulta a API Jikan Externa para fornecer todas as informações necessárias sobre os animes."
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Api Local MyAnimes v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
