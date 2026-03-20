namespace SnakeGame.Core.Tests;

/// <summary>
/// 验证通关判定只依赖当前长度和关卡配置，不受界面层影响。
/// </summary>
public sealed class LevelProgressionServiceTests
{
    private readonly LevelProgressionService service = new();

    [Fact]
    public void Evaluate_does_not_advance_before_target_length_is_reached()
    {
        // 长度未达标时应保持在当前关卡。
        var result = service.Evaluate(new LevelProgressionRequest(5, 1, CreateLevels()));

        Assert.False(result.ShouldAdvance);
        Assert.False(result.IsGameCompleted);
        Assert.Null(result.NextLevelNumber);
    }

    [Fact]
    public void Evaluate_advances_to_the_next_level_when_target_is_reached()
    {
        // 达到目标长度后应明确给出下一关编号。
        var result = service.Evaluate(new LevelProgressionRequest(6, 1, CreateLevels()));

        Assert.True(result.ShouldAdvance);
        Assert.False(result.IsGameCompleted);
        Assert.Equal(2, result.NextLevelNumber);
    }

    [Fact]
    public void Evaluate_marks_game_complete_when_last_level_target_is_reached()
    {
        // 最后一关达标时不再返回下一关，而是标记整轮闯关完成。
        var result = service.Evaluate(new LevelProgressionRequest(9, 3, CreateLevels()));

        Assert.True(result.ShouldAdvance);
        Assert.True(result.IsGameCompleted);
        Assert.Null(result.NextLevelNumber);
    }

    private static IReadOnlyList<LevelConfig> CreateLevels()
    {
        // 这里故意让后续关卡速度更快，贴近实际配置趋势。
        return
        [
            new LevelConfig(1, 6, 3, TimeSpan.FromMilliseconds(360), 3, false, 2, 2, []),
            new LevelConfig(2, 8, 3, TimeSpan.FromMilliseconds(300), 5, false, 2, 2, []),
            new LevelConfig(3, 9, 3, TimeSpan.FromMilliseconds(240), 7, false, 2, 2, [])
        ];
    }
}
