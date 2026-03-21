namespace SnakeGame.Core;

/// <summary>
/// 表示棋盘上的一个网格坐标。
/// </summary>
public readonly record struct GridPosition(int X, int Y);

/// <summary>
/// 表示可玩区域的宽高。
/// </summary>
public readonly record struct GridSize(int Width, int Height);

public enum Direction
{
    Up,
    Down,
    Left,
    Right
}

public enum GameMode
{
    Story,
    Practice
}

public enum MovingObstacleStatus
{
    Moving,
    PausedByBody
}

public enum AppleSpawnFailureReason
{
    None,
    NoLegalCell
}

public enum LayoutFailureReason
{
    None,
    InvalidBoardSize,
    InitialSnakeLengthInvalid,
    TargetLengthExceedsSafeCapacity
}

public enum CollisionType
{
    None,
    SnakeHeadHitObstacle,
    SwapWithSnakeHead
}

public enum GameSessionStatus
{
    Running,
    GameOver,
    Completed
}

public enum GameStepStatus
{
    Running,
    LevelAdvanced,
    GameOver,
    Completed
}

/// <summary>
/// 描述单个关卡的核心规则配置。
/// </summary>
public sealed record LevelConfig(
    int LevelNumber,
    int TargetLength,
    int InitialSnakeLength,
    TimeSpan BaseStepInterval,
    int FixedObstacleCount,
    bool EnablesMovingObstacles,
    double BoostMultiplier,
    int SafeCapacityMargin,
    IReadOnlyList<IReadOnlyList<GridPosition>> MovingObstacleTracks);

/// <summary>
/// 表示一个可直接用于运行时的完整关卡定义，包含初始蛇身和固定障碍布局。
/// </summary>
public sealed record LevelDefinition(
    LevelConfig Config,
    IReadOnlyList<GridPosition> InitialSnakeSegments,
    Direction InitialDirection,
    IReadOnlySet<GridPosition> FixedObstacles);

/// <summary>
/// 记录一个移动障碍当前所在的轨道位置和运动状态。
/// </summary>
public sealed record MovingObstacleState(
    IReadOnlyList<GridPosition> Track,
    int TrackIndex,
    int DirectionSign,
    MovingObstacleStatus Status);

/// <summary>
/// 苹果生成校验所需的完整局面快照。
/// </summary>
public sealed record AppleSpawnRequest(
    GridSize BoardSize,
    IReadOnlyList<GridPosition> SnakeSegments,
    IReadOnlySet<GridPosition> FixedObstacles,
    IReadOnlyList<MovingObstacleState> MovingObstacles,
    IReadOnlySet<GridPosition> ReservedTrackCells);

/// <summary>
/// 苹果生成搜索的结果。
/// </summary>
public sealed record AppleSpawnResult(
    bool HasLegalCell,
    GridPosition? Position,
    AppleSpawnFailureReason FailureReason);

/// <summary>
/// 用于校验关卡开局布局是否可玩的输入。
/// </summary>
public sealed record LevelLayoutValidationRequest(
    GridSize BoardSize,
    LevelConfig Level,
    IReadOnlySet<GridPosition> FixedObstacles,
    IReadOnlySet<GridPosition> ReservedTrackCells);

/// <summary>
/// 返回安全容量信息，以及第一个触发的布局失败原因。
/// </summary>
public sealed record LevelLayoutValidationResult(
    bool IsPlayable,
    int SafeCapacity,
    LayoutFailureReason FailureReason);

/// <summary>
/// 用于判断是否满足进入下一关条件的输入。
/// </summary>
public sealed record LevelProgressionRequest(
    int CurrentLength,
    int CurrentLevelNumber,
    IReadOnlyList<LevelConfig> Levels);

/// <summary>
/// 关卡推进规则的判定结果。
/// </summary>
public sealed record LevelProgressionResult(
    bool ShouldAdvance,
    bool IsGameCompleted,
    int? NextLevelNumber);

/// <summary>
/// 当前逻辑帧中用于结算移动障碍的局面快照。
/// </summary>
public sealed record MovingObstacleResolutionRequest(
    GridSize BoardSize,
    GridPosition SnakeHeadCurrent,
    GridPosition SnakeHeadNext,
    IReadOnlySet<GridPosition> SnakeBodySegments,
    IReadOnlySet<GridPosition> FixedObstacles,
    IReadOnlyList<MovingObstacleState> Obstacles);

/// <summary>
/// 包含移动障碍更新后的状态，以及本帧检测到的致命碰撞结果。
/// </summary>
public sealed record MovingObstacleResolution(
    IReadOnlyList<MovingObstacleState> Obstacles,
    CollisionType CollisionType);

/// <summary>
/// 记录跨帧的双击加速检测状态。
/// </summary>
public sealed record AccelerationState(
    Direction CurrentDirection,
    bool IsBoosting,
    TimeSpan? LastTapAt,
    Direction? LastTapDirection);

/// <summary>
/// 判断加速开始或结束所需的标准化输入。
/// </summary>
public sealed record AccelerationRequest(
    Direction CurrentDirection,
    Direction? PressedDirection,
    Direction? HeldDirection,
    TimeSpan Timestamp,
    TimeSpan DoubleTapWindow,
    double BoostMultiplier);

/// <summary>
/// 返回下一帧的检测状态以及本帧应使用的速度倍率。
/// </summary>
public sealed record AccelerationResult(
    AccelerationState State,
    double SpeedMultiplier);

/// <summary>
/// 用户可配置的音频开关与音量设置。
/// </summary>
public sealed record AudioSettings(
    bool BgmEnabled,
    bool SfxEnabled,
    float BgmVolume,
    float SfxVolume);

/// <summary>
/// 一条持久化保存的排行榜记录。
/// </summary>
public sealed record ScoreRecord(
    string PlayerName,
    int Score,
    int ReachedLevel,
    GameMode Mode,
    TimeSpan Duration);

/// <summary>
/// 创建新游戏会话所需的启动参数。
/// </summary>
public sealed record GameSessionSeed(
    string PlayerName,
    GameMode Mode,
    GridSize BoardSize,
    IReadOnlyList<LevelDefinition> Levels,
    int StartLevelNumber);

/// <summary>
/// 表示当前游戏核心状态快照。
/// </summary>
public sealed record GameSession(
    string PlayerName,
    GameMode Mode,
    GridSize BoardSize,
    IReadOnlyList<LevelDefinition> Levels,
    int CurrentLevelNumber,
    IReadOnlyList<GridPosition> SnakeSegments,
    Direction CurrentDirection,
    IReadOnlySet<GridPosition> FixedObstacles,
    IReadOnlyList<MovingObstacleState> MovingObstacles,
    GridPosition? ApplePosition,
    int Score,
    TimeSpan Elapsed,
    AccelerationState Acceleration,
    GameSessionStatus Status);

/// <summary>
/// 一次逻辑推进所需的标准化输入。
/// </summary>
public sealed record GameStepInput(
    Direction? PressedDirection,
    Direction? HeldDirection,
    TimeSpan Timestamp);

/// <summary>
/// 一次逻辑推进后的结果，包括新状态、业务状态和是否需要写榜。
/// </summary>
public sealed record GameStepResult(
    GameSession Session,
    GameStepStatus Status,
    bool AteApple,
    bool IsDeadlock,
    double SpeedMultiplier,
    CollisionType CollisionType,
    ScoreRecord? ScoreRecordToPersist);

public interface IAudioSettingsStore
{
    /// <summary>
    /// 读取已保存的音频设置；如果尚未保存过配置则返回空。
    /// </summary>
    AudioSettings? Load();

    /// <summary>
    /// 持久化保存最新的音频设置快照。
    /// </summary>
    void Save(AudioSettings settings);
}
