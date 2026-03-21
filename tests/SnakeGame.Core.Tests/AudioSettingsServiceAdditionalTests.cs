using Xunit;

namespace SnakeGame.Core.Tests;

/// <summary>
/// AudioSettingsService 的补充测试用例
/// </summary>
public class AudioSettingsServiceAdditionalTests
{
    [Fact]
    public void Save_persists_settings_and_returns_same_instance()
    {
        // 验证 Save 方法能够正确持久化设置并返回相同的实例。
        var store = new InMemoryAudioSettingsStore();
        var service = new AudioSettingsService(store);

        var settings = new AudioSettings(false, true, 0.5f, 0.7f);
        var result = service.Save(settings);

        // 应返回同一个对象引用。
        Assert.Same(settings, result);
        
        // 存储层应已保存。
        Assert.NotNull(store.Current);
        Assert.Same(settings, store.Current);
        Assert.False(store.Current!.BgmEnabled);
        Assert.True(store.Current.SfxEnabled);
        Assert.Equal(0.5f, store.Current.BgmVolume);
        Assert.Equal(0.7f, store.Current.SfxVolume);
    }

    [Fact]
    public void Default_returns_expected_default_values()
    {
        // 验证 Default 方法返回约定的默认值。
        var defaults = AudioSettingsService.Default();

        Assert.True(defaults.BgmEnabled);
        Assert.True(defaults.SfxEnabled);
        Assert.Equal(0.65f, defaults.BgmVolume);
        Assert.Equal(0.85f, defaults.SfxVolume);
    }

    [Fact]
    public void LoadOrCreateDefaults_returns_stored_settings_when_available()
    {
        // 当存储层已有存档时，应返回存储的值而非默认值。
        var store = new InMemoryAudioSettingsStore
        {
            Current = new AudioSettings(false, false, 0.3f, 0.4f)
        };
        var service = new AudioSettingsService(store);

        var result = service.LoadOrCreateDefaults();

        Assert.False(result.BgmEnabled);
        Assert.False(result.SfxEnabled);
        Assert.Equal(0.3f, result.BgmVolume);
        Assert.Equal(0.4f, result.SfxVolume);
    }

    [Fact]
    public void SetBgmEnabled_persists_other_settings_unchanged()
    {
        // 切换 BGM 开关时，其他设置应保持不变。
        var store = new InMemoryAudioSettingsStore
        {
            Current = new AudioSettings(true, false, 0.5f, 0.6f)
        };
        var service = new AudioSettingsService(store);

        var updated = service.SetBgmEnabled(false);

        Assert.False(updated.BgmEnabled);
        Assert.False(updated.SfxEnabled); // 保持不变
        Assert.Equal(0.5f, updated.BgmVolume); // 保持不变
        Assert.Equal(0.6f, updated.SfxVolume); // 保持不变
    }
}
