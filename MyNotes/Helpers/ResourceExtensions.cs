using Microsoft.Windows.ApplicationModel.Resources;
using System.Diagnostics;

namespace MyNotes.Helpers;

public static class ResourceExtensions
{
    private static readonly ResourceLoader _resourceLoader = new();

    public static string GetLocalized(this string resourceKey)
    {
        string Text = string.Empty;
		try
		{
            Text = _resourceLoader.GetString(resourceKey);
        }
		catch (Exception ex)
		{
            Debug.Print($"Last Resource Key: {resourceKey}, Error message:{ex.Message}");
            LogWriter.Log($"Last Resource Key: {resourceKey}, Error message:{ex.Message}", LogWriter.LogLevel.Debug);
		}
        return Text;
    }
}
