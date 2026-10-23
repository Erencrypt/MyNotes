using Velopack;

namespace MyNotes.Services.Interfaces;

public interface IAppUpdateService
{
    bool IsInstalled { get; }
    Task<UpdateInfo?> CheckForUpdatesAsync();
    Task DownloadUpdatesAsync(UpdateInfo updateInfo);
    void ApplyUpdatesAndRestart(UpdateInfo updateInfo);
}
