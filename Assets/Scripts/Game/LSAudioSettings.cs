using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Music and SFX volume plus on/off, saved in PlayerPrefs and pushed into the
/// GameAudioMixer's exposed parameters.
///
/// Sliders are linear 0..1 but a mixer works in decibels, so the value is
/// converted on the way in. The mixer lives in Resources so any scene can reach
/// it without an Inspector reference.
/// </summary>
public static class LSAudioSettings
{
    public const string MusicVolumeParameter = "MusicVolume";
    public const string SfxVolumeParameter = "SFXVolume";

    private const string MixerResourceName = "GameAudioMixer";

    private const string MusicVolumeKey = "SETTINGS_AUDIO_MUSIC_VOLUME";
    private const string SfxVolumeKey = "SETTINGS_AUDIO_SFX_VOLUME";
    private const string MusicEnabledKey = "SETTINGS_AUDIO_MUSIC_ENABLED";
    private const string SfxEnabledKey = "SETTINGS_AUDIO_SFX_ENABLED";

    /// <summary>Mixer value that counts as silence.</summary>
    private const float MinDecibels = -80f;
    private const float MaxDecibels = 0f;

    private static AudioMixer cachedMixer;

    public static AudioMixer Mixer
    {
        get
        {
            if (cachedMixer == null)
                cachedMixer = Resources.Load<AudioMixer>(MixerResourceName);

            return cachedMixer;
        }
    }

    public static float MusicVolume
    {
        get { return PlayerPrefs.GetFloat(MusicVolumeKey, 0.6f); }
        set
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
            Apply();
        }
    }

    public static float SfxVolume
    {
        get { return PlayerPrefs.GetFloat(SfxVolumeKey, 1f); }
        set
        {
            PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
            Apply();
        }
    }

    public static bool MusicEnabled
    {
        get { return PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1; }
        set
        {
            PlayerPrefs.SetInt(MusicEnabledKey, value ? 1 : 0);
            Apply();
        }
    }

    public static bool SfxEnabled
    {
        get { return PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1; }
        set
        {
            PlayerPrefs.SetInt(SfxEnabledKey, value ? 1 : 0);
            Apply();
        }
    }

    /// <summary>
    /// Pushes the saved values into the mixer.
    ///
    /// Returns false when the mixer refused them. AudioMixer.SetFloat silently fails
    /// during the first frames after startup, before the audio system is live, so the
    /// caller has to retry - LSSettingsRuntime owns that retry loop.
    /// </summary>
    public static bool Apply()
    {
        AudioMixer mixer = Mixer;

        if (mixer == null)
        {
            Debug.LogWarning("[LSAudioSettings] GameAudioMixer not found in Resources. Volume settings are inactive.");
            return false;
        }

        bool music = mixer.SetFloat(MusicVolumeParameter, ToDecibels(MusicEnabled ? MusicVolume : 0f));
        bool sfx = mixer.SetFloat(SfxVolumeParameter, ToDecibels(SfxEnabled ? SfxVolume : 0f));

        return music && sfx;
    }

    /// <summary>
    /// True when the mixer currently holds the saved values.
    ///
    /// Something in Unity's audio startup resets exposed parameters after the first
    /// frame, so callers poll this and re-Apply instead of assuming one successful
    /// SetFloat sticks forever.
    /// </summary>
    public static bool IsApplied()
    {
        AudioMixer mixer = Mixer;

        if (mixer == null)
            return true;   // nothing to enforce

        float music, sfx;

        if (!mixer.GetFloat(MusicVolumeParameter, out music)) return false;
        if (!mixer.GetFloat(SfxVolumeParameter, out sfx)) return false;

        return Mathf.Abs(music - ToDecibels(MusicEnabled ? MusicVolume : 0f)) < 0.1f
            && Mathf.Abs(sfx - ToDecibels(SfxEnabled ? SfxVolume : 0f)) < 0.1f;
    }

    /// <summary>
    /// A linear slider maps badly onto loudness, so the value is converted with a
    /// log curve. That keeps the bottom half of the slider actually useful.
    /// </summary>
    private static float ToDecibels(float linear)
    {
        if (linear <= 0.0001f)
            return MinDecibels;

        return Mathf.Clamp(Mathf.Log10(linear) * 20f, MinDecibels, MaxDecibels);
    }
}
