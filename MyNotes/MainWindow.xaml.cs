using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;
using MyNotes.Helpers;

namespace MyNotes;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "AppDisplayName".GetLocalized();
        MicaBackdrop systemBackdrop = new();
        systemBackdrop.Kind = MicaKind.Base;
        SystemBackdrop = systemBackdrop;
    }
}
