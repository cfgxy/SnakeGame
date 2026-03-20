namespace SnakeGame.Core.Tests;

/// <summary>
/// 验证双击加速只在满足时间窗和持续按住条件时生效。
/// </summary>
public sealed class AccelerationResolverTests
{
    private readonly AccelerationResolver resolver = new();

    [Fact]
    public void Resolve_enables_boost_when_current_direction_is_double_tapped_and_held()
    {
        // 第一拍只记录状态，不应立即加速；第二拍且持续按住后才进入加速。
        var initial = new AccelerationState(Direction.Right, false, null, null);
        var afterFirstTap = resolver.Resolve(
            initial,
            new AccelerationRequest(
                Direction.Right,
                Direction.Right,
                Direction.Right,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(200),
                2));

        var afterSecondTap = resolver.Resolve(
            afterFirstTap.State,
            new AccelerationRequest(
                Direction.Right,
                Direction.Right,
                Direction.Right,
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(200),
                2));

        // 断言最终已经进入加速，倍率取配置值。
        Assert.True(afterSecondTap.State.IsBoosting);
        Assert.Equal(2, afterSecondTap.SpeedMultiplier);
    }

    [Fact]
    public void Resolve_disables_boost_when_current_direction_is_released()
    {
        // 即使之前已经加速，只要当前方向不再保持按住，就必须恢复正常速度。
        var previous = new AccelerationState(
            Direction.Right,
            true,
            TimeSpan.FromMilliseconds(200),
            Direction.Right);

        var result = resolver.Resolve(
            previous,
            new AccelerationRequest(
                Direction.Right,
                null,
                null,
                TimeSpan.FromMilliseconds(260),
                TimeSpan.FromMilliseconds(200),
                2));

        Assert.False(result.State.IsBoosting);
        Assert.Equal(1, result.SpeedMultiplier);
    }

    [Fact]
    public void Resolve_does_not_boost_when_second_tap_exceeds_the_double_tap_window()
    {
        // 超出双击时间窗后，第二次按下只能算新的一拍，不能触发加速。
        var previous = new AccelerationState(
            Direction.Right,
            false,
            TimeSpan.FromMilliseconds(100),
            Direction.Right);

        var result = resolver.Resolve(
            previous,
            new AccelerationRequest(
                Direction.Right,
                Direction.Right,
                Direction.Right,
                TimeSpan.FromMilliseconds(350),
                TimeSpan.FromMilliseconds(200),
                2));

        Assert.False(result.State.IsBoosting);
        Assert.Equal(1, result.SpeedMultiplier);
    }

    [Fact]
    public void Resolve_resets_boost_tracking_when_another_direction_is_pressed()
    {
        // 玩家试图换向时，应清空原方向的双击上下文，避免误触加速。
        var previous = new AccelerationState(
            Direction.Right,
            true,
            TimeSpan.FromMilliseconds(200),
            Direction.Right);

        var result = resolver.Resolve(
            previous,
            new AccelerationRequest(
                Direction.Right,
                Direction.Up,
                Direction.Up,
                TimeSpan.FromMilliseconds(210),
                TimeSpan.FromMilliseconds(200),
                2));

        Assert.False(result.State.IsBoosting);
        Assert.Null(result.State.LastTapAt);
        Assert.Null(result.State.LastTapDirection);
        Assert.Equal(1, result.SpeedMultiplier);
    }
}
