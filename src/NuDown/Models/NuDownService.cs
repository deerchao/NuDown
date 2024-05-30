using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NuDown.Models;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NuDown.ViewModels;

public class NuDownService
{
    public ILogger Logger { get; set; } = NullLogger.Instance;

    public NuDownConfig Config { get; }


    public NuDownService()
    {
        var root = GetRootFolderPath();
        var configFile = Path.Combine(root, "config.json");
        if (File.Exists(configFile))
        {
            var json = File.ReadAllText(configFile, Encoding.UTF8);
            Config = JsonSerializer.Deserialize<NuDownConfig>(json);
        }
        else
        {
            Config = new NuDownConfig();
        }
    }

    private string GetRootFolderPath()
    {
        return Path.GetDirectoryName(GetType().Assembly.Location)!;
    }


    public async Task<List<PackageSearchViewModel>> Query(string keyword, bool includePreview, int skip, int take,
        CancellationToken cancellationToken)
    {
        var repository = Repository.Factory.GetCoreV3(Config.RepositoryUrl);
        var resource = await repository.GetResourceAsync<PackageSearchResource>(cancellationToken);
        var searchFilter = new SearchFilter(includePreview);

        var results = await resource.SearchAsync(
            keyword,
            searchFilter,
            skip: skip,
            take: take,
            Logger,
            cancellationToken);

        var items = results.Select(x => new PackageSearchViewModel
        {
            Id = x.Identity.Id,
            Version = x.Identity.Version.ToString(),
            Author = x.Authors,
            Description = x.Description,
            Url = x.ProjectUrl?.ToString() ?? "",
            Downloads = x.DownloadCount ?? 0,
            IsPrerelease = x.Identity.Version.IsPrerelease,
        });

        return items.ToList();
    }

    public async Task<List<PackageReleaseViewModel>> GetReleases(string packageId, bool includePreview,
        CancellationToken cancellationToken)
    {
        var cache = new SourceCacheContext();
        var repository = Repository.Factory.GetCoreV3(Config.RepositoryUrl);
        var resource = await repository.GetResourceAsync<PackageMetadataResource>(cancellationToken);

        var packages = await resource.GetMetadataAsync(
            packageId,
            includePrerelease: true,
            includeUnlisted: false,
            cache,
            Logger,
            cancellationToken);
        if (!includePreview)
            packages = packages.Where(x => !x.Identity.Version.IsPrerelease);

        var items = packages.OrderByDescending(x => x.Identity.Version)
            .Select(x => new PackageReleaseViewModel
            {
                Id = x.Identity.Id,
                Version = x.Identity.Version.ToString(),
                PublishDate = x.Published,
                License = x.LicenseMetadata?.LicenseExpression.ToString() ??
                          x.LicenseUrl?.ToString() ?? x.LicenseMetadata?.License ?? "",
                DownloadCount = x.DownloadCount ?? 0,
            });

        return items.ToList();
    }

    public async Task DownloadPackage(string packageId, string version, CancellationToken cancellationToken)
    {
        var cache = new SourceCacheContext();
        var repository = Repository.Factory.GetCoreV3(Config.RepositoryUrl);
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

        var packageVersion = new NuGetVersion(version);
        using var packageStream = new MemoryStream();

        await resource.CopyNupkgToStreamAsync(
            packageId,
            packageVersion,
            packageStream,
            cache,
            Logger,
            cancellationToken);

        var folder = Path.Combine(GetRootFolderPath(), Config.DownloadFolder);
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var name = $"{packageId}.{version}.nupkg";
        var path = Path.Combine(folder, name);
        await File.WriteAllBytesAsync(path, packageStream.ToArray(), cancellationToken);
    }

    public void SaveConfig()
    {
        var root = GetRootFolderPath();
        var configFile = Path.Combine(root, "config.json");
        var json = JsonSerializer.Serialize(Config);
        File.WriteAllText(configFile, json, Encoding.UTF8);
    }
}