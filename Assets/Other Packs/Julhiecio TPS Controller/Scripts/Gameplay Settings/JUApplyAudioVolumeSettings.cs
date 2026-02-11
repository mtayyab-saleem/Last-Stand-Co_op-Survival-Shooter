using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.GameSettings
{
    /// <summary>
    /// Apply the <seealso cref="JUTPS.GameSettings.JUGameSettings.AudioVolume"/> to an AudioSource.
    /// </summary>
    public class JUApplyAudioVolumeSettings : MonoBehaviour
    {
        /// <summary>
        /// The audio source that will receive the volume settings.
        /// </summary>
        public AudioSource AudioSource;

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
        /// Call to sync the audio volume with the <seealso cref="JUTPS.GameSettings.JUGameSettings.AudioVolume"/>.
        /// </summary>
        public void ApplySettings()
        {
            if (!AudioSource)
                return;

            AudioSource.volume = JUGameSettings.AudioVolume;
        }
    }
}