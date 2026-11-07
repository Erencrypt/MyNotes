using WinUI3Localizer;

namespace MyNotes.Helpers
{
    /// <summary>
    /// WinUI3Localizer has no built-in fallback language: a key that is missing or empty in the
    /// current language resolves to an empty string. This fills every non-default language
    /// dictionary with the default language's value for any key that is missing or blank,
    /// so both code (GetLocalizedString) and XAML (l:Uids.Uid) fall back to English.
    /// Must be called after the localizer is built and before SetLanguage.
    /// </summary>
    public static class LocalizationFallback
    {
        public static void ApplyFallback(ILocalizer localizer, string fallbackLanguage)
        {
            var dictionaries = localizer.GetLanguageDictionaries().ToList();
            var fallback = dictionaries.FirstOrDefault(d =>
                string.Equals(d.Language, fallbackLanguage, StringComparison.OrdinalIgnoreCase));

            if (fallback is null)
                return;

            var fallbackItems = fallback.GetItems()
                .Where(i => !string.IsNullOrWhiteSpace(i.Value))
                .GroupBy(i => i.StringResourceItemName, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

            foreach (var dictionary in dictionaries)
            {
                if (ReferenceEquals(dictionary, fallback))
                    continue;

                foreach (var (name, fallbackItem) in fallbackItems)
                {
                    if (dictionary.TryGetItems(fallbackItem.Uid, out var items))
                    {
                        var index = items.FindIndex(i => string.Equals(i.StringResourceItemName, name, StringComparison.Ordinal));
                        if (index >= 0)
                        {
                            // Key exists but is blank: replace it with the fallback value.
                            if (string.IsNullOrWhiteSpace(items[index].Value))
                                items[index] = fallbackItem with { };
                            continue;
                        }
                    }

                    // Key doesn't exist in this language at all.
                    dictionary.AddItem(fallbackItem with { });
                }
            }
        }
    }
}
