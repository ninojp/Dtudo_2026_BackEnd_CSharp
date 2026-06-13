using ApiCSharp.Services;
using ApiJikanCSharp.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore; // Adicione esta linha

var builder = WebApplication.CreateBuilder(args);

var localDbConnection = builder.Configuration.GetConnectionString("LocalDbConnection");

// Add services to the container.
//===============================
builder.Services.AddDbContext<MyAnimesContext>(opts => opts.UseSqlServer(localDbConnection));

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

// Configuração do HttpClient para Jikan API
builder.Services.AddHttpClient<IJikanService, JikanService>(client =>
{
    client.BaseAddress = new Uri("https://api.jikan.moe/v4/");
    client.DefaultRequestHeaders.Add("User-Agent", "ApiCSharp-JikanClient/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var dtudoSiteOrigin = "http://localhost:5173";

// Configuração de CORS para permitir acesso apenas do frontend DtudoSite
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(dtudoSiteOrigin)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

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

app.MapGet("/health/db", async () =>
{
    try
    {
        await using var connection = new SqlConnection(localDbConnection);
        await connection.OpenAsync();

        await using var command = new SqlCommand("SELECT 1", connection);
        var result = await command.ExecuteScalarAsync();

        return Results.Ok(new
        {
            status = "ok",
            database = "LocalDB",
            queryResult = result
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Falha na conexão com o banco local",
            detail: ex.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();
