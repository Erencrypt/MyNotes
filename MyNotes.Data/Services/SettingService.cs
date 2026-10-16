using Microsoft.EntityFrameworkCore;
using MyNotes.Data.Context;
using MyNotes.Data.Models;
using MyNotes.Data.Services.Interfaces;
using System.Text.Json;

namespace MyNotes.Data.Services
{
    /// <summary>
    /// Service implementation for settings operations
    /// </summary>
    public class SettingService : ISettingService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public SettingService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var setting = await context.Settings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
                return defaultValue;

            try
            {
                return JsonSerializer.Deserialize<T>(setting.Value);
            }
            catch
            {
                return defaultValue;
            }
        }

        public async Task SetSettingAsync<T>(string key, T value)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var setting = await context.Settings
                .FirstOrDefaultAsync(s => s.Key == key);

            var serializedValue = JsonSerializer.Serialize(value);
            var typeName = typeof(T).FullName ?? typeof(T).Name;

            if (setting == null)
            {
                setting = new Setting
                {
                    Key = key,
                    Value = serializedValue,
                    Type = typeName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.Settings.Add(setting);
            }
            else
            {
                setting.Value = serializedValue;
                setting.Type = typeName;
                setting.UpdatedAt = DateTime.UtcNow;
                context.Settings.Update(setting);
            }

            await context.SaveChangesAsync();
        }

        public async Task RemoveSettingAsync(string key)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var setting = await context.Settings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting != null)
            {
                setting.DeletedAt = DateTime.Now;
                setting.UpdatedAt = DateTime.Now;
                await context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasSettingAsync(string key)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Settings
                .AnyAsync(s => s.Key == key);
        }
    }
}