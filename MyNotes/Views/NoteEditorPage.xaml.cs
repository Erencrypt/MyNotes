using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MyNotes.Helpers;
using MyNotes.ViewModels;
using Windows.System;
using Windows.UI;
using WinUI3Localizer;

namespace MyNotes.Views
{
    /// <summary>
    /// Note editor page for adding/editing notes
    /// </summary>
    public sealed partial class NoteEditorPage : Page
    {
        private readonly ILocalizer _Localizer = Localizer.Get();
        public NoteEditorViewModel ViewModel { get; }

        public NoteEditorPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<NoteEditorViewModel>();
            DataContext = ViewModel;

            //NoteEditor.SelectionFlyout = null;

            TitleTextBox.PlaceholderText = _Localizer.GetLocalizedString("NoteEditor_TitleTextBoxPlaceholder");
            NoteEditor.PlaceholderText = _Localizer.GetLocalizedString("NoteEditor_EditorPlaceholder");
            FindBox.PlaceholderText = _Localizer.GetLocalizedString("NoteEditor_FindPlaceholder");
            ToolTipService.SetToolTip(BtnSaveFile, _Localizer.GetLocalizedString("NoteEditor_SaveTooltip"));
            ToolTipService.SetToolTip(FontColorButton, _Localizer.GetLocalizedString("NoteEditor_FontColorTooltip"));
            ToolTipService.SetToolTip(AlignButton, _Localizer.GetLocalizedString("NoteEditor_AlignmentTooltip"));
            ToolTipService.SetToolTip(ListButton, _Localizer.GetLocalizedString("NoteEditor_ListTypeTooltip"));
            ToolTipService.SetToolTip(BoldButton, _Localizer.GetLocalizedString("NoteEditor_BoldTooltip"));
            ToolTipService.SetToolTip(ItalicButton, _Localizer.GetLocalizedString("NoteEditor_ItalicTooltip"));
            ToolTipService.SetToolTip(UnderlineButton, _Localizer.GetLocalizedString("NoteEditor_UnderlineTooltip"));
            ToolTipService.SetToolTip(StrikeButton, _Localizer.GetLocalizedString("NoteEditor_StrikethroughTooltip"));


        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Data.Models.Note note)
            {
                // Load existing note for editing
                await ViewModel.LoadNoteAsync(note.Id);
                // Update RichEditBox content after loading
                UpdateRichEditBoxContent();
            }
            if (ViewModel.NoteId == Guid.Empty)
            {
                DeleteButton.Visibility = Visibility.Collapsed;
                BtnSaveFile.IsEnabled = false;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var title = TitleTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(title))
            {
                AppInfoBar.Show("Validation Error", "Title is required.", AlertInfoBarSeverity.Warning);
                TitleTextBox.Focus(FocusState.Programmatic);
                return;
            }
            NoteEditor.Document.GetText(TextGetOptions.None, out string plainText);
            if (string.IsNullOrWhiteSpace(plainText.Trim('\r')))
            {
                AppInfoBar.Show("Validation Error", "Content is required.", AlertInfoBarSeverity.Warning);
                NoteEditor.Focus(FocusState.Programmatic);
                return;
            }

            // Update ViewModel content from RichEditBox before saving
            UpdateViewModelContent();
            await ViewModel.SaveNoteCommand.ExecuteAsync(null);

            Button senderbtn = (Button)sender;
            if (senderbtn.Name == "BtnSaveFile")
            {
                BtnSaveFile.IsEnabled = false;
                BtnSaveFile.Content = new FontIcon { Glyph = "\uE73E" }; // Change to checkmark icon
                Timer timer = new((state) =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        BtnSaveFile.IsEnabled = true;
                        BtnSaveFile.Content = new FontIcon { Glyph = "\uE74E" }; // Change back to save icon
                    });
                }, null, 2000, Timeout.Infinite);

                AppInfoBar.Show("Saved", "Your note was saved successfully.", AlertInfoBarSeverity.Success);
            }
            else
            {
                AppInfoBar.Show("Saved", "Your note was saved successfully.", AlertInfoBarSeverity.Success);
                // Navigate back to notes list
                NavigationHelper.GoBack();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back to notes list
            NavigationHelper.GoBack();
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
                    FindBox.Text = selectedtext;
                    FindBox.Focus(FocusState.Programmatic);
                }
            }
        }

        private void FindBoxHighlightMatches()
        {
            FindBoxRemoveHighlights();

            Color highlightBackgroundColor = (Color)App.Current.Resources["SystemColorHighlightColor"];
            Color highlightForegroundColor = (Color)App.Current.Resources["SystemColorHighlightTextColor"];

            var textToFind = FindBox.Text;
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
            SolidColorBrush defaultBackground = (NoteEditor.Background as SolidColorBrush)!;
            SolidColorBrush defaultForeground = (NoteEditor.Foreground as SolidColorBrush)!;

            documentRange.CharacterFormat.BackgroundColor = defaultBackground.Color;
            documentRange.CharacterFormat.ForegroundColor = defaultForeground.Color;
        }
        private async void SpellCheck_Checked(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveSpellCheckChange(true);
            NoteEditor.IsSpellCheckEnabled = true;
        }

        private async void SpellCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveSpellCheckChange(false);
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
                NoteEditor.PlaceholderText = _Localizer.GetLocalizedString("NoteEditor_EditorPlaceholder");
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

            FontColorButton.Flyout.Hide();
            NoteEditor.Focus(FocusState.Keyboard);
        }

        private void FindBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FindBoxHighlightMatches();
        }

        private void FindBox_GotFocus(object sender, RoutedEventArgs e)
        {
            FindBoxHighlightMatches();
        }

        private void FindBox_LostFocus(object sender, RoutedEventArgs e)
        {
            FindBoxRemoveHighlights();
        }

        /// <summary>
        /// Updates the ViewModel's Content property with the current RichEditBox content
        /// </summary>
        private void UpdateViewModelContent()
        {
            ViewModel.Title = TitleTextBox.Text;
            NoteEditor.Document.GetText(TextGetOptions.FormatRtf, out string rtfContent);
            ViewModel.Content = rtfContent;
        }

        /// <summary>
        /// Updates the RichEditBox content with the ViewModel's Content property
        /// </summary>
        private void UpdateRichEditBoxContent()
        {
            if (!string.IsNullOrEmpty(ViewModel.Content))
            {
                NoteEditor.Document.SetText(TextSetOptions.FormatRtf, ViewModel.Content);
            }
            else
            {
                NoteEditor.Document.SetText(TextSetOptions.None, string.Empty);
            }
        }
        private async void DeleteButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            DeleteFlyout.Hide();
            if (ViewModel.NoteId != Guid.Empty)
            {
                await ViewModel.DeleteNote(ViewModel.NoteId);
            }
        }
    }
}
