using System;

namespace NuDown.Models;

public class PackageRelease
{
    public string Id { get; init; }
    public string Version { get; init; }
    public DateTimeOffset? PublishDate { get; init; }
    public string License { get; init; }
    public long DownloadCount { get; init; }
    public bool LocalExists { get; init; }
}