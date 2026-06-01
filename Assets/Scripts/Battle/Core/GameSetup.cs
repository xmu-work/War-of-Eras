using System.Collections.Generic;

namespace WarOfEras.Battle.Core
{
    public enum GameDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    public sealed class BattleMapDefinition
    {
        public BattleMapDefinition(string id, string displayName, string resourcePath, string description)
        {
            Id = id;
            DisplayName = displayName;
            ResourcePath = resourcePath;
            Description = description;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string ResourcePath { get; }
        public string Description { get; }
    }

    public static class GameSession
    {
        private static readonly BattleMapDefinition pixelFrontlineThreeLanes = new BattleMapDefinition(
            "pixel_frontline_three_lanes",
            "\u50cf\u7d20\u6218\u7ebf - \u65f6\u4ee3\u53d8\u4f53",
            "Battle/Maps/PixelFrontline_Barbarian",
            "\u540c\u4e00\u4e09\u8def\u6218\u573a\u5e03\u5c40\u968f\u65f6\u4ee3\u8fdb\u5316\u5207\u6362\u4e3b\u9898\uff1a\u86ee\u8352\u3001\u673a\u68b0\u3001\u7535\u529b\u3001\u6838\u80fd\u548c\u661f\u6d77\u90fd\u6709\u72ec\u7acb\u50cf\u7d20\u5730\u56fe\u3002");

        private static readonly BattleMapDefinition[] maps = { pixelFrontlineThreeLanes };

        static GameSession()
        {
            SelectedMap = pixelFrontlineThreeLanes;
            Difficulty = GameDifficulty.Normal;
        }

        public static IReadOnlyList<BattleMapDefinition> AvailableMaps => maps;
        public static BattleMapDefinition SelectedMap { get; private set; }
        public static GameDifficulty Difficulty { get; private set; }

        public static string DifficultyName
        {
            get
            {
                switch (Difficulty)
                {
                    case GameDifficulty.Easy:
                        return "\u7b80\u5355";
                    case GameDifficulty.Hard:
                        return "\u56f0\u96be";
                    default:
                        return "\u4e2d\u7b49";
                }
            }
        }

        public static float PlayerStartingCoins
        {
            get
            {
                switch (Difficulty)
                {
                    case GameDifficulty.Easy:
                        return 225f;
                    case GameDifficulty.Hard:
                        return 140f;
                    default:
                        return 175f;
                }
            }
        }

        public static float IncomePerSecond
        {
            get
            {
                switch (Difficulty)
                {
                    case GameDifficulty.Easy:
                        return 10f;
                    case GameDifficulty.Hard:
                        return 7f;
                    default:
                        return 8f;
                }
            }
        }

        public static float EnemyHealthScale
        {
            get
            {
                switch (Difficulty)
                {
                    case GameDifficulty.Easy:
                        return 0.8f;
                    case GameDifficulty.Hard:
                        return 1.22f;
                    default:
                        return 1f;
                }
            }
        }

        public static float EnemyDamageScale
        {
            get
            {
                switch (Difficulty)
                {
                    case GameDifficulty.Easy:
                        return 0.82f;
                    case GameDifficulty.Hard:
                        return 1.18f;
                    default:
                        return 1f;
                }
            }
        }

        public static float EnemySpawnIntervalScale
        {
            get
            {
                switch (Difficulty)
                {
                    case GameDifficulty.Easy:
                        return 1.24f;
                    case GameDifficulty.Hard:
                        return 0.78f;
                    default:
                        return 1f;
                }
            }
        }

        public static float InitialEnemySpawnDelay
        {
            get
            {
                switch (Difficulty)
                {
                    case GameDifficulty.Easy:
                        return 4f;
                    case GameDifficulty.Hard:
                        return 1.8f;
                    default:
                        return 2.5f;
                }
            }
        }

        public static void SelectMap(BattleMapDefinition map)
        {
            SelectedMap = map ?? pixelFrontlineThreeLanes;
        }

        public static void SelectDifficulty(GameDifficulty difficulty)
        {
            Difficulty = difficulty;
        }
    }
}
