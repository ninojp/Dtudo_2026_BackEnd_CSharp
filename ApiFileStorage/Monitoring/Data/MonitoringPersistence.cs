using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ApiFileStorage.Monitoring.Data;

public static class MonitoringPersistence
{
    public const string DatabaseName = "DtudoFileMonitoring";
    public const string ConnectionName = "FileMonitoring";

    public static IServiceCollection AddMonitoringPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("Monitoring:Enabled"))
        {
            return services;
        }

        services.AddDbContext<MonitoringDbContext>(options =>
            options.UseSqlServer(ValidateConnectionString(configuration.GetConnectionString(ConnectionName))));
        services.AddScoped<MonitoringInventoryStore>();
        services.AddSingleton(MonitoringRoots.Collections());
        services.AddSingleton<MonitoringReader>();
        services.AddSingleton<MonitoringSessions>();
        services.AddHostedService(provider => provider.GetRequiredService<MonitoringSessions>());
        return services;
    }

    public static string ValidateConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:FileMonitoring must be configured separately from the anime catalog.");
        }

        SqlConnectionStringBuilder connection;
        try
        {
            connection = new SqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("ConnectionStrings:FileMonitoring is invalid.");
        }

        if (!string.Equals(connection.InitialCatalog, DatabaseName, StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(connection.AttachDBFilename))
        {
            throw new InvalidOperationException("Monitoring requires the dedicated DtudoFileMonitoring database without AttachDBFilename.");
        }

        return connection.ConnectionString;
    }
}

public sealed class MonitoringDbContextFactory : IDesignTimeDbContextFactory<MonitoringDbContext>
{
    public MonitoringDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<MonitoringDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = args.Contains("--offline", StringComparer.Ordinal)
            ? "Server=127.0.0.1,1;Database=DtudoFileMonitoring;Integrated Security=True;Encrypt=True;Connect Timeout=1"
            : configuration.GetConnectionString(MonitoringPersistence.ConnectionName);

        return new MonitoringDbContext(new DbContextOptionsBuilder<MonitoringDbContext>()
            .UseSqlServer(MonitoringPersistence.ValidateConnectionString(connectionString))
            .Options);
    }
}
