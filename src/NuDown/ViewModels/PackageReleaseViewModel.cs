using System;

namespace NuDown.ViewModels;

public class PackageReleaseViewModel
{
    public string Id { get; init; }
    public string Version { get; init; }
    public DateTimeOffset? PublishDate { get; init; }
    public string License { get; init; }
    public long DownloadCount { get; init; }
}