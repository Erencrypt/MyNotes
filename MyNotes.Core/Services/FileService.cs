using MyNotes.Core.Contracts.Services;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MyNotes.Core.Services;

public class FileService : IFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public T Read<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }

        return default;
    }

    public void Save<T>(string folderPath, string fileName, T content)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileContent = JsonSerializer.Serialize(content, JsonOptions);
        File.WriteAllTextAsync(Path.Combine(folderPath, fileName), fileContent, Encoding.UTF8);
    }

    public void Delete(string folderPath, string fileName)
    {
        if (!string.IsNullOrEmpty(fileName) && File.Exists(Path.Combine(folderPath, fileName)))
        {
            File.Delete(Path.Combine(folderPath, fileName));
        }
    }
}
