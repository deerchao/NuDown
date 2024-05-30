namespace NuDown.Models;

public class NuDownConfig
{
    public string DownloadFolder { get; set; } = ".";

    public string RepositoryUrl { get; set; } = "https://api.nuget.org/v3/index.json";
}