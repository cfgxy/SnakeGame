namespace SnakeGame.Core.Tests;

/// <summary>
/// 验证移动障碍在主动碰撞、被蛇身阻挡和轨道反向时的结算规则。
/// </summary>
public sealed class MovingObstacleResolverTests
{
    private readonly MovingObstacleResolver resolver = new();

    [Fact]
    public void Resolve_reports_head_collision_when_obstacle_moves_into_snake_target_cell()
    {
        // 障碍下一步正好进入蛇头目标格，应判定为主动碰撞致死。
        var obstacle = new MovingObstacleState(
            [new GridPosition(3, 1), new GridPosition(2, 1)],
            0,
            1,
            MovingObstacleStatus.Moving);

        var result = resolver.Resolve(new MovingObstacleResolutionRequest(
            new GridSize(6, 6),
            new GridPosition(1, 1),
            new GridPosition(2, 1),
            new HashSet<GridPosition> { new(1, 2) },
            new HashSet<GridPosition>(),
            [obstacle]));

        // 这里只关心碰撞类型，障碍自身位置更新由其他用例覆盖。
        Assert.Equal(CollisionType.SnakeHeadHitObstacle, result.CollisionType);
    }

    [Fact]
    public void Resolve_reports_swap_collision_when_head_and_obstacle_exchange_cells()
    {
        // 蛇头和障碍交换位置也是致命碰撞，不能被当作普通穿过。
        var obstacle = new MovingObstacleState(
            [new GridPosition(2, 1), new GridPosition(1, 1)],
            0,
            1,
            MovingObstacleStatus.Moving);

        var result = resolver.Resolve(new MovingObstacleResolutionRequest(
            new GridSize(6, 6),
            new GridPosition(1, 1),
            new GridPosition(2, 1),
            new HashSet<GridPosition> { new(1, 2) },
            new HashSet<GridPosition>(),
            [obstacle]));

        Assert.Equal(CollisionType.SwapWithSnakeHead, result.CollisionType);
    }

    [Fact]
    public void Resolve_pauses_obstacle_when_snake_body_blocks_the_next_track_cell()
    {
        // 被蛇身挡住时，障碍应停在原地并进入暂停状态。
        var obstacle = new MovingObstacleState(
            [new GridPosition(1, 1), new GridPosition(2, 1)],
            0,
            1,
            MovingObstacleStatus.Moving);

        var result = resolver.Resolve(new MovingObstacleResolutionRequest(
            new GridSize(6, 6),
            new GridPosition(0, 0),
            new GridPosition(0, 1),
            new HashSet<GridPosition> { new(2, 1) },
            new HashSet<GridPosition>(),
            [obstacle]));

        var updated = Assert.Single(result.Obstacles);
        // 断言障碍没有前进，但保留原方向，便于之后恢复。
        Assert.Equal(CollisionType.None, result.CollisionType);
        Assert.Equal(0, updated.TrackIndex);
        Assert.Equal(1, updated.DirectionSign);
        Assert.Equal(MovingObstacleStatus.PausedByBody, updated.Status);
    }

    [Fact]
    public void Resolve_resumes_paused_obstacle_when_body_no_longer_blocks_the_track()
    {
        // 蛇身移开后，之前暂停的障碍应继续沿原方向移动。
        var obstacle = new MovingObstacleState(
            [new GridPosition(1, 1), new GridPosition(2, 1)],
            0,
            1,
            MovingObstacleStatus.PausedByBody);

        var result = resolver.Resolve(new MovingObstacleResolutionRequest(
            new GridSize(6, 6),
            new GridPosition(0, 0),
            new GridPosition(0, 1),
            new HashSet<GridPosition>(),
            new HashSet<GridPosition>(),
            [obstacle]));

        var updated = Assert.Single(result.Obstacles);
        Assert.Equal(CollisionType.None, result.CollisionType);
        Assert.Equal(1, updated.TrackIndex);
        Assert.Equal(1, updated.DirectionSign);
        Assert.Equal(MovingObstacleStatus.Moving, updated.Status);
    }

    [Fact]
    public void Resolve_reverses_direction_at_track_end()
    {
        // 走到轨道端点时应立即反向，而不是越界或停住。
        var obstacle = new MovingObstacleState(
            [new GridPosition(0, 0), new GridPosition(1, 0), new GridPosition(2, 0)],
            2,
            1,
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
        Assert.Equal(-1, updated.DirectionSign);
        Assert.Equal(MovingObstacleStatus.Moving, updated.Status);
    }
}
