namespace ApiMyAnimes.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    public string LocalDbConnection { get; set; } = string.Empty;
}
