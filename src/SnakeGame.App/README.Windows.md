# SnakeGame - Windows 版

## 🎮 运行说明

### 重要：正确的解压方式

**❌ 错误做法**（会导致游戏无法运行）:
```
SnakeGame-Windows-x64.zip
  └── 解压到当前文件夹
      └── SnakeGame-Windows-x64/  ← 多了一层！
          └── SnakeGame-Windows-x64/
              └── SnakeGame.App.exe
```

**✅ 正确做法**:
```
SnakeGame-Windows-x64.zip
  └── 解压到新文件夹（例如 D:\Games\SnakeGame\）
      └── SnakeGame.App.exe  ← 直接在这里
      └── Content/
```

### 步骤

1. **创建新文件夹**
   ```
   D:\Games\SnakeGame\
   ```

2. **解压 ZIP 到新文件夹**
   - 右键 `SnakeGame-Windows-x64.zip`
   - 选择"全部解压缩"
   - 目标路径：`D:\Games\SnakeGame\`

3. **运行游戏**
   ```
   D:\Games\SnakeGame\SnakeGame.App.exe
   ```

### 文件结构

正确的文件结构应该是：

```
SnakeGame/
├── SnakeGame.App.exe      ← 游戏主程序
├── Content/
│   ├── UIFont.xnb         ← 字体文件
│   ├── sprites/
│   │   ├── snake/
│   │   ├── food/
│   │   └── ui/
│   └── backgrounds/
└── README.Windows.md      ← 本文件
```

### 常见问题

**Q: 双击没反应怎么办？**

A: 请检查文件结构是否正确。如果路径是 `SnakeGame-Windows-x64\SnakeGame-Windows-x64\` 说明解压了两次，需要重新解压。

**Q: 如何查看错误日志？**

A: 日志位置：
```
%APPDATA%\SnakeGame\logs\startup.log
%APPDATA%\SnakeGame\logs\crash-*.log
```

打开方法：
1. 按 `Win + R`
2. 输入：`%APPDATA%\SnakeGame\logs\`
3. 按回车

**Q: 命令行运行**

A: 如果想看详细错误，可以用命令行运行：
```powershell
cd D:\Games\SnakeGame
.\SnakeGame.App.exe
```

### 系统要求

- **操作系统**: Windows 10/11 x64
- **.NET**: 已自包含，无需单独安装
- **显卡**: 支持 DirectX 11

### 控制

- **方向键** 或 **WASD**: 控制蛇的移动
- **P**: 暂停/继续
- **Esc**: 返回主菜单

---

**游戏版本**: V1.0.1  
**发布日期**: 2026-03-21
