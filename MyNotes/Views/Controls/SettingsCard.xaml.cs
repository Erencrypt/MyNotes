using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;


namespace MyNotes.Views.Controls
{
    /// <summary>
    /// Reusable settings-page card that contains an icon, a title,
    /// an optional description, and an arbitrary input control.
    ///
    /// Dependency properties
    /// ─────────────────────
    /// Icon            — FontIcon glyph string, e.g. "&#xE777;"
    /// Title           — Card heading  (x:Uid maps to  Uid.Title)
    /// Description     — Sub-heading   (x:Uid maps to  Uid.Description)
    ///                   When empty/null the title spans both rows and
    ///                   is vertically centred automatically.
    /// ControlContent  — Any UIElement placed on the right side.
    ///                   This is also the [ContentProperty] so you can
    ///                   put the child element directly inside the tag.
    ///
    /// Usage example
    /// ─────────────
    ///   <controls:SettingsCard Icon="&#xE777;" x:Uid="Settings_Startup">
    ///       <ToggleSwitch Toggled="StartupToggle_Toggled" />
    ///   </controls:SettingsCard>
    ///
    ///   Resources.resw entries:
    ///       Settings_Startup.Title          → "Run at startup"
    ///       Settings_Startup.Description    → "Launch the app when Windows starts"
    ///       Settings_Startup.Icon           → (optional override)
    /// </summary>
    [ContentProperty(Name = nameof(ControlContent))]
    public sealed partial class SettingsCard : UserControl
    {
        // ── Dependency Properties ────────────────────────────────

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(string),
                typeof(SettingsCard),
                new PropertyMetadata(string.Empty, OnIconChanged));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(SettingsCard),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(
                nameof(Description),
                typeof(string),
                typeof(SettingsCard),
                new PropertyMetadata(string.Empty, OnDescriptionChanged));

        public static readonly DependencyProperty ControlContentProperty =
            DependencyProperty.Register(
                nameof(ControlContent),
                typeof(object),
                typeof(SettingsCard),
                new PropertyMetadata(null));

        // ── CLR wrappers ─────────────────────────────────────────

        /// <summary>FontIcon glyph, e.g. <c>"&#xE777;"</c></summary>
        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Card heading. Set via x:Uid (Uid.Title in .resw) or directly.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Optional sub-heading. Set via x:Uid (Uid.Description in .resw) or
        /// directly. Leave empty to hide and let the title fill both rows.
        /// </summary>
        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        /// <summary>
        /// The input control placed on the right side of the card
        /// (ToggleSwitch, ComboBox, etc.).
        /// This is the default content property — you can nest the
        /// element directly inside the <SettingsCard> tag.
        /// </summary>
        public object ControlContent
        {
            get => GetValue(ControlContentProperty);
            set => SetValue(ControlContentProperty, value);
        }
        public SettingsCard()
        {
            InitializeComponent();
            // Apply initial state for Description after the template is ready.
            Loaded += (_, _) => ApplyDescriptionState(Description);
        }

        // ── Property-changed callbacks ───────────────────────────

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var card = (SettingsCard)d;
            card.IconElement.Glyph = (string)e.NewValue;
        }

        private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var card = (SettingsCard)d;
            card.ApplyDescriptionState((string)e.NewValue);
        }

        // ── Helpers ──────────────────────────────────────────────

        /// <summary>
        /// Shows/hides the description row and adjusts the title's RowSpan
        /// and vertical alignment to match.
        /// </summary>
        private void ApplyDescriptionState(string? description)
        {
            bool hasDescription = !string.IsNullOrEmpty(description);

            DescriptionBlock.Visibility = hasDescription
                ? Visibility.Visible
                : Visibility.Collapsed;

            // Span title across both rows when there is no description.
            Grid.SetRowSpan(TitleBlock, hasDescription ? 1 : 2);

            // Center vertically when spanning; align to bottom of the top
            // row when a description sits beneath it.
            TitleBlock.VerticalAlignment = hasDescription
                ? VerticalAlignment.Bottom
                : VerticalAlignment.Center;

            if (hasDescription)
                DescriptionBlock.VerticalAlignment = VerticalAlignment.Top;
        }
    }
}
