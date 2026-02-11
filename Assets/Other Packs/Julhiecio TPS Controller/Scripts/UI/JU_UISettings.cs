using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using JUTPS.InputEvents;
using JUTPS.GameSettings;

namespace JUTPS.UI
{
    /// <summary>
    /// The game settings screen.
    /// </summary>
    public class JU_UISettings : MonoBehaviour
    {
        /// <summary>
        /// The controls settings screen.
        /// </summary>
        [System.Serializable]
        public class ControlsUI
        {
            /// <summary>
            /// The min camera rotation sensitive, can't be greater than <see cref="MaxRotationSensitive"/>.
            /// </summary>
            [Min(0.1f)] public float MinRotationSensitive;

            /// <summary>
            /// The max camera rotation sensitive, can't be less than <see cref="MinRotationSensitive"/>.
            /// </summary>
            [Min(0.2f)] public float MaxRotationSensitive;

            /// <summary>
            /// The camera rotation sensitive UI slider.
            /// </summary>
            public Slider RotationSensitive;

            /// <summary>
            /// The toggle to invert the camera vertical orientation.
            /// </summary>
            public Toggle InvertVertical;

            /// <summary>
            /// The toggle to invert the camera horizontal orientation.
            /// </summary>
            public Toggle InvertHorizontal;

            public ControlsUI()
            {
                MinRotationSensitive = 0.1f;
                MaxRotationSensitive = 10f;
            }

            internal void Setup()
            {
                if (RotationSensitive)
                {
                    RotationSensitive.minValue = MinRotationSensitive;
                    RotationSensitive.maxValue = MaxRotationSensitive;
                    RotationSensitive.value = JUGameSettings.CameraSensibility;
                    RotationSensitive.onValueChanged.AddListener(OnChangeCameraSensitive);
                }

                if (InvertVertical)
                {
                    InvertVertical.isOn = JUGameSettings.CameraInvertVertical;
                    InvertVertical.onValueChanged.AddListener(OnToggleInvertCameraVertical);
                }

                if (InvertHorizontal)
                {
                    InvertHorizontal.isOn = JUGameSettings.CameraInvertHorizontal;
                    InvertHorizontal.onValueChanged.AddListener(OnToggleInvertCameraHorizontal);
                }
            }

            private void OnChangeCameraSensitive(float sensitive)
            {
                JUGameSettings.CameraSensibility = sensitive;
            }

            private void OnToggleInvertCameraVertical(bool invert)
            {
                JUGameSettings.CameraInvertVertical = invert;
            }

            private void OnToggleInvertCameraHorizontal(bool invert)
            {
                JUGameSettings.CameraInvertHorizontal = invert;
            }
        }

        /// <summary>
        /// The graphics settings screen.
        /// </summary>
        [System.Serializable]
        public class GraphicsUI
        {
            /// <summary>
            /// The min render scale allowed, can't be greater than <see cref="MaxRenderScale"/>.
            /// </summary>
            [Min(0.1f)] public float MinRenderScale;

            /// <summary>
            /// The max render scale allowed, can't be less than <see cref="MinRenderScale"/>
            /// </summary>
            [Min(0.2f)] public float MaxRenderScale;

            /// <summary>
            /// The quality settings UI dropdown.
            /// </summary>
            public Dropdown Quality;

            /// <summary>
            /// The render scale UI slider.
            /// </summary>
            public Slider RenderScale;

            public GraphicsUI()
            {
                MinRenderScale = 0.25f;
                MaxRenderScale = 1;
            }

            internal void Setup()
            {
                if (Quality)
                {
                    Quality.value = JUGameSettings.GraphicsQuality;
                    Quality.onValueChanged.AddListener(OnChangeQuality);
                }

                if (RenderScale)
                {
                    RenderScale.minValue = MinRenderScale;
                    RenderScale.maxValue = MaxRenderScale;
                    RenderScale.value = JUGameSettings.RenderScale;
                    RenderScale.onValueChanged.AddListener(OnChangeRenderScale);
                }
            }

            private void OnChangeQuality(int qualityIndex)
            {
                JUGameSettings.GraphicsQuality = qualityIndex;
            }

            private void OnChangeRenderScale(float scale)
            {
                JUGameSettings.RenderScale = scale;
            }
        }

        /// <summary>
        /// The audio settings screen.
        /// </summary>
        [System.Serializable]
        public class AudioUI
        {
            /// <summary>
            /// The min audio volume, can be greater than <see cref="MaxVolume"/>.
            /// </summary>
            [Range(0, 1)] public float MinVolume;

            /// <summary>
            /// The max audio volume, can be less than <see cref="MinVolume"/>.
            /// </summary>
            [Range(0, 1)] public float MaxVolume;

            /// <summary>
            /// The UI slider to control the audio volume.
            /// </summary>
            public Slider Volume;

            public AudioUI()
            {
                MinVolume = 0;
                MaxVolume = 1;
            }

            internal void Setup()
            {
                if (Volume)
                {
                    Volume.minValue = MinVolume;
                    Volume.maxValue = MaxVolume;
                    Volume.value = JUGameSettings.AudioVolume;
                    Volume.onValueChanged.AddListener(OnChangeVolume);
                }
            }

            private void OnChangeVolume(float volume)
            {
                JUGameSettings.AudioVolume = volume;
            }
        }

        /// <summary>
        /// Use inputs to exit of the pause screen instead of use UI buttons.
        /// </summary>
        public MultipleActionEvent CloseScreenAction;

        /// <summary>
        /// The exit settings screen UI button.
        /// </summary>
        public Button ExitButton;

        /// <summary>
        /// An event called when the screen is closed.
        /// </summary>
        public UnityEvent OnClose;

        /// <summary>
        /// The controls settings screen.
        /// </summary>
        public ControlsUI ControlsScreen;

        /// <summary>
        /// The graphics settings screen
        /// </summary>
        public GraphicsUI GraphicsScreen;

        /// <summary>
        /// The audio settings screen.
        /// </summary>
        public AudioUI AudioScreen;

        private void Awake()
        {
            Setup();
        }

        private void OnEnable()
        {
            CloseScreenAction.Enable();
        }

        private void OnDisable()
        {
            CloseScreenAction.Disable();
        }

        private void Setup()
        {
            if (ExitButton)
                ExitButton.onClick.AddListener(OnPressExitButton);

            CloseScreenAction.OnButtonsDown.AddListener(OnPressExitButton);

            ControlsScreen.Setup();
            GraphicsScreen.Setup();
            AudioScreen.Setup();
        }

        private void OnPressExitButton()
        {
            Close();
        }

        /// <summary>
        /// Close the settings screen if is opened.
        /// </summary>
        public void Close()
        {
            // Is already inactive.
            if (!gameObject.activeSelf)
                return;

            gameObject.SetActive(false);
            OnClose.Invoke();
        }
    }
}