namespace SnakeGame.Core.Tests;

/// <summary>
/// 补充验证加速状态在持续按住同方向时的保持行为。
/// </summary>
public sealed class AccelerationResolverAdditionalTests
{
    private readonly AccelerationResolver resolver = new();

    [Fact]
    public void Resolve_keeps_boosting_while_current_direction_remains_held()
    {
        // 一旦已经进入加速态，只要玩家没有松开当前方向，就应持续保持加速。
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
                Direction.Right,
                TimeSpan.FromMilliseconds(260),
                TimeSpan.FromMilliseconds(200),
                2d));

        Assert.True(result.State.IsBoosting);
        Assert.Equal(2d, result.SpeedMultiplier);
        Assert.Equal(previous.LastTapAt, result.State.LastTapAt);
        Assert.Equal(previous.LastTapDirection, result.State.LastTapDirection);
    }
}
