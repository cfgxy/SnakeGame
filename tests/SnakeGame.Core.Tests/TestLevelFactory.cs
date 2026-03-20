namespace SnakeGame.Core.Tests;

/// <summary>
/// 集中构造核心层测试用的关卡数据，避免每个测试文件重复拼装样板对象。
/// </summary>
internal static class TestLevelFactory
{
    public static LevelDefinition CreateLevelDefinition(
        int levelNumber,
        int targetLength,
        IReadOnlyList<GridPosition> initialSnakeSegments,
        Direction initialDirection,
        IReadOnlySet<GridPosition>? fixedObstacles = null,
        IReadOnlyList<IReadOnlyList<GridPosition>>? movingObstacleTracks = null,
        int safeCapacityMargin = 1,
        int stepMilliseconds = 300,
        double boostMultiplier = 2d)
    {
        return new LevelDefinition(
            new LevelConfig(
                levelNumber,
                targetLength,
                initialSnakeSegments.Count,
                TimeSpan.FromMilliseconds(stepMilliseconds),
                fixedObstacles?.Count ?? 0,
                movingObstacleTracks is { Count: > 0 },
                boostMultiplier,
                safeCapacityMargin,
                movingObstacleTracks ?? []),
            initialSnakeSegments,
            initialDirection,
            fixedObstacles ?? new HashSet<GridPosition>());
    }

    public static LevelConfig CreateLevelConfig(
        int levelNumber,
        int targetLength,
        int initialSnakeLength,
        int safeCapacityMargin = 1,
        int stepMilliseconds = 300,
        int fixedObstacleCount = 0,
        bool enablesMovingObstacles = false,
        double boostMultiplier = 2d,
        IReadOnlyList<IReadOnlyList<GridPosition>>? movingObstacleTracks = null)
    {
        return new LevelConfig(
            levelNumber,
            targetLength,
            initialSnakeLength,
            TimeSpan.FromMilliseconds(stepMilliseconds),
            fixedObstacleCount,
            enablesMovingObstacles,
            boostMultiplier,
            safeCapacityMargin,
            movingObstacleTracks ?? []);
    }
}
