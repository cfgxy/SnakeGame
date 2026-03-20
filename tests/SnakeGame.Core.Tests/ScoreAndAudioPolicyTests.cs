namespace SnakeGame.Core.Tests;

/// <summary>
/// 验证排行榜写入策略和音频设置服务的默认行为。
/// </summary>
public sealed class ScoreAndAudioPolicyTests
{
    [Fact]
    public void ShouldRecord_returns_true_for_story_mode()
    {
        // 闯关模式需要写入总榜。
        var policy = new ScoreSubmissionPolicy();

        Assert.True(policy.ShouldRecord(GameMode.Story));
    }

    [Fact]
    public void ShouldRecord_returns_false_for_practice_mode()
    {
        // 练习模式只用于选关试玩，不应污染总榜数据。
        var policy = new ScoreSubmissionPolicy();

        Assert.False(policy.ShouldRecord(GameMode.Practice));
    }

    [Fact]
    public void LoadOrCreateDefaults_returns_default_settings_when_store_is_empty()
    {
        // 当本地还没有存档时，服务应返回约定的默认音频设置。
        var store = new InMemoryAudioSettingsStore();
        var service = new AudioSettingsService(store);

        var result = service.LoadOrCreateDefaults();

        // 默认值既是产品预期，也是后续界面初始化的基础。
        Assert.True(result.BgmEnabled);
        Assert.True(result.SfxEnabled);
        Assert.Equal(0.65f, result.BgmVolume);
        Assert.Equal(0.85f, result.SfxVolume);
    }

    [Fact]
    public void SetBgmEnabled_updates_and_persists_the_settings()
    {
        // 切换 BGM 开关后，不仅返回值要变化，持久化层也要同步更新。
        var store = new InMemoryAudioSettingsStore
        {
            Current = new AudioSettings(true, true, 0.65f, 0.85f)
        };

        var service = new AudioSettingsService(store);
        var updated = service.SetBgmEnabled(false);

        Assert.False(updated.BgmEnabled);
        Assert.NotNull(store.Current);
        Assert.False(store.Current!.BgmEnabled);
    }
}

/// <summary>
/// 用于单元测试的内存版音频设置存储替身，避免依赖真实文件系统。
/// </summary>
internal sealed class InMemoryAudioSettingsStore : IAudioSettingsStore
{
    public AudioSettings? Current { get; set; }

    public AudioSettings? Load()
    {
        return Current;
    }

    public void Save(AudioSettings settings)
    {
        Current = settings;
    }
}
