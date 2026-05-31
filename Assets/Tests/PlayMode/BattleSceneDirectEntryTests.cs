using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace WarOfEras.Tests
{
    public sealed class BattleSceneDirectEntryTests
    {
        [UnityTest]
        public IEnumerator BattleSceneDirectEntry_UsesSceneAuthoredLayout()
        {
            yield return LoadScene("Battle");
            yield return null;

            var layout = FindFirstObjectByType(LayoutType);
            Assert.NotNull(layout, "Battle scene should include a BattleLayout component.");
            AssertLayoutValid(layout);

            var controller = FindFirstObjectByType(ControllerType);
            Assert.NotNull(controller, "Battle scene should include a BattleGameController.");

            AssertSpriteAt("Player Base Art", GetVector3Property(layout, "PlayerBasePosition"));
            AssertSpriteAt("Enemy Base Art", GetVector3Property(layout, "EnemyBasePosition"));

            var playerWells = InvokeVector3Array(layout, "GetPlayerResourceWellPositions");
            var enemyWells = InvokeVector3Array(layout, "GetEnemyResourceWellPositions");
            AssertSpriteAt("Player Resource Well Site 1", playerWells[0]);
            AssertSpriteAt("Player Resource Well Site 2", playerWells[1]);
            AssertSpriteAt("Enemy Resource Well Site 1", enemyWells[0]);
            AssertSpriteAt("Enemy Resource Well Site 2", enemyWells[1]);

            var route = InvokeVector3Array(layout, "GetLaneRoute", 1);
            Assert.GreaterOrEqual(route.Length, 2);
            var unit = SpawnPlayerUnit(controller, 1);
            Assert.That(Vector3.Distance(unit.transform.position, route[0]), Is.LessThan(0.02f));

            var distanceBefore = Vector3.Distance(unit.transform.position, route[1]);
            yield return new WaitForSeconds(0.75f);
            var distanceAfter = Vector3.Distance(unit.transform.position, route[1]);
            Assert.That(distanceAfter, Is.LessThan(distanceBefore), "Spawned unit should move toward the next route marker.");
        }

        [UnityTest]
        public IEnumerator RouteSelection_DispatchesUnitsOnAuthoredLane()
        {
            yield return LoadScene("Battle");
            yield return null;

            var layout = FindFirstObjectByType(LayoutType);
            Assert.NotNull(layout);
            var controller = FindFirstObjectByType(ControllerType);
            Assert.NotNull(controller);

            var laneRoute = InvokeVector3Array(layout, "GetLaneRoute", 1);
            Assert.GreaterOrEqual(laneRoute.Length, 3);

            var selectRoute = ControllerType.GetMethod("SelectRouteTo", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(selectRoute);
            selectRoute.Invoke(controller, new object[] { laneRoute[laneRoute.Length / 2] });

            var definitions = GetPlayerDefinitions(controller);
            var dispatch = ControllerType.GetMethod("DispatchUnitOnActiveRoute", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(dispatch);
            dispatch.Invoke(controller, new[] { definitions[0] });

            var unit = Object.FindObjectsByType(ActiveUnitType, FindObjectsSortMode.None).Cast<Component>().Single();
            var routePointsField = ActiveUnitType.GetField("routePoints", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(routePointsField);
            var unitRoute = (Vector3[])routePointsField.GetValue(unit);

            Assert.AreEqual(laneRoute.Length, unitRoute.Length, "Dispatched units should use the full authored lane route.");
            Assert.That(Vector3.Distance(unitRoute[0], laneRoute[0]), Is.LessThan(0.02f));
            Assert.That(Vector3.Distance(unitRoute[unitRoute.Length - 1], laneRoute[laneRoute.Length - 1]), Is.LessThan(0.02f));
        }

        [UnityTest]
        public IEnumerator FirstAgeUpgrade_AppliesMachineWorkshopAssets()
        {
            yield return LoadScene("Battle");
            yield return null;

            var controller = FindFirstObjectByType(ControllerType);
            Assert.NotNull(controller);

            var buildTowerAt = ControllerType.GetMethod("BuildTowerAt", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(buildTowerAt);
            buildTowerAt.Invoke(controller, new object[] { 0 });
            var existingTower = Object.FindObjectsByType(TowerType, FindObjectsSortMode.None).Cast<Component>().Single();
            AssertSpriteResource(GetFirstTowerFrame(existingTower), "Barbarian/Towers/BoneTower/attack_01");

            var baseRenderer = GameObject.Find("Player Base Art").GetComponent<SpriteRenderer>();
            Assert.NotNull(baseRenderer);
            AssertSpriteResource(baseRenderer.sprite, "Barbarian/Base/Base");

            var threshold = InvokeInt(controller, "GetCurrentEraThreshold");
            SetPrivateField(controller, "eraValue", (float)threshold);
            InvokeUpgradeAge(controller, "Attack");
            yield return null;

            Assert.AreEqual(1, GetPrivateField<int>(controller, "ageIndex"));
            AssertSpriteResource(baseRenderer.sprite, "Machine/Base/Base");
            AssertSpriteResource(GetFirstTowerFrame(existingTower), "Machine/Towers/GearTower/attack_01");

            AssertUnitButtonLabels(controller, "\u9f7f\u8f6e\u5175", "\u84b8\u6c7d\u5f29\u624b", "\u94c1\u8f6e\u7834\u57ce\u8f66");
            AssertTowerButtonLabel(controller, "\u9f7f\u8f6e\u629b\u70ae\u5854");

            var definitions = GetPlayerDefinitions(controller);
            Assert.AreEqual("GearSoldier", GetStringProperty(definitions[0], "Key"));
            AssertSpriteResource(GetFirstUnitMoveFrame(definitions[0]), "Machine/Units/GearSoldier/move_01");

            var unit = SpawnPlayerUnit(controller, 1);
            var definition = GetProperty<object>(unit, "Definition");
            Assert.AreEqual("GearSoldier", GetStringProperty(definition, "Key"));
            AssertSpriteResource(GetFirstUnitMoveFrame(definition), "Machine/Units/GearSoldier/move_01");
        }

        [UnityTest]
        public IEnumerator MainMenuStart_LoadsBattleSceneWithLayout()
        {
            yield return LoadScene("MainMenu");
            yield return null;

            var menu = FindFirstObjectByType(MainMenuType);
            Assert.NotNull(menu, "Main menu controller should exist.");

            var startGame = MainMenuType.GetMethod("StartGame", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(startGame, "Main menu should expose a StartGame handler.");
            startGame.Invoke(menu, null);

            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "Battle");
            yield return null;

            var layout = FindFirstObjectByType(LayoutType);
            Assert.NotNull(layout, "Battle scene loaded from main menu should include BattleLayout.");
            AssertLayoutValid(layout);
            Assert.NotNull(FindFirstObjectByType(ControllerType));
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName);
            Assert.NotNull(operation, "Could not load scene: " + sceneName);
            while (!operation.isDone)
            {
                yield return null;
            }
        }

        private static void AssertSpriteAt(string objectName, Vector3 expectedPosition)
        {
            var target = GameObject.Find(objectName);
            Assert.NotNull(target, "Missing runtime object: " + objectName);
            Assert.That(Vector3.Distance(target.transform.position, expectedPosition), Is.LessThan(0.02f), objectName + " is not at its layout marker.");
            Assert.That(target.transform.localScale.x, Is.GreaterThan(0f), objectName + " should have a positive visual scale.");
        }

        private static Component SpawnPlayerUnit(Component controller, int laneIndex)
        {
            var definitions = GetPlayerDefinitions(controller);

            var spawnUnit = ControllerType.GetMethod("SpawnUnit", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(spawnUnit);
            spawnUnit.Invoke(controller, new object[] { definitions[0], 0, laneIndex, 1f, 1f, 1f, null, false });

            var units = Object.FindObjectsByType(ActiveUnitType, FindObjectsSortMode.None).Cast<Component>().ToArray();
            Assert.Greater(units.Length, 0);
            return units.OrderBy(unit => unit.transform.position.x).First();
        }

        private static object[] GetPlayerDefinitions(Component controller)
        {
            var definitionsField = ControllerType.GetField("playerUnitDefinitions", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(definitionsField);
            var definitions = (object[])definitionsField.GetValue(controller);
            Assert.NotNull(definitions);
            Assert.Greater(definitions.Length, 0);
            return definitions;
        }

        private static System.Type TowerType => RequiredType("WarOfEras.Battle.Core.BattleTower");

        private static void InvokeUpgradeAge(Component controller, string pathName)
        {
            var evolutionType = ControllerType.GetNestedType("EvolutionPath", BindingFlags.NonPublic);
            Assert.NotNull(evolutionType);
            var value = System.Enum.Parse(evolutionType, pathName);
            var upgradeAge = ControllerType.GetMethod("UpgradeAge", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(upgradeAge);
            upgradeAge.Invoke(controller, new[] { value });
        }

        private static int InvokeInt(Component target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            return (int)method.Invoke(target, null);
        }

        private static void SetPrivateField<T>(Component target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(Component target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return (T)field.GetValue(target);
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property);
            return (T)property.GetValue(target);
        }

        private static string GetStringProperty(object target, string propertyName)
        {
            return GetProperty<string>(target, propertyName);
        }

        private static Sprite GetFirstUnitMoveFrame(object definition)
        {
            var frames = GetProperty<Sprite[]>(definition, "MoveFrames");
            Assert.NotNull(frames);
            Assert.Greater(frames.Length, 0);
            return frames[0];
        }

        private static Sprite GetFirstTowerFrame(Component tower)
        {
            var framesField = TowerType.GetField("frames", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(framesField);
            var frames = (Sprite[])framesField.GetValue(tower);
            Assert.NotNull(frames);
            Assert.Greater(frames.Length, 0);
            return frames[0];
        }

        private static void AssertSpriteResource(Sprite sprite, string expectedName)
        {
            Assert.NotNull(sprite);
            Assert.AreEqual(expectedName, sprite.name);
        }

        private static void AssertUnitButtonLabels(Component controller, params string[] expectedLabels)
        {
            var buttonsField = ControllerType.GetField("unitButtons", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(buttonsField);
            var bindings = ((System.Collections.IEnumerable)buttonsField.GetValue(controller)).Cast<object>().ToArray();
            Assert.GreaterOrEqual(bindings.Length, expectedLabels.Length);

            for (var i = 0; i < expectedLabels.Length; i++)
            {
                var label = GetProperty<Text>(bindings[i], "Label");
                Assert.NotNull(label);
                StringAssert.Contains(expectedLabels[i], label.text);
            }
        }

        private static void AssertTowerButtonLabel(Component controller, string expectedLabel)
        {
            var button = GetPrivateField<Button>(controller, "towerButton");
            Assert.NotNull(button);
            var labels = button.GetComponentsInChildren<Text>(true);
            Assert.IsTrue(labels.Any(label => label.text.Contains(expectedLabel)), "Tower button should contain label: " + expectedLabel);
        }

        private static System.Type ControllerType => RequiredType("WarOfEras.Battle.Core.BattleGameController");
        private static System.Type LayoutType => RequiredType("WarOfEras.Battle.Core.BattleLayout");
        private static System.Type ActiveUnitType => RequiredType("WarOfEras.Battle.Core.BattleUnit");
        private static System.Type MainMenuType => RequiredType("WarOfEras.MainMenu.MainMenuController");

        private static System.Type RequiredType(string typeName)
        {
            var type = System.Type.GetType(typeName + ", Assembly-CSharp");
            Assert.NotNull(type, "Missing runtime type: " + typeName);
            return type;
        }

        private static Component FindFirstObjectByType(System.Type type)
        {
            return Object.FindObjectsByType(type, FindObjectsSortMode.None).Cast<Component>().FirstOrDefault();
        }

        private static void AssertLayoutValid(Component layout)
        {
            var parameters = new object[] { null };
            var validate = LayoutType.GetMethod("Validate", BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(validate);
            var valid = (bool)validate.Invoke(layout, parameters);
            Assert.IsTrue(valid, (string)parameters[0]);
        }

        private static Vector3 GetVector3Property(Component target, string propertyName)
        {
            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property);
            return (Vector3)property.GetValue(target);
        }

        private static Vector3[] InvokeVector3Array(Component target, string methodName, params object[] parameters)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(method);
            return (Vector3[])method.Invoke(target, parameters);
        }
    }
}
