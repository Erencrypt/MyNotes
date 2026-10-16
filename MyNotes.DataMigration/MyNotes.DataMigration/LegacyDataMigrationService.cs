using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace MyNotes.DataMigration
{
    internal sealed class LegacyDataMigrationService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public  int CleanupLegacyFilesOnly()
        {
            var files = EnumerateLegacyFiles().ToList();
            foreach (var file in files)
            {
                File.Delete(file);
            }
            RemoveEmptyDirectories();
            return files.Count;
        }

        public async Task<LegacyDataMigrationResult> MigrateAsync(CancellationToken cancellationToken = default)
        {
            var data = await ReadLegacyDataAsync(cancellationToken);
            await using var connection = new SqliteConnection( new SqliteConnectionStringBuilder { DataSource = LegacyDataLocations.DatabasePath }.ToString());
            await connection.OpenAsync(cancellationToken);
            await using var transaction = connection.BeginTransaction();

            await InsertTasksAsync(connection, transaction, data, cancellationToken);
            await InsertNotesAsync(connection, transaction, data.Notes, cancellationToken);
            await InsertRemindersAsync(connection, transaction, data.Reminders, cancellationToken);
            transaction.Commit();

            var files = EnumerateLegacyFiles().ToList();
            foreach (var file in files)
            {
                File.Delete(file);
            }
            RemoveEmptyDirectories();

            return new LegacyDataMigrationResult(data.TaskItems.Count, data.Notes.Count, data.Reminders.Count, data.Files.Count);
        }

        private static async Task<ImportData> ReadLegacyDataAsync(CancellationToken cancellationToken)
        {
            var data = new ImportData();
            var plannerFile = Path.Combine(LegacyDataLocations.ApplicationDataPath, "PlannerBoardData.json");
            if (File.Exists(plannerFile))
            {
                await using var stream = File.OpenRead(plannerFile);
                var board = await JsonSerializer.DeserializeAsync<LegacyTaskBoard>(stream, JsonOptions, cancellationToken)
                    ?? throw new InvalidDataException($"Planner board file '{plannerFile}' is empty or invalid.");

                AddTasks(board.ToDoTasks, "To Do", data);
                AddTasks(board.InProgressTasks, "In Progress", data);
                AddTasks(board.DoneTasks, "Done", data);
                data.Files.Add(plannerFile);
            }

            await ReadNotesAsync(LegacyDataLocations.NotesPath, deleted: false, data, cancellationToken);
            await ReadRemindersAsync(LegacyDataLocations.RemindersPath, deleted: false, data, cancellationToken);
            await ReadNotesAsync(LegacyDataLocations.TrashPath, deleted: true, data, cancellationToken);
            await ReadRemindersAsync(LegacyDataLocations.TrashPath, deleted: true, data, cancellationToken);
            return data;
        }

        private static void AddTasks(IEnumerable<LegacyTask>? tasks, string listName, ImportData data)
        {
            var list = new ImportedTaskList(Guid.NewGuid(), listName, data.TaskLists.Count);
            data.TaskLists.Add(list);
            foreach (var task in tasks ?? [])
            {
                data.TaskItems.Add(new ImportedTask(
                    Guid.NewGuid(),
                    task.Title ?? string.Empty,
                    task.Text ?? string.Empty,
                    list.Id));
            }
        }

        private static async Task ReadNotesAsync(string directory, bool deleted, ImportData data, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(directory))
            { return; }

            foreach (var path in Directory.EnumerateFiles(directory)
                         .Where(file => Path.GetExtension(file).Equals(".rtf", StringComparison.OrdinalIgnoreCase)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var timestamp = File.GetLastWriteTime(path);
                data.Notes.Add(new ImportedNote(
                    Guid.NewGuid(),
                    Path.GetFileNameWithoutExtension(path),
                    await File.ReadAllTextAsync(path, cancellationToken),
                    timestamp,
                    deleted ? timestamp : null));
                data.Files.Add(path);
            }
        }

        private static async Task ReadRemindersAsync(string directory, bool deleted, ImportData data, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(directory))
            { return; }

            foreach (var path in Directory.EnumerateFiles(directory)
                         .Where(file => Path.GetExtension(file).Equals(".json", StringComparison.OrdinalIgnoreCase)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await using var stream = File.OpenRead(path);
                var reminder = await JsonSerializer.DeserializeAsync<LegacyReminder>(stream, JsonOptions, cancellationToken)
                    ?? throw new InvalidDataException($"Reminder file '{path}' is empty or invalid.");

                var timestamp = File.GetLastWriteTime(path);
                var fileName = Path.GetFileNameWithoutExtension(path);
                var id = Guid.TryParse(reminder.Id, out var parsedId) && parsedId != Guid.Empty
                    ? parsedId
                    : Guid.TryParse(fileName, out parsedId) && parsedId != Guid.Empty ? parsedId : Guid.NewGuid();

                data.Reminders.Add(new ImportedReminder(
                    id,
                    reminder.ReminderHeader ?? string.Empty,
                    reminder.ReminderText ?? string.Empty,
                    ParseReminderTime(reminder.DateTime, timestamp),
                    reminder.Repeat,
                    reminder.Alarm,
                    timestamp,
                    deleted ? timestamp : null));
                data.Files.Add(path);
            }
        }

        private static async Task InsertTasksAsync(SqliteConnection connection, SqliteTransaction transaction, ImportData data, CancellationToken cancellationToken)
        {
            var order = await GetNextTaskListOrderAsync(connection, transaction, cancellationToken);
            foreach (var list in data.TaskLists)
            {
                await ExecuteAsync(connection, transaction,
                    """
                    INSERT INTO TaskLists (Id, Name, HeaderColor, CreatedAt, UpdatedAt, DeletedAt, "Order")
                    VALUES ($id, $name, $color, $created, $updated, NULL, $order)
                    """,
                    cancellationToken,
                    ("$id", list.Id.ToString()),
                    ("$name", list.Name),
                    ("$color", "#6C757D"),
                    ("$created", DateTime.Now),
                    ("$updated", DateTime.Now),
                    ("$order", order++));
            }

            foreach (var task in data.TaskItems)
            {
                await ExecuteAsync(connection, transaction,
                    """
                    INSERT INTO TaskItems (Id, Title, Text, TaskListId, CreatedAt, UpdatedAt, DeletedAt, "Order")
                    VALUES ($id, $title, $text, $listId, $created, $updated, NULL, 0)
                    """,
                    cancellationToken,
                    ("$id", task.Id.ToString()),
                    ("$title", task.Title),
                    ("$text", task.Text),
                    ("$listId", task.TaskListId.ToString()),
                    ("$created", DateTime.Now),
                    ("$updated", DateTime.Now));
            }
        }

        private static async Task InsertNotesAsync(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<ImportedNote> notes, CancellationToken cancellationToken)
        {
            foreach (var note in notes)
            {
                await ExecuteAsync(connection, transaction,
                    """
                    INSERT INTO Notes (Id, Title, Content, CreatedAt, UpdatedAt, DeletedAt)
                    VALUES ($id, $title, $content, $created, $updated, $deleted)
                    """,
                    cancellationToken,
                    ("$id", note.Id.ToString()),
                    ("$title", note.Title),
                    ("$content", note.Content),
                    ("$created", note.Timestamp),
                    ("$updated", note.Timestamp),
                    ("$deleted", note.DeletedAt));
            }
        }

        private static async Task InsertRemindersAsync(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<ImportedReminder> reminders, CancellationToken cancellationToken)
        {
            var usedIds = new HashSet<Guid>();
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT Id FROM Reminders";
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    if (Guid.TryParse(reader.GetString(0), out var id))
                    {
                        usedIds.Add(id);
                    }
                }
            }

            foreach (var reminder in reminders)
            {
                while (!usedIds.Add(reminder.Id))
                {
                    reminder.Id = Guid.NewGuid();
                }

                await ExecuteAsync(connection, transaction,
                    """
                    INSERT INTO Reminders
                        (Id, ReminderHeader, ReminderText, ReminderDateTime, Repeat, Alarm, IsActive, NoteId, CreatedAt, UpdatedAt, DeletedAt)
                    VALUES
                        ($id, $header, $text, $dateTime, $repeat, $alarm, 1, NULL, $created, $updated, $deleted)
                    """,
                    cancellationToken,
                    ("$id", reminder.Id.ToString()),
                    ("$header", reminder.Header),
                    ("$text", reminder.Text),
                    ("$dateTime", reminder.DateTime),
                    ("$repeat", reminder.Repeat),
                    ("$alarm", reminder.Alarm),
                    ("$created", reminder.Timestamp),
                    ("$updated", reminder.Timestamp),
                    ("$deleted", reminder.DeletedAt));
            }
        }

        private static async Task<int> GetNextTaskListOrderAsync( SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT COALESCE(MAX(\"Order\"), -1) + 1 FROM TaskLists";
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        }

        private static async Task ExecuteAsync( SqliteConnection connection, SqliteTransaction transaction, string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            foreach (var (name, value) in parameters)
            {
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static DateTime ParseReminderTime(string? value, DateTime timestamp)
        {
            if (TimeOnly.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var time)
                || TimeOnly.TryParse(value, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.AllowWhiteSpaces, out time))
            {
                return timestamp.Date.Add(time.ToTimeSpan());
            }

            if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var dateTime)
                || DateTime.TryParse(value, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.AllowWhiteSpaces, out dateTime))
            {
                return dateTime;
            }

            throw new InvalidDataException($"Invalid reminder DateTime value '{value}'.");
        }

        private static void RemoveEmptyDirectories()
        {
            foreach (var directory in new[]
                     {
                         LegacyDataLocations.ApplicationDataPath,
                         LegacyDataLocations.NotesPath,
                         LegacyDataLocations.RemindersPath,
                         LegacyDataLocations.TrashPath
                     })
            {
                if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
        }

        private static IEnumerable<string> EnumerateLegacyFiles()
        {
            foreach (var directory in new[] {
                         LegacyDataLocations.NotesPath,
                         LegacyDataLocations.RemindersPath,
                         LegacyDataLocations.TrashPath,
                         LegacyDataLocations.ApplicationDataPath,})
            {
                if (!Directory.Exists(directory))
                { continue; }

                foreach (var file in Directory.EnumerateFiles(directory))
                {
                    var extension = Path.GetExtension(file);
                    if (extension.Equals(".rtf", StringComparison.OrdinalIgnoreCase)
                        || extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return file;
                    }
                }
            }
        }

        private sealed class ImportData
        {
            public List<ImportedTaskList> TaskLists { get; } = [];
            public List<ImportedTask> TaskItems { get; } = [];
            public List<ImportedNote> Notes { get; } = [];
            public List<ImportedReminder> Reminders { get; } = [];
            public HashSet<string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed record ImportedTaskList(Guid Id, string Name, int Order);
        private sealed record ImportedTask(Guid Id, string Title, string Text, Guid TaskListId);
        private sealed record ImportedNote(Guid Id, string Title, string Content, DateTime Timestamp, DateTime? DeletedAt);
        private sealed record LegacyTaskBoard(List<LegacyTask>? ToDoTasks, List<LegacyTask>? InProgressTasks, List<LegacyTask>? DoneTasks);
        private sealed record LegacyTask(string? Title, string? Text);

        private sealed class ImportedReminder(
            Guid id,
            string header,
            string text,
            DateTime dateTime,
            bool repeat,
            bool alarm,
            DateTime timestamp,
            DateTime? deletedAt)
        {
            public Guid Id { get; set; } = id;
            public string Header { get; } = header;
            public string Text { get; } = text;
            public DateTime DateTime { get; } = dateTime;
            public bool Repeat { get; } = repeat;
            public bool Alarm { get; } = alarm;
            public DateTime Timestamp { get; } = timestamp;
            public DateTime? DeletedAt { get; } = deletedAt;
        }

        private sealed record LegacyReminder(
            string? Id,
            string? ReminderHeader,
            string? ReminderText,
            string? DateTime,
            bool Repeat,
            bool Alarm);
    }
}
