using SnakeGame.Core;

namespace SnakeGame.App;

/// <summary>
/// 提供首个可运行版本的关卡目录。
/// </summary>
internal static class LevelCatalog
{
    public static GridSize BoardSize => new(24, 18);

    public static IReadOnlyList<LevelDefinition> Create()
    {
        return
        [
            CreateLevel(1, 6, 360, new HashSet<GridPosition>(), []),
            CreateLevel(2, 8, 320, VerticalWall(7, 4, 6, 16, 4, 6), []),
            CreateLevel(3, 10, 280, BoxRing(5, 3, 6, 5), []),
            CreateLevel(4, 12, 240, ZigzagWalls(), []),
            CreateLevel(5, 14, 220, CrossWalls(), [HorizontalTrack(17, 3, 7)]),
            CreateLevel(6, 16, 200, DoubleRooms(), [HorizontalTrack(17, 3, 7), VerticalTrack(20, 4, 12)])
        ];
    }

    private static LevelDefinition CreateLevel(
        int levelNumber,
        int targetLength,
        int stepMilliseconds,
        IReadOnlySet<GridPosition> fixedObstacles,
        IReadOnlyList<IReadOnlyList<GridPosition>> movingTracks)
    {
        var initialSnake = new[]
        {
            new GridPosition(3, 9),
            new GridPosition(2, 9),
            new GridPosition(1, 9)
        };

        return new LevelDefinition(
            new LevelConfig(
                levelNumber,
                targetLength,
                initialSnake.Length,
                TimeSpan.FromMilliseconds(stepMilliseconds),
                fixedObstacles.Count,
                movingTracks.Count > 0,
                2d,
                4,
                movingTracks),
            initialSnake,
            Direction.Right,
            fixedObstacles);
    }

    private static IReadOnlySet<GridPosition> VerticalWall(int x1, int y1, int count1, int x2, int y2, int count2)
    {
        var result = new HashSet<GridPosition>();
        for (var index = 0; index < count1; index++)
        {
            result.Add(new GridPosition(x1, y1 + index));
        }

        for (var index = 0; index < count2; index++)
        {
            result.Add(new GridPosition(x2, y2 + index));
        }

        return result;
    }

    private static IReadOnlySet<GridPosition> BoxRing(int x, int y, int width, int height)
    {
        var result = new HashSet<GridPosition>();
        for (var offset = 0; offset < width; offset++)
        {
            result.Add(new GridPosition(x + offset, y));
            result.Add(new GridPosition(x + offset, y + height - 1));
        }

        for (var offset = 1; offset < height - 1; offset++)
        {
            result.Add(new GridPosition(x, y + offset));
            result.Add(new GridPosition(x + width - 1, y + offset));
        }

        return result;
    }

    private static IReadOnlySet<GridPosition> ZigzagWalls()
    {
        return new HashSet<GridPosition>
        {
            new(8, 3), new(9, 3), new(10, 3),
            new(10, 4), new(10, 5), new(10, 6),
            new(12, 8), new(13, 8), new(14, 8),
            new(14, 9), new(14, 10), new(14, 11),
            new(6, 12), new(7, 12), new(8, 12),
            new(8, 13), new(8, 14)
        };
    }

    private static IReadOnlySet<GridPosition> CrossWalls()
    {
        var result = new HashSet<GridPosition>();
        for (var x = 8; x <= 15; x++)
        {
            result.Add(new GridPosition(x, 8));
        }

        for (var y = 4; y <= 13; y++)
        {
            if (y != 8)
            {
                result.Add(new GridPosition(12, y));
            }
        }

        return result;
    }

    private static IReadOnlySet<GridPosition> DoubleRooms()
    {
        var result = new HashSet<GridPosition>();
        foreach (var cell in BoxRing(6, 3, 5, 5))
        {
            result.Add(cell);
        }

        foreach (var cell in BoxRing(13, 9, 5, 5))
        {
            result.Add(cell);
        }

        result.Add(new GridPosition(11, 11));
        result.Add(new GridPosition(12, 11));
        return result;
    }

    private static IReadOnlyList<GridPosition> HorizontalTrack(int startX, int y, int endX)
    {
        var track = new List<GridPosition>();
        for (var x = startX; x >= endX; x--)
        {
            track.Add(new GridPosition(x, y));
        }

        return track;
    }

    private static IReadOnlyList<GridPosition> VerticalTrack(int x, int startY, int endY)
    {
        var track = new List<GridPosition>();
        for (var y = startY; y <= endY; y++)
        {
            track.Add(new GridPosition(x, y));
        }

        return track;
    }
}
