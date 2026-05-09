using System.Collections.Generic;
using Platformer.Mechanics;
using Platformer.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Platformer.UI
{
    /// <summary>
    /// The MetaGameController is responsible for switching control between the high level
    /// contexts of the application, eg the Main Menu and Gameplay systems.
    /// </summary>
    public class MetaGameController : MonoBehaviour
    {
        /// <summary>
        /// The main UI object which used for the menu.
        /// </summary>
        public MainUIController mainMenu;

        /// <summary>
        /// A list of canvas objects which are used during gameplay (when the main ui is turned off)
        /// </summary>
        public Canvas[] gamePlayCanvasii;

        /// <summary>
        /// The game controller.
        /// </summary>
        public GameController gameController;

        bool showMainCanvas = false;
        private InputAction m_MenuAction;

        void Awake()
        {
            CacheReferences();
        }

        void OnEnable()
        {
            CacheReferences();
            _ToggleMainMenu(showMainCanvas);
            m_MenuAction = InputSystem.actions.FindAction("Player/Menu");
        }

        void CacheReferences()
        {
            if (mainMenu == null)
            {
                mainMenu = FindFirstObjectByType<MainUIController>();
            }

            if (gameController == null)
            {
                gameController = FindFirstObjectByType<GameController>();
            }

            if (gamePlayCanvasii == null || gamePlayCanvasii.Length == 0)
            {
                var allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                var gameplayCanvases = new List<Canvas>(allCanvases.Length);
                var mainMenuCanvas = mainMenu != null ? mainMenu.GetComponentInParent<Canvas>() : null;

                foreach (var canvas in allCanvases)
                {
                    if (canvas != null && canvas != mainMenuCanvas)
                    {
                        gameplayCanvases.Add(canvas);
                    }
                }

                gamePlayCanvasii = gameplayCanvases.ToArray();
            }
        }

        /// <summary>
        /// Turn the main menu on or off.
        /// </summary>
        /// <param name="show"></param>
        public void ToggleMainMenu(bool show)
        {
            if (this.showMainCanvas != show)
            {
                _ToggleMainMenu(show);
            }
        }

        void _ToggleMainMenu(bool show)
        {
            if (show)
            {
                Time.timeScale = 0;
                if (mainMenu != null) mainMenu.gameObject.SetActive(true);
                if (gamePlayCanvasii != null)
                {
                    foreach (var i in gamePlayCanvasii)
                    {
                        if (i != null) i.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                Time.timeScale = 1;
                if (mainMenu != null) mainMenu.gameObject.SetActive(false);
                if (gamePlayCanvasii != null)
                {
                    foreach (var i in gamePlayCanvasii)
                    {
                        if (i != null) i.gameObject.SetActive(true);
                    }
                }
            }
            this.showMainCanvas = show;
        }

        void Update()
        {
            if (m_MenuAction != null && m_MenuAction.WasPressedThisFrame())
            {
                ToggleMainMenu(show: !showMainCanvas);
            }
        }

    }
}
