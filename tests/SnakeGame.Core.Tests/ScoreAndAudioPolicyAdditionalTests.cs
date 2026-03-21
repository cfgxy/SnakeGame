namespace SnakeGame.Core.Tests;

/// <summary>
/// 补充验证音频设置服务的显式保存与已有配置读取行为。
/// </summary>
public sealed class ScoreAndAudioPolicyAdditionalTests
{
    [Fact]
    public void LoadOrCreateDefaults_returns_existing_settings_when_store_has_value()
    {
        // 已有持久化配置时，服务应原样返回而不是覆盖成默认值。
        var store = new InMemoryAudioSettingsStore
        {
            Current = new AudioSettings(false, true, 0.2f, 0.6f)
        };

        var service = new AudioSettingsService(store);
        var result = service.LoadOrCreateDefaults();

        Assert.False(result.BgmEnabled);
        Assert.True(result.SfxEnabled);
        Assert.Equal(0.2f, result.BgmVolume);
        Assert.Equal(0.6f, result.SfxVolume);
    }

    [Fact]
    public void Save_returns_and_persists_the_supplied_settings()
    {
        // 显式保存接口后续会被应用层设置页复用，这里要确认它不会丢失任何字段。
        var store = new InMemoryAudioSettingsStore();
        var service = new AudioSettingsService(store);
        var settings = new AudioSettings(false, false, 0.1f, 0.3f);

        var result = service.Save(settings);

        Assert.Equal(settings, result);
        Assert.Equal(settings, store.Current);
    }
}
