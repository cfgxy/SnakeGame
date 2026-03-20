using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SnakeGame.Core;
using Color = Microsoft.Xna.Framework.Color;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace SnakeGame.App;

internal sealed class SnakeGameApp : Game
{
    private const int WindowWidth = 1280;
    private const int WindowHeight = 720;
    private const int CellSize = 28;
    private const int MaxBufferedDirections = 2;
    private static readonly TimeSpan BoostDoubleTapWindow = TimeSpan.FromMilliseconds(200);

    private readonly GraphicsDeviceManager graphics;
    private readonly AccelerationResolver accelerationResolver;
    private readonly GameEngine gameEngine;
    private readonly LeaderboardStore leaderboardStore;
    private readonly AudioSettingsService audioSettingsService;
    private readonly IReadOnlyList<LevelDefinition> levels;

    private SpriteBatch spriteBatch = null!;
    private Texture2D pixel = null!;
    private SpriteFont font = null!;
    private KeyboardState previousKeyboard;

    private AppScreen screen = AppScreen.MainMenu;
    private GameSession? session;
    private GameStepResult? lastStepResult;
    private AudioSettings audioSettings;
    private IReadOnlyList<ScoreRecord> leaderboard;
    private TimeSpan stepAccumulator = TimeSpan.Zero;
    private TimeSpan inputClock = TimeSpan.Zero;
    private int menuIndex;
    private int selectedLevelIndex;
    private bool paused;
    private readonly Queue<Direction> bufferedDirections = new();
    private AccelerationState? runtimeAcceleration;

    public SnakeGameApp()
    {
        graphics = new GraphicsDeviceManager(this);
        graphics.PreferredBackBufferWidth = WindowWidth;
        graphics.PreferredBackBufferHeight = WindowHeight;
        graphics.SynchronizeWithVerticalRetrace = true;

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "贪吃蛇 v1.0";

        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SnakeGame");

        accelerationResolver = new AccelerationResolver();
        gameEngine = new GameEngine();
        leaderboardStore = new LeaderboardStore(Path.Combine(dataDirectory, "leaderboard.json"));
        audioSettingsService = new AudioSettingsService(new AudioSettingsFileStore(Path.Combine(dataDirectory, "settings.json")));
        levels = LevelCatalog.Create();
        leaderboard = leaderboardStore.Load();
        audioSettings = audioSettingsService.LoadOrCreateDefaults();
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
        pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData([Color.White]);
        font = Content.Load<SpriteFont>("UIFont");
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        if (IsKeyPressed(keyboard, Keys.F4) && keyboard.IsKeyDown(Keys.LeftAlt))
        {
            Exit();
        }

        switch (screen)
        {
            case AppScreen.MainMenu:
                UpdateMainMenu(keyboard);
                break;
            case AppScreen.LevelSelect:
                UpdateLevelSelect(keyboard);
                break;
            case AppScreen.Leaderboard:
                UpdateLeaderboard(keyboard);
                break;
            case AppScreen.Settings:
                UpdateSettings(keyboard);
                break;
            case AppScreen.Playing:
                UpdatePlaying(gameTime, keyboard);
                break;
            case AppScreen.Result:
                UpdateResult(keyboard);
                break;
        }

        previousKeyboard = keyboard;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 26, 36));

        spriteBatch.Begin();
        DrawBackdrop();

        switch (screen)
        {
            case AppScreen.MainMenu:
                DrawMainMenu();
                break;
            case AppScreen.LevelSelect:
                DrawLevelSelect();
                break;
            case AppScreen.Leaderboard:
                DrawLeaderboard();
                break;
            case AppScreen.Settings:
                DrawSettings();
                break;
            case AppScreen.Playing:
                DrawPlaying();
                break;
            case AppScreen.Result:
                DrawResult();
                break;
        }

        spriteBatch.End();
        base.Draw(gameTime);
    }

    private void UpdateMainMenu(KeyboardState keyboard)
    {
        const int menuCount = 5;

        if (IsKeyPressed(keyboard, Keys.Down))
        {
            menuIndex = (menuIndex + 1) % menuCount;
        }

        if (IsKeyPressed(keyboard, Keys.Up))
        {
            menuIndex = (menuIndex - 1 + menuCount) % menuCount;
        }

        if (!IsKeyPressed(keyboard, Keys.Enter))
        {
            return;
        }

        switch (menuIndex)
        {
            case 0:
                StartGame(GameMode.Story, 1);
                break;
            case 1:
                selectedLevelIndex = 0;
                screen = AppScreen.LevelSelect;
                break;
            case 2:
                leaderboard = leaderboardStore.Load();
                screen = AppScreen.Leaderboard;
                break;
            case 3:
                screen = AppScreen.Settings;
                break;
            case 4:
                Exit();
                break;
        }
    }

    private void UpdateLevelSelect(KeyboardState keyboard)
    {
        if (IsKeyPressed(keyboard, Keys.Down))
        {
            selectedLevelIndex = (selectedLevelIndex + 1) % levels.Count;
        }

        if (IsKeyPressed(keyboard, Keys.Up))
        {
            selectedLevelIndex = (selectedLevelIndex - 1 + levels.Count) % levels.Count;
        }

        if (IsKeyPressed(keyboard, Keys.Escape))
        {
            screen = AppScreen.MainMenu;
            return;
        }

        if (IsKeyPressed(keyboard, Keys.Enter))
        {
            StartGame(GameMode.Practice, levels[selectedLevelIndex].Config.LevelNumber);
        }
    }

    private void UpdateLeaderboard(KeyboardState keyboard)
    {
        if (IsKeyPressed(keyboard, Keys.Escape) || IsKeyPressed(keyboard, Keys.Enter))
        {
            screen = AppScreen.MainMenu;
        }
    }

    private void UpdateSettings(KeyboardState keyboard)
    {
        if (IsKeyPressed(keyboard, Keys.Left) || IsKeyPressed(keyboard, Keys.Right) || IsKeyPressed(keyboard, Keys.Enter))
        {
            audioSettings = audioSettingsService.SetBgmEnabled(!audioSettings.BgmEnabled);
        }

        if (IsKeyPressed(keyboard, Keys.Escape))
        {
            screen = AppScreen.MainMenu;
        }
    }

    private void UpdatePlaying(GameTime gameTime, KeyboardState keyboard)
    {
        if (session is null)
        {
            return;
        }

        if (IsKeyPressed(keyboard, Keys.Escape))
        {
            session = null;
            screen = AppScreen.MainMenu;
            paused = false;
            stepAccumulator = TimeSpan.Zero;
            inputClock = TimeSpan.Zero;
            bufferedDirections.Clear();
            runtimeAcceleration = null;
            return;
        }

        if (IsKeyPressed(keyboard, Keys.P))
        {
            paused = !paused;
        }

        if (paused)
        {
            return;
        }

        stepAccumulator += gameTime.ElapsedGameTime;
        inputClock += gameTime.ElapsedGameTime;
        var currentLevel = session.Levels.Single(level => level.Config.LevelNumber == session.CurrentLevelNumber).Config;

        var pressedDirection = GetPressedDirection(keyboard);
        if (pressedDirection.HasValue)
        {
            BufferDirectionInput(pressedDirection.Value);
        }

        var heldDirection = GetHeldDirection(keyboard);
        UpdateAccelerationState(pressedDirection, heldDirection, currentLevel);
        var currentSpeedMultiplier = runtimeAcceleration?.IsBoosting == true ? currentLevel.BoostMultiplier : 1d;
        var stepInterval = TimeSpan.FromTicks((long)(currentLevel.BaseStepInterval.Ticks / currentSpeedMultiplier));

        while (stepAccumulator >= stepInterval && screen == AppScreen.Playing && session.Status == GameSessionStatus.Running)
        {
            stepAccumulator -= stepInterval;

            var bufferedDirection = DequeueBufferedDirection();

            var result = gameEngine.Step(
                session,
                new GameStepInput(bufferedDirection, heldDirection, session.Elapsed + currentLevel.BaseStepInterval));

            if (result.Status == GameStepStatus.LevelAdvanced)
            {
                runtimeAcceleration = result.Session.Acceleration;
            }

            session = ApplyRuntimeAcceleration(result.Session);
            lastStepResult = result with
            {
                Session = session,
                SpeedMultiplier = currentSpeedMultiplier
            };

            if (result.ScoreRecordToPersist is not null)
            {
                leaderboardStore.Save(result.ScoreRecordToPersist);
                leaderboard = leaderboardStore.Load();
            }

            if (result.Status is GameStepStatus.GameOver or GameStepStatus.Completed)
            {
                screen = AppScreen.Result;
                paused = false;
                break;
            }
        }
    }

    private void UpdateResult(KeyboardState keyboard)
    {
        if (IsKeyPressed(keyboard, Keys.Enter) || IsKeyPressed(keyboard, Keys.Escape))
        {
            session = null;
            lastStepResult = null;
            inputClock = TimeSpan.Zero;
            bufferedDirections.Clear();
            runtimeAcceleration = null;
            screen = AppScreen.MainMenu;
        }
    }

    private void StartGame(GameMode mode, int startLevelNumber)
    {
        session = gameEngine.CreateSession(new GameSessionSeed(
            "玩家一",
            mode,
            LevelCatalog.BoardSize,
            levels,
            startLevelNumber));

        lastStepResult = null;
        paused = false;
        stepAccumulator = TimeSpan.Zero;
        inputClock = TimeSpan.Zero;
        bufferedDirections.Clear();
        runtimeAcceleration = session.Acceleration;
        session = ApplyRuntimeAcceleration(session);
        screen = session.Status == GameSessionStatus.GameOver ? AppScreen.Result : AppScreen.Playing;
    }

    private void DrawBackdrop()
    {
        DrawRectangle(new Rectangle(0, 0, WindowWidth, WindowHeight), new Color(10, 18, 26));
        DrawRectangle(new Rectangle(40, 40, WindowWidth - 80, WindowHeight - 80), new Color(20, 32, 44));
    }

    private void DrawMainMenu()
    {
        DrawTitle("贪吃蛇 v1.0");

        var options = new[]
        {
            "开始闯关",
            "选关练习",
            "排行榜",
            "设置",
            "退出"
        };

        DrawMenuList(options, menuIndex, 240);
        DrawFooter("上下选择  回车确认");
    }

    private void DrawLevelSelect()
    {
        DrawTitle("请选择关卡");
        var options = levels
            .Select(level => $"第 {level.Config.LevelNumber} 关  目标长度 {level.Config.TargetLength}")
            .ToArray();

        DrawMenuList(options, selectedLevelIndex, 220);
        DrawFooter("回车开始练习  Esc 返回");
    }

    private void DrawLeaderboard()
    {
        DrawTitle("排行榜");
        var y = 180f;

        if (leaderboard.Count == 0)
        {
            spriteBatch.DrawString(font, "当前还没有记录", new Vector2(420, y), Color.White);
        }
        else
        {
            for (var index = 0; index < leaderboard.Count; index++)
            {
                var item = leaderboard[index];
                var line = $"{index + 1}. {item.PlayerName}  分数 {item.Score}  到达第 {item.ReachedLevel} 关";
                spriteBatch.DrawString(font, line, new Vector2(280, y), Color.White);
                y += 34f;
            }
        }

        DrawFooter("回车或 Esc 返回");
    }

    private void DrawSettings()
    {
        DrawTitle("设置");
        var bgmText = audioSettings.BgmEnabled ? "BGM: 开启" : "BGM: 关闭";
        spriteBatch.DrawString(font, bgmText, new Vector2(420, 260), new Color(255, 226, 124));
        spriteBatch.DrawString(font, "左右键或回车切换", new Vector2(420, 320), Color.White);
        DrawFooter("Esc 返回");
    }

    private void DrawPlaying()
    {
        if (session is null)
        {
            return;
        }

        var boardWidth = session.BoardSize.Width * CellSize;
        var boardHeight = session.BoardSize.Height * CellSize;
        const int boardX = 130;
        const int boardY = 110;

        DrawRectangle(new Rectangle(boardX - 8, boardY - 8, boardWidth + 16, boardHeight + 16), new Color(33, 48, 63));
        DrawRectangle(new Rectangle(boardX, boardY, boardWidth, boardHeight), new Color(16, 28, 38));

        DrawGrid(boardX, boardY, session.BoardSize);

        foreach (var obstacle in session.FixedObstacles)
        {
            DrawCell(boardX, boardY, obstacle, new Color(235, 167, 84));
        }

        foreach (var obstacle in session.MovingObstacles)
        {
            DrawCell(boardX, boardY, obstacle.Track[obstacle.TrackIndex], new Color(232, 110, 82));
        }

        if (session.ApplePosition.HasValue)
        {
            DrawCell(boardX, boardY, session.ApplePosition.Value, new Color(211, 61, 81));
        }

        for (var index = session.SnakeSegments.Count - 1; index >= 0; index--)
        {
            var color = index == 0 ? new Color(121, 231, 164) : new Color(63, 180, 122);
            DrawCell(boardX, boardY, session.SnakeSegments[index], color);
        }

        DrawHud(session, boardX + boardWidth + 40, boardY);

        if (paused)
        {
            DrawOverlayLabel("暂停中");
        }
    }

    private void DrawResult()
    {
        DrawPlaying();

        if (session is null)
        {
            return;
        }

        var title = session.Status == GameSessionStatus.Completed ? "闯关完成" : "游戏结束";
        var footer = session.Mode == GameMode.Story ? "回车返回主菜单" : "练习结束  回车返回主菜单";
        DrawOverlayPanel(title, $"分数 {session.Score}  当前第 {session.CurrentLevelNumber} 关", footer);
    }

    private void DrawHud(GameSession currentSession, int x, int y)
    {
        var currentLevel = currentSession.Levels.Single(level => level.Config.LevelNumber == currentSession.CurrentLevelNumber).Config;
        var modeText = currentSession.Mode == GameMode.Story ? "闯关模式" : "练习模式";
        var lines = new[]
        {
            $"当前关卡: {currentSession.CurrentLevelNumber}",
            $"模式: {modeText}",
            $"分数: {currentSession.Score}",
            $"长度: {currentSession.SnakeSegments.Count}",
            $"目标: {currentLevel.TargetLength}",
            $"BGM: {(audioSettings.BgmEnabled ? "开启" : "关闭")}",
            paused ? "状态: 暂停中" : "状态: 游戏中",
            currentSession.Acceleration.IsBoosting ? "速度: 加速中" : "速度: 普通",
            "方向键 / WASD 移动",
            "P 暂停  Esc 主菜单"
        };

        for (var index = 0; index < lines.Length; index++)
        {
            spriteBatch.DrawString(font, lines[index], new Vector2(x, y + (index * 34)), Color.White);
        }
    }

    private void DrawGrid(int boardX, int boardY, GridSize boardSize)
    {
        for (var x = 0; x <= boardSize.Width; x++)
        {
            DrawRectangle(new Rectangle(boardX + (x * CellSize), boardY, 1, boardSize.Height * CellSize), new Color(34, 52, 66));
        }

        for (var y = 0; y <= boardSize.Height; y++)
        {
            DrawRectangle(new Rectangle(boardX, boardY + (y * CellSize), boardSize.Width * CellSize, 1), new Color(34, 52, 66));
        }
    }

    private void DrawCell(int boardX, int boardY, GridPosition position, Color color)
    {
        DrawRectangle(
            new Rectangle(boardX + (position.X * CellSize) + 2, boardY + (position.Y * CellSize) + 2, CellSize - 4, CellSize - 4),
            color);
    }

    private void DrawMenuList(IReadOnlyList<string> options, int activeIndex, float startY)
    {
        for (var index = 0; index < options.Count; index++)
        {
            var color = index == activeIndex ? new Color(255, 226, 124) : Color.White;
            var prefix = index == activeIndex ? ">" : " ";
            spriteBatch.DrawString(font, $"{prefix} {options[index]}", new Vector2(420, startY + (index * 52)), color);
        }
    }

    private void DrawTitle(string text)
    {
        spriteBatch.DrawString(font, text, new Vector2(420, 110), new Color(121, 231, 164));
    }

    private void DrawFooter(string text)
    {
        spriteBatch.DrawString(font, text, new Vector2(390, 620), new Color(189, 206, 220));
    }

    private void DrawOverlayLabel(string text)
    {
        DrawOverlayPanel(text, string.Empty, string.Empty);
    }

    private void DrawOverlayPanel(string title, string message, string footer)
    {
        var box = new Rectangle(350, 230, 540, 220);
        DrawRectangle(box, new Color(10, 18, 26) * 0.95f);
        DrawRectangle(new Rectangle(box.X, box.Y, box.Width, 2), new Color(255, 226, 124));
        spriteBatch.DrawString(font, title, new Vector2(box.X + 40, box.Y + 30), new Color(255, 226, 124));

        if (!string.IsNullOrWhiteSpace(message))
        {
            spriteBatch.DrawString(font, message, new Vector2(box.X + 40, box.Y + 95), Color.White);
        }

        if (!string.IsNullOrWhiteSpace(footer))
        {
            spriteBatch.DrawString(font, footer, new Vector2(box.X + 40, box.Y + 155), new Color(189, 206, 220));
        }
    }

    private void DrawRectangle(Rectangle rectangle, Color color)
    {
        spriteBatch.Draw(pixel, rectangle, color);
    }

    // 低速关卡里，转向输入需要跨帧缓存，否则很容易在一次逻辑步进之间被采样丢失。
    private void BufferDirectionInput(Direction direction)
    {
        var referenceDirection = bufferedDirections.Count > 0
            ? bufferedDirections.Last()
            : session?.CurrentDirection;

        if (referenceDirection.HasValue)
        {
            if (direction == referenceDirection.Value || IsOpposite(direction, referenceDirection.Value))
            {
                return;
            }
        }

        if (bufferedDirections.Count >= MaxBufferedDirections)
        {
            return;
        }

        bufferedDirections.Enqueue(direction);
    }

    private Direction? DequeueBufferedDirection()
    {
        return bufferedDirections.Count > 0 ? bufferedDirections.Dequeue() : null;
    }

    // 双击加速要按真实按键时间判定，不能跟蛇移动一步才结算一次，否则慢速关卡永远触发不了。
    private void UpdateAccelerationState(Direction? pressedDirection, Direction? heldDirection, LevelConfig currentLevel)
    {
        if (session is null)
        {
            return;
        }

        runtimeAcceleration ??= session.Acceleration;
        var result = accelerationResolver.Resolve(
            runtimeAcceleration,
            new AccelerationRequest(
                session.CurrentDirection,
                pressedDirection,
                heldDirection,
                inputClock,
                BoostDoubleTapWindow,
                currentLevel.BoostMultiplier));

        runtimeAcceleration = result.State;
        session = ApplyRuntimeAcceleration(session);
    }

    private GameSession ApplyRuntimeAcceleration(GameSession currentSession)
    {
        if (runtimeAcceleration is null)
        {
            return currentSession;
        }

        runtimeAcceleration = runtimeAcceleration with { CurrentDirection = currentSession.CurrentDirection };
        return currentSession with { Acceleration = runtimeAcceleration };
    }

    private Direction? GetPressedDirection(KeyboardState keyboard)
    {
        if (IsKeyPressed(keyboard, Keys.Up) || IsKeyPressed(keyboard, Keys.W))
        {
            return Direction.Up;
        }

        if (IsKeyPressed(keyboard, Keys.Down) || IsKeyPressed(keyboard, Keys.S))
        {
            return Direction.Down;
        }

        if (IsKeyPressed(keyboard, Keys.Left) || IsKeyPressed(keyboard, Keys.A))
        {
            return Direction.Left;
        }

        if (IsKeyPressed(keyboard, Keys.Right) || IsKeyPressed(keyboard, Keys.D))
        {
            return Direction.Right;
        }

        return null;
    }

    private static Direction? GetHeldDirection(KeyboardState keyboard)
    {
        if (keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.W))
        {
            return Direction.Up;
        }

        if (keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.S))
        {
            return Direction.Down;
        }

        if (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A))
        {
            return Direction.Left;
        }

        if (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D))
        {
            return Direction.Right;
        }

        return null;
    }

    private bool IsKeyPressed(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) && previousKeyboard.IsKeyUp(key);
    }

    private static bool IsOpposite(Direction inputDirection, Direction referenceDirection)
    {
        return (inputDirection, referenceDirection) switch
        {
            (Direction.Up, Direction.Down) => true,
            (Direction.Down, Direction.Up) => true,
            (Direction.Left, Direction.Right) => true,
            (Direction.Right, Direction.Left) => true,
            _ => false
        };
    }
}

