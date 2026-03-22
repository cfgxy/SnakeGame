using System.Text.Json;
using SnakeGame.App.Logging;
using SnakeGame.Core;

namespace SnakeGame.App;

/// <summary>
/// 负责把音频设置保存到本地 JSON 文件。
/// 包含完整的错误处理和日志记录。
/// </summary>
internal sealed class AudioSettingsFileStore : IAudioSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string settingsPath;
    private readonly GameLogger? logger;

    public AudioSettingsFileStore(string settingsPath, GameLogger? logger = null)
    {
        this.settingsPath = settingsPath;
        this.logger = logger;
    }

    /// <summary>
    /// 加载音频设置。
    /// 如果文件损坏或不存在，返回 null。
    /// </summary>
    public AudioSettings? Load()
    {
        try
        {
            if (!File.Exists(settingsPath))
            {
                return null;
            }

            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<AudioSettings>(json);
        }
        catch (JsonException ex)
        {
            // JSON 格式错误，备份旧文件并返回 null
            logger?.Warning("音频设置文件格式错误，将使用默认设置", ex);
            BackupCorruptedFile();
            return null;
        }
        catch (IOException ex)
        {
            logger?.Warning("无法读取音频设置文件", ex);
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger?.Warning("无权限访问音频设置文件", ex);
            return null;
        }
    }

    /// <summary>
    /// 保存音频设置。
    /// </summary>
    /// <returns>是否保存成功</returns>
    public bool Save(AudioSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 先写入临时文件，再替换，避免写入失败导致数据丢失
            var tempPath = settingsPath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(settings, JsonOptions));
            
            // 原子替换
            File.Move(tempPath, settingsPath, overwrite: true);
            
            return true;
        }
        catch (IOException ex)
        {
            logger?.Error("保存音频设置失败", ex);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger?.Error("无权限保存音频设置", ex);
            return false;
        }
    }

    /// <summary>
    /// 备份损坏的文件。
    /// </summary>
    private void BackupCorruptedFile()
    {
        try
        {
            if (File.Exists(settingsPath))
            {
                var backupPath = $"{settingsPath}.corrupted.{DateTime.Now:yyyyMMddHHmmss}";
                File.Move(settingsPath, backupPath);
                logger?.Info($"已备份损坏的设置文件到: {backupPath}");
            }
        }
        catch (Exception ex)
        {
            logger?.Warning("备份损坏文件失败", ex);
        }
    }
}