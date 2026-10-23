using MyNotes.Services.Interfaces;
using Velopack;
using Velopack.Sources;

namespace MyNotes.Services;

public sealed class AppUpdateService : IAppUpdateService
{
    private const string ReleaseFeedUrl = "https://github.com/Erencrypt/MyNotes";
    private readonly UpdateManager _updateManager = new(new GithubSource(ReleaseFeedUrl, accessToken: null, prerelease: false));

    public bool IsInstalled => _updateManager.IsInstalled;

    public Task<UpdateInfo?> CheckForUpdatesAsync() =>
        _updateManager.CheckForUpdatesAsync();

    public Task DownloadUpdatesAsync(UpdateInfo updateInfo) =>
        _updateManager.DownloadUpdatesAsync(updateInfo);

    public void ApplyUpdatesAndRestart(UpdateInfo updateInfo) =>
        _updateManager.ApplyUpdatesAndRestart(updateInfo.TargetFullRelease);
}
