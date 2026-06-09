using ApiCSharp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configuração do HttpClient para Jikan API
builder.Services.AddHttpClient<IJikanService, JikanService>(client =>
{
    client.BaseAddress = new Uri("https://api.jikan.moe/v4/");
    client.DefaultRequestHeaders.Add("User-Agent", "ApiCSharp-JikanClient/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Configuração de CORS para permitir acesso do frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Servir arquivos estáticos (HTML, CSS, JS)
app.UseStaticFiles();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

// Rota padrão para o index.html
app.MapFallbackToFile("index.html");

app.Run();
