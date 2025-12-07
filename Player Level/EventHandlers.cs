using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using System.Collections.Generic;
using Exiled.API.Enums;
using PlayerRoles;
using MEC;

namespace KillExpSystem
{
    public class EventHandlers
    {
        private readonly PlayerDataManager dataManager;
        private Dictionary<string, string> originalPlayerNames;

        public EventHandlers(PlayerDataManager dataManager)
        {
            this.dataManager = dataManager;
            this.originalPlayerNames = new Dictionary<string, string>();
        }

        public void OnPlayerVerified(VerifiedEventArgs ev)
        {
            if (ev.Player == null) return;

            var plugin = KillExpSystem.Instance;
            if (plugin == null) return;

            // 保存玩家原始名称
            string originalName = ev.Player.Nickname;
            originalPlayerNames[ev.Player.UserId] = originalName;

            // 获取玩家数据
            string playerId = ev.Player.UserId;
            var playerData = dataManager.GetPlayerData(playerId);
            playerData.PlayerName = originalName;

            // 更新玩家显示名称
            UpdatePlayerDisplayName(ev.Player, playerData);

            if (plugin.Config.Debug)
            {
                Log.Debug($"玩家验证: {originalName} -> [{playerData.Level}]{originalName}");
            }
        }

        public void OnPlayerDying(DyingEventArgs ev)
        {
            if (ev.Player == null || ev.Attacker == null) return;
            if (ev.Player == ev.Attacker) return;

            var plugin = KillExpSystem.Instance;
            if (plugin == null) return;

            // 获取攻击者数据
            string attackerId = ev.Attacker.UserId;
            var attackerData = dataManager.GetPlayerData(attackerId);

            // 获取受害者角色
            var victimRole = ev.Player.Role.Type;

            // 计算经验值
            int baseExp = plugin.Config.ExpPerKill;
            float multiplier = 1.0f;

            if (plugin.Config.RoleExpMultipliers.TryGetValue(victimRole, out float roleMultiplier))
            {
                multiplier = roleMultiplier;
            }

            int expGained = (int)(baseExp * multiplier);

            // 记录击杀和经验
            attackerData.TotalKills++;
            int oldLevel = attackerData.Level;
            attackerData.AddExp(expGained, plugin);

            // 保存数据
            dataManager.SavePlayerData(attackerData);

            // 更新攻击者显示名称
            if (plugin.Config.ShowLevelInName && attackerData.Level > oldLevel)
            {
                UpdatePlayerDisplayName(ev.Attacker, attackerData);
            }

            // 发送消息给玩家
            string message = $"<color=red>击杀奖励 +{expGained} EX</color>";
            

            ev.Attacker.ShowHint(message, 5f);

            if (plugin.Config.Debug)
            {
                Log.Debug($"玩家 {ev.Attacker.Nickname} 击杀 {ev.Player.Nickname} ({victimRole}) 获得 {expGained} 经验");
            }
        }

        // 新增：处理玩家撤离事件
        public void OnPlayerEscaping(EscapingEventArgs ev)
        {
            if (ev.Player == null) return;

            var plugin = KillExpSystem.Instance;
            if (plugin == null) return;

            // 只对D级人员和科学家给予撤离经验
            if (ev.Player.Role.Type == RoleTypeId.ClassD || ev.Player.Role.Type == RoleTypeId.Scientist)
            {
                // 获取玩家数据
                string playerId = ev.Player.UserId;
                var playerData = dataManager.GetPlayerData(playerId);

                // 给予撤离经验
                int escapeExp = plugin.Config.ExpPerEscape;
                int oldLevel = playerData.Level;

                playerData.TotalEscapes++;
                playerData.AddExp(escapeExp, plugin);

                // 保存数据
                dataManager.SavePlayerData(playerData);

                // 更新玩家显示名称
                if (plugin.Config.ShowLevelInName && playerData.Level > oldLevel)
                {
                    UpdatePlayerDisplayName(ev.Player, playerData);
                }

                // 发送消息给玩家
                string escapeMessage = $"<color=green>撤离成功 +{escapeExp} EXP</color>";
                if (playerData.Level > oldLevel)
                {
                    escapeMessage += $"\n<color=yellow>升级! 当前等级: {playerData.Level}</color>";
                }

                ev.Player.ShowHint(escapeMessage, 5f);

                if (plugin.Config.Debug)
                {
                    Log.Debug($"玩家 {ev.Player.Nickname} ({ev.Player.Role.Type}) 撤离成功，获得 {escapeExp} 经验");
                }
            }
        }

        public void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (ev.Player == null || !ev.Player.IsConnected) return;

            var plugin = KillExpSystem.Instance;
            if (plugin == null || !plugin.Config.ShowLevelInName) return;

            // 角色变更后更新显示名称
            var data = dataManager.GetPlayerData(ev.Player.UserId);
            if (data != null)
            {
                // 使用协程延迟更新，确保角色变更完成
                Timing.CallDelayed(0.5f, () =>
                {
                    if (ev.Player != null && ev.Player.IsConnected)
                    {
                        UpdatePlayerDisplayName(ev.Player, data);
                    }
                });
            }
        }

        public void OnWaitingForPlayers()
        {
            Log.Debug("服务器准备中，击杀经验系统就绪...");
        }

        public void OnPlayerLeft(LeftEventArgs ev)
        {
            if (ev.Player == null) return;

            // 玩家离开时保存数据并移除原始名称记录
            var data = dataManager.GetPlayerData(ev.Player.UserId);
            if (data != null)
            {
                dataManager.SavePlayerData(data);
            }

            if (originalPlayerNames.ContainsKey(ev.Player.UserId))
            {
                originalPlayerNames.Remove(ev.Player.UserId);
            }
        }

        /// <summary>
        /// 更新玩家显示名称，格式为 [等级]原名称
        /// </summary>
        public void UpdatePlayerDisplayName(Player player, PlayerData data)
        {
            var plugin = KillExpSystem.Instance;
            if (plugin == null || !plugin.Config.ShowLevelInName || player == null) return;

            try
            {
                // 获取原始名称
                string originalName = player.Nickname;
                if (originalPlayerNames.ContainsKey(player.UserId))
                {
                    originalName = originalPlayerNames[player.UserId];
                }
                else
                {
                    originalPlayerNames[player.UserId] = originalName;
                }

                // 设置显示名称为 [等级]原名称 格式
                string displayName = $"[Lv.{data.Level}] {originalName}";

                // 使用DisplayNickname属性来修改显示名称
                player.DisplayNickname = displayName;

                if (plugin.Config.Debug)
                {
                    Log.Debug($"更新玩家显示名称: {originalName} -> {displayName}");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"更新玩家显示名称失败 {player.Nickname}: {ex}");
            }
        }

        /// <summary>
        /// 重置玩家显示名称为原始名称
        /// </summary>
        public void ResetPlayerDisplayName(Player player)
        {
            if (player == null) return;

            try
            {
                if (originalPlayerNames.ContainsKey(player.UserId))
                {
                    player.DisplayNickname = null; // 重置为原始名称
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"重置玩家显示名称失败 {player.Nickname}: {ex}");
            }
        }
    }
}