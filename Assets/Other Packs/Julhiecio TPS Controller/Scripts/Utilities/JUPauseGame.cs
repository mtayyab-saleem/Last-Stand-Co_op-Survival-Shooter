using UnityEngine;
using UnityEngine.Events;
using JUTPS.InputEvents;
using JUTPSEditor.JUHeader;

namespace JUTPS
{
    /// <summary>
    /// The pause game system.
    /// </summary>
    [AddComponentMenu("JU TPS/Utilities/JU Pause Game")]
    public class JUPauseGame : MonoBehaviour
    {
        private static JUPauseGame _instance;
        private FX.JUSlowmotion _slowmotionInstance;

        /// <summary>
        /// If true, the user can pause the game using the inputs.
        /// </summary>
        [JUHeader("Pause Input")]
        public bool ControlsEnabled;

        /// <summary>
        /// The inputs used to pause/continue the game.
        /// </summary>
        public MultipleActionEvent PauseInputs;

        /// <summary>
        /// Called when the game is paused.
        /// </summary>
        [JUHeader("On Pause Events")]
        public UnityEvent OnPause;

        /// <summary>
        /// Called when the game is unpaused.
        /// </summary>
        public UnityEvent OnContinue;

        /// <summary>
        /// Return true if the game is paused.
        /// </summary>
        public static bool IsPaused { get; private set; }

        /// <summary>
        /// Returns the pause manager on the scene, if not exist, create one. Can have only one instance.
        /// </summary>
        public static JUPauseGame Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = FindObjectOfType<JUPauseGame>(true);

                    if (!_instance)
                    {
                        _instance = new GameObject("JU Pause Game").AddComponent<JUPauseGame>();
                        _instance.PauseInputs.OnButtonsDown.AddListener(Pause);
                    }
                }
                return _instance;
            }
            set { }
        }

        /// <summary>
        /// Create an instance of the <seealso cref="JUPauseGame"/> component.
        /// </summary>
        public JUPauseGame()
        {
            ControlsEnabled = true;
        }

        private void Awake()
        {
            if (this != Instance)
            {
                Destroy(this);
                return;
            }

            _slowmotionInstance = FindObjectOfType<FX.JUSlowmotion>();
            PauseInputs.OnButtonsDown.AddListener(OnPressPauseInput);
        }

        private void OnEnable()
        {
            PauseInputs.Enable();
        }

        private void OnDisable()
        {
            PauseInputs.Disable();
        }

        private void OnDestroy()
        {
            // Fix problem on unload the current scene if the game is paused, 
            // after load another scene the IsPaused continues as true.
            Continue();
        }

        private void OnPressPauseInput()
        {
            if (!ControlsEnabled)
                return;

            SetPaused(!IsPaused);
        }

        /// <summary>
        /// Pause the game.
        /// </summary>
        public static void Pause()
        {
            SetPaused(true);
        }

        /// <summary>
        /// Continue the game if is paused.
        /// </summary>
        public static void Continue()
        {
            SetPaused(false);
        }

        /// <summary>
        /// Set the game as paused of unpaused.
        /// </summary>
        /// <param name="paused">If true, the game is paused, freezing the time, if not, the game will be continued.</param>
        public static void SetPaused(bool paused)
        {
            if (IsPaused == paused)
                return;

            IsPaused = !IsPaused;
            Time.timeScale = IsPaused ? 0 : 1;

            if (Instance._slowmotionInstance)
                Instance._slowmotionInstance.EnableSlowmotion = !IsPaused;

            if (IsPaused) Instance.OnPause.Invoke();
            else Instance.OnContinue.Invoke();
        }
    }
}