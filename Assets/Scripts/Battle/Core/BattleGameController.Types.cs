using UnityEngine;
using UnityEngine.UI;

namespace WarOfEras.Battle.Core
{
    public sealed partial class BattleGameController
    {
        private sealed class UnitButtonBinding
        {
            public UnitButtonBinding(Button button, Text label, UnitDefinition definition)
            {
                Button = button;
                Label = label;
                Definition = definition;
            }

            public Button Button { get; }
            public Text Label { get; }
            public UnitDefinition Definition { get; set; }
        }

        private sealed class TowerButtonBinding
        {
            public TowerButtonBinding(Button button, Text label, int towerIndex, TowerDefinition definition)
            {
                Button = button;
                Label = label;
                TowerIndex = towerIndex;
                Definition = definition;
            }

            public Button Button { get; }
            public Text Label { get; }
            public int TowerIndex { get; }
            public TowerDefinition Definition { get; set; }
        }

        private sealed class AgeVisualSet
        {
            public AgeVisualSet(string key, string mapSpritePath, string baseSpritePath, string unitRoot, string[] unitFrameFolders, string[] towerFramePrefixes, Color fallbackTint)
            {
                Key = key;
                MapSpritePath = mapSpritePath;
                BaseSpritePath = baseSpritePath;
                UnitRoot = unitRoot;
                UnitFrameFolders = unitFrameFolders;
                TowerFramePrefixes = towerFramePrefixes;
                FallbackTint = fallbackTint;
            }

            public string Key { get; }
            public string MapSpritePath { get; }
            public string BaseSpritePath { get; }
            public string UnitRoot { get; }
            public string[] UnitFrameFolders { get; }
            public string[] TowerFramePrefixes { get; }
            public Color FallbackTint { get; }
        }

        private readonly struct RouteEdge
        {
            public RouteEdge(int a, int b)
            {
                A = a;
                B = b;
            }

            public int A { get; }
            public int B { get; }
        }

        private readonly struct RouteTarget
        {
            public RouteTarget(int edgeA, int edgeB, Vector3 position)
            {
                EdgeA = edgeA;
                EdgeB = edgeB;
                Position = position;
            }

            public int EdgeA { get; }
            public int EdgeB { get; }
            public Vector3 Position { get; }
        }

        private sealed class BuildPlacementPreview
        {
            public BuildPlacementPreview(GameObject root, int slotIndex)
            {
                Root = root;
                SlotIndex = slotIndex;
            }

            public GameObject Root { get; }
            public int SlotIndex { get; }
        }
    }
}
