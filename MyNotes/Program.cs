using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using MyNotes.Helpers;
using System.Runtime.InteropServices;
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

        // Single instance: decide before any XAML is created.
        var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        var mainInstance = AppInstance.FindOrRegisterForKey("MyNotes");
        if (!mainInstance.IsCurrent)
        {
            RedirectToMainInstance(mainInstance, activationArgs);
            return;
        }
        mainInstance.Activated += App.MainInstance_Activated;

        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(uint dwProcessId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string? lpName);

    [DllImport("kernel32.dll")]
    private static extern bool SetEvent(IntPtr hEvent);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("ole32.dll")]
    private static extern uint CoWaitForMultipleObjects(uint dwFlags, uint dwMilliseconds, ulong nHandles, IntPtr[] pHandles, out uint dwIndex);

    /// <summary>
    /// Hands this launch over to the running instance and lets it take the foreground.
    /// The redirect runs on another thread with a non-blocking wait, so the STA isn't blocked
    /// (pattern from the Windows App SDK single-instance guide).
    /// </summary>
    private static void RedirectToMainInstance(AppInstance mainInstance, AppActivationArguments activationArgs)
    {
        // This process was just started by the user, so it holds the foreground right; the running
        // instance doesn't, and without AllowSetForegroundWindow its Activate() would only flash the taskbar.
        AllowSetForegroundWindow(mainInstance.ProcessId);

        var redirectDone = CreateEvent(IntPtr.Zero, true, false, null);
        Task.Run(() =>
        {
            try
            {
                mainInstance.RedirectActivationToAsync(activationArgs).AsTask().Wait();
            }
            catch (Exception ex)
            {
                LogWriter.Log($"Failed to redirect activation to the running instance: {ex}", LogWriter.LogLevel.Error);
            }
            finally
            {
                SetEvent(redirectDone);
            }
        });

        const uint CWMO_DEFAULT = 0;
        const uint INFINITE = 0xFFFFFFFF;
        _ = CoWaitForMultipleObjects(CWMO_DEFAULT, INFINITE, 1, [redirectDone], out _);
        CloseHandle(redirectDone);
    }
}
