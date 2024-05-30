using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuGet.Packaging;

namespace NuDown.ViewModels;

public partial class NuDownViewModel : ViewModelBase
{
    private readonly NuDownService _service = new();
    private CancellationTokenSource? _packagesLoadCts;
    private CancellationTokenSource? _releasesLoadCts;
    private CancellationTokenSource? _downloadCts;

    [ObservableProperty] private string _query = "";

    [ObservableProperty] private bool _includePreview;

    [ObservableProperty] private PackageSearchViewModel? _selectedPackage;

    [ObservableProperty] private PackageReleaseViewModel? _selectedRelease;

    [ObservableProperty] private bool _canDownload;

    [ObservableProperty] private string _prompt = "";

    [ObservableProperty] private bool _packagesLoading;

    [ObservableProperty] private bool _releasesLoading;

    [ObservableProperty] private PackageReleaseViewModel? _downloadingRelease;

    [ObservableProperty] private string _downloadFolder;

    [ObservableProperty] private string _repositoryUrl;


    public NuDownViewModel()
    {
        var config = _service.Config;
        DownloadFolder = config.DownloadFolder;
        RepositoryUrl = config.RepositoryUrl;
    }

    public ObservableCollection<PackageSearchViewModel> Packages { get; } = new();

    public ObservableCollection<PackageReleaseViewModel> Releases { get; } = new();


    [RelayCommand(CanExecute = nameof(CanDownload))]
    private void Download()
    {
        if (_downloadCts is not null)
            return;

        var release = SelectedRelease;
        if (release is null)
            return;

        _downloadCts = new();
        DownloadRelease(release, _downloadCts.Token).ContinueWith(t => _downloadCts = null);
    }


    partial void OnQueryChanged(string value)
    {
        _packagesLoadCts?.Cancel();
        _packagesLoadCts = new();

        PerformSearch(value, IncludePreview, _packagesLoadCts.Token);
    }


    partial void OnIncludePreviewChanged(bool value)
    {
        _packagesLoadCts?.Cancel();
        _packagesLoadCts = new();
        PerformSearch(Query, value, _packagesLoadCts.Token);
    }

    partial void OnSelectedPackageChanged(PackageSearchViewModel? value)
    {
        _releasesLoadCts?.Cancel();
        _releasesLoadCts = null;

        Releases.Clear();

        if (value is null)
        {
            SelectedRelease = null;
        }
        else
        {
            _releasesLoadCts = new();
            LoadReleases(value.Id, IncludePreview, _releasesLoadCts.Token);
        }
    }

    partial void OnSelectedReleaseChanged(PackageReleaseViewModel? value)
    {
        CanDownload = DownloadingRelease is null && value is not null;
    }

    partial void OnDownloadingReleaseChanged(PackageReleaseViewModel? value)
    {
        CanDownload = value is null && SelectedRelease is not null;
    }

    partial void OnCanDownloadChanged(bool value)
    {
        _ = value;
        DownloadCommand.NotifyCanExecuteChanged();
    }


    private async Task PerformSearch(string query, bool includePreview, CancellationToken cancellationToken)
    {
        // ReSharper disable once MethodSupportsCancellation
        await Task.Delay(200);

        if (cancellationToken.IsCancellationRequested)
            return;

        SelectedPackage = null;
        Packages.Clear();

        PackagesLoading = true;
        try
        {
            var urlChanged = RepositoryUrl != _service.Config.RepositoryUrl;
            if (urlChanged)
                _service.Config.RepositoryUrl = RepositoryUrl;

            var items = await _service.Query(query, includePreview, 0, 100, cancellationToken);

            Packages.AddRange(items);
            SelectedPackage = items.FirstOrDefault();

            if (urlChanged)
                _service.SaveConfig();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Prompt = $"Search failed: {exception.Message}";
        }
        finally
        {
            PackagesLoading = false;
        }
    }

    private async Task LoadReleases(string packageId, bool includePreview, CancellationToken cancellationToken)
    {
        ReleasesLoading = true;
        try
        {
            var urlChanged = RepositoryUrl != _service.Config.RepositoryUrl;
            if (urlChanged)
                _service.Config.RepositoryUrl = RepositoryUrl;

            var releases = await _service.GetReleases(packageId, includePreview, cancellationToken);

            Releases.Clear();
            Releases.AddRange(releases);
            SelectedRelease = releases.FirstOrDefault();

            if (urlChanged)
                _service.SaveConfig();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Prompt = $"Load version list failed: {exception.Message}";
        }
        finally
        {
            ReleasesLoading = false;
        }
    }

    private async Task DownloadRelease(PackageReleaseViewModel release, CancellationToken cancellationToken)
    {
        DownloadingRelease = release;

        try
        {
            var changed = RepositoryUrl != _service.Config.RepositoryUrl ||
                          DownloadFolder != _service.Config.DownloadFolder;
            if (changed)
            {
                _service.Config.RepositoryUrl = RepositoryUrl;
                _service.Config.DownloadFolder = DownloadFolder;
            }

            await _service.DownloadPackage(release.Id, release.Version, cancellationToken);

            if (changed)
                _service.SaveConfig();
        }
        catch (Exception exception)
        {
            Prompt = $"Download failed: {exception.Message}";
        }
        finally
        {
            DownloadingRelease = null;
        }
    }
}