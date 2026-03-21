using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SnakeGame.App.Rendering;

/// <summary>
/// 图层管理器 - 控制渲染顺序和图层开关
/// </summary>
public sealed class LayerManager
{
    private readonly List<IRenderLayer> _layers = new();
    
    /// <summary>
    /// 添加图层（按添加顺序渲染）
    /// </summary>
    public void AddLayer(IRenderLayer layer)
    {
        _layers.Add(layer);
    }
    
    /// <summary>
    /// 渲染所有图层
    /// </summary>
    public void DrawAll(SpriteBatch spriteBatch, GameTime gameTime)
    {
        foreach (var layer in _layers)
        {
            if (layer.IsEnabled)
            {
                layer.Draw(spriteBatch, gameTime);
            }
        }
    }
    
    /// <summary>
    /// 清除所有图层
    /// </summary>
    public void Clear()
    {
        _layers.Clear();
    }
}

/// <summary>
/// 渲染图层接口
/// </summary>
public interface IRenderLayer
{
    /// <summary>
    /// 图层是否启用
    /// </summary>
    bool IsEnabled { get; set; }
    
    /// <summary>
    /// 图层名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 绘制图层
    /// </summary>
    void Draw(SpriteBatch spriteBatch, GameTime gameTime);
}

/// <summary>
/// 背景图层
/// </summary>
public sealed class BackgroundLayer : IRenderLayer
{
    private readonly SpriteRenderer _renderer;
    
    public bool IsEnabled { get; set; } = true;
    public string Name => "Background";
    
    public BackgroundLayer(SpriteRenderer renderer)
    {
        _renderer = renderer;
    }
    
    public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        _renderer.DrawBackground(spriteBatch);
    }
}

/// <summary>
/// 地面网格图层
/// </summary>
public sealed class GridLayer : IRenderLayer
{
    private readonly SpriteRenderer _renderer;
    private readonly int _boardX;
    private readonly int _boardY;
    private readonly int _boardWidth;
    private readonly int _boardHeight;
    private readonly int _cellSize;
    
    public bool IsEnabled { get; set; } = true;
    public string Name => "Grid";
    
    public GridLayer(SpriteRenderer renderer, int boardX, int boardY, int boardWidth, int boardHeight, int cellSize)
    {
        _renderer = renderer;
        _boardX = boardX;
        _boardY = boardY;
        _boardWidth = boardWidth;
        _boardHeight = boardHeight;
        _cellSize = cellSize;
    }
    
    public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        _renderer.DrawGrid(spriteBatch, _boardX, _boardY, _boardWidth, _boardHeight, _cellSize);
    }
}

/// <summary>
/// 食物图层
/// </summary>
public sealed class FoodLayer : IRenderLayer
{
    private readonly SpriteRenderer _renderer;
    private readonly Func<Vector2?> _getFoodPosition;
    private readonly Func<int> _getFoodFrame;
    private readonly int _cellSize;
    private readonly int _boardX;
    private readonly int _boardY;
    
    public bool IsEnabled { get; set; } = true;
    public string Name => "Food";
    
    public FoodLayer(
        SpriteRenderer renderer,
        Func<Vector2?> getFoodPosition,
        Func<int> getFoodFrame,
        int cellSize,
        int boardX,
        int boardY)
    {
        _renderer = renderer;
        _getFoodPosition = getFoodPosition;
        _getFoodFrame = getFoodFrame;
        _cellSize = cellSize;
        _boardX = boardX;
        _boardY = boardY;
    }
    
    public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        var position = _getFoodPosition();
        if (position.HasValue)
        {
            var drawPos = new Vector2(_boardX + position.Value.X * _cellSize, _boardY + position.Value.Y * _cellSize);
            _renderer.DrawFood(spriteBatch, drawPos, _cellSize, _getFoodFrame());
        }
    }
}

/// <summary>
/// 蛇图层
/// </summary>
public sealed class SnakeLayer : IRenderLayer
{
    private readonly SpriteRenderer _renderer;
    private readonly Func<IReadOnlyList<Vector2>> _getSnakeSegments;
    private readonly Func<Vector2> _getHeadDirection;
    private readonly int _cellSize;
    private readonly int _boardX;
    private readonly int _boardY;
    
    public bool IsEnabled { get; set; } = true;
    public string Name => "Snake";
    
    public SnakeLayer(
        SpriteRenderer renderer,
        Func<IReadOnlyList<Vector2>> getSnakeSegments,
        Func<Vector2> getHeadDirection,
        int cellSize,
        int boardX,
        int boardY)
    {
        _renderer = renderer;
        _getSnakeSegments = getSnakeSegments;
        _getHeadDirection = getHeadDirection;
        _cellSize = cellSize;
        _boardX = boardX;
        _boardY = boardY;
    }
    
    public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        var segments = _getSnakeSegments();
        if (segments.Count == 0) return;
        
        // 绘制蛇尾
        _renderer.DrawSnakeTail(spriteBatch, 
            new Vector2(_boardX + segments[^1].X * _cellSize, _boardY + segments[^1].Y * _cellSize),
            _cellSize);
        
        // 绘制蛇身（从尾到头，不包括头和尾）
        for (int i = segments.Count - 2; i > 0; i--)
        {
            _renderer.DrawSnakeBody(spriteBatch,
                new Vector2(_boardX + segments[i].X * _cellSize, _boardY + segments[i].Y * _cellSize),
                _cellSize);
        }
        
        // 绘制蛇头
        var headPos = new Vector2(_boardX + segments[0].X * _cellSize, _boardY + segments[0].Y * _cellSize);
        var direction = _getHeadDirection();
        var rotation = direction switch
        {
            var d when d == new Vector2(0, -1) => 0,          // 上
            var d when d == new Vector2(1, 0) => MathF.PI / 2,  // 右
            var d when d == new Vector2(0, 1) => MathF.PI,     // 下
            var d when d == new Vector2(-1, 0) => -MathF.PI / 2, // 左
            _ => 0
        };
        
        _renderer.DrawSnakeHead(spriteBatch, headPos, _cellSize, rotation);
    }
}
