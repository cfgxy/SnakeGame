namespace SnakeGame.Core.Tests;

/// <summary>
/// 补充覆盖 GameEngine 尚未命中的分支，包括异常路径、边界碰撞和通关终局。
/// </summary>
public sealed class GameEngineAdditionalTests
{
    private readonly GameEngine engine = new();

    [Fact]
    public void CreateSession_returns_game_over_when_initial_layout_has_no_legal_apple()
    {
        // 开局如果已经不存在任何安全苹果点，业务层应直接把会话标记为失败。
        var level = TestLevelFactory.CreateLevelDefinition(
            1,
            5,
            [new GridPosition(1, 1), new GridPosition(1, 2), new GridPosition(0, 2), new GridPosition(0, 1)],
            Direction.Up,
            new HashSet<GridPosition>
            {
                new(0, 0),
                new(2, 0),
                new(2, 1),
                new(2, 2)
            },
            safeCapacityMargin: 0);

        var session = engine.CreateSession(new GameSessionSeed(
            "测试玩家",
            GameMode.Story,
            new GridSize(3, 3),
            [level],
            1));

        Assert.Equal(GameSessionStatus.GameOver, session.Status);
        Assert.Null(session.ApplePosition);
    }

    [Fact]
    public void CreateSession_throws_when_level_layout_is_invalid()
    {
        // 无法满足目标长度安全容量的关卡不应进入运行阶段。
        var invalidLevel = TestLevelFactory.CreateLevelDefinition(
            1,
            6,
            [new GridPosition(1, 1), new GridPosition(0, 1)],
            Direction.Right,
            new HashSet<GridPosition>
            {
                new(0, 0),
                new(0, 2),
                new(2, 0),
                new(2, 2)
            },
            safeCapacityMargin: 2);

        Assert.Throws<InvalidOperationException>(() => engine.CreateSession(new GameSessionSeed(
            "测试玩家",
            GameMode.Story,
            new GridSize(3, 3),
            [invalidLevel],
            1)));
    }

    [Fact]
    public void Step_throws_when_session_is_not_running()
    {
        // 终局会话再继续推进属于调用方错误，应立即抛出异常。
        var session = engine.CreateSession(new GameSessionSeed(
            "测试玩家",
            GameMode.Story,
            new GridSize(4, 4),
            [TestLevelFactory.CreateLevelDefinition(1, 5, [new GridPosition(1, 1), new GridPosition(0, 1)], Direction.Right)],
            1)) with
        {
            Status = GameSessionStatus.GameOver
        };

        Assert.Throws<InvalidOperationException>(() => engine.Step(
            session,
            new GameStepInput(null, Direction.Right, TimeSpan.FromMilliseconds(100))));
    }

    [Fact]
    public void Step_returns_game_over_when_snake_moves_outside_the_board()
    {
        // 撞墙仍然属于即时死亡分支，不能被后续逻辑覆盖。
        var session = engine.CreateSession(new GameSessionSeed(
            "测试玩家",
            GameMode.Story,
            new GridSize(4, 4),
            [TestLevelFactory.CreateLevelDefinition(1, 5, [new GridPosition(0, 1), new GridPosition(1, 1)], Direction.Left)],
            1)) with
        {
            ApplePosition = new GridPosition(3, 3)
        };

        var result = engine.Step(session, new GameStepInput(null, Direction.Left, TimeSpan.FromMilliseconds(100)));

        Assert.Equal(GameStepStatus.GameOver, result.Status);
        Assert.Equal(GameSessionStatus.GameOver, result.Session.Status);
        Assert.Equal(Direction.Left, result.Session.CurrentDirection);
    }

    [Fact]
    public void Step_returns_game_over_when_snake_hits_its_body()
    {
        // 非吃苹果移动时，除去会前移的尾巴之外，撞到其他身体格都必须判死。
        var session = engine.CreateSession(new GameSessionSeed(
            "测试玩家",
            GameMode.Story,
            new GridSize(4, 4),
            [TestLevelFactory.CreateLevelDefinition(
                1,
                6,
                [new GridPosition(1, 1), new GridPosition(2, 1), new GridPosition(2, 2), new GridPosition(1, 2)],
                Direction.Right)],
            1)) with
        {
            ApplePosition = new GridPosition(0, 0)
        };

        var result = engine.Step(session, new GameStepInput(null, Direction.Right, TimeSpan.FromMilliseconds(100)));

        Assert.Equal(GameStepStatus.GameOver, result.Status);
        Assert.Equal(GameSessionStatus.GameOver, result.Session.Status);
    }

    [Fact]
    public void Step_allows_head_to_move_into_the_previous_tail_cell_when_not_eating()
    {
        // 不吃苹果时尾巴会同步前移，因此踏入“当前尾巴格”应被允许。
        var session = engine.CreateSession(new GameSessionSeed(
            "测试玩家",
            GameMode.Story,
            new GridSize(4, 4),
            [TestLevelFactory.CreateLevelDefinition(
                1,
                6,
                [new GridPosition(1, 1), new GridPosition(1, 2), new GridPosition(0, 2), new GridPosition(0, 1)],
                Direction.Left)],
            1)) with
        {
            ApplePosition = new GridPosition(3, 3)
        };

        var result = engine.Step(session, new GameStepInput(null, Direction.Left, TimeSpan.FromMilliseconds(100)));

        Assert.Equal(GameStepStatus.Running, result.Status);
        Assert.Equal(GameSessionStatus.Running, result.Session.Status);
        Assert.Equal(new GridPosition(0, 1), result.Session.SnakeSegments[0]);
        Assert.Equal(4, result.Session.SnakeSegments.Count);
    }

    [Fact]
    public void Step_ignores_opposite_direction_input()
    {
        // 反向输入必须被忽略，否则会允许蛇瞬间掉头撞到自己。
        var session = engine.CreateSession(new GameSessionSeed(
            "测试玩家",
            GameMode.Story,
            new GridSize(5, 5),
            [TestLevelFactory.CreateLevelDefinition(1, 6, [new GridPosition(1, 1), new GridPosition(0, 1)], Direction.Right)],
            1)) with
        {
            ApplePosition = new GridPosition(4, 4)
        };

        var result = engine.Step(session, new GameStepInput(Direction.Left, Direction.Left, TimeSpan.FromMilliseconds(100)));

        Assert.Equal(GameStepStatus.Running, result.Status);
        Assert.Equal(Direction.Right, result.Session.CurrentDirection);
        Assert.Equal(new GridPosition(2, 1), result.Session.SnakeSegments[0]);
    }

    [Fact]
    public void Step_returns_completed_when_last_level_target_is_reached()
    {
        // 最后一关达标后应进入 Completed，而不是继续等待下一关。
        var session = engine.CreateSession(new GameSessionSeed(
            "测试玩家",
            GameMode.Story,
            new GridSize(5, 5),
            [TestLevelFactory.CreateLevelDefinition(1, 3, [new GridPosition(1, 1), new GridPosition(0, 1)], Direction.Right)],
            1)) with
        {
            ApplePosition = new GridPosition(2, 1)
        };

        var result = engine.Step(session, new GameStepInput(null, Direction.Right, TimeSpan.FromMilliseconds(100)));

        Assert.Equal(GameStepStatus.Completed, result.Status);
        Assert.Equal(GameSessionStatus.Completed, result.Session.Status);
        Assert.Null(result.Session.ApplePosition);
        Assert.NotNull(result.ScoreRecordToPersist);
        Assert.Equal(1, result.Session.Score);
    }

    [Fact]
    public void Step_returns_game_over_when_next_level_cannot_spawn_apple()
    {
        // 如果升级后的下一关开局就无解，当前这一步应直接转化为死局结算。
        var impossibleNextLevel = TestLevelFactory.CreateLevelDefinition(
            2,
            5,
            [new GridPosition(1, 1), new GridPosition(1, 2), new GridPosition(0, 2), new GridPosition(0, 1)],
            Direction.Up,
            new HashSet<GridPosition>
            {
                new(0, 0),
                new(2, 0),
                new(2, 1),
                new(2, 2)
            },
            safeCapacityMargin: 0);

        var levels = new[]
        {
            TestLevelFactory.CreateLevelDefinition(1, 3, [new GridPosition(0, 0), new GridPosition(0, 1)], Direction.Right, safeCapacityMargin: 0),
            impossibleNextLevel
        };

        var session = engine.CreateSession(new GameSessionSeed(
            "测试玩家",
            GameMode.Story,
            new GridSize(3, 3),
            levels,
            1)) with
        {
            ApplePosition = new GridPosition(1, 0)
        };

        var result = engine.Step(session, new GameStepInput(null, Direction.Right, TimeSpan.FromMilliseconds(100)));

        Assert.Equal(GameStepStatus.GameOver, result.Status);
        Assert.True(result.AteApple);
        Assert.True(result.IsDeadlock);
        Assert.Equal(GameSessionStatus.GameOver, result.Session.Status);
        Assert.Equal(2, result.Session.CurrentLevelNumber);
        Assert.Null(result.Session.ApplePosition);
        Assert.Equal(levels[1].InitialSnakeSegments, result.Session.SnakeSegments);
    }
}
