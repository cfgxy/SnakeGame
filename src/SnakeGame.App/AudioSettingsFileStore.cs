using System.Text.Json;
using SnakeGame.Core;

namespace SnakeGame.App;

/// <summary>
/// 负责把音频设置保存到本地 JSON 文件。
/// </summary>
internal sealed class AudioSettingsFileStore : IAudioSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string settingsPath;

    public AudioSettingsFileStore(string settingsPath)
    {
        this.settingsPath = settingsPath;
    }

    public AudioSettings? Load()
    {
        if (!File.Exists(settingsPath))
        {
            return null;
        }

        var json = File.ReadAllText(settingsPath);
        return JsonSerializer.Deserialize<AudioSettings>(json);
    }

    public void Save(AudioSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
