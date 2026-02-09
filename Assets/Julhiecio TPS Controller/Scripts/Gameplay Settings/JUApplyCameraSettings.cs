using UnityEngine;
using JUTPS.CameraSystems;

namespace JUTPS.GameSettings
{
    /// <summary>
    /// Apply the camera settings with <seealso cref="JUGameSettings"/>.
    /// </summary>
    public class JUApplyCameraSettings : MonoBehaviour
    {
        /// <summary>
        /// The camera to apply the settings.
        /// </summary>
        public JUCameraController CameraController;

        private void Awake()
        {
            JUGameSettings.OnApplySettings += ApplySettings;
        }

        private void OnDestroy()
        {
            JUGameSettings.OnApplySettings -= ApplySettings;
        }

        private void OnEnable()
        {
            ApplySettings();
        }

        /// <summary>
        /// Apply the camera settings with <seealso cref="JUGameSettings"/>.
        /// </summary>
        public void ApplySettings()
        {
            if (!CameraController)
                return;

            CameraController.GeneralSensibility = JUGameSettings.CameraSensibility;
            CameraController.GeneralVerticalSensibility = JUGameSettings.CameraSensibility;

            CameraController.InvertVertical = JUGameSettings.CameraInvertVertical;
            CameraController.InvertHorizontal = JUGameSettings.CameraInvertHorizontal;
        }
    }
}