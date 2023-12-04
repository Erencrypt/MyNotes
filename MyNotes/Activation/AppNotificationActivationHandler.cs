using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace MyNotes.Activation;

public class AppNotificationActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    //private readonly INavigationService _navigationService;
    //private readonly IAppNotificationService _notificationService;

    public AppNotificationActivationHandler(/*INavigationService navigationService, IAppNotificationService notificationService*/)
    {
        //_navigationService = navigationService;
        //_notificationService = notificationService;
    }

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        return AppInstance.GetCurrent().GetActivatedEventArgs()?.Kind == ExtendedActivationKind.AppNotification;
    }

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        await Task.CompletedTask;
    }
}
