# Death Protection

一个《杀戮尖塔2》(Slay the Spire 2) 的模组，在玩家即将死亡时提供保护，防止突然暴毙。

## 功能特性

- **死亡保护**：当玩家即将死亡时，自动拦截死亡事件
- **两种保护模式**：
  - **返回主菜单**：存档保留，玩家可以点击"继续游戏"重新开始
  - **自动重开**：自动重新加载存档，回到上一个存档点
- **全面覆盖**：支持所有死亡场景
  - 战斗伤害死亡
  - Doom（死咒）死亡
  - 事件扣血死亡
  - 放血卡牌（如 Bloodletting、Hemokinesis）
  - 毒素/燃烧死亡
  - 最大生命值减少死亡
  - Boss 特殊机制（如沙虫吞噬）
- **不影响正常放弃**：玩家主动放弃游戏时不会被拦截

### 与游戏内遗物/药水的兼容

Mod 会优先让游戏内的死亡保护机制生效，只有在没有任何保护手段时才会触发：

- **蜥蜴尾巴 (Lizard Tail)**：游戏内遗物，死亡时触发可恢复生命值。Mod 会检测到该遗物并让游戏原逻辑执行，不会拦截。
- **瓶中精灵 (Fairy in a Bottle)**：游戏内药水，死亡时自动使用可恢复生命值。Mod 同样会检测并让原逻辑执行。
- **其他死亡保护效果**：Mod 通过 `Hook.ShouldDie` 检测所有可能的死亡保护来源，确保游戏内机制优先生效。

只有当玩家没有任何死亡保护手段时，Mod 才会介入并执行保护逻辑。

## 安装方法

### 前置要求

- 《杀戮尖塔2》游戏
- [GodotModLoader](https://github.com/GodotModLoader/GodotModLoader)（或游戏内置的 Mod 加载器）

### 安装步骤

1. 下载最新版本的 Mod 文件
2. 将 `DeathProtection` 文件夹放入游戏的 `mods` 目录
3. 启动游戏，Mod 会自动加载

### 文件结构

```
Slay the Spire 2/
└── mods/
    └── DeathProtection/
        ├── DeathProtection.json
        ├── DeathProtection.pck
        └── DeathProtection.dll
```

## 配置选项

Mod 启动后，可以在游戏内的 Mod 配置界面进行设置：

| 选项 | 说明 | 默认值 |
|------|------|--------|
| **启用** | 是否启用死亡保护 | 开启 |
| **模式** | 保护模式：返回主菜单 / 自动重开 | 返回主菜单 |

## 工作原理

Mod 使用 Harmony Patch 拦截 `CreatureCmd.Kill` 方法：

1. 检测是否有玩家即将死亡
2. 检测是否所有玩家都会死亡
3. 如果满足条件，阻止死亡事件并执行保护逻辑
4. 根据配置返回主菜单或重新加载存档

## 构建

### 环境要求

- .NET SDK 8.0+
- 游戏安装目录（用于引用游戏 DLL）

### 构建步骤

1. 克隆仓库
2. 修改 `DeathProtection.csproj` 中的游戏路径
3. 运行构建命令：

```bash
dotnet build -c Release
```

构建产物会自动复制到游戏的 `mods/DeathProtection/` 目录。

## 兼容性

- 游戏版本：Slay the Spire 2 Early Access
- 理论上兼容大多数其他 Mod
- 如果与其他修改死亡机制的 Mod 冲突，请提交 Issue

## 已知问题

- 自动重开模式下，如果存档点就在导致死亡的房间，可能会再次触发死亡

## 更新日志

### v1.0.0
- 初始版本
- 支持两种保护模式
- 覆盖所有死亡场景

## 许可证

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request！

---

如果这个 Mod 对你有帮助，欢迎给个 Star ⭐