using System.Collections.Generic;
using UnityEngine;

namespace WarOfEras.Battle.Core
{
    public sealed class BattleLayout : MonoBehaviour
    {
        // 可选的场景布局契约：如果场景里存在这些标记点，控制器会优先使用它们覆盖代码内置坐标。
        [SerializeField] private float unitVisualScale = 1.24f;
        [SerializeField] private float baseVisualScale = 0.56f;
        [SerializeField] private float towerVisualScale = 0.76f;
        [SerializeField] private float resourceWellVisualScale = 0.84f;

        private readonly List<string> validationErrors = new List<string>();

        public float UnitVisualScale => unitVisualScale;
        public float BaseVisualScale => baseVisualScale;
        public float TowerVisualScale => towerVisualScale;
        public float ResourceWellVisualScale => resourceWellVisualScale;

        public Transform PlayerBasePoint => FindMarker("Layout/Bases/PlayerBasePoint");
        public Transform EnemyBasePoint => FindMarker("Layout/Bases/EnemyBasePoint");

        public Vector3 PlayerBasePosition => GetPosition(PlayerBasePoint);
        public Vector3 EnemyBasePosition => GetPosition(EnemyBasePoint);

        public Vector3[] GetLaneRoute(int laneIndex)
        {
            var laneRoot = FindMarker("Layout/Routes/Lane_" + laneIndex);
            if (laneRoot == null)
            {
                return new Vector3[0];
            }

            var points = new Vector3[laneRoot.childCount];
            for (var i = 0; i < laneRoot.childCount; i++)
            {
                points[i] = laneRoot.GetChild(i).position;
            }

            return points;
        }

        public Vector3[] GetPlayerTowerPositions()
        {
            return GetChildPositions("Layout/Towers", "PlayerTowerSlot_");
        }

        public Vector3[] GetEnemyTowerPositions()
        {
            return GetChildPositions("Layout/Towers", "EnemyTowerSlot_");
        }

        public Vector3[] GetPlayerResourceWellPositions()
        {
            return GetChildPositions("Layout/ResourceWells", "PlayerWellSlot_");
        }

        public Vector3[] GetEnemyResourceWellPositions()
        {
            return GetChildPositions("Layout/ResourceWells", "EnemyWellSlot_");
        }

        public bool Validate(out string errorSummary)
        {
            // 启动时做一次轻量校验，发现缺少关键标记就退回控制器里的默认战场数据。
            validationErrors.Clear();
            RequireMarker("Layout");
            RequireMarker("Layout/Routes");
            RequireMarker("Layout/Bases");
            RequireMarker("Layout/Towers");
            RequireMarker("Layout/ResourceWells");
            RequireMarker("Layout/Bases/PlayerBasePoint");
            RequireMarker("Layout/Bases/EnemyBasePoint");
            RequireCount("player tower slots", GetPlayerTowerPositions(), 3);
            RequireCount("enemy tower slots", GetEnemyTowerPositions(), 3);
            RequireCount("player resource well slots", GetPlayerResourceWellPositions(), 1);
            RequireCount("enemy resource well slots", GetEnemyResourceWellPositions(), 1);

            for (var i = 0; i < 3; i++)
            {
                RequireCount("Lane_" + i + " route points", GetLaneRoute(i), 2);
            }

            RequirePositiveScale(nameof(unitVisualScale), unitVisualScale);
            RequirePositiveScale(nameof(baseVisualScale), baseVisualScale);
            RequirePositiveScale(nameof(towerVisualScale), towerVisualScale);
            RequirePositiveScale(nameof(resourceWellVisualScale), resourceWellVisualScale);

            errorSummary = validationErrors.Count == 0
                ? string.Empty
                : string.Join("\n", validationErrors);
            return validationErrors.Count == 0;
        }

        private Transform FindMarker(string path)
        {
            return transform.Find(path);
        }

        private void RequireMarker(string path)
        {
            if (FindMarker(path) == null)
            {
                validationErrors.Add("BattleLayout missing marker: " + path);
            }
        }

        private void RequireCount(string label, Vector3[] values, int expected)
        {
            if (values.Length < expected)
            {
                validationErrors.Add("BattleLayout needs at least " + expected + " " + label + ", found " + values.Length + ".");
            }
        }

        private void RequirePositiveScale(string label, float value)
        {
            if (value <= 0f)
            {
                validationErrors.Add("BattleLayout " + label + " must be greater than zero.");
            }
        }

        private Vector3[] GetChildPositions(string parentPath, string prefix)
        {
            var parent = FindMarker(parentPath);
            if (parent == null)
            {
                return new Vector3[0];
            }

            var positions = new List<Vector3>();
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    positions.Add(child.position);
                }
            }

            return positions.ToArray();
        }

        private static Vector3 GetPosition(Transform marker)
        {
            return marker != null ? marker.position : Vector3.zero;
        }
    }
}
