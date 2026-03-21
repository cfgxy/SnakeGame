using System.Text.Json;
using SnakeGame.Core;

namespace SnakeGame.App;

/// <summary>
/// 负责保存和读取本地排行榜数据。
/// </summary>
internal sealed class LeaderboardStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string leaderboardPath;

    public LeaderboardStore(string leaderboardPath)
    {
        this.leaderboardPath = leaderboardPath;
    }

    public IReadOnlyList<ScoreRecord> Load()
    {
        if (!File.Exists(leaderboardPath))
        {
            return Array.Empty<ScoreRecord>();
        }

        var json = File.ReadAllText(leaderboardPath);
        var items = JsonSerializer.Deserialize<List<ScoreRecord>>(json);
        return items ?? new List<ScoreRecord>();
    }

    public void Save(ScoreRecord record)
    {
        var items = Load()
            .Append(record)
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.ReachedLevel)
            .ThenBy(item => item.Duration)
            .Take(10)
            .ToArray();

        Directory.CreateDirectory(Path.GetDirectoryName(leaderboardPath)!);
        File.WriteAllText(leaderboardPath, JsonSerializer.Serialize(items, JsonOptions));
    }
}
