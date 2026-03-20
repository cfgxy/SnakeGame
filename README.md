# SnakeGame

一个基于 `MonoGame + C# + .NET 8` 的中文贪吃蛇实验项目。

这是一个 **VibeCoding + TDD** 的实验性仓库：  
项目通过“先明确规则与测试，再逐步实现”的方式推进，当前 `V1.0` 以核心玩法落地为主，`SnakeGame.Core` 使用单元测试保护核心规则，`SnakeGame.App` 当前暂不纳入单元测试覆盖范围。

## 项目特性

- 中文界面与中文文档
- 多关卡闯关模式与选关练习模式
- 固定障碍与移动障碍
- 苹果可达性与吃后存活性校验
- 基于目标长度的进关机制
- 快速双击并按住当前方向触发加速
- 本地排行榜与音频设置持久化
- GitHub CI 与 Tag Release 发布流程

## 技术架构

项目按“核心规则”和“应用表现”分层：

### `src/SnakeGame.Core`

负责纯业务规则，不依赖 MonoGame：

- 棋盘与会话模型
- 苹果刷新与可达性校验
- 关卡容量校验与进关规则
- 移动障碍状态机
- 双击加速判定
- 分数提交策略与音频设置服务
- `GameEngine` 核心状态推进

### `src/SnakeGame.App`

负责 MonoGame 桌面应用层：

- 程序启动与崩溃日志
- 菜单、选关、暂停、结果界面
- 棋盘绘制、HUD 绘制、中文字体资源
- 本地排行榜文件存储
- 本地音频设置文件存储
- 关卡目录与内容管线

### `tests/SnakeGame.Core.Tests`

当前仅覆盖核心层 `SnakeGame.Core`：

- 苹果生成
- 关卡容量校验
- 进关规则
- 移动障碍解析
- 双击加速解析
- `GameEngine` 主流程
- 分数策略与音频设置服务

## 环境要求

- Windows
- `.NET 8 SDK`
- MonoGame 内容管线本地工具（已通过 `.config/dotnet-tools.json` 管理）

## 编译与运行

### 1. 还原本地工具

```powershell
dotnet tool restore
```

### 2. 编译项目

```powershell
dotnet build SnakeGame.sln -c Release
```

### 3. 启动游戏

```powershell
dotnet run --project src\SnakeGame.App\SnakeGame.App.csproj
```

如果只想直接运行已编译产物，可执行：

```powershell
src\SnakeGame.App\bin\Release\net8.0-windows\SnakeGame.App.exe
```

## 运行测试

当前测试范围限定为 `SnakeGame.Core`：

```powershell
dotnet test tests\SnakeGame.Core.Tests\SnakeGame.Core.Tests.csproj -c Release
```

## 游戏玩法

- `方向键` / `WASD`：移动蛇头
- 快速双击并按住“当前前进方向”的按键：进入 `x2` 加速
- 松开该方向键：恢复当前关卡基础速度
- `P`：暂停 / 继续
- `Esc`：返回主菜单
- 吃到苹果会增长并加分
- 达到当前关卡目标长度后自动进入下一关
- 第 `5` 关开始出现移动障碍：
  - 蛇头主动撞上会直接失败
  - 蛇身接触时障碍会暂停，脱离后继续沿原方向移动

## 仓库结构

```text
SnakeGame
├─ docs
├─ src
│  ├─ SnakeGame.App
│  └─ SnakeGame.Core
├─ tests
│  └─ SnakeGame.Core.Tests
└─ .github
   └─ workflows
```

## 发布方式

仓库内已提供 GitHub Actions：

- `CI`：在提交和 PR 时自动构建并运行核心层测试
- `Release`：推送形如 `v1.0.0` 的 Tag 后，自动打包并发布到 GitHub Releases

## 当前版本说明

- 当前基线版本：`V1.0`
- 核心层测试覆盖率较高，可作为后续迭代基线
- 应用层自动化测试缺失已登记为 `V1.1` 技术债，详见 `docs/V1.1 技术债与版本发布计划.md`

## 实验性说明

这是一个以“AI 协作编码流程”和“TDD 驱动核心规则落地”为目标的实验仓库。  
它更关注：

- 规则是否清晰
- 测试是否能保护核心逻辑
- 从需求、计划、测试到实现的链路是否可追踪

而不是一开始就追求完整商业化打磨。
