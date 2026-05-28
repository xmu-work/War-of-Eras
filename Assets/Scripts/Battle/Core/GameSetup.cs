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
        private static readonly BattleMapDefinition forestThreeLanes = new BattleMapDefinition(
            "barbarian_forest_three_lanes",
            "\u86ee\u8352\u90e8\u843d - \u6218\u573a\u6837\u672c v2",
            "Barbarian/Maps/ForestThreeLanes",
            "\u9ad8\u6e05\u771f\u5b9e\u5730\u56fe\uff0c\u4e09\u6761\u9053\u8def\u7a7f\u8fc7\u6cb3\u6d41\u4e0e\u636e\u70b9\u3002");

        private static readonly BattleMapDefinition[] maps = { forestThreeLanes };

        static GameSession()
        {
            SelectedMap = forestThreeLanes;
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
            SelectedMap = map ?? forestThreeLanes;
        }

        public static void SelectDifficulty(GameDifficulty difficulty)
        {
            Difficulty = difficulty;
        }
    }
}
