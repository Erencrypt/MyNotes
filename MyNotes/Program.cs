using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using MyNotes.Helpers;
using Velopack;
using WinRT;

namespace MyNotes;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build()
            .OnFirstRun(_ =>
            {
                if (RuntimeHelper.IsMSIX)
                {
                    return;
                }

                var processPath = AppUtilities.GetProcessPath()
                    ?? throw new InvalidOperationException("The application executable path is unavailable.");
                AppUtilities.SetStartupEntry(AppConfig.StartupRegistryValueName, processPath);
            })
            .Run();
        ComWrappersSupport.InitializeComWrappers();
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
