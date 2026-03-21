namespace SnakeGame.Core;

/// <summary>
/// 查找一个当前可达且吃下后仍能保证蛇继续存活的苹果落点。
/// </summary>
public sealed class AppleSpawnService
{
    private static readonly GridPosition[] NeighborOffsets =
    [
        new GridPosition(0, -1),
        new GridPosition(1, 0),
        new GridPosition(0, 1),
        new GridPosition(-1, 0)
    ];

    public AppleSpawnResult SelectSpawnCell(AppleSpawnRequest request)
    {
        return SelectSpawnCellInternal(request, null);
    }

    // 游戏运行时使用随机候选点，避免苹果长期固定刷在左上角等首个合法格子。
    public AppleSpawnResult SelectSpawnCell(AppleSpawnRequest request, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return SelectSpawnCellInternal(request, random);
    }

    private AppleSpawnResult SelectSpawnCellInternal(AppleSpawnRequest request, Random? random)
    {
        if (request.SnakeSegments.Count == 0)
        {
            return new AppleSpawnResult(false, null, AppleSpawnFailureReason.NoLegalCell);
        }

        var reservedCells = new HashSet<GridPosition>(request.FixedObstacles);
        reservedCells.UnionWith(request.ReservedTrackCells);

        foreach (var obstacle in request.MovingObstacles)
        {
            reservedCells.Add(obstacle.Track[obstacle.TrackIndex]);
        }

        var legalCandidates = random is null ? null : new List<GridPosition>();

        foreach (var candidate in EnumerateBoard(request.BoardSize))
        {
            if (reservedCells.Contains(candidate) || request.SnakeSegments.Contains(candidate))
            {
                continue;
            }

            // 先验证蛇头当前是否能够走到该候选苹果位置。
            var path = FindPath(
                request.BoardSize,
                request.SnakeSegments[0],
                candidate,
                BuildBlockedCells(request.SnakeSegments, reservedCells));

            if (path.Count == 0)
            {
                continue;
            }

            // 再模拟一次实际吃苹果后的蛇身变化，确保新蛇头仍然可以接回新蛇尾。
            var simulatedSnake = SimulateSnakeMove(request.SnakeSegments, path, candidate);
            var safeBlockedCells = BuildBlockedCells(simulatedSnake, reservedCells);
            var newHead = simulatedSnake[0];
            var newTail = simulatedSnake[^1];
            safeBlockedCells.Remove(newTail);

            var safePath = FindPath(request.BoardSize, newHead, newTail, safeBlockedCells);
            if (safePath.Count > 0)
            {
                if (legalCandidates is null)
                {
                    return new AppleSpawnResult(true, candidate, AppleSpawnFailureReason.None);
                }

                legalCandidates.Add(candidate);
            }
        }

        if (legalCandidates is { Count: > 0 })
        {
            var selected = legalCandidates[random!.Next(legalCandidates.Count)];
            return new AppleSpawnResult(true, selected, AppleSpawnFailureReason.None);
        }

        return new AppleSpawnResult(false, null, AppleSpawnFailureReason.NoLegalCell);
    }

    private static HashSet<GridPosition> BuildBlockedCells(
        IReadOnlyList<GridPosition> snakeSegments,
        HashSet<GridPosition> reservedCells)
    {
        var blocked = new HashSet<GridPosition>(reservedCells);

        // 这里故意不把尾巴算成永久阻塞，因为路径校验是在“下一段移动过程”上进行的。
        for (var index = 0; index < snakeSegments.Count - 1; index++)
        {
            blocked.Add(snakeSegments[index]);
        }

        return blocked;
    }

    private static List<GridPosition> SimulateSnakeMove(
        IReadOnlyList<GridPosition> snakeSegments,
        IReadOnlyList<GridPosition> path,
        GridPosition applePosition)
    {
        var simulated = snakeSegments.ToList();

        for (var index = 0; index < path.Count; index++)
        {
            var nextHead = path[index];
            simulated.Insert(0, nextHead);

            // 只有最后一步真正吃到苹果时，蛇身才会额外增长一格。
            if (nextHead != applePosition || index != path.Count - 1)
            {
                simulated.RemoveAt(simulated.Count - 1);
            }
        }

        return simulated;
    }

    private static IReadOnlyList<GridPosition> FindPath(
        GridSize boardSize,
        GridPosition start,
        GridPosition target,
        HashSet<GridPosition> blocked)
    {
        // 每一步代价都相同，这里用广度优先搜索就足够了。
        var queue = new Queue<GridPosition>();
        var previous = new Dictionary<GridPosition, GridPosition?>();
        queue.Enqueue(start);
        previous[start] = null;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == target)
            {
                return ReconstructPath(previous, target);
            }

            foreach (var neighbor in EnumerateNeighbors(current, boardSize))
            {
                if (blocked.Contains(neighbor) || previous.ContainsKey(neighbor))
                {
                    continue;
                }

                previous[neighbor] = current;
                queue.Enqueue(neighbor);
            }
        }

        return Array.Empty<GridPosition>();
    }

    private static IReadOnlyList<GridPosition> ReconstructPath(
        Dictionary<GridPosition, GridPosition?> previous,
        GridPosition target)
    {
        var path = new List<GridPosition>();
        var current = target;

        while (previous[current] is GridPosition prior)
        {
            path.Add(current);
            current = prior;
        }

        path.Reverse();
        return path;
    }

    private static IEnumerable<GridPosition> EnumerateBoard(GridSize boardSize)
    {
        for (var y = 0; y < boardSize.Height; y++)
        {
            for (var x = 0; x < boardSize.Width; x++)
            {
                yield return new GridPosition(x, y);
            }
        }
    }

    private static IEnumerable<GridPosition> EnumerateNeighbors(GridPosition position, GridSize boardSize)
    {
        foreach (var offset in NeighborOffsets)
        {
            var candidate = new GridPosition(position.X + offset.X, position.Y + offset.Y);
            if (candidate.X < 0 || candidate.Y < 0 || candidate.X >= boardSize.Width || candidate.Y >= boardSize.Height)
            {
                continue;
            }

            yield return candidate;
        }
    }
}

/// <summary>
/// 拒绝那些无法安全支撑目标长度的关卡布局。
/// </summary>
public sealed class LevelLayoutValidator
{
    public LevelLayoutValidationResult Validate(LevelLayoutValidationRequest request)
    {
        if (request.BoardSize.Width <= 0 || request.BoardSize.Height <= 0)
        {
            return new LevelLayoutValidationResult(false, 0, LayoutFailureReason.InvalidBoardSize);
        }

        if (request.Level.InitialSnakeLength >= request.Level.TargetLength)
        {
            return new LevelLayoutValidationResult(false, 0, LayoutFailureReason.InitialSnakeLengthInvalid);
        }

        // 轨道格需要预留给移动障碍，因此不能计入真实可用的安全容量。
        var totalCells = request.BoardSize.Width * request.BoardSize.Height;
        var safeCapacity = totalCells - request.FixedObstacles.Count - request.ReservedTrackCells.Count;
        var minimumNeeded = request.Level.TargetLength + request.Level.SafeCapacityMargin;

        if (safeCapacity < minimumNeeded)
        {
            return new LevelLayoutValidationResult(false, safeCapacity, LayoutFailureReason.TargetLengthExceedsSafeCapacity);
        }

        return new LevelLayoutValidationResult(true, safeCapacity, LayoutFailureReason.None);
    }
}

/// <summary>
/// 判断当前蛇长是否足以通关或完成整轮闯关。
/// </summary>
public sealed class LevelProgressionService
{
    public LevelProgressionResult Evaluate(LevelProgressionRequest request)
    {
        var current = request.Levels.Single(level => level.LevelNumber == request.CurrentLevelNumber);
        if (request.CurrentLength < current.TargetLength)
        {
            return new LevelProgressionResult(false, false, null);
        }

        var next = request.Levels
            .Where(level => level.LevelNumber > request.CurrentLevelNumber)
            .OrderBy(level => level.LevelNumber)
            .FirstOrDefault();

        if (next is null)
        {
            return new LevelProgressionResult(true, true, null);
        }

        return new LevelProgressionResult(true, false, next.LevelNumber);
    }
}

/// <summary>
/// 沿轨道推进移动障碍，并结算它们的特殊碰撞规则。
/// </summary>
public sealed class MovingObstacleResolver
{
    public MovingObstacleResolution Resolve(MovingObstacleResolutionRequest request)
    {
        var nextStates = new List<MovingObstacleState>(request.Obstacles.Count);
        var collisionType = CollisionType.None;

        foreach (var obstacle in request.Obstacles)
        {
            var currentPosition = obstacle.Track[obstacle.TrackIndex];
            var direction = obstacle.DirectionSign == 0 ? 1 : obstacle.DirectionSign;
            var nextIndex = obstacle.TrackIndex + direction;

            if (nextIndex < 0 || nextIndex >= obstacle.Track.Count)
            {
                direction *= -1;
                nextIndex = obstacle.TrackIndex + direction;
            }

            // 如果下一步会撞上固定障碍，则视为轨道被截断，需要立刻反向。
            var nextPosition = obstacle.Track[nextIndex];
            if (request.FixedObstacles.Contains(nextPosition))
            {
                direction *= -1;
                nextIndex = obstacle.TrackIndex + direction;
                nextPosition = obstacle.Track[nextIndex];
            }

            // 交换位置碰撞必须先判断，否则会被更宽泛的蛇头重叠碰撞吞掉。
            if (currentPosition == request.SnakeHeadNext && nextPosition == request.SnakeHeadCurrent)
            {
                collisionType = CollisionType.SwapWithSnakeHead;
            }
            else if (nextPosition == request.SnakeHeadNext || nextPosition == request.SnakeHeadCurrent)
            {
                collisionType = CollisionType.SnakeHeadHitObstacle;
            }

            if (collisionType != CollisionType.None)
            {
                nextStates.Add(new MovingObstacleState(obstacle.Track, nextIndex, direction, MovingObstacleStatus.Moving));
                continue;
            }

            // 蛇身挡住时，障碍停在原位，但要保留方向以便后续恢复移动。
            if (request.SnakeBodySegments.Contains(nextPosition))
            {
                nextStates.Add(new MovingObstacleState(
                    obstacle.Track,
                    obstacle.TrackIndex,
                    direction,
                    MovingObstacleStatus.PausedByBody));
                continue;
            }

            nextStates.Add(new MovingObstacleState(
                obstacle.Track,
                nextIndex,
                direction,
                MovingObstacleStatus.Moving));
        }

        return new MovingObstacleResolution(nextStates, collisionType);
    }
}

/// <summary>
/// 独立于键盘 API 的“双击并按住”加速判定器。
/// </summary>
public sealed class AccelerationResolver
{
    public AccelerationResult Resolve(AccelerationState previousState, AccelerationRequest request)
    {
        var isBoosting = previousState.IsBoosting && request.HeldDirection == request.CurrentDirection;
        TimeSpan? lastTapAt = previousState.LastTapAt;
        Direction? lastTapDirection = previousState.LastTapDirection;

        if (request.PressedDirection == request.CurrentDirection)
        {
            // 只有在时间窗内第二次按下同方向并持续按住时，才会进入加速。
            var isDoubleTap =
                previousState.LastTapDirection == request.CurrentDirection &&
                previousState.LastTapAt.HasValue &&
                request.Timestamp - previousState.LastTapAt.Value <= request.DoubleTapWindow;

            isBoosting = isDoubleTap && request.HeldDirection == request.CurrentDirection;
            lastTapAt = request.Timestamp;
            lastTapDirection = request.CurrentDirection;
        }
        else if (request.PressedDirection.HasValue)
        {
            isBoosting = false;
            lastTapAt = null;
            lastTapDirection = null;
        }
        else if (request.HeldDirection != request.CurrentDirection)
        {
            isBoosting = false;
        }

        var nextState = new AccelerationState(request.CurrentDirection, isBoosting, lastTapAt, lastTapDirection);
        var speedMultiplier = isBoosting ? request.BoostMultiplier : 1d;
        return new AccelerationResult(nextState, speedMultiplier);
    }
}

/// <summary>
/// 闯关模式计入总榜，练习模式只在本局内生效，不写入共享排行榜。
/// </summary>
public sealed class ScoreSubmissionPolicy
{
    public bool ShouldRecord(GameMode mode)
    {
        return mode == GameMode.Story;
    }
}

/// <summary>
/// 在存储抽象之上提供默认音频设置和持久化辅助逻辑。
/// </summary>
public sealed class AudioSettingsService
{
    private readonly IAudioSettingsStore store;

    public AudioSettingsService(IAudioSettingsStore store)
    {
        this.store = store;
    }

    public AudioSettings LoadOrCreateDefaults()
    {
        return store.Load() ?? Default();
    }

    public AudioSettings Save(AudioSettings settings)
    {
        store.Save(settings);
        return settings;
    }

    public AudioSettings SetBgmEnabled(bool enabled)
    {
        var current = LoadOrCreateDefaults();
        var updated = current with { BgmEnabled = enabled };
        store.Save(updated);
        return updated;
    }

    public static AudioSettings Default()
    {
        return new AudioSettings(true, true, 0.65f, 0.85f);
    }
}
