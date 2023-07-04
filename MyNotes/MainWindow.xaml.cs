using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;
using MyNotes.Contracts.Services;
using MyNotes.Helpers;

namespace MyNotes;

public sealed partial class MainWindow : WindowEx
{
    private readonly ILocalSettingsService localSettingsService;
    private readonly string BackDropKey = "BackDrop";
    private MicaKind micaKind;
    public MainWindow()
    {
        InitializeComponent();
        localSettingsService= App.GetService<ILocalSettingsService>();
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "AppDisplayName".GetLocalized();
        BackDropState();
    }
    public async void BackDropState()
    {
        MicaKind backdrop = await localSettingsService.ReadSettingAsync<MicaKind>(BackDropKey);
        if (backdrop.ToString() != null)
        {
            micaKind = backdrop;
        }
        else if (backdrop.ToString() == null)
        {
            _ = localSettingsService.SaveSettingAsync(BackDropKey, MicaKind.Base);
        }
        MicaBackdrop systemBackdrop = new()
        {
            Kind = micaKind
        };
        SystemBackdrop = systemBackdrop;
    }
}
