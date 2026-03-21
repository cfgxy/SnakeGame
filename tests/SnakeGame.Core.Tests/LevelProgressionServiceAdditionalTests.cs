namespace SnakeGame.Core.Tests;

/// <summary>
/// 补充验证关卡推进逻辑面对无序配置列表时仍能选择正确的下一关。
/// </summary>
public sealed class LevelProgressionServiceAdditionalTests
{
    private readonly LevelProgressionService service = new();

    [Fact]
    public void Evaluate_selects_the_smallest_higher_level_number_from_unsorted_levels()
    {
        // 真实配置文件不一定天然有序，推进规则应主动选择编号最小的下一关。
        var levels = new[]
        {
            TestLevelFactory.CreateLevelConfig(3, 10, 3),
            TestLevelFactory.CreateLevelConfig(1, 6, 3),
            TestLevelFactory.CreateLevelConfig(2, 8, 3)
        };

        var result = service.Evaluate(new LevelProgressionRequest(6, 1, levels));

        Assert.True(result.ShouldAdvance);
        Assert.False(result.IsGameCompleted);
        Assert.Equal(2, result.NextLevelNumber);
    }
}
