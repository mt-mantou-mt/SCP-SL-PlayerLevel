using Exiled.API.Features;
using CommandSystem;
using System;
using System.Linq;
using System.Text;

namespace KillExpSystem
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class LevelCommand : ICommand
    {
        public string Command => "lv";
        public string[] Aliases => new[] { "level", "ex", "lvl" };
        public string Description => "查看你的等级和经验信息";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            if (player == null)
            {
                response = "无法获取玩家信息!";
                return false;
            }

            var data = KillExpSystem.Instance.GetPlayerData(player.UserId);
            if (data == null)
            {
                response = "无法找到你的数据!";
                return false;
            }

            int requiredExp = data.GetRequiredExp();
            float progress = requiredExp > 0 ? (float)data.CurrentExp / requiredExp * 100 : 0;

            response = $"\n=== 玩家信息 ===\n" +
                      $"名称: {data.PlayerName}\n" +
                      $"等级: <color=yellow>{data.Level}</color>\n" +
                      $"经验: {data.CurrentExp}/{requiredExp} ({progress:F1}%)\n" +
                      $"总击杀: {data.TotalKills}\n" +
                      $"总撤离: {data.TotalEscapes}\n" +
                      $"总经验: {data.TotalExp}\n" +
                      $"=================";
            return true;
        }
    }

    [CommandHandler(typeof(ClientCommandHandler))]
    public class KillLeaderboardCommand : ICommand
    {
        public string Command => "ktop";
        public string[] Aliases => new[] { "killtop", "killboard" };
        public string Description => "查看击杀排行榜";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var plugin = KillExpSystem.Instance;
            if (plugin == null)
            {
                response = "插件未加载!";
                return false;
            }

            try
            {
                var args = arguments.ToArray();
                bool onlineOnly = args.Length > 0 && (args[0].ToLower() == "online" || args[0].ToLower() == "o");
                int count = 10; // 默认显示前10名

                // 检查是否有指定显示数量
                if (args.Length > 0 && int.TryParse(args[0], out int parsedCount) && parsedCount > 0)
                {
                    count = Math.Min(parsedCount, 20); // 限制最多显示20名
                }

                var dataManager = (plugin.GetType().GetField("dataManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(plugin)) as PlayerDataManager;
                if (dataManager == null)
                {
                    response = "无法访问数据管理器!";
                    return false;
                }

                // 获取击杀排行榜数据
                var killLeaderboard = onlineOnly ?
                    dataManager.GetOnlineKillLeaderboard(count) :
                    dataManager.GetKillLeaderboard(count);

                if (killLeaderboard.Count == 0)
                {
                    response = "没有找到击杀数据!";
                    return true;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"=== 击杀排行榜 {(onlineOnly ? "(在线)" : "(全服)")} ===");
                sb.AppendLine("排名 | 击杀数 | 等级 | 玩家名称");
                sb.AppendLine("----------------------------------------");

                for (int i = 0; i < killLeaderboard.Count; i++)
                {
                    var data = killLeaderboard[i];
                    string medal = i == 0 ? "🥇" : i == 1 ? "🥈" : i == 2 ? "🥉" : $"{i + 1:00}.";
                    string status = onlineOnly ? "●" : (Player.Get(data.UserId)?.IsConnected == true ? "●" : "○");

                    sb.AppendLine($"{medal} | {data.TotalKills:0000} 杀 | LV.{data.Level:00} | {status} {data.PlayerName}");
                }

                sb.AppendLine("----------------------------------------");
                if (!onlineOnly)
                {
                    sb.AppendLine($"使用: .killtop online - 只显示在线玩家");
                }
                sb.AppendLine($"使用: .killtop <数量> - 显示指定数量的玩家 (最多20名)");

                response = sb.ToString();
                return true;
            }
            catch (Exception ex)
            {
                response = $"执行命令时出错: {ex.Message}";
                return false;
            }
        }
    }

    [CommandHandler(typeof(ClientCommandHandler))]
    public class EscapeLeaderboardCommand : ICommand
    {
        public string Command => "estop";
        public string[] Aliases => new[] { "es","etop" };
        public string Description => "查看撤离排行榜";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var plugin = KillExpSystem.Instance;
            if (plugin == null)
            {
                response = "插件未加载!";
                return false;
            }

            try
            {
                var args = arguments.ToArray();
                bool onlineOnly = args.Length > 0 && (args[0].ToLower() == "online" || args[0].ToLower() == "o");
                int count = 10; // 默认显示前10名

                // 检查是否有指定显示数量
                if (args.Length > 0 && int.TryParse(args[0], out int parsedCount) && parsedCount > 0)
                {
                    count = Math.Min(parsedCount, 20); // 限制最多显示20名
                }

                var dataManager = (plugin.GetType().GetField("dataManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(plugin)) as PlayerDataManager;
                if (dataManager == null)
                {
                    response = "无法访问数据管理器!";
                    return false;
                }

                // 获取撤离排行榜数据
                var escapeLeaderboard = onlineOnly ?
                    dataManager.GetOnlineEscapeLeaderboard(count) :
                    dataManager.GetEscapeLeaderboard(count);

                if (escapeLeaderboard.Count == 0)
                {
                    response = "没有找到撤离数据!";
                    return true;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"=== 撤离排行榜 {(onlineOnly ? "(在线)" : "(全服)")} ===");
                sb.AppendLine("排名 | 撤离数 | 等级 | 玩家名称");
                sb.AppendLine("----------------------------------------");

                for (int i = 0; i < escapeLeaderboard.Count; i++)
                {
                    var data = escapeLeaderboard[i];
                    string medal = i == 0 ? "🥇" : i == 1 ? "🥈" : i == 2 ? "🥉" : $"{i + 1:00}.";
                    string status = onlineOnly ? "●" : (Player.Get(data.UserId)?.IsConnected == true ? "●" : "○");

                    sb.AppendLine($"{medal} | {data.TotalEscapes:0000} 次 | LV.{data.Level:00} | {status} {data.PlayerName}");
                }

                sb.AppendLine("----------------------------------------");
                if (!onlineOnly)
                {
                    sb.AppendLine($"使用: .escapetop online - 只显示在线玩家");
                }
                sb.AppendLine($"使用: .escapetop <数量> - 显示指定数量的玩家 (最多20名)");

                response = sb.ToString();
                return true;
            }
            catch (Exception ex)
            {
                response = $"执行命令时出错: {ex.Message}";
                return false;
            }
        }
    }

    [CommandHandler(typeof(ClientCommandHandler))]
    public class TopCommand : ICommand
    {
        public string Command => "top";
        public string[] Aliases => new[] { "leaderboard", "rank" };
        public string Description => "查看等级排行榜";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var plugin = KillExpSystem.Instance;
            if (plugin == null)
            {
                response = "插件未加载!";
                return false;
            }

            try
            {
                var args = arguments.ToArray();
                bool onlineOnly = args.Length > 0 && (args[0].ToLower() == "online" || args[0].ToLower() == "o");
                int count = 10; // 默认显示前10名

                if (args.Length > 0 && int.TryParse(args[0], out int parsedCount) && parsedCount > 0)
                {
                    count = Math.Min(parsedCount, 20);
                }

                var playerData = onlineOnly ?
                    plugin.GetAllPlayerData().Where(d => Player.Get(d.UserId)?.IsConnected == true).Take(count).ToList() :
                    plugin.GetAllPlayerData().Take(count).ToList();

                if (playerData.Count == 0)
                {
                    response = "没有找到玩家数据!";
                    return true;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"=== 等级排行榜 {(onlineOnly ? "(在线)" : "(全服)")} TOP {count} ===");
                sb.AppendLine("排名 | 等级 | 名称 | 总经验 | 总击杀 | 总撤离");
                sb.AppendLine("----------------------------------------");

                for (int i = 0; i < playerData.Count; i++)
                {
                    var data = playerData[i];
                    string medal = i == 0 ? "🥇" : i == 1 ? "🥈" : i == 2 ? "🥉" : $"{i + 1:00}.";
                    string status = onlineOnly ? "●" : (Player.Get(data.UserId)?.IsConnected == true ? "●" : "○");

                    sb.AppendLine($"{medal} | LV.{data.Level:000} | {status} {data.PlayerName} | {data.TotalExp} | {data.TotalKills} | {data.TotalEscapes}");
                }

                sb.AppendLine("----------------------------------------");
                if (!onlineOnly)
                {
                    sb.AppendLine($"使用: .top online - 只显示在线玩家");
                }
                sb.AppendLine($"使用: .top <数量> - 显示指定数量的玩家 (最多20名)");

                response = sb.ToString();
                return true;
            }
            catch (Exception ex)
            {
                response = $"执行命令时出错: {ex.Message}";
                return false;
            }
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class AdminStatsCommand : ICommand
    {
        public string Command => "playerstats";
        public string[] Aliases => new[] { "pstats", "allstats" };
        public string Description => "查看所有玩家统计信息 (管理员)";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement))
            {
                response = "权限不足! 需要 PlayersManagement 权限。";
                return false;
            }

            var plugin = KillExpSystem.Instance;
            if (plugin == null)
            {
                response = "插件未加载!";
                return false;
            }

            try
            {
                var args = arguments.ToArray();
                bool onlineOnly = args.Length > 0 && (args[0].ToLower() == "online" || args[0].ToLower() == "o");

                var playerData = onlineOnly ?
                    plugin.GetAllPlayerData().Where(d => Player.Get(d.UserId)?.IsConnected == true).ToList() :
                    plugin.GetAllPlayerData();

                if (playerData.Count == 0)
                {
                    response = "没有找到玩家数据!";
                    return true;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"=== 全服玩家统计 ({playerData.Count} 名玩家) {(onlineOnly ? "(在线)" : "")} ===");
                sb.AppendLine("格式: 等级 | 经验 | 击杀 | 撤离 | 名称");
                sb.AppendLine("----------------------------------------");

                int rank = 1;
                foreach (var data in playerData.Take(20)) // 只显示前20名
                {
                    string status = Player.Get(data.UserId)?.IsConnected == true ? "●" : "○";
                    sb.AppendLine($"{rank:00}. [{data.Level:000}] | {data.TotalExp:000000} | {data.TotalKills:0000} | {data.TotalEscapes:0000} | {status} {data.PlayerName}");
                    rank++;
                }

                if (playerData.Count > 20)
                {
                    sb.AppendLine($"... 还有 {playerData.Count - 20} 名玩家");
                }

                sb.AppendLine("----------------------------------------");
                sb.AppendLine($"使用: .playerstats online - 只显示在线玩家");

                response = sb.ToString();
                return true;
            }
            catch (Exception ex)
            {
                response = $"执行命令时出错: {ex.Message}";
                return false;
            }
        }
    }

    
    
}