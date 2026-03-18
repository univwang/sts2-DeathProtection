namespace DeathProtection;

/// <summary>
/// 死亡保护模式
/// </summary>
public enum ProtectionMode
{
    /// <summary>
    /// 返回主菜单（存档保留，可点击"继续游戏"）
    /// </summary>
    ReturnToMenu,

    /// <summary>
    /// 自动重开（重新加载存档）
    /// </summary>
    AutoRestart
}

/// <summary>
/// 死亡保护配置
/// </summary>
public static class DeathProtectionConfig
{
    /// <summary>
    /// 是否启用死亡保护
    /// </summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>
    /// 保护模式
    /// </summary>
    public static ProtectionMode Mode { get; set; } = ProtectionMode.ReturnToMenu;
}