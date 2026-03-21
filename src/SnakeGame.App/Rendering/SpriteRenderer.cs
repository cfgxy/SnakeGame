using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SnakeGame.App.Rendering;

/// <summary>
/// 精灵渲染器 - 管理所有游戏资源的加载和绘制
/// </summary>
public sealed class SpriteRenderer
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _content;
    
    // 蛇资源
    public Texture2D SnakeHead { get; private set; } = null!;
    public Texture2D SnakeBody { get; private set; } = null!;
    public Texture2D SnakeTail { get; private set; } = null!;
    
    // 食物资源（3 帧动画）
    public Texture2D FoodFrame0 { get; private set; } = null!;
    public Texture2D FoodFrame1 { get; private set; } = null!;
    public Texture2D FoodFrame2 { get; private set; } = null!;
    
    // 背景资源
    public Texture2D BackgroundGradient { get; private set; } = null!;
    public Texture2D GridTile { get; private set; } = null!;
    
    // UI 资源
    public Texture2D ButtonNormal { get; private set; } = null!;
    public Texture2D ButtonHover { get; private set; } = null!;
    public Texture2D ButtonPressed { get; private set; } = null!;
    public Texture2D ButtonDisabled { get; private set; } = null!;
    
    // 粒子资源
    public Texture2D ParticleCircle { get; private set; } = null!;
    public Texture2D ParticleStar { get; private set; } = null!;
    public Texture2D ParticleSpark { get; private set; } = null!;
    
    // 通用资源
    public Texture2D Pixel { get; private set; } = null!;
    public SpriteFont Font { get; private set; } = null!;
    
    public SpriteRenderer(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _graphicsDevice = graphicsDevice;
        _content = content;
    }
    
    /// <summary>
    /// 加载所有资源
    /// </summary>
    public void LoadContent()
    {
        // 创建白色像素纹理（用于绘制矩形）
        Pixel = new Texture2D(_graphicsDevice, 1, 1);
        Pixel.SetData([Microsoft.Xna.Framework.Color.White]);
        
        // 加载字体
        Font = _content.Load<SpriteFont>("UIFont");
        
        // 加载蛇资源
        SnakeHead = _content.Load<Texture2D>("sprites/snake/snake_head");
        SnakeBody = _content.Load<Texture2D>("sprites/snake/snake_body");
        SnakeTail = _content.Load<Texture2D>("sprites/snake/snake_tail");
        
        // 加载食物资源
        FoodFrame0 = _content.Load<Texture2D>("sprites/food/food_apple_0");
        FoodFrame1 = _content.Load<Texture2D>("sprites/food/food_apple_1");
        FoodFrame2 = _content.Load<Texture2D>("sprites/food/food_apple_2");
        
        // 加载背景资源
        BackgroundGradient = _content.Load<Texture2D>("backgrounds/background_gradient");
        GridTile = _content.Load<Texture2D>("backgrounds/grid_tile");
        
        // 加载 UI 资源
        ButtonNormal = _content.Load<Texture2D>("sprites/ui/button_normal");
        ButtonHover = _content.Load<Texture2D>("sprites/ui/button_hover");
        ButtonPressed = _content.Load<Texture2D>("sprites/ui/button_pressed");
        ButtonDisabled = _content.Load<Texture2D>("sprites/ui/button_disabled");
        
        // 加载粒子资源
        ParticleCircle = _content.Load<Texture2D>("sprites/particles/particle_circle");
        ParticleStar = _content.Load<Texture2D>("sprites/particles/particle_star");
        ParticleSpark = _content.Load<Texture2D>("sprites/particles/particle_spark");
    }
    
    /// <summary>
    /// 绘制背景（全屏渐变）
    /// </summary>
    public void DrawBackground(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(BackgroundGradient, Vector2.Zero, Microsoft.Xna.Framework.Color.White);
    }
    
    /// <summary>
    /// 绘制地面网格（平铺）
    /// </summary>
    public void DrawGrid(SpriteBatch spriteBatch, int boardX, int boardY, int boardWidth, int boardHeight, int cellSize)
    {
        for (int x = 0; x <= boardWidth / cellSize; x++)
        {
            for (int y = 0; y <= boardHeight / cellSize; y++)
            {
                spriteBatch.Draw(
                    GridTile,
                    new Microsoft.Xna.Framework.Rectangle(boardX + x * cellSize, boardY + y * cellSize, cellSize, cellSize),
                    Microsoft.Xna.Framework.Color.White * 0.4f);
            }
        }
    }
    
    /// <summary>
    /// 绘制蛇头
    /// </summary>
    public void DrawSnakeHead(SpriteBatch spriteBatch, Vector2 position, int cellSize, float rotation = 0)
    {
        var origin = new Vector2(SnakeHead.Width / 2f, SnakeHead.Height / 2f);
        spriteBatch.Draw(
            SnakeHead,
            position + new Vector2(cellSize / 2f, cellSize / 2f),
            null,
            Microsoft.Xna.Framework.Color.White,
            rotation,
            origin,
            (float)cellSize / SnakeHead.Height,
            SpriteEffects.None,
            0);
    }
    
    /// <summary>
    /// 绘制蛇身节段
    /// </summary>
    public void DrawSnakeBody(SpriteBatch spriteBatch, Vector2 position, int cellSize)
    {
        var scale = (float)cellSize / SnakeBody.Height;
        var offset = new Vector2((cellSize - SnakeBody.Width * scale) / 2f, (cellSize - SnakeBody.Height * scale) / 2f);
        spriteBatch.Draw(SnakeBody, position + offset, null, Microsoft.Xna.Framework.Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }
    
    /// <summary>
    /// 绘制蛇尾
    /// </summary>
    public void DrawSnakeTail(SpriteBatch spriteBatch, Vector2 position, int cellSize, float rotation = 0)
    {
        var origin = new Vector2(SnakeTail.Width / 2f, SnakeTail.Height / 2f);
        spriteBatch.Draw(
            SnakeTail,
            position + new Vector2(cellSize / 2f, cellSize / 2f),
            null,
            Microsoft.Xna.Framework.Color.White,
            rotation,
            origin,
            (float)cellSize / SnakeTail.Height,
            SpriteEffects.None,
            0);
    }
    
    /// <summary>
    /// 绘制食物（带动画帧选择）
    /// </summary>
    public void DrawFood(SpriteBatch spriteBatch, Vector2 position, int cellSize, int frameIndex)
    {
        var texture = frameIndex switch
        {
            0 => FoodFrame0,
            1 => FoodFrame1,
            2 => FoodFrame2,
            _ => FoodFrame0
        };
        
        var origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
        spriteBatch.Draw(
            texture,
            position + new Vector2(cellSize / 2f, cellSize / 2f),
            null,
            Microsoft.Xna.Framework.Color.White,
            0,
            origin,
            (float)cellSize / texture.Height,
            SpriteEffects.None,
            0);
    }
    
    /// <summary>
    /// 绘制按钮
    /// </summary>
    public void DrawButton(SpriteBatch spriteBatch, Vector2 position, ButtonState state, string text)
    {
        var texture = state switch
        {
            ButtonState.Normal => ButtonNormal,
            ButtonState.Hover => ButtonHover,
            ButtonState.Pressed => ButtonPressed,
            ButtonState.Disabled => ButtonDisabled,
            _ => ButtonNormal
        };
        
        spriteBatch.Draw(texture, position, Microsoft.Xna.Framework.Color.White);
        
        // 绘制文字（居中）
        var textSize = Font.MeasureString(text);
        var textPosition = position + new Vector2(
            (texture.Width - textSize.X) / 2f,
            (texture.Height - textSize.Y) / 2f);
        
        var textColor = state == ButtonState.Disabled ? Microsoft.Xna.Framework.Color.Gray : Microsoft.Xna.Framework.Color.White;
        spriteBatch.DrawString(Font, text, textPosition, textColor);
    }
    
    /// <summary>
    /// 绘制矩形（使用白色像素纹理）
    /// </summary>
    public void DrawRectangle(SpriteBatch spriteBatch, Microsoft.Xna.Framework.Rectangle rectangle, Microsoft.Xna.Framework.Color color)
    {
        spriteBatch.Draw(Pixel, rectangle, color);
    }
    
    /// <summary>
    /// 绘制圆形（使用粒子纹理）
    /// </summary>
    public void DrawCircle(SpriteBatch spriteBatch, Vector2 position, float scale, Microsoft.Xna.Framework.Color color)
    {
        spriteBatch.Draw(ParticleCircle, position, null, color, 0, 
            new Vector2(ParticleCircle.Width / 2f, ParticleCircle.Height / 2f), 
            scale, SpriteEffects.None, 0);
    }
}

/// <summary>
/// 按钮状态枚举
/// </summary>
public enum ButtonState
{
    Normal,
    Hover,
    Pressed,
    Disabled
}
