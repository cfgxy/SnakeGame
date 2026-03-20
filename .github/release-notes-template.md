# SnakeGame __VERSION__

## 版本定位

- 中文桌面版贪吃蛇 `V1.0`
- `VibeCoding + TDD` 实验性项目阶段成果
- 技术栈：`C# + .NET 8 + MonoGame`

## 本次发布内容

- 发布平台：`Windows x64`
- 安装包：`__PACKAGE_NAME__`
- 校验文件：`__CHECKSUM_NAME__`

## 主要功能

- 多关卡闯关模式与选关练习模式
- 固定障碍与移动障碍
- 苹果可达性与吃后存活性校验
- 基于目标长度的进关机制
- 双击并按住当前方向触发加速
- 本地排行榜与音频设置持久化

## 代码结构

- `SnakeGame.Core`：核心规则、状态推进与策略服务
- `SnakeGame.App`：MonoGame 桌面应用层与界面渲染
- `SnakeGame.Core.Tests`：核心层单元测试

## 测试情况

- `SnakeGame.Core` 单元测试：`49/49` 通过
- 行覆盖率：`98.01%`
- 分支覆盖率：`93.47%`

## 使用说明

1. 下载 `__PACKAGE_NAME__`
2. 解压后运行 `SnakeGame.App.exe`
3. 使用 `方向键` 或 `WASD` 控制蛇移动

## 已知边界

- 当前单元测试覆盖范围限定为 `SnakeGame.Core`
- `SnakeGame.App` 自动化测试缺失已登记为 `V1.1` 技术债

## 关联文档

- `README.md`
- `docs/V1.0 发版声明.md`
- `docs/V1.1 技术债与版本发布计划.md`
