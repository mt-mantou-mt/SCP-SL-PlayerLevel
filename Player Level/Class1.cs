using System;
using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using PlayerRoles;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace KillExpSystem
{
    public class KillExpSystem : Plugin<Config>
    {
        public static KillExpSystem Instance { get; private set; }

        public override string Name => "KillExpSystem";
        public override string Prefix => "KES";
        public override string Author => "YourName";
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredExiledVersion => new Version(9, 8, 1);

        private PlayerDataManager dataManager;
        private EventHandlers eventHandlers;

        public override void OnEnabled()
        {
            Instance = this;
            dataManager = new PlayerDataManager();
            eventHandlers = new EventHandlers(dataManager);

            Exiled.Events.Handlers.Player.Dying += eventHandlers.OnPlayerDying;
            Exiled.Events.Handlers.Server.WaitingForPlayers += eventHandlers.OnWaitingForPlayers;
            Exiled.Events.Handlers.Player.Left += eventHandlers.OnPlayerLeft;
            Exiled.Events.Handlers.Player.Verified += eventHandlers.OnPlayerVerified;
            Exiled.Events.Handlers.Player.ChangingRole += eventHandlers.OnChangingRole;
            Exiled.Events.Handlers.Player.Escaping += eventHandlers.OnPlayerEscaping;

            Log.Info("击杀经验系统已启动!");
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Dying -= eventHandlers.OnPlayerDying;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= eventHandlers.OnWaitingForPlayers;
            Exiled.Events.Handlers.Player.Left -= eventHandlers.OnPlayerLeft;
            Exiled.Events.Handlers.Player.Verified -= eventHandlers.OnPlayerVerified;
            Exiled.Events.Handlers.Player.ChangingRole -= eventHandlers.OnChangingRole;
            Exiled.Events.Handlers.Player.Escaping -= eventHandlers.OnPlayerEscaping;

            // 重置所有玩家的显示名称
            foreach (Player player in Player.List)
            {
                eventHandlers.ResetPlayerDisplayName(player);
            }

            dataManager?.SaveAllData();
            eventHandlers = null;
            dataManager = null;
            Instance = null;

            Log.Info("击杀经验系统已关闭!");
            base.OnDisabled();
        }

        public PlayerData GetPlayerData(string userId) => dataManager?.GetPlayerData(userId);
        public List<PlayerData> GetAllPlayerData() => dataManager?.GetAllPlayerData();
    }

    public class Config : IConfig
    {
        [Description("是否启用插件")]
        public bool IsEnabled { get; set; } = true;

        [Description("调试模式")]
        public bool Debug { get; set; } = false;

        [Description("每次击杀获得的经验值")]
        public int ExpPerKill { get; set; } = 25;

        [Description("D级人员或科学家撤离获得的经验值")]
        public int ExpPerEscape { get; set; } = 50;

        [Description("升级所需基础经验值")]
        public int BaseExpRequired { get; set; } = 100;

        [Description("经验增长系数 (每级增加的经验 = 基础经验 * 等级 * 增长系数)")]
        public float ExpGrowthFactor { get; set; } = 1.2f;

        [Description("最大等级")]
        public int MaxLevel { get; set; } = 10000;

        [Description("数据文件保存路径")]
        public string DataFilePath { get; set; } = "玩家数据";

        [Description("是否在玩家名称前显示等级")]
        public bool ShowLevelInName { get; set; } = true;

        [Description("不同阵营击杀奖励倍率")]
        public Dictionary<RoleTypeId, float> RoleExpMultipliers { get; set; } = new Dictionary<RoleTypeId, float>
        {
            { RoleTypeId.Scp049, 6.0f },    // SCP击杀获得双倍经验
            { RoleTypeId.Scp096, 6.0f },
            { RoleTypeId.Scp106, 6.0f },
            { RoleTypeId.Scp173, 6.0f },
            { RoleTypeId.Scp939, 6.0f },
            { RoleTypeId.ClassD, 1.0f },
            { RoleTypeId.Scp3114,6.0f },
            { RoleTypeId.Scientist, 1.0f },
            { RoleTypeId.FacilityGuard, 1.0f },
            { RoleTypeId.NtfPrivate, 1.0f },
            { RoleTypeId.NtfSergeant, 1.0f },
            { RoleTypeId.NtfCaptain, 1.0f },
            { RoleTypeId.ChaosConscript, 1.0f },
            { RoleTypeId.ChaosMarauder, 1.0f },
            { RoleTypeId.ChaosRepressor, 1.0f },
            { RoleTypeId.ChaosRifleman, 1.0f }
        };
    }
}