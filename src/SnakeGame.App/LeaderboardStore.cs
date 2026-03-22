using System.Text.Json;
using SnakeGame.App.Logging;
using SnakeGame.Core;

namespace SnakeGame.App;

/// <summary>
/// 负责保存和读取本地排行榜数据。
/// 包含完整的错误处理和日志记录。
/// </summary>
internal sealed class LeaderboardStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string leaderboardPath;
    private readonly GameLogger? logger;

    public LeaderboardStore(string leaderboardPath, GameLogger? logger = null)
    {
        this.leaderboardPath = leaderboardPath;
        this.logger = logger;
    }

    /// <summary>
    /// 加载排行榜数据。
    /// 如果文件损坏或不存在，返回空列表。
    /// </summary>
    public IReadOnlyList<ScoreRecord> Load()
    {
        try
        {
            if (!File.Exists(leaderboardPath))
            {
                return Array.Empty<ScoreRecord>();
            }

            var json = File.ReadAllText(leaderboardPath);
            var items = JsonSerializer.Deserialize<List<ScoreRecord>>(json);
            return items ?? new List<ScoreRecord>();
        }
        catch (JsonException ex)
        {
            // JSON 格式错误，备份旧文件并返回空列表
            logger?.Warning("排行榜文件格式错误，将重置排行榜", ex);
            BackupCorruptedFile();
            return Array.Empty<ScoreRecord>();
        }
        catch (IOException ex)
        {
            logger?.Warning("无法读取排行榜文件", ex);
            return Array.Empty<ScoreRecord>();
        }
        catch (UnauthorizedAccessException ex)
        {
            logger?.Warning("无权限访问排行榜文件", ex);
            return Array.Empty<ScoreRecord>();
        }
    }

    /// <summary>
    /// 保存排行榜记录。
    /// </summary>
    /// <returns>是否保存成功</returns>
    public bool Save(ScoreRecord record)
    {
        try
        {
            var items = Load()
                .Append(record)
                .OrderByDescending(item => item.Score)
                .ThenByDescending(item => item.ReachedLevel)
                .ThenBy(item => item.Duration)
                .Take(10)
                .ToArray();

            var directory = Path.GetDirectoryName(leaderboardPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 先写入临时文件，再替换，避免写入失败导致数据丢失
            var tempPath = leaderboardPath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(items, JsonOptions));
            
            // 原子替换
            File.Move(tempPath, leaderboardPath, overwrite: true);
            
            return true;
        }
        catch (IOException ex)
        {
            logger?.Error("保存排行榜失败", ex);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger?.Error("无权限保存排行榜", ex);
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
            if (File.Exists(leaderboardPath))
            {
                var backupPath = $"{leaderboardPath}.corrupted.{DateTime.Now:yyyyMMddHHmmss}";
                File.Move(leaderboardPath, backupPath);
                logger?.Info($"已备份损坏的排行榜文件到: {backupPath}");
            }
        }
        catch (Exception ex)
        {
            logger?.Warning("备份损坏文件失败", ex);
        }
    }
}