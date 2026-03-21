using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SnakeGame.App.Rendering;

/// <summary>
/// 粒子系统 - 管理吃食物、死亡等特效
/// </summary>
public sealed class ParticleSystem
{
    private readonly System.Collections.Generic.List<Particle> _particles = new();
    private readonly SpriteRenderer _renderer;
    private readonly Random _random = new();
    
    public ParticleSystem(SpriteRenderer renderer)
    {
        _renderer = renderer;
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
            
            var particle = new Particle
            {
                Position = position,
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                StartScale = _random.NextFloat(0.5f, 1.0f),
                EndScale = 0.1f,
                Color = GetParticleColor(type),
                Lifetime = _random.NextFloat(0.3f, 0.6f),
                MaxLifetime = 0.6f
            };
            
            _particles.Add(particle);
        }
    }
    
    private static Color GetParticleColor(ParticleType type)
    {
        return type switch
        {
            ParticleType.Food => new Color(255, 230, 109),  // 柠檬黄
            ParticleType.Death => new Color(255, 255, 255), // 白色
            ParticleType.Celebration => new Color(255, 107, 107), // 珊瑚红
            _ => Color.White
        };
    }
    
    /// <summary>
    /// 清除所有粒子
    /// </summary>
    public void Clear()
    {
        _particles.Clear();
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
/// 单个粒子
/// </summary>
sealed class Particle
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public float StartScale { get; set; }
    public float EndScale { get; set; }
    public float Scale { get; set; }
    public Color Color { get; set; }
    public float Lifetime { get; set; }
    public float MaxLifetime { get; set; }
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
