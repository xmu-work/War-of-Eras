#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WarOfEras.EditorTools
{
    public static class AutoPlayFromMainMenu
    {
        public static void Run()
        {
            // 编辑器辅助入口：统一从主菜单开始播放，避免直接进 Battle 时漏掉会话选择流程。
            const string mainMenuScene = "Assets/Scenes/MainMenu.unity";

            if (!System.IO.File.Exists(mainMenuScene))
            {
                Debug.LogError($"AutoPlay failed: scene not found at {mainMenuScene}");
                return;
            }

            EditorSceneManager.OpenScene(mainMenuScene);
            EditorApplication.delayCall += () =>
            {
                Debug.Log("AutoPlay: starting from MainMenu scene.");
                EditorApplication.isPlaying = true;
            };
        }
    }
}
#endif
