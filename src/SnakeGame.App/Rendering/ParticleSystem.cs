using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SnakeGame.App.Rendering;

/// <summary>
/// 粒子系统 - 管理吃食物、死亡等特效
/// 使用对象池优化 GC 性能
/// </summary>
public sealed class ParticleSystem
{
    private readonly System.Collections.Generic.List<Particle> _particles = new(64);
    private readonly ObjectPool<Particle> _particlePool;
    private readonly SpriteRenderer _renderer;
    private readonly Random _random = new();

    public ParticleSystem(SpriteRenderer renderer)
    {
        _renderer = renderer;
        _particlePool = new ObjectPool<Particle>(256, p => p.Reset());
    }

    /// <summary>
    /// 更新所有粒子
    /// </summary>
    public void Update(GameTime gameTime)
    {
        var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];
            particle.Lifetime -= elapsed;
            particle.Position += particle.Velocity * elapsed;
            particle.Scale = MathHelper.Lerp(particle.StartScale, particle.EndScale, 1 - particle.Lifetime / particle.MaxLifetime);

            if (particle.Lifetime <= 0)
            {
                // 归还对象池而非销毁
                _particlePool.Return(particle);
                _particles.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 绘制所有粒子
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var particle in _particles)
        {
            var color = particle.Color * (particle.Lifetime / particle.MaxLifetime);
            _renderer.DrawCircle(spriteBatch, particle.Position, particle.Scale, color);
        }
    }

    /// <summary>
    /// 在指定位置爆发粒子
    /// </summary>
    public void Emit(Vector2 position, ParticleType type, int count = 10)
    {
        for (int i = 0; i < count; i++)
        {
            var angle = _random.NextFloat(0, MathHelper.TwoPi);
            var speed = _random.NextFloat(50, 150);

            // 从对象池获取粒子
            var particle = _particlePool.Rent();
            particle.Position = position;
            particle.Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            particle.StartScale = _random.NextFloat(0.5f, 1.0f);
            particle.EndScale = 0.1f;
            particle.Scale = particle.StartScale;
            particle.Color = GetParticleColor(type);
            particle.Lifetime = _random.NextFloat(0.3f, 0.6f);
            particle.MaxLifetime = 0.6f;

            _particles.Add(particle);
        }
    }

    private static Microsoft.Xna.Framework.Color GetParticleColor(ParticleType type)
    {
        return type switch
        {
            ParticleType.Food => new Microsoft.Xna.Framework.Color(255, 230, 109),  // 柠檬黄
            ParticleType.Death => new Microsoft.Xna.Framework.Color(255, 255, 255), // 白色
            ParticleType.Celebration => new Microsoft.Xna.Framework.Color(255, 107, 107), // 珊瑚红
            _ => Microsoft.Xna.Framework.Color.White
        };
    }

    /// <summary>
    /// 清除所有粒子
    /// </summary>
    public void Clear()
    {
        // 归还所有粒子到池
        foreach (var particle in _particles)
        {
            _particlePool.Return(particle);
        }
        _particles.Clear();
    }

    /// <summary>
    /// 获取对象池统计信息（用于调试）
    /// </summary>
    public (int Active, int Pooled) GetPoolStats()
    {
        return (_particles.Count, _particlePool.Count);
    }
}

/// <summary>
/// 粒子类型
/// </summary>
public enum ParticleType
{
    Food,
    Death,
    Celebration
}

/// <summary>
/// 单个粒子 - 实现可池化接口
/// </summary>
sealed class Particle : IPoolable
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public float StartScale { get; set; }
    public float EndScale { get; set; }
    public float Scale { get; set; }
    public Microsoft.Xna.Framework.Color Color { get; set; }
    public float Lifetime { get; set; }
    public float MaxLifetime { get; set; }

    /// <summary>
    /// 重置粒子状态（归还池时调用）
    /// </summary>
    public void Reset()
    {
        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
        StartScale = 0;
        EndScale = 0;
        Scale = 0;
        Color = Microsoft.Xna.Framework.Color.White;
        Lifetime = 0;
        MaxLifetime = 0;
    }
}

/// <summary>
/// Random 扩展方法
/// </summary>
static class RandomExtensions
{
    public static float NextFloat(this Random random, float minValue, float maxValue)
    {
        return (float)(random.NextDouble() * (maxValue - minValue) + minValue);
    }
}