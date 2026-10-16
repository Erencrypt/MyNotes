using CommunityToolkit.Mvvm.Input;
using MyNotes.Data.Models;
using MyNotes.Data.Models.Enums;
using MyNotes.Data.Services.Interfaces;
using MyNotes.Helpers;
using MyNotes.Services.Interfaces;
using MyNotes.Views;
using System.Collections.ObjectModel;


namespace MyNotes.ViewModels
{
    /// <summary>
    /// ViewModel for the reminder editor page
    /// </summary>
    public partial class ReminderEditorViewModel : BaseViewModel
    {
        private readonly IReminderService _reminderService;
        private readonly INoteService _noteService;
        private readonly IReminderScheduler _scheduler;
        private readonly ISettingService _settingService;

        public ReminderEditorViewModel(IReminderService reminderService, INoteService noteService, IReminderScheduler scheduler, ISettingService settingService)
        {
            _reminderService = reminderService;
            _noteService = noteService;
            _scheduler = scheduler;
            _settingService = settingService;

            // Initialize with current date/time
            ReminderDate = DateTime.Today;
            ReminderTime = DateTime.Now.TimeOfDay;
        }

        // Reminder properties
        private Guid _reminderId = Guid.Empty;
        private string _header = string.Empty;
        private string _text = string.Empty;
        private DateTimeOffset _reminderDate = DateTimeOffset.Now.Date;
        private TimeSpan _reminderTime = DateTime.Now.TimeOfDay;
        private bool _repeat = false;
        private bool _alarm = false;
        private bool _isActive = true;
        private string _hourFormat = "12HourClock";

        // Note association
        private ObservableCollection<Note> _availableNotes = new();
        private Note? _selectedNote;

        public Guid ReminderId
        {
            get => _reminderId;
            set => SetProperty(ref _reminderId, value);
        }

        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public DateTimeOffset ReminderDate
        {
            get => _reminderDate;
            set => SetProperty(ref _reminderDate, value);
        }

        public TimeSpan ReminderTime
        {
            get => _reminderTime;
            set => SetProperty(ref _reminderTime, value);
        }

        public bool Repeat
        {
            get => _repeat;
            set => SetProperty(ref _repeat, value);
        }

        public bool Alarm
        {
            get => _alarm;
            set => SetProperty(ref _alarm, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public ObservableCollection<Note> AvailableNotes
        {
            get => _availableNotes;
            set => SetProperty(ref _availableNotes, value);
        }

        public Note? SelectedNote
        {
            get => _selectedNote;
            set => SetProperty(ref _selectedNote, value);
        }

        public string HourFormat
        {
            get => _hourFormat;
            set => SetProperty(ref _hourFormat, value);
        }
        /// <summary>
        /// Command to save the reminder
        /// </summary>
        [RelayCommand]
        public async Task SaveReminderAsync()
        {
            try
            {
                IsLoading = true;

                Reminder reminder;
                if (ReminderId == Guid.Empty) // New reminder
                {
                    reminder = new Reminder
                    {
                        ReminderHeader = Header.Trim(),
                        ReminderText = Text?.Trim() ?? string.Empty,
                        ReminderDateTime = ReminderDate.Date.Add(ReminderTime),
                        Repeat = Repeat,
                        Alarm = Alarm,
                        IsActive = IsActive,
                        NoteId = SelectedNote?.Id,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    // Schedule if it's in the future or if it's a repeating reminder (always schedule repeating)
                    if (reminder.ReminderDateTime >= DateTime.Now || reminder.Repeat)
                    {
                        _scheduler.Schedule(reminder);
                    }
                }
                else // Existing reminder
                {
                    reminder = (await _reminderService.GetReminderByIdAsync(ReminderId))!;
                    reminder.ReminderHeader = Header.Trim();
                    reminder.ReminderText = Text?.Trim() ?? string.Empty;
                    reminder.ReminderDateTime = ReminderDate.Date.Add(ReminderTime);
                    reminder.Repeat = Repeat;
                    reminder.Alarm = Alarm;
                    reminder.IsActive = IsActive;
                    reminder.NoteId = SelectedNote?.Id;
                    reminder.UpdatedAt = DateTime.Now;

                    // Reschedule if it's in the future or if it's repeating
                    if (reminder.Repeat)
                    {
                        // For repeating reminders, apply the same forward-adjustment as InitializeAsync:
                        // use the user-chosen time-of-day; if it has already passed today, push to tomorrow.
                        var reminderTime = reminder.ReminderDateTime.TimeOfDay;
                        var todayOccurrence = DateTime.Now.Date.Add(reminderTime);
                        reminder.ReminderDateTime = todayOccurrence <= DateTime.Now
                            ? DateTime.Now.Date.AddDays(1).Add(reminderTime)
                            : todayOccurrence;
                        _scheduler.Reschedule(reminder);
                    }
                    else if (reminder.ReminderDateTime >= DateTime.Now)
                    {
                        _scheduler.Reschedule(reminder);
                    }
                    else
                    {
                        // If time has passed and not repeating, unschedule it
                        _scheduler.Unschedule(reminder.Id);
                    }
                }

                if (ReminderId == Guid.Empty)
                {
                    await _reminderService.CreateReminderAsync(reminder);
                }
                else
                {
                    await _reminderService.UpdateReminderAsync(reminder);
                }
            }
            catch (Exception ex)
            {
                AppInfoBar.Show("Error", $"An error occurred while saving the reminder: {ex.Message}", AlertInfoBarSeverity.Error);
                System.Diagnostics.Debug.WriteLine($"Error saving reminder: {ex.Message}");
                throw; // Re-throw to let the UI handle it
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Load an existing reminder for editing
        /// </summary>
        public async Task LoadReminderAsync(Guid reminderId)
        {
            try
            {
                IsLoading = true;
                var reminder = await _reminderService.GetReminderByIdAsync(reminderId);

                if (reminder != null)
                {
                    ReminderId = reminder.Id;
                    Header = reminder.ReminderHeader ?? string.Empty;
                    Text = reminder.ReminderText ?? string.Empty;
                    ReminderDate = new DateTimeOffset(reminder.ReminderDateTime.Date);
                    ReminderTime = reminder.ReminderDateTime.TimeOfDay;
                    Repeat = reminder.Repeat;
                    Alarm = reminder.Alarm;
                    IsActive = reminder.IsActive;

                    if (reminder.Note != null)
                    {
                        SelectedNote = reminder.Note;
                    }
                }
            }
            catch (Exception ex)
            {
                AppInfoBar.Show("Error", $"An error occurred while loading the reminder: {ex.Message}", AlertInfoBarSeverity.Error);
                System.Diagnostics.Debug.WriteLine($"Error loading reminder: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Load available notes for association
        /// </summary>
        private async Task LoadAvailableNotesAsync()
        {
            try
            {
                var notes = await _noteService.GetActiveNotesAsync();
                AvailableNotes.Clear();

                foreach (var note in notes)
                {
                    AvailableNotes.Add(note);
                }
            }
            catch (Exception ex)
            {
                AppInfoBar.Show("Error", $"An error occurred while loading the notes: {ex.Message}", AlertInfoBarSeverity.Error);
                System.Diagnostics.Debug.WriteLine($"Error loading notes: {ex.Message}");
            }
        }

        public async override Task Initialize()
        {
            await LoadAvailableNotesAsync();
            TimeFormat savedHourFormat = await _settingService.GetSettingAsync<TimeFormat>(DataKeys.HourFormat);
            HourFormat = savedHourFormat == TimeFormat.TwelveHour ? "12HourClock" : "24HourClock";
        }

        public async Task DeleteReminder(Guid id)
        {
            await _reminderService.SoftDeleteReminderAsync(id);
            NavigationHelper.NavigateTo<RemindersListPage>();
        }
    }
}
