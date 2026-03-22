using System.Text.Json;
using SnakeGame.App.Logging;
using SnakeGame.Core;

namespace SnakeGame.App.Tests;

/// <summary>
/// 文件存储测试。
/// </summary>
public class FileStoreTests : IDisposable
{
    private readonly string testDirectory;

    public FileStoreTests()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), $"SnakeGameTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    #region LeaderboardStore 测试

    [Fact]
    public void LeaderboardStore_Load_NonExistentFile_ShouldReturnEmpty()
    {
        // Arrange
        var path = Path.Combine(testDirectory, "leaderboard.json");
        var store = new LeaderboardStore(path);

        // Act
        var result = store.Load();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void LeaderboardStore_SaveAndLoad_ShouldPersistData()
    {
        // Arrange
        var path = Path.Combine(testDirectory, "leaderboard.json");
        var store = new LeaderboardStore(path);
        var record = new ScoreRecord("Player1", 100, 5, TimeSpan.FromMinutes(2), GameMode.Classic);

        // Act
        store.Save(record);
        var result = store.Load();

        // Assert
        result.Should().HaveCount(1);
        result[0].PlayerName.Should().Be("Player1");
        result[0].Score.Should().Be(100);
    }

    [Fact]
    public void LeaderboardStore_Load_CorruptedJson_ShouldReturnEmptyAndBackup()
    {
        // Arrange
        var path = Path.Combine(testDirectory, "leaderboard.json");
        File.WriteAllText(path, "{ invalid json }");
        var store = new LeaderboardStore(path);

        // Act
        var result = store.Load();

        // Assert
        result.Should().BeEmpty();
        // 原文件应该被备份
        Directory.GetFiles(testDirectory, "leaderboard.json.corrupted.*")
            .Should().HaveCount(1);
    }

    [Fact]
    public void LeaderboardStore_Save_ShouldKeepTop10()
    {
        // Arrange
        var path = Path.Combine(testDirectory, "leaderboard.json");
        var store = new LeaderboardStore(path);

        // Act - 保存 15 条记录
        for (int i = 0; i < 15; i++)
        {
            store.Save(new ScoreRecord($"Player{i}", i * 10, 1, TimeSpan.FromMinutes(1), GameMode.Classic));
        }
        var result = store.Load();

        // Assert - 只保留前 10 条
        result.Should().HaveCount(10);
        result[0].Score.Should().Be(140); // 最高分
        result[9].Score.Should().Be(50);  // 第 10 名
    }

    [Fact]
    public void LeaderboardStore_Save_ShouldOrderByScoreDescending()
    {
        // Arrange
        var path = Path.Combine(testDirectory, "leaderboard.json");
        var store = new LeaderboardStore(path);
        store.Save(new ScoreRecord("Player1", 100, 1, TimeSpan.FromMinutes(1), GameMode.Classic));
        store.Save(new ScoreRecord("Player2", 200, 1, TimeSpan.FromMinutes(1), GameMode.Classic));
        store.Save(new ScoreRecord("Player3", 150, 1, TimeSpan.FromMinutes(1), GameMode.Classic));

        // Act
        var result = store.Load();

        // Assert
        result[0].Score.Should().Be(200);
        result[1].Score.Should().Be(150);
        result[2].Score.Should().Be(100);
    }

    #endregion

    #region AudioSettingsFileStore 测试

    [Fact]
    public void AudioSettingsFileStore_Load_NonExistentFile_ShouldReturnNull()
    {
        // Arrange
        var path = Path.Combine(testDirectory, "settings.json");
        var store = new AudioSettingsFileStore(path);

        // Act
        var result = store.Load();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AudioSettingsFileStore_SaveAndLoad_ShouldPersistData()
    {
        // Arrange
        var path = Path.Combine(testDirectory, "settings.json");
        var store = new AudioSettingsFileStore(path);
        var settings = new AudioSettings { MasterVolume = 0.5f, MusicVolume = 0.8f, SfxVolume = 0.6f };

        // Act
        store.Save(settings);
        var result = store.Load();

        // Assert
        result.Should().NotBeNull();
        result!.MasterVolume.Should().Be(0.5f);
        result.MusicVolume.Should().Be(0.8f);
        result.SfxVolume.Should().Be(0.6f);
    }

    [Fact]
    public void AudioSettingsFileStore_Load_CorruptedJson_ShouldReturnNullAndBackup()
    {
        // Arrange
        var path = Path.Combine(testDirectory, "settings.json");
        File.WriteAllText(path, "{ invalid json }");
        var store = new AudioSettingsFileStore(path);

        // Act
        var result = store.Load();

        // Assert
        result.Should().BeNull();
        // 原文件应该被备份
        Directory.GetFiles(testDirectory, "settings.json.corrupted.*")
            .Should().HaveCount(1);
    }

    [Fact]
    public void AudioSettingsFileStore_Save_ShouldReturnTrueOnSuccess()
    {
        // Arrange
        var path = Path.Combine(testDirectory, "settings.json");
        var store = new AudioSettingsFileStore(path);
        var settings = new AudioSettings { MasterVolume = 1.0f, MusicVolume = 1.0f, SfxVolume = 1.0f };

        // Act
        var result = store.Save(settings);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}