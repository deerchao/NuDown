namespace NuDown.Models;

public class PackageSummary
{
    public string Id { get; init; }

    public string Description { get; init; }

    public string Version { get; init; }
    public string Author { get; init; }

    public bool IsPrerelease { get; init; }
    public long Downloads { get; init; }

    public string Url { get; init; }
}