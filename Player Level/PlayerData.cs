using System;

namespace KillExpSystem
{
    [Serializable]
    public class PlayerData
    {
        public string UserId { get; set; }
        public string PlayerName { get; set; }
        public int Level { get; set; } = 1;
        public int CurrentExp { get; set; } = 0;
        public int TotalKills { get; set; } = 0;
        public int TotalExp { get; set; } = 0;
        public int TotalEscapes { get; set; } = 0; // 新增：总撤离次数
        public DateTime FirstSeen { get; set; } = DateTime.Now;
        public DateTime LastSeen { get; set; } = DateTime.Now;

        public int GetRequiredExp()
        {
            var plugin = KillExpSystem.Instance;
            if (plugin == null) return 100;

            return (int)(plugin.Config.BaseExpRequired * Level * plugin.Config.ExpGrowthFactor);
        }

        public void AddExp(int exp, KillExpSystem plugin)
        {
            if (Level >= plugin.Config.MaxLevel) return;

            CurrentExp += exp;
            TotalExp += exp;

            int requiredExp = GetRequiredExp();
            while (CurrentExp >= requiredExp && Level < plugin.Config.MaxLevel)
            {
                CurrentExp -= requiredExp;
                Level++;
                requiredExp = GetRequiredExp();
            }
        }

        public override string ToString()
        {
            return $"玩家: {PlayerName} | 等级: {Level} | 经验: {CurrentExp}/{GetRequiredExp()} | 总击杀: {TotalKills} | 总撤离: {TotalEscapes}";
        }
    }
}