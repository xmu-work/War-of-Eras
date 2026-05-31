using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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

            AssertSpriteAt("Player Barbarian Base Art", GetVector3Property(layout, "PlayerBasePosition"));
            AssertSpriteAt("Enemy Barbarian Base Art", GetVector3Property(layout, "EnemyBasePosition"));

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
            return units[0];
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
