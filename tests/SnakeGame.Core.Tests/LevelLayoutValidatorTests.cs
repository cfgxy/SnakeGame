namespace SnakeGame.Core.Tests;

/// <summary>
/// 验证关卡布局校验器会正确识别非法地图和安全容量不足的配置。
/// </summary>
public sealed class LevelLayoutValidatorTests
{
    private readonly LevelLayoutValidator validator = new();

    [Fact]
    public void Validate_returns_invalid_when_board_size_is_not_positive()
    {
        // 棋盘宽高只要有一个非正数，就应该直接判为非法布局。
        var request = new LevelLayoutValidationRequest(
            new GridSize(0, 5),
            CreateLevelConfig(targetLength: 6, initialSnakeLength: 3, margin: 2),
            new HashSet<GridPosition>(),
            new HashSet<GridPosition>());

        var result = validator.Validate(request);

        // 断言返回的失败原因是棋盘尺寸非法。
        Assert.False(result.IsPlayable);
        Assert.Equal(LayoutFailureReason.InvalidBoardSize, result.FailureReason);
    }

    [Fact]
    public void Validate_returns_invalid_when_initial_snake_length_is_not_smaller_than_target()
    {
        // 初始蛇长不能大于等于目标长度，否则关卡没有推进意义。
        var request = new LevelLayoutValidationRequest(
            new GridSize(8, 8),
            CreateLevelConfig(targetLength: 4, initialSnakeLength: 4, margin: 1),
            new HashSet<GridPosition>(),
            new HashSet<GridPosition>());

        var result = validator.Validate(request);

        // 断言这里命中的是初始长度配置错误，而不是其他失败原因。
        Assert.False(result.IsPlayable);
        Assert.Equal(LayoutFailureReason.InitialSnakeLengthInvalid, result.FailureReason);
    }

    [Fact]
    public void Validate_counts_reserved_track_cells_against_safe_capacity()
    {
        // 固定障碍和轨道保留格都应占用安全容量。
        var request = new LevelLayoutValidationRequest(
            new GridSize(4, 4),
            CreateLevelConfig(targetLength: 10, initialSnakeLength: 3, margin: 1),
            new HashSet<GridPosition> { new(0, 0), new(1, 0) },
            new HashSet<GridPosition> { new(2, 0), new(3, 0) });

        var result = validator.Validate(request);

        // 4x4 共 16 格，扣除 2 个固定障碍和 2 个轨道格后应剩 12 格。
        Assert.True(result.IsPlayable);
        Assert.Equal(12, result.SafeCapacity);
    }

    [Fact]
    public void Validate_returns_invalid_when_target_exceeds_safe_capacity_after_margin()
    {
        // 目标长度还要加上安全冗余，超过容量就必须在开局前拦截。
        var request = new LevelLayoutValidationRequest(
            new GridSize(4, 4),
            CreateLevelConfig(targetLength: 12, initialSnakeLength: 3, margin: 2),
            new HashSet<GridPosition> { new(0, 0), new(1, 0) },
            new HashSet<GridPosition> { new(2, 0), new(3, 0) });

        var result = validator.Validate(request);

        // 断言返回的容量值可用于后续关卡调参。
        Assert.False(result.IsPlayable);
        Assert.Equal(12, result.SafeCapacity);
        Assert.Equal(LayoutFailureReason.TargetLengthExceedsSafeCapacity, result.FailureReason);
    }

    private static LevelConfig CreateLevelConfig(int targetLength, int initialSnakeLength, int margin)
    {
        // 测试只关心容量和长度规则，其他字段使用稳定默认值即可。
        return new LevelConfig(
            1,
            targetLength,
            initialSnakeLength,
            TimeSpan.FromMilliseconds(300),
            0,
            false,
            2,
            margin,
            []);
    }
}
