namespace SnakeGame.Core.Tests;

/// <summary>
/// 补充验证苹果生成器的异常输入和随机候选点选择行为。
/// </summary>
public sealed class AppleSpawnServiceAdditionalTests
{
    private readonly AppleSpawnService service = new();

    [Fact]
    public void SelectSpawnCell_returns_no_legal_cell_when_snake_is_empty()
    {
        // 没有蛇头就无法定义可达性与存活性校验，服务应直接返回无解。
        var request = new AppleSpawnRequest(
            new GridSize(4, 4),
            [],
            new HashSet<GridPosition>(),
            [],
            new HashSet<GridPosition>());

        var result = service.SelectSpawnCell(request);

        Assert.False(result.HasLegalCell);
        Assert.Null(result.Position);
        Assert.Equal(AppleSpawnFailureReason.NoLegalCell, result.FailureReason);
    }

    [Fact]
    public void SelectSpawnCell_random_overload_can_pick_a_non_first_safe_candidate()
    {
        // 随机重载用于运行时避免总是选择棋盘遍历中的第一个合法格子。
        var request = new AppleSpawnRequest(
            new GridSize(2, 2),
            [new GridPosition(1, 1), new GridPosition(1, 0)],
            new HashSet<GridPosition>(),
            [],
            new HashSet<GridPosition>());

        var result = service.SelectSpawnCell(request, new Random(0));

        Assert.True(result.HasLegalCell);
        Assert.Equal(new GridPosition(0, 1), result.Position);
        Assert.Equal(AppleSpawnFailureReason.None, result.FailureReason);
    }

    [Fact]
    public void SelectSpawnCell_random_overload_throws_when_random_is_null()
    {
        // 运行时随机源由调用方提供，传入空值应尽早失败而不是静默回退。
        var request = new AppleSpawnRequest(
            new GridSize(2, 2),
            [new GridPosition(1, 1), new GridPosition(1, 0)],
            new HashSet<GridPosition>(),
            [],
            new HashSet<GridPosition>());

        Assert.Throws<ArgumentNullException>(() => service.SelectSpawnCell(request, null!));
    }
}
