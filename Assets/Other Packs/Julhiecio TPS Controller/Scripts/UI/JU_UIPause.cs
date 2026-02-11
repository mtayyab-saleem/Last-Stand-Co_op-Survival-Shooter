using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JUTPS.UI
{
    /// <summary>
    /// The game pause screen.
    /// </summary>
    public class JU_UIPause : MonoBehaviour
    {
        /// <summary>
        /// Cursor visibility state.
        /// </summary>
        public enum CursorVisibility
        {
            /// <summary>
            /// The cursor is visible.
            /// </summary>
            Show,

            /// <summary>
            /// The cursor is invisible.
            /// </summary>
            Hide
        }

        /// <summary>
        /// The cursor lock state.
        /// </summary>
        public enum CursorControl
        {
            /// <summary>
            /// The cursor can move.
            /// </summary>
            Free,

            /// <summary>
            /// The cursor is locked on screen center.
            /// </summary>
            Lock
        }

        /// <summary>
        /// The cursor visibility state when the game is paused.
        /// </summary>
        [Header("Cursor On Pause")]
        public CursorVisibility CursorVisibilityOnPause;

        /// <summary>
        /// The cursor lock mode when the game is paused.
        /// </summary>
        public CursorControl CursorControlOnPause;

        /// <summary>
        /// The cursor visibility when the game is unpaused.
        /// </summary>
        [Space]
        public CursorVisibility CursorVisibilityOnContinue;

        /// <summary>
        /// The cursor lock state when the game is unpaused.
        /// </summary>
        public CursorControl CursorControlOnContinue;

        /// <summary>
        /// The scene name of the menu scene, used when the <see cref="MainMenuButton"/> is pressed.
        /// </summary>
        [Header("Scenes")]
        [SerializeField] private string MainMenuScene;

        /// <summary>
        /// The Pause screen UI.
        /// </summary>
        [Header("Screens")]
        public GameObject PauseScreen;

        /// <summary>
        /// The game settings screen, can be accessed by the pause screen.
        /// </summary>
        public JU_UISettings SettingsScreen;

        /// <summary>
        /// The "continue game" button, used to unpause the game calling <seealso cref="JUPauseGame.Continue"/>.
        /// </summary>
        [Header("Buttons")]
        public Button ContinueButton;

        /// <summary>
        /// The "game settings" button, shows the settings screen. <para/>
        /// See <seealso cref="JU_UISettings"/>
        /// </summary>
        public Button SettingsButton;

        /// <summary>
        /// The button used to go to the game main menu.
        /// </summary>
        public Button MainMenuButton;

        /// <summary>
        /// The button used to close the game application.
        /// </summary>
        public Button ExitGameButton;

        /// <summary>
        /// The game pause system.
        /// </summary>
        public JUPauseGame PauseManager
        {
            get => JUPauseGame.Instance;
        }

        private void Awake()
        {
            Setup();
        }

        private void OnDestroy()
        {
            Unsetup();
        }

        private void Setup()
        {
            if (PauseScreen) PauseScreen.gameObject.SetActive(false);
            if (ContinueButton) ContinueButton.onClick.AddListener(OnPressContinueButton);
            if (SettingsButton) SettingsButton.onClick.AddListener(OnPressSettingsButton);
            if (MainMenuButton) MainMenuButton.onClick.AddListener(OnPressMainMenuButton);
            if (ExitGameButton) ExitGameButton.onClick.AddListener(OnPressExitGameButton);

            if (PauseManager)
            {
                PauseManager.OnPause.AddListener(OnPauseGame);
                PauseManager.OnContinue.AddListener(OnContinueGame);
            }

            if (SettingsScreen)
            {
                SettingsScreen.gameObject.SetActive(false);
                SettingsScreen.OnClose.AddListener(OnCloseSettingsScreen);
            }
        }

        private void Unsetup()
        {
            if (PauseManager)
            {
                PauseManager.OnPause.RemoveListener(OnPauseGame);
                PauseManager.OnContinue.RemoveListener(OnContinueGame);
            }
        }

        private void SetCursorMode(CursorVisibility visibility, CursorControl control)
        {
            switch (visibility)
            {
                case CursorVisibility.Show:
                    Cursor.visible = true;
                    break;
                case CursorVisibility.Hide:
                    Cursor.visible = false;
                    break;
                default:
                    break;
            }

            switch (control)
            {
                case CursorControl.Free:
                    Cursor.lockState = CursorLockMode.None;
                    break;
                case CursorControl.Lock:
                    Cursor.lockState = CursorLockMode.Locked;
                    break;
                default:
                    break;
            }
        }

        private void OnCloseSettingsScreen()
        {
            if (PauseManager)
                PauseManager.ControlsEnabled = true;

            PauseScreen.gameObject.SetActive(true);
        }

        private void OnPauseGame()
        {
            if (!PauseScreen)
                return;

            SetCursorMode(CursorVisibilityOnPause, CursorControlOnPause);
            PauseScreen.SetActive(true);
        }

        private void OnContinueGame()
        {
            if (!PauseScreen)
                return;

            SetCursorMode(CursorVisibilityOnContinue, CursorControlOnContinue);
            PauseScreen.SetActive(false);
        }

        private void OnPressContinueButton()
        {
            JUPauseGame.Continue();
        }

        private void OnPressSettingsButton()
        {
            if (SettingsScreen) SettingsScreen.gameObject.SetActive(true);
            if (PauseScreen) PauseScreen.gameObject.SetActive(false);

            // Can't unpause the game if isn't on pause screen.
            if (PauseManager)
                PauseManager.ControlsEnabled = false;
        }

        private void OnPressMainMenuButton()
        {
            if (string.IsNullOrEmpty(MainMenuScene))
                return;

            SceneManager.LoadSceneAsync(MainMenuScene);

            // Disable the screen to avoid any user interaction when the game is loading another scene.
            gameObject.SetActive(false);
        }

        private void OnPressExitGameButton()
        {
            Application.Quit();
        }
    }
}