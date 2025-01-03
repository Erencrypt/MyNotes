﻿using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MyNotes.Contracts.Services;
using MyNotes.Helpers;
using MyNotes.ViewModels;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;

namespace MyNotes.Views;

public sealed partial class NoteDetailsPage : Page
{
    private readonly StorageFolder notesFolder = App.StorageFolder;
    private readonly ILocalSettingsService localSettingsService;
    private DispatcherTimer? dispatcherTimer;
    private readonly string SpellcheckKey = "SpellCheck";
    private readonly string SaveWhenExitKey = "SaveWhenExit";
    private bool saveWhenExit;

    public NoteDetailsViewModel ViewModel
    {
        get;
    }
    public NoteDetailsPage()
    {
        ViewModel = App.GetService<NoteDetailsViewModel>();
        InitializeComponent();
        localSettingsService = App.GetService<ILocalSettingsService>();
        LoadDocument();
        SaveWhenExitState();
        SpellCheckState();
        NoteEditor.SelectionFlyout = null;

        noteName.Title = "NoteDetails_NoteNameTitle".GetLocalized();
        NoteEditor.PlaceholderText = "NoteDetails_EditorPlaceholder".GetLocalized();
        findBox.PlaceholderText = "NoteDetails_FindPlaceholder".GetLocalized();
        ToolTipService.SetToolTip(BtnSaveFile, "NoteDetails_SaveTooltip".GetLocalized());
        ToolTipService.SetToolTip(fontColorButton, "NoteDetails_FontColorTooltip".GetLocalized());
        ToolTipService.SetToolTip(AlignButton, "NoteDetails_AlignmentTooltip".GetLocalized());
        ToolTipService.SetToolTip(listButton, "NoteDetails_ListTypeTooltip".GetLocalized());
        ToolTipService.SetToolTip(boldButton, "NoteDetails_BoldTooltip".GetLocalized());
        ToolTipService.SetToolTip(italicButton, "NoteDetails_ItalicTooltip".GetLocalized());
        ToolTipService.SetToolTip(boldButton, "NoteDetails_UnderlineTooltip".GetLocalized());
        ToolTipService.SetToolTip(StrikeButton, "NoteDetails_StrikethroughTooltip".GetLocalized());
    }

    public async void SaveWhenExitState() => saveWhenExit = await localSettingsService.ReadSettingAsync<bool>(SaveWhenExitKey);
    public async void SpellCheckState()
    {
        bool check = await localSettingsService.ReadSettingAsync<bool>(SpellcheckKey);
        if (check.ToString() != null)
        {
            SpellCheck.IsChecked = check;
            NoteEditor.IsSpellCheckEnabled = check;
        }
        else if (check.ToString() == null)
        {
            _ = localSettingsService.SaveSettingAsync(SpellcheckKey, true);
            SpellCheck.IsChecked = true;
            NoteEditor.IsSpellCheckEnabled = true;
        }
    }
    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        if (saveWhenExit)
        {
            SaveFile(false);
        }
    }
    private async void LoadDocument()
    {
        // Open a text file.
        if (ShellPage.NoteName != null)
        {
            string directory = notesFolder.Path + @"\Notes\" + ShellPage.NoteName + ".rtf";
            StorageFile file = await StorageFile.GetFileFromPathAsync(directory);

            if (file != null)
            {
                using IRandomAccessStream randAccStream = await file.OpenAsync(FileAccessMode.Read);
                NoteEditor.Document.LoadFromStream(TextSetOptions.FormatRtf, randAccStream);
                noteName.Message = ShellPage.NoteName;
            }
        }
    }
    private async void SaveFile(bool showInfo)
    {
        string directory = notesFolder.Path + @"\Notes\" + ShellPage.NoteName + ".rtf";
        StorageFile file = await StorageFile.GetFileFromPathAsync(directory);
        if (file != null)
        {
            try
            {
                using IRandomAccessStream randAccStream = await file.OpenAsync(FileAccessMode.ReadWrite);
                NoteEditor.Document.SaveToStream(TextGetOptions.FormatRtf, randAccStream);
                if (showInfo)
                {
                    InfoBar("Success".GetLocalized(), InfoBarSeverity.Success, "NoteDetails_SuccessMessage".GetLocalized());
                }
            }
            catch (Exception ex)
            {
                InfoBar("Error".GetLocalized(), InfoBarSeverity.Error, "NoteDetails_ErrorMessage".GetLocalized() + ex.Message);
                LogWriter.Log(ex.Message, LogWriter.LogLevel.Error);
            }
        }
    }
    private void InfoBar(string title, InfoBarSeverity type, string message)
    {
        infobar.Title = title;
        infobar.Severity = type;
        infobar.Message = message;
        infobar.IsOpen = true;
        CreateTimer();
    }
    private void CreateTimer()
    {
        dispatcherTimer = new DispatcherTimer();
        dispatcherTimer.Tick += DispatcherTimer_Tick;
        dispatcherTimer.Interval = TimeSpan.FromSeconds(2);
        dispatcherTimer.Start();
    }
    private void DispatcherTimer_Tick(object sender, object e)
    {
        try
        {
            infobar.IsOpen = false;
            dispatcherTimer?.Stop();
            dispatcherTimer = null;
            BtnSaveFile.IsEnabled = true;
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex.Message, LogWriter.LogLevel.Error);
        }
    }
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveFile(true);
        BtnSaveFile.IsEnabled = false;
    }
    private void NoteEditor_ProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
    {
        var selectedtext = NoteEditor.Document.Selection.Text;
        if (selectedtext.Length > 0)
        {
            var key1 = args.Modifiers;
            var key2 = args.Key;
            if (key1 == VirtualKeyModifiers.Control && key2 == VirtualKey.F)
            {
                findBox.Text = selectedtext;
                findBox.Focus(FocusState.Programmatic);
            }
        }
    }

    private void FindBoxHighlightMatches()
    {
        FindBoxRemoveHighlights();

        Color highlightBackgroundColor = (Color)App.Current.Resources["SystemColorHighlightColor"];
        Color highlightForegroundColor = (Color)App.Current.Resources["SystemColorHighlightTextColor"];

        var textToFind = findBox.Text;
        if (textToFind != null)
        {
            ITextRange searchRange = NoteEditor.Document.GetRange(0, 0);
            while (searchRange.FindText(textToFind, TextConstants.MaxUnitCount, FindOptions.None) > 0)
            {
                searchRange.CharacterFormat.BackgroundColor = highlightBackgroundColor;
                searchRange.CharacterFormat.ForegroundColor = highlightForegroundColor;
            }
        }
    }
    private void FindBoxRemoveHighlights()
    {
        ITextRange documentRange = NoteEditor.Document.GetRange(0, TextConstants.MaxUnitCount);
        SolidColorBrush? defaultBackground = NoteEditor.Background as SolidColorBrush;
        SolidColorBrush? defaultForeground = NoteEditor.Foreground as SolidColorBrush;

        documentRange.CharacterFormat.BackgroundColor = defaultBackground.Color;
        documentRange.CharacterFormat.ForegroundColor = defaultForeground.Color;
    }

    private async void SpellCheck_Checked(object sender, RoutedEventArgs e)
    {
        await localSettingsService.SaveSettingAsync(SpellcheckKey, true);
        NoteEditor.IsSpellCheckEnabled = true;
    }

    private async void SpellCheck_Unchecked(object sender, RoutedEventArgs e)
    {
        await localSettingsService.SaveSettingAsync(SpellcheckKey, false);
        NoteEditor.IsSpellCheckEnabled = false;
    }

    private void NoteEditor_GettingFocus(UIElement sender, GettingFocusEventArgs args)
    {
        NoteEditor.PlaceholderText = string.Empty;
    }

    private void NoteEditor_LosingFocus(UIElement sender, LosingFocusEventArgs args)
    {
        NoteEditor.TextDocument.GetText(TextGetOptions.UseObjectText, out string outtext);
        if (outtext == string.Empty)
        {
            NoteEditor.PlaceholderText = "NoteDetails_EditorPlaceholder".GetLocalized();
        }
    }

    private void BoldButton_Click(object sender, RoutedEventArgs e)
    {
        NoteEditor.Document.Selection.CharacterFormat.Bold = FormatEffect.Toggle;
    }

    private void ItalicButton_Click(object sender, RoutedEventArgs e)
    {
        NoteEditor.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;
    }
    private void UnderlineButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = NoteEditor.Document.Selection;
        selected.CharacterFormat.Underline = selected.CharacterFormat.Underline == UnderlineType.Single ? UnderlineType.None : UnderlineType.Single;
    }
    private void StrikeButton_Click(object sender, RoutedEventArgs e)
    {
        NoteEditor.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.Toggle;
    }
    private void ListButton_Click(object sender, RoutedEventArgs e)
    {
        Button clickedListType = (Button)sender;
        var listType = (MarkerType)Convert.ToUInt32(clickedListType.CommandParameter);
        var selection = NoteEditor.Document.Selection;
        var currentStyle = selection.ParagraphFormat.ListType;
        selection.ParagraphFormat.ListType = currentStyle == listType ? MarkerType.None : listType;
    }
    private void AlignButton_Click(object sender, RoutedEventArgs e)
    {
        Button clickedAlignType = (Button)sender;
        var alignType = (ParagraphAlignment)Convert.ToUInt32(clickedAlignType.CommandParameter);
        var selection = NoteEditor.Document.Selection;
        var currentStyle = selection.ParagraphFormat.Alignment;
        selection.ParagraphFormat.Alignment = currentStyle == alignType ? ParagraphAlignment.Undefined : alignType;
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        // Extract the color of the button that was clicked.
        Button clickedColor = (Button)sender;
        var rectangle = (Microsoft.UI.Xaml.Shapes.Rectangle)clickedColor.Content;
        var color = ((SolidColorBrush)rectangle.Fill).Color;

        NoteEditor.Document.Selection.CharacterFormat.ForegroundColor = color;

        fontColorButton.Flyout.Hide();
        NoteEditor.Focus(FocusState.Keyboard);
    }

    private void findBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        FindBoxHighlightMatches();
    }

    private void findBox_GotFocus(object sender, RoutedEventArgs e)
    {
        FindBoxHighlightMatches();
    }

    private void findBox_LostFocus(object sender, RoutedEventArgs e)
    {
        FindBoxRemoveHighlights();
    }


}
