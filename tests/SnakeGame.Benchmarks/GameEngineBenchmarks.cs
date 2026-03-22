using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using SnakeGame.Core;

namespace SnakeGame.Benchmarks;

/// <summary>
/// GameEngine 核心性能基准测试。
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class GameEngineBenchmarks
{
    private GameEngine engine = null!;
    private GameSession session = null!;

    [GlobalSetup]
    public void Setup()
    {
        engine = new GameEngine();
        var levels = CreateTestLevels();
        var seed = new GameSessionSeed(
            "BenchmarkPlayer",
            GameMode.Classic,
            new GridSize(40, 30),
            levels,
            1);
        session = engine.CreateSession(seed);
    }

    [Benchmark(Description = "GameEngine.Step - 单帧更新")]
    public void GameStep()
    {
        var input = new GameStepInput(
            session.CurrentDirection,
            null,
            null,
            TimeSpan.FromMilliseconds(16));
        var result = engine.Step(session, input);
        session = result.Session;
    }

    [Benchmark(Description = "创建新游戏会话")]
    public void CreateSession()
    {
        var levels = CreateTestLevels();
        var seed = new GameSessionSeed(
            "BenchmarkPlayer",
            GameMode.Classic,
            new GridSize(40, 30),
            levels,
            1);
        engine.CreateSession(seed);
    }

    private static IReadOnlyList<LevelDefinition> CreateTestLevels()
    {
        return new List<LevelDefinition>
        {
            new LevelDefinition(
                1,
                "Test Level",
                new LevelConfig(
                    initialSpeed: TimeSpan.FromMilliseconds(150),
                    speedIncrement: TimeSpan.FromMilliseconds(5),
                    maxSpeed: TimeSpan.FromMilliseconds(50),
                    boostMultiplier: 2.0,
                    foodCount: 1,
                    movingObstacleCount: 0),
                new List<Obstacle>())
        };
    }
}