using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MyNotes.Core.Helpers;

public static class Json
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public static async Task<T> ToObjectAsync<T>(string value)
    {
        return await Task.Run(() =>
        {
            return JsonSerializer.Deserialize<T>(value, JsonOptions);
        });
    }

    public static async Task<string> StringifyAsync(object value)
    {
        return await Task.Run(() =>
        {
            return JsonSerializer.Serialize(value, JsonOptions);
        });
    }
}
