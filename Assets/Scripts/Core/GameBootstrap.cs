using UnityEngine;
using UnityEngine.SceneManagement;

namespace WarOfEras.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private Color backgroundColor = new Color(0.08f, 0.1f, 0.12f);

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle buttonStyle;
        private GameObject unitMarker;
        private string sceneName;

        private void Awake()
        {
            sceneName = gameObject.scene.name;
            EnsureCamera();
            CreateSceneContent();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (sceneName == "MainMenu")
                {
                    Application.Quit();
                }
                else
                {
                    SceneManager.LoadScene("MainMenu");
                }
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                if (sceneName == "MainMenu")
                {
                    SceneManager.LoadScene("Battle");
                }
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                SceneManager.LoadScene("MainMenu");
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene("Result");
            }

            if (sceneName == "Battle" && unitMarker != null)
            {
                var x = Mathf.PingPong(Time.time * 1.6f, 6f) - 3f;
                unitMarker.transform.position = new Vector3(x, -0.75f, 0f);
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            var panelWidth = Mathf.Min(620f, Screen.width - 48f);
            var panelHeight = sceneName == "Battle" ? 360f : 330f;
            var panel = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);

            GUILayout.BeginArea(panel);
            GUILayout.Label("War of Eras", titleStyle);
            GUILayout.Space(10f);

            if (sceneName == "MainMenu")
            {
                GUILayout.Label("Main Menu", subtitleStyle);
                GUILayout.Space(22f);
                if (GUILayout.Button("Start Battle", buttonStyle, GUILayout.Height(48f)))
                {
                    SceneManager.LoadScene("Battle");
                }
                if (GUILayout.Button("Result Screen", buttonStyle, GUILayout.Height(48f)))
                {
                    SceneManager.LoadScene("Result");
                }
                if (GUILayout.Button("Quit", buttonStyle, GUILayout.Height(48f)))
                {
                    Application.Quit();
                }
                GUILayout.Space(16f);
                GUILayout.Label("Return/Space: start  |  Esc: quit", bodyStyle);
            }
            else if (sceneName == "Battle")
            {
                GUILayout.Label("Battle Prototype Running", subtitleStyle);
                GUILayout.Space(16f);
                GUILayout.Label("This is the first runnable shell. The moving marker below confirms scene updates are active.", bodyStyle);
                GUILayout.Space(22f);
                if (GUILayout.Button("Finish Battle", buttonStyle, GUILayout.Height(48f)))
                {
                    SceneManager.LoadScene("Result");
                }
                if (GUILayout.Button("Back to Menu", buttonStyle, GUILayout.Height(48f)))
                {
                    SceneManager.LoadScene("MainMenu");
                }
                GUILayout.Space(16f);
                GUILayout.Label("R: result  |  M/Esc: menu", bodyStyle);
            }
            else
            {
                GUILayout.Label("Result", subtitleStyle);
                GUILayout.Space(22f);
                GUILayout.Label("Prototype session complete.", bodyStyle);
                GUILayout.Space(22f);
                if (GUILayout.Button("Play Again", buttonStyle, GUILayout.Height(48f)))
                {
                    SceneManager.LoadScene("Battle");
                }
                if (GUILayout.Button("Back to Menu", buttonStyle, GUILayout.Height(48f)))
                {
                    SceneManager.LoadScene("MainMenu");
                }
            }

            GUILayout.EndArea();
        }

        private void EnsureCamera()
        {
            var activeCamera = Camera.main;
            if (activeCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                activeCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            activeCamera.transform.position = new Vector3(0f, 0f, -10f);
            activeCamera.orthographic = true;
            activeCamera.orthographicSize = 3f;
            activeCamera.clearFlags = CameraClearFlags.SolidColor;
            activeCamera.backgroundColor = backgroundColor;
        }

        private void CreateSceneContent()
        {
            if (sceneName != "Battle")
            {
                return;
            }

            CreateLabel("Base", new Vector3(-3.35f, -1.45f, 0f), 0.22f, new Color(0.76f, 0.86f, 1f), FontStyle.Bold);
            CreateLabel("Enemy Base", new Vector3(3.35f, -1.45f, 0f), 0.22f, new Color(1f, 0.76f, 0.68f), FontStyle.Bold);
            unitMarker = CreateLabel("Unit", new Vector3(-3f, -0.75f, 0f), 0.24f, new Color(0.95f, 0.9f, 0.58f), FontStyle.Bold);
        }

        private static GameObject CreateLabel(string value, Vector3 position, float size, Color color, FontStyle style)
        {
            var labelObject = new GameObject(value);
            labelObject.transform.position = position;

            var text = labelObject.AddComponent<TextMesh>();
            text.text = value;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = size;
            text.color = color;
            text.fontStyle = style;

            return labelObject;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 52,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.82f, 0.9f, 1f) }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                wordWrap = true,
                normal = { textColor = new Color(0.75f, 0.82f, 0.9f) }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
