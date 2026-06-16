using ApiJikan.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//===============================
builder.Services.AddControllers().AddNewtonsoftJson();

builder.Services.AddMemoryCache();

builder.Services.AddEndpointsApiExplorer();

// Configuração do Swagger para documentação da API
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Api Jikan - Consulta Externa",
        Version = "v1",
        Description = "Esta é uma Api de consulta à API externa Jikan (MyAnimeList). Fornece endpoints para buscar informações detalhadas sobre animes."
    });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
//==========================================
// Configuração do HttpClient para Jikan API
builder.Services.AddHttpClient<IJikanService, JikanService>(client =>
{
    client.BaseAddress = new Uri("https://api.jikan.moe/v4/");
    client.DefaultRequestHeaders.Add("User-Agent", "ApiJikan-Client/1.0");
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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Api Jikan v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
