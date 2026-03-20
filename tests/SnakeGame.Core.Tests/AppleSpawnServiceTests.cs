namespace SnakeGame.Core.Tests;

/// <summary>
/// 验证苹果生成器会过滤危险落点，只返回真正可吃且安全的格子。
/// </summary>
public sealed class AppleSpawnServiceTests
{
    private readonly AppleSpawnService service = new();

    [Fact]
    public void SelectSpawnCell_returns_a_safe_reachable_cell()
    {
        // 构造一个存在安全苹果点的局面，要求服务返回该唯一合法解。
        var request = new AppleSpawnRequest(
            new GridSize(4, 4),
            [new GridPosition(1, 1), new GridPosition(1, 2)],
            new HashSet<GridPosition>
            {
                new(0, 0),
                new(0, 1),
                new(0, 2),
            },
            [new MovingObstacleState([new GridPosition(3, 0), new GridPosition(3, 1)], 0, 1, MovingObstacleStatus.Moving)],
            new HashSet<GridPosition>());

        var result = service.SelectSpawnCell(request);

        // 断言苹果成功生成，并且位置符合当前布局下的安全路径要求。
        Assert.True(result.HasLegalCell);
        Assert.Equal(new GridPosition(1, 0), result.Position);
        Assert.Equal(AppleSpawnFailureReason.None, result.FailureReason);
    }

    [Fact]
    public void SelectSpawnCell_skips_track_cells_and_moving_obstacle_current_cells()
    {
        // 轨道保留格和移动障碍当前格即使为空，也不能作为苹果落点。
        var request = new AppleSpawnRequest(
            new GridSize(5, 4),
            [new GridPosition(2, 1), new GridPosition(2, 2)],
            new HashSet<GridPosition>(),
            [new MovingObstacleState([new GridPosition(0, 0), new GridPosition(0, 1)], 0, 1, MovingObstacleStatus.Moving)],
            new HashSet<GridPosition> { new(1, 0) });

        var result = service.SelectSpawnCell(request);

        // 断言生成结果明确避开轨道格和移动障碍当前位置。
        Assert.True(result.HasLegalCell);
        Assert.NotEqual(new GridPosition(0, 0), result.Position);
        Assert.NotEqual(new GridPosition(1, 0), result.Position);
        Assert.Equal(new GridPosition(2, 0), result.Position);
    }

    [Fact]
    public void SelectSpawnCell_returns_no_legal_cell_when_every_candidate_breaks_tail_connectivity()
    {
        // 这个局面中虽然存在空格，但所有候选点都会让蛇吃完后无法再接回尾巴。
        var request = new AppleSpawnRequest(
            new GridSize(3, 3),
            [new GridPosition(1, 1), new GridPosition(1, 2), new GridPosition(0, 2), new GridPosition(0, 1)],
            new HashSet<GridPosition>
            {
                new(0, 0),
                new(2, 0),
                new(2, 1),
                new(2, 2)
            },
            [],
            new HashSet<GridPosition>());

        var result = service.SelectSpawnCell(request);

        // 断言服务会返回死局，而不是把苹果放到表面可达但实际危险的位置。
        Assert.False(result.HasLegalCell);
        Assert.Null(result.Position);
        Assert.Equal(AppleSpawnFailureReason.NoLegalCell, result.FailureReason);
    }
}
