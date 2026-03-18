using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace DeathProtection.Patches;

/// <summary>
/// 死亡保护 Patch
/// 拦截 CreatureCmd.Kill 方法，在所有玩家死亡时阻止游戏结束并执行保护逻辑
/// </summary>
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Kill), typeof(IReadOnlyCollection<Creature>), typeof(bool))]
public static class DeathProtectionPatch
{
    /// <summary>
    /// 是否正在执行保护逻辑
    /// </summary>
    private static bool _isProtecting = false;

    /// <summary>
    /// Prefix Patch：在 Kill 方法执行前检查
    /// </summary>
    [HarmonyPrefix]
    public static bool Prefix(IReadOnlyCollection<Creature> creatures, bool force, ref Task __result)
    {
        if (!DeathProtectionConfig.Enabled || _isProtecting)
        {
            return true; // 允许原始方法执行
        }

        // 如果是放弃游戏，允许原始方法执行
        if (RunManager.Instance.IsAbandoned)
        {
            return true;
        }

        // 检查是否有玩家在 creatures 中
        bool hasPlayer = false;
        foreach (var creature in creatures)
        {
            if (creature.IsPlayer)
            {
                hasPlayer = true;
                break;
            }
        }

        if (!hasPlayer)
        {
            return true; // 没有玩家，允许原始方法执行
        }

        // 获取 runState
        IRunState? runState = null;
        foreach (var creature in creatures)
        {
            if (creature.IsPlayer)
            {
                runState = creature.Player?.RunState;
                break;
            }
        }

        if (runState == null)
        {
            return true;
        }

        // 检查是否所有玩家都会死亡（当前 HP <= 0 或者已经在 creatures 中）
        bool allPlayersWillDie = true;
        foreach (var player in runState.Players)
        {
            if (player.Creature.CurrentHp > 0 && !creatures.Contains(player.Creature))
            {
                allPlayersWillDie = false;
                break;
            }
        }

        if (!allPlayersWillDie)
        {
            return true; // 不是所有玩家死亡，允许原始方法执行
        }

        MainFile.Logger.Info("[DeathProtection] All players will die! Activating protection...");

        // 标记正在保护
        _isProtecting = true;

        // 替换原始方法的执行结果
        __result = ExecuteProtectionAsync(creatures, force);

        // 阻止原始方法执行
        return false;
    }

    /// <summary>
    /// 执行保护逻辑（替代原始 Kill 方法的执行）
    /// </summary>
    private static async Task ExecuteProtectionAsync(IReadOnlyCollection<Creature> creatures, bool force)
    {
        try
        {
            // 只对怪物执行死亡逻辑，不对玩家执行
            foreach (Creature creature in creatures.ToList())
            {
                if (!creature.IsPlayer)
                {
                    await KillWithoutCheckingWinCondition(creature, force);
                }
            }

            // 根据配置执行保护逻辑
            switch (DeathProtectionConfig.Mode)
            {
                case ProtectionMode.ReturnToMenu:
                    await ReturnToMenu();
                    break;
                case ProtectionMode.AutoRestart:
                    await AutoRestart();
                    break;
            }
        }
        finally
        {
            _isProtecting = false;
        }
    }

    /// <summary>
    /// 调用 KillWithoutCheckingWinCondition（通过反射）
    /// </summary>
    private static async Task KillWithoutCheckingWinCondition(Creature creature, bool force)
    {
        var method = typeof(CreatureCmd).GetMethod("KillWithoutCheckingWinCondition",
            BindingFlags.NonPublic | BindingFlags.Static);
        if (method != null)
        {
            var task = (Task?)method.Invoke(null, new object[] { creature, force, 0 });
            if (task != null)
            {
                await task;
            }
        }
    }

    /// <summary>
    /// 返回主菜单（存档保留，玩家可以点击"继续游戏"）
    /// </summary>
    private static async Task ReturnToMenu()
    {
        MainFile.Logger.Info("[DeathProtection] Returning to main menu...");

        // 清理战斗状态
        if (CombatManager.Instance.IsInProgress)
        {
            CombatManager.Instance.LoseCombat();
        }

        // 停止音乐
        NRun.Instance?.RunMusicController?.StopMusic();

        // 返回主菜单
        await NGame.Instance.ReturnToMainMenu();

        MainFile.Logger.Info("[DeathProtection] Returned to main menu. You can click 'Continue' to resume.");
    }

    /// <summary>
    /// 自动重开（重新加载存档）
    /// </summary>
    private static async Task AutoRestart()
    {
        MainFile.Logger.Info("[DeathProtection] Auto-restarting run...");

        // 检查是否有存档
        if (!SaveManager.Instance.HasRunSave)
        {
            MainFile.Logger.Warn("[DeathProtection] No save found, returning to main menu.");
            await ReturnToMenu();
            return;
        }

        // 检查是否为单机模式
        if (RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
        {
            MainFile.Logger.Warn("[DeathProtection] Not in singleplayer mode, returning to main menu.");
            await ReturnToMenu();
            return;
        }

        try
        {
            // 清理当前状态
            RunManager.Instance.ActionQueueSet.Reset();
            NRunMusicController.Instance.StopMusic();
            RunManager.Instance.CleanUp();

            MainFile.Logger.Info("[DeathProtection] Cleaned up, loading save...");

            // 加载存档
            ReadSaveResult<SerializableRun> runSave = SaveManager.Instance.LoadRunSave();
            SerializableRun serializableRun = runSave.SaveData;
            RunState runState = RunState.FromSerializable(serializableRun);

            MainFile.Logger.Info($"[DeathProtection] Loaded save, continuing with character: {serializableRun.Players[0].CharacterId}");

            // 重新设置并加载游戏
            RunManager.Instance.SetUpSavedSinglePlayer(runState, serializableRun);
            NGame.Instance.ReactionContainer.InitializeNetworking(new NetSingleplayerGameService());
            await NGame.Instance.LoadRun(runState, serializableRun.PreFinishedRoom);

            MainFile.Logger.Info("[DeathProtection] Run restarted successfully!");
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Error($"[DeathProtection] Failed to restart run: {ex.Message}");
            await ReturnToMenu();
        }
    }
}