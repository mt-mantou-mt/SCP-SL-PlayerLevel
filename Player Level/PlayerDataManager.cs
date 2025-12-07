using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using Exiled.API.Features;

namespace KillExpSystem
{
    public class PlayerDataManager
    {
        private Dictionary<string, PlayerData> playerDataCache;
        private string dataDirectory;

        public PlayerDataManager()
        {
            playerDataCache = new Dictionary<string, PlayerData>();
            dataDirectory = Path.Combine(Paths.Configs, KillExpSystem.Instance.Config.DataFilePath);

            if (!Directory.Exists(dataDirectory))
                Directory.CreateDirectory(dataDirectory);
        }

        public PlayerData GetPlayerData(string userId)
        {
            if (playerDataCache.TryGetValue(userId, out var data))
                return data;

            string filePath = Path.Combine(dataDirectory, $"{userId}.json");
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    data = JsonSerializer.Deserialize<PlayerData>(json);
                    playerDataCache[userId] = data;
                    return data;
                }
                catch (Exception ex)
                {
                    Log.Error($"加载玩家数据失败 {userId}: {ex}");
                }
            }

            data = new PlayerData { UserId = userId };
            playerDataCache[userId] = data;
            return data;
        }

        public void SavePlayerData(PlayerData data)
        {
            try
            {
                data.LastSeen = DateTime.Now;
                string filePath = Path.Combine(dataDirectory, $"{data.UserId}.json");
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Log.Error($"保存玩家数据失败 {data.UserId}: {ex}");
            }
        }

        public void SaveAllData()
        {
            foreach (var data in playerDataCache.Values)
            {
                SavePlayerData(data);
            }
        }

        public List<PlayerData> GetAllPlayerData()
        {
            // 首先返回缓存中的数据
            var cachedData = playerDataCache.Values.ToList();

            // 然后从文件系统加载所有玩家数据
            try
            {
                if (!Directory.Exists(dataDirectory))
                    return cachedData;

                var allData = new List<PlayerData>();
                var filePaths = Directory.GetFiles(dataDirectory, "*.json");

                foreach (var filePath in filePaths)
                {
                    try
                    {
                        string json = File.ReadAllText(filePath);
                        var data = JsonSerializer.Deserialize<PlayerData>(json);

                        // 如果缓存中有更新的数据，使用缓存数据
                        if (playerDataCache.ContainsKey(data.UserId))
                        {
                            allData.Add(playerDataCache[data.UserId]);
                        }
                        else
                        {
                            allData.Add(data);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"加载玩家数据文件失败 {filePath}: {ex}");
                    }
                }

                return allData.OrderByDescending(d => d.Level).ThenByDescending(d => d.TotalExp).ToList();
            }
            catch (Exception ex)
            {
                Log.Error($"获取所有玩家数据失败: {ex}");
                return cachedData;
            }
        }

        public List<PlayerData> GetOnlinePlayerData()
        {
            var onlineData = new List<PlayerData>();
            foreach (var player in Player.List)
            {
                var data = GetPlayerData(player.UserId);
                if (data != null)
                {
                    onlineData.Add(data);
                }
            }
            return onlineData.OrderByDescending(d => d.Level).ThenByDescending(d => d.TotalExp).ToList();
        }

        // 获取击杀排行榜
        public List<PlayerData> GetKillLeaderboard(int count = 10)
        {
            var allData = GetAllPlayerData();
            return allData.Where(d => d.TotalKills > 0)
                         .OrderByDescending(d => d.TotalKills)
                         .ThenByDescending(d => d.Level)
                         .Take(count)
                         .ToList();
        }

        // 获取在线玩家击杀排行榜
        public List<PlayerData> GetOnlineKillLeaderboard(int count = 10)
        {
            var onlineData = GetOnlinePlayerData();
            return onlineData.Where(d => d.TotalKills > 0)
                           .OrderByDescending(d => d.TotalKills)
                           .ThenByDescending(d => d.Level)
                           .Take(count)
                           .ToList();
        }

        // 新增方法：获取撤离排行榜
        public List<PlayerData> GetEscapeLeaderboard(int count = 10)
        {
            var allData = GetAllPlayerData();
            return allData.Where(d => d.TotalEscapes > 0)
                         .OrderByDescending(d => d.TotalEscapes)
                         .ThenByDescending(d => d.Level)
                         .Take(count)
                         .ToList();
        }

        // 新增方法：获取在线玩家撤离排行榜
        public List<PlayerData> GetOnlineEscapeLeaderboard(int count = 10)
        {
            var onlineData = GetOnlinePlayerData();
            return onlineData.Where(d => d.TotalEscapes > 0)
                           .OrderByDescending(d => d.TotalEscapes)
                           .ThenByDescending(d => d.Level)
                           .Take(count)
                           .ToList();
        }
    }
}