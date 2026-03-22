using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;

namespace SnakeGame.Benchmarks;

/// <summary>
/// 数据结构性能基准测试。
/// 主要测试蛇身数据结构的各种实现方案性能。
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class DataStructureBenchmarks
{
    private readonly Random random = new(42);
    private List<GridPosition> snakeList = null!;
    private ImmutableArray<GridPosition> snakeImmutable = null!;
    private GridPosition[] snakeArray = null!;

    [Params(10, 50, 100, 200)]
    public int SnakeLength { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var positions = Enumerable.Range(0, SnakeLength)
            .Select(i => new GridPosition(i, 0))
            .ToList();

        snakeList = new List<GridPosition>(positions);
        snakeImmutable = positions.ToImmutableArray();
        snakeArray = positions.ToArray();
    }

    [Benchmark(Description = "List - 移动蛇头并移除蛇尾")]
    public List<GridPosition> List_MoveSnake()
    {
        var newHead = new GridPosition(
            snakeList[0].X + 1,
            snakeList[0].Y);
        snakeList.Insert(0, newHead);
        snakeList.RemoveAt(snakeList.Count - 1);
        return snakeList;
    }

    [Benchmark(Description = "ImmutableArray - 移动蛇头并移除蛇尾")]
    public ImmutableArray<GridPosition> ImmutableArray_MoveSnake()
    {
        var builder = snakeImmutable.ToBuilder();
        var newHead = new GridPosition(
            builder[0].X + 1,
            builder[0].Y);
        builder.Insert(0, newHead);
        builder.RemoveAt(builder.Count - 1);
        snakeImmutable = builder.ToImmutable();
        return snakeImmutable;
    }

    [Benchmark(Description = "Array + Span - 移动蛇头并移除蛇尾")]
    public GridPosition[] Array_MoveSnake()
    {
        // 使用 Span 进行内存拷贝
        var newHead = new GridPosition(
            snakeArray[0].X + 1,
            snakeArray[0].Y);

        // 后移所有元素
        Array.Copy(snakeArray, 0, snakeArray, 1, snakeArray.Length - 1);
        snakeArray[0] = newHead;
        return snakeArray;
    }

    [Benchmark(Description = "List - 碰撞检测（遍历蛇身）")]
    public bool List_CollisionCheck()
    {
        var head = snakeList[0];
        for (int i = 1; i < snakeList.Count; i++)
        {
            if (snakeList[i].Equals(head))
                return true;
        }
        return false;
    }

    [Benchmark(Description = "Array - 碰撞检测（遍历蛇身）")]
    public bool Array_CollisionCheck()
    {
        var head = snakeArray[0];
        for (int i = 1; i < snakeArray.Length; i++)
        {
            if (snakeArray[i].Equals(head))
                return true;
        }
        return false;
    }
}

/// <summary>
/// 网格位置，与 SnakeGame.Core 对齐。
/// </summary>
public readonly record struct GridPosition(int X, int Y);