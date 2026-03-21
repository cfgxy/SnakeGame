namespace SnakeGame.Core.Tests;

/// <summary>
/// 以业务流程为中心验证 GameEngine 的完整状态推进，而不是只测单个规则服务。
/// </summary>
public sealed class GameEngineTests
{
    private readonly GameEngine engine = new();

    [Fact]
    public void CreateSession_creates_a_running_session_with_a_legal_apple()
    {
        // 开局时应自动生成一个合法苹果，且不能落在蛇身、固定障碍或移动障碍轨道上。
        var seed = new GameSessionSeed(
            "测试玩家",
            GameMode.Story,
            new GridSize(6, 6),
            [CreateLevelDefinition(1, 6, [new GridPosition(1, 1), new GridPosition(0, 1)], Direction.Right)],
            1);

        var session = engine.CreateSession(seed);

        Assert.Equal(GameSessionStatus.Running, session.Status);
        Assert.NotNull(session.ApplePosition);
        Assert.DoesNotContain(session.ApplePosition!.Value, session.SnakeSegments);
        Assert.DoesNotContain(session.ApplePosition!.Value, session.FixedObstacles);
    }

    [Fact]
    public void Step_grows_the_snake_and_respawns_apple_after_eating()
    {
        // 吃到苹果后，蛇身长度和分数都要增长，同时重新生成下一个苹果。
        var session = engine.CreateSession(new GameSessionSeed(
            "测试玩家",
            GameMode.Story,
            new GridSize(6, 6),
            [CreateLevelDefinition(1, 6, [new GridPosition(1, 1), new GridPosition(0, 1)], Direction.Right)],
            1)) with
        {
            ApplePosition = new GridPosition(2, 1)
        };

        var result = engine.Step(session, new GameStepInput(null, Direction.Right, TimeSpan.FromMilliseconds(100)));

        Assert.Equal(GameStepStatus.Running, result.Status);
        Assert.True(result.AteApple);
        Assert.Equal(3, result.Session.SnakeSegments.Count);
        Assert.Equal(new GridPosition(2, 1), result.Session.SnakeSegments[0]);
        Assert.Equal(1, result.Session.Score);
        Assert.NotNull(result.Session.ApplePosition);
        Assert.NotEqual(new GridPosition(2, 1), result.Session.ApplePosition!.Value);
    }

    [Fact]
    public void Step_advances_to_the_next_level_when_target_length_is_reached()
    {
        // 达到关卡目标长度后，应重置到下一关的初始布局，而不是继续沿用当前局面。
        var levels = new[]
        {
            CreateLevelDefinition(1, 3, [new GridPosition(1, 1), new GridPosition(0, 1)], Direction.Right),
            CreateLevelDefinition(
                2,
                6,
                [new GridPosition(2, 2), new GridPosition(1, 2)],
                Direction.Down,
                new HashSet<GridPosition> { new(0, 0) })
        };

        var session = engine.CreateSession(new GameSessionSeed(
            "测试玩家",
            GameMode.Story,
            new GridSize(6, 6),
            levels,
            1)) with
        {
            ApplePosition = new GridPosition(2, 1)
        };

        var result = engine.Step(session, new GameStepInput(null, Direction.Right, TimeSpan.FromMilliseconds(100)));

        Assert.Equal(GameStepStatus.LevelAdvanced, result.Status);
        Assert.Equal(GameSessionStatus.Running, result.Session.Status);
        Assert.Equal(2, result.Session.CurrentLevelNumber);
        Assert.Equal(levels[1].InitialSnakeSegments, result.Session.SnakeSegments);
        Assert.Equal(levels[1].InitialDirection, result.Session.CurrentDirection);
        Assert.Equal(1, result.Session.Score);
        Assert.NotNull(result.Session.ApplePosition);
    }

    [Fact]
    public void Step_returns_deadlock_game_over_when_no_legal_apple_exists_after_eating()
    {
        // 这个局面在吃到苹果后会形成“无安全苹果点”的死局，业务层必须直接结束本局。
        var level = CreateLevelDefinition(
            1,
            5,
            [new GridPosition(1, 2), new GridPosition(0, 2), new GridPosition(0, 1)],
            Direction.Up,
            new HashSet<GridPosition> { new(0, 0), new(2, 0), new(2, 1), new(2, 2) },
            safeCapacityMargin: 0);

        var session = engine.CreateSession(new GameSessionSeed(
            "测试玩家",
            GameMode.Story,
            new GridSize(3, 3),
            [level],
            1)) with
        {
            ApplePosition = new GridPosition(1, 1)
        };

        var result = engine.Step(session, new GameStepInput(null, Direction.Up, TimeSpan.FromMilliseconds(100)));

        Assert.Equal(GameStepStatus.GameOver, result.Status);
        Assert.True(result.AteApple);
        Assert.True(result.IsDeadlock);
        Assert.Equal(GameSessionStatus.GameOver, result.Session.Status);
        Assert.Null(result.Session.ApplePosition);
    }

    [Fact]
    public void Step_returns_game_over_when_moving_obstacle_hits_snake_head_target_cell()
    {
        // 主动移动的障碍打到蛇头目标格时，应由业务引擎判定为死亡。
        var level = CreateLevelDefinition(
            1,
            8,
            [new GridPosition(1, 1), new GridPosition(0, 1)],
            Direction.Right,
            movingObstacleTracks:
            [
                [new GridPosition(3, 1), new GridPosition(2, 1)]
            ]);

        var session = engine.CreateSession(new GameSessionSeed(
            "测试玩家",
            GameMode.Story,
            new GridSize(6, 6),
            [level],
            1)) with
        {
            ApplePosition = new GridPosition(5, 5)
        };

        var result = engine.Step(session, new GameStepInput(null, Direction.Right, TimeSpan.FromMilliseconds(100)));

        Assert.Equal(GameStepStatus.GameOver, result.Status);
        Assert.Equal(GameSessionStatus.GameOver, result.Session.Status);
        Assert.Equal(CollisionType.SnakeHeadHitObstacle, result.CollisionType);
    }

    [Fact]
    public void Step_applies_double_tap_boost_and_clears_it_after_release()
    {
        // 双击并按住当前方向才会加速，松手后应立即恢复正常倍率。
        var session = engine.CreateSession(new GameSessionSeed(
            "测试玩家",
            GameMode.Story,
            new GridSize(8, 4),
            [CreateLevelDefinition(1, 8, [new GridPosition(1, 1), new GridPosition(0, 1)], Direction.Right)],
            1)) with
        {
            ApplePosition = new GridPosition(7, 3)
        };

        var firstStep = engine.Step(session, new GameStepInput(Direction.Right, Direction.Right, TimeSpan.FromMilliseconds(100)));
        var secondStep = engine.Step(firstStep.Session with { ApplePosition = new GridPosition(7, 3) }, new GameStepInput(Direction.Right, Direction.Right, TimeSpan.FromMilliseconds(250)));
        var thirdStep = engine.Step(secondStep.Session with { ApplePosition = new GridPosition(7, 3) }, new GameStepInput(null, null, TimeSpan.FromMilliseconds(260)));

        Assert.Equal(1d, firstStep.SpeedMultiplier);
        Assert.False(firstStep.Session.Acceleration.IsBoosting);
        Assert.Equal(2d, secondStep.SpeedMultiplier);
        Assert.True(secondStep.Session.Acceleration.IsBoosting);
        Assert.Equal(1d, thirdStep.SpeedMultiplier);
        Assert.False(thirdStep.Session.Acceleration.IsBoosting);
    }

    [Fact]
    public void Step_generates_score_record_on_story_mode_game_over()
    {
        // 闯关模式结束时，业务层应产出一条待持久化的成绩记录。
        var session = engine.CreateSession(new GameSessionSeed(
            "测试玩家",
            GameMode.Story,
            new GridSize(4, 4),
            [CreateLevelDefinition(1, 6, [new GridPosition(3, 1), new GridPosition(2, 1)], Direction.Right)],
            1)) with
        {
            ApplePosition = new GridPosition(0, 0),
            Score = 3
        };

        var result = engine.Step(session, new GameStepInput(null, Direction.Right, TimeSpan.FromMilliseconds(100)));

        Assert.Equal(GameStepStatus.GameOver, result.Status);
        Assert.NotNull(result.ScoreRecordToPersist);
        Assert.Equal(3, result.ScoreRecordToPersist!.Score);
        Assert.Equal(GameMode.Story, result.ScoreRecordToPersist.Mode);
    }

    [Fact]
    public void Step_does_not_generate_score_record_on_practice_mode_game_over()
    {
        // 练习模式即使死亡，也不应该产出总榜记录。
        var session = engine.CreateSession(new GameSessionSeed(
            "测试玩家",
            GameMode.Practice,
            new GridSize(4, 4),
            [CreateLevelDefinition(1, 6, [new GridPosition(3, 1), new GridPosition(2, 1)], Direction.Right)],
            1)) with
        {
            ApplePosition = new GridPosition(0, 0),
            Score = 3
        };

        var result = engine.Step(session, new GameStepInput(null, Direction.Right, TimeSpan.FromMilliseconds(100)));

        Assert.Equal(GameStepStatus.GameOver, result.Status);
        Assert.Null(result.ScoreRecordToPersist);
    }

    private static LevelDefinition CreateLevelDefinition(
        int levelNumber,
        int targetLength,
        IReadOnlyList<GridPosition> initialSnakeSegments,
        Direction initialDirection,
        IReadOnlySet<GridPosition>? fixedObstacles = null,
        IReadOnlyList<IReadOnlyList<GridPosition>>? movingObstacleTracks = null,
        int safeCapacityMargin = 1)
    {
        // 测试工厂集中生成关卡，避免每个用例都重复拼装样板数据。
        return new LevelDefinition(
            new LevelConfig(
                levelNumber,
                targetLength,
                initialSnakeSegments.Count,
                TimeSpan.FromMilliseconds(300 - ((levelNumber - 1) * 40)),
                fixedObstacles?.Count ?? 0,
                movingObstacleTracks is { Count: > 0 },
                2,
                safeCapacityMargin,
                movingObstacleTracks ?? []),
            initialSnakeSegments,
            initialDirection,
            fixedObstacles ?? new HashSet<GridPosition>());
    }
}
