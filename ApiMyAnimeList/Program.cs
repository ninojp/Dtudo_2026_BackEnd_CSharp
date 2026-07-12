using ApiMyAnimeList.Configuration;
using ApiMyAnimeList.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddOptions<MyAnimeListOptions>()
    .Bind(builder.Configuration.GetSection(MyAnimeListOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "MyAnimeList:BaseUrl inválida.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), "MyAnimeList:ClientId não configurado.")
    .ValidateOnStart();

var malOptions = builder.Configuration.GetSection(MyAnimeListOptions.SectionName).Get<MyAnimeListOptions>() ?? new();
builder.Services.AddHttpClient<MyAnimeListClient>(client =>
{
    client.BaseAddress = new Uri(malOptions.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(Math.Max(5, malOptions.TimeoutSeconds));
    client.DefaultRequestHeaders.Add("X-MAL-CLIENT-ID", malOptions.ClientId);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Dtudo-ApiMyAnimeList/1.0");
});

builder.Services.AddCors(options => options.AddPolicy("AllowFrontend", policy =>
    policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");
app.UseAuthorization();

app.MapControllers();

app.Run();
