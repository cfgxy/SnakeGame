namespace SnakeGame.Core.Tests;

/// <summary>
/// 补充验证移动障碍在特殊轨道状态下的更新策略。
/// </summary>
public sealed class MovingObstacleResolverAdditionalTests
{
    private readonly MovingObstacleResolver resolver = new();

    [Fact]
    public void Resolve_reverses_direction_when_a_fixed_obstacle_blocks_the_next_track_cell()
    {
        // 即使轨道本身合法，下一格若被固定障碍占用，也要立即反向而不是穿过去。
        var obstacle = new MovingObstacleState(
            [new GridPosition(0, 0), new GridPosition(1, 0), new GridPosition(2, 0)],
            1,
            1,
            MovingObstacleStatus.Moving);

        var result = resolver.Resolve(new MovingObstacleResolutionRequest(
            new GridSize(6, 6),
            new GridPosition(4, 4),
            new GridPosition(4, 5),
            new HashSet<GridPosition>(),
            new HashSet<GridPosition> { new(2, 0) },
            [obstacle]));

        var updated = Assert.Single(result.Obstacles);
        Assert.Equal(CollisionType.None, result.CollisionType);
        Assert.Equal(0, updated.TrackIndex);
        Assert.Equal(-1, updated.DirectionSign);
        Assert.Equal(MovingObstacleStatus.Moving, updated.Status);
    }

    [Fact]
    public void Resolve_treats_zero_direction_sign_as_forward()
    {
        // 轨道状态被外部数据污染成 0 时，解析器应回退到正向前进而不是停住。
        var obstacle = new MovingObstacleState(
            [new GridPosition(1, 1), new GridPosition(2, 1)],
            0,
            0,
            MovingObstacleStatus.Moving);

        var result = resolver.Resolve(new MovingObstacleResolutionRequest(
            new GridSize(6, 6),
            new GridPosition(4, 4),
            new GridPosition(4, 5),
            new HashSet<GridPosition>(),
            new HashSet<GridPosition>(),
            [obstacle]));

        var updated = Assert.Single(result.Obstacles);
        Assert.Equal(CollisionType.None, result.CollisionType);
        Assert.Equal(1, updated.TrackIndex);
        Assert.Equal(1, updated.DirectionSign);
        Assert.Equal(MovingObstacleStatus.Moving, updated.Status);
    }
}
