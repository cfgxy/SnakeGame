namespace SnakeGame.Core;

/// <summary>
/// 负责按业务规则推进整局游戏状态的核心引擎。
/// </summary>
public sealed class GameEngine
{
    private readonly AppleSpawnService appleSpawnService;
    private readonly LevelLayoutValidator levelLayoutValidator;
    private readonly LevelProgressionService levelProgressionService;
    private readonly MovingObstacleResolver movingObstacleResolver;
    private readonly AccelerationResolver accelerationResolver;
    private readonly ScoreSubmissionPolicy scoreSubmissionPolicy;

    public GameEngine()
        : this(
            new AppleSpawnService(),
            new LevelLayoutValidator(),
            new LevelProgressionService(),
            new MovingObstacleResolver(),
            new AccelerationResolver(),
            new ScoreSubmissionPolicy())
    {
    }

    public GameEngine(
        AppleSpawnService appleSpawnService,
        LevelLayoutValidator levelLayoutValidator,
        LevelProgressionService levelProgressionService,
        MovingObstacleResolver movingObstacleResolver,
        AccelerationResolver accelerationResolver,
        ScoreSubmissionPolicy scoreSubmissionPolicy)
    {
        this.appleSpawnService = appleSpawnService;
        this.levelLayoutValidator = levelLayoutValidator;
        this.levelProgressionService = levelProgressionService;
        this.movingObstacleResolver = movingObstacleResolver;
        this.accelerationResolver = accelerationResolver;
        this.scoreSubmissionPolicy = scoreSubmissionPolicy;
    }

    public GameSession CreateSession(GameSessionSeed seed)
    {
        var level = GetLevel(seed.Levels, seed.StartLevelNumber);
        ValidateLevel(seed.BoardSize, level);

        var session = CreateSessionForLevel(seed.PlayerName, seed.Mode, seed.BoardSize, seed.Levels, level, 0, TimeSpan.Zero);
        var spawnResult = SpawnApple(session);
        var status = spawnResult.HasLegalCell ? GameSessionStatus.Running : GameSessionStatus.GameOver;

        return session with
        {
            ApplePosition = spawnResult.Position,
            Status = status
        };
    }

    public GameStepResult Step(GameSession session, GameStepInput input)
    {
        if (session.Status != GameSessionStatus.Running)
        {
            throw new InvalidOperationException("只有运行中的会话才能继续推进。");
        }

        var currentLevel = GetLevel(session.Levels, session.CurrentLevelNumber);
        var nextDirection = ResolveDirection(session.CurrentDirection, input.PressedDirection);
        // 加速判定独立于具体输入设备，只依赖标准化后的方向与时间戳。
        var acceleration = accelerationResolver.Resolve(
            session.Acceleration,
            new AccelerationRequest(
                nextDirection,
                input.PressedDirection,
                input.HeldDirection,
                input.Timestamp,
                TimeSpan.FromMilliseconds(200),
                currentLevel.Config.BoostMultiplier));

        var snakeHeadCurrent = session.SnakeSegments[0];
        var snakeHeadNext = Offset(snakeHeadCurrent, nextDirection);
        var elapsed = input.Timestamp;

        // 先处理墙体和固定障碍碰撞，这些碰撞不依赖移动障碍的本帧结算。
        if (IsOutsideBoard(snakeHeadNext, session.BoardSize) || currentLevel.FixedObstacles.Contains(snakeHeadNext))
        {
            return CreateTerminalResult(
                session with
                {
                    CurrentDirection = nextDirection,
                    Elapsed = elapsed,
                    Acceleration = acceleration.State,
                    Status = GameSessionStatus.GameOver
                },
                GameStepStatus.GameOver,
                false,
                false);
        }

        var ateApple = session.ApplePosition.HasValue && session.ApplePosition.Value == snakeHeadNext;
        if (HitsSnakeBody(session.SnakeSegments, snakeHeadNext, ateApple))
        {
            return CreateTerminalResult(
                session with
                {
                    CurrentDirection = nextDirection,
                    Elapsed = elapsed,
                    Acceleration = acceleration.State,
                    Status = GameSessionStatus.GameOver
                },
                GameStepStatus.GameOver,
                false,
                false);
        }

        var obstacleResolution = movingObstacleResolver.Resolve(new MovingObstacleResolutionRequest(
            session.BoardSize,
            snakeHeadCurrent,
            snakeHeadNext,
            new HashSet<GridPosition>(session.SnakeSegments.Skip(1)),
            currentLevel.FixedObstacles,
            session.MovingObstacles));

        // 移动障碍采用统一结算，避免蛇和障碍在同一帧里出现先后顺序歧义。
        if (obstacleResolution.CollisionType != CollisionType.None)
        {
            return CreateTerminalResult(
                session with
                {
                    CurrentDirection = nextDirection,
                    MovingObstacles = obstacleResolution.Obstacles,
                    Elapsed = elapsed,
                    Acceleration = acceleration.State,
                    Status = GameSessionStatus.GameOver
                },
                GameStepStatus.GameOver,
                false,
                false,
                obstacleResolution.CollisionType);
        }

        var updatedSnake = MoveSnake(session.SnakeSegments, snakeHeadNext, ateApple);
        var runningSession = session with
        {
            SnakeSegments = updatedSnake,
            CurrentDirection = nextDirection,
            MovingObstacles = obstacleResolution.Obstacles,
            Score = session.Score + (ateApple ? 1 : 0),
            Elapsed = elapsed,
            Acceleration = acceleration.State
        };

        if (ateApple)
        {
            // 吃到苹果后才有可能触发通关或重新生成苹果。
            var progression = levelProgressionService.Evaluate(new LevelProgressionRequest(
                updatedSnake.Count,
                session.CurrentLevelNumber,
                session.Levels.Select(levelDefinition => levelDefinition.Config).ToArray()));

            if (progression.IsGameCompleted)
            {
                var completedSession = runningSession with
                {
                    ApplePosition = null,
                    Status = GameSessionStatus.Completed
                };

                return new GameStepResult(
                    completedSession,
                GameStepStatus.Completed,
                true,
                false,
                acceleration.SpeedMultiplier,
                CollisionType.None,
                CreateScoreRecordIfNeeded(completedSession));
            }

            if (progression.ShouldAdvance && progression.NextLevelNumber.HasValue)
            {
                var nextLevel = GetLevel(session.Levels, progression.NextLevelNumber.Value);
                ValidateLevel(session.BoardSize, nextLevel);

                var advancedSession = CreateSessionForLevel(
                    session.PlayerName,
                    session.Mode,
                    session.BoardSize,
                    session.Levels,
                    nextLevel,
                    runningSession.Score,
                    elapsed);

                // 进入下一关时使用新关卡的初始蛇身和障碍布局重新开局。
                var nextApple = SpawnApple(advancedSession);
                var finalSession = advancedSession with
                {
                    ApplePosition = nextApple.Position,
                    Status = nextApple.HasLegalCell ? GameSessionStatus.Running : GameSessionStatus.GameOver
                };

                if (!nextApple.HasLegalCell)
                {
                    return new GameStepResult(
                        finalSession,
                        GameStepStatus.GameOver,
                        true,
                        true,
                        acceleration.SpeedMultiplier,
                        CollisionType.None,
                        CreateScoreRecordIfNeeded(finalSession));
                }

                return new GameStepResult(
                    finalSession,
                    GameStepStatus.LevelAdvanced,
                    true,
                    false,
                    acceleration.SpeedMultiplier,
                    CollisionType.None,
                    null);
            }

            var spawnResult = SpawnApple(runningSession);
            var spawnedSession = runningSession with { ApplePosition = spawnResult.Position };
            if (!spawnResult.HasLegalCell)
            {
                // 没有合法苹果点意味着当前局面已经无解，业务上直接结束本局。
                var deadlockSession = spawnedSession with
                {
                    ApplePosition = null,
                    Status = GameSessionStatus.GameOver
                };

                return new GameStepResult(
                    deadlockSession,
                    GameStepStatus.GameOver,
                    true,
                    true,
                    acceleration.SpeedMultiplier,
                    CollisionType.None,
                    CreateScoreRecordIfNeeded(deadlockSession));
            }

            return new GameStepResult(
                spawnedSession,
                GameStepStatus.Running,
                true,
                false,
                acceleration.SpeedMultiplier,
                CollisionType.None,
                null);
        }

        return new GameStepResult(
            runningSession,
            GameStepStatus.Running,
            false,
            false,
            acceleration.SpeedMultiplier,
            CollisionType.None,
            null);
    }

    private static GameSession CreateSessionForLevel(
        string playerName,
        GameMode mode,
        GridSize boardSize,
        IReadOnlyList<LevelDefinition> levels,
        LevelDefinition level,
        int score,
        TimeSpan elapsed)
    {
        return new GameSession(
            playerName,
            mode,
            boardSize,
            levels,
            level.Config.LevelNumber,
            level.InitialSnakeSegments.ToArray(),
            level.InitialDirection,
            new HashSet<GridPosition>(level.FixedObstacles),
            CreateMovingObstacles(level.Config),
            null,
            score,
            elapsed,
            new AccelerationState(level.InitialDirection, false, null, null),
            GameSessionStatus.Running);
    }

    private static IReadOnlyList<MovingObstacleState> CreateMovingObstacles(LevelConfig config)
    {
        return config.MovingObstacleTracks
            .Select(track => new MovingObstacleState(track.ToArray(), 0, 1, MovingObstacleStatus.Moving))
            .ToArray();
    }

    private AppleSpawnResult SpawnApple(GameSession session)
    {
        return appleSpawnService.SelectSpawnCell(
            new AppleSpawnRequest(
            session.BoardSize,
            session.SnakeSegments,
            session.FixedObstacles,
            session.MovingObstacles,
            CollectReservedTrackCells(GetLevel(session.Levels, session.CurrentLevelNumber).Config)),
            Random.Shared);
    }

    private void ValidateLevel(GridSize boardSize, LevelDefinition level)
    {
        var validation = levelLayoutValidator.Validate(new LevelLayoutValidationRequest(
            boardSize,
            level.Config,
            level.FixedObstacles,
            CollectReservedTrackCells(level.Config)));

        if (!validation.IsPlayable)
        {
            throw new InvalidOperationException($"关卡 {level.Config.LevelNumber} 配置非法：{validation.FailureReason}");
        }
    }

    private static IReadOnlySet<GridPosition> CollectReservedTrackCells(LevelConfig config)
    {
        var result = new HashSet<GridPosition>();
        foreach (var track in config.MovingObstacleTracks)
        {
            foreach (var position in track)
            {
                result.Add(position);
            }
        }

        return result;
    }

    private static IReadOnlyList<GridPosition> MoveSnake(
        IReadOnlyList<GridPosition> snakeSegments,
        GridPosition nextHead,
        bool ateApple)
    {
        var updated = snakeSegments.ToList();
        updated.Insert(0, nextHead);

        if (!ateApple)
        {
            updated.RemoveAt(updated.Count - 1);
        }

        return updated;
    }

    private static bool HitsSnakeBody(
        IReadOnlyList<GridPosition> snakeSegments,
        GridPosition nextHead,
        bool ateApple)
    {
        var body = snakeSegments.Skip(1).ToList();
        if (!ateApple && body.Count > 0)
        {
            // 不吃苹果时尾巴会前移，因此允许蛇头踏入“当前尾巴”所在格。
            body.RemoveAt(body.Count - 1);
        }

        return body.Contains(nextHead);
    }

    private static Direction ResolveDirection(Direction currentDirection, Direction? pressedDirection)
    {
        if (!pressedDirection.HasValue || IsOpposite(currentDirection, pressedDirection.Value))
        {
            return currentDirection;
        }

        return pressedDirection.Value;
    }

    private static bool IsOpposite(Direction currentDirection, Direction pressedDirection)
    {
        return (currentDirection, pressedDirection) switch
        {
            (Direction.Up, Direction.Down) => true,
            (Direction.Down, Direction.Up) => true,
            (Direction.Left, Direction.Right) => true,
            (Direction.Right, Direction.Left) => true,
            _ => false
        };
    }

    private static GridPosition Offset(GridPosition position, Direction direction)
    {
        return direction switch
        {
            Direction.Up => new GridPosition(position.X, position.Y - 1),
            Direction.Down => new GridPosition(position.X, position.Y + 1),
            Direction.Left => new GridPosition(position.X - 1, position.Y),
            Direction.Right => new GridPosition(position.X + 1, position.Y),
            _ => position
        };
    }

    private static bool IsOutsideBoard(GridPosition position, GridSize boardSize)
    {
        return position.X < 0 ||
               position.Y < 0 ||
               position.X >= boardSize.Width ||
               position.Y >= boardSize.Height;
    }

    private GameStepResult CreateTerminalResult(
        GameSession session,
        GameStepStatus status,
        bool ateApple,
        bool isDeadlock,
        CollisionType collisionType = CollisionType.None)
    {
        // 写榜与否只由模式决定，死亡原因不会改变这条业务规则。
        return new GameStepResult(
            session,
            status,
            ateApple,
            isDeadlock,
            session.Acceleration.IsBoosting ? GetLevel(session.Levels, session.CurrentLevelNumber).Config.BoostMultiplier : 1d,
            collisionType,
            CreateScoreRecordIfNeeded(session));
    }

    private ScoreRecord? CreateScoreRecordIfNeeded(GameSession session)
    {
        if (!scoreSubmissionPolicy.ShouldRecord(session.Mode))
        {
            return null;
        }

        return new ScoreRecord(
            session.PlayerName,
            session.Score,
            session.CurrentLevelNumber,
            session.Mode,
            session.Elapsed);
    }

    private static LevelDefinition GetLevel(IReadOnlyList<LevelDefinition> levels, int levelNumber)
    {
        return levels.Single(level => level.Config.LevelNumber == levelNumber);
    }
}
