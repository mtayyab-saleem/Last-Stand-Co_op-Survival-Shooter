using UnityEngine;

/// <summary>
/// Which music track belongs to which scene.
/// Scenes that point at the same clip keep it playing instead of restarting it,
/// so walking Menu -> Lobby does not cut the track.
/// </summary>
[CreateAssetMenu(fileName = "MusicLibrary", menuName = "Last Stand/Music Library")]
public class MusicLibrarySO : ScriptableObject
{
    [System.Serializable]
    public class SceneTrack
    {
        [Tooltip("Scene name exactly as the scene asset is named, e.g. MenuScene.")]
        public string sceneName;

        [Tooltip("Leave empty to play nothing in that scene.")]
        public AudioClip clip;
    }

    [Header("Tracks")]
    [Tooltip("One row per scene. Point two scenes at the same clip to share a track.")]
    public SceneTrack[] tracks;

    [Header("Playback")]
    [Range(0f, 1f)]
    [Tooltip("Per-track trim. The player's Music slider is applied on top of this by the mixer.")]
    public float trackVolume = 1f;

    [Min(0f)]
    [Tooltip("Seconds to fade the old track out and the new one in.")]
    public float fadeSeconds = 1.25f;

    public bool loop = true;

    public AudioClip GetClip(string sceneName)
    {
        if (tracks == null)
            return null;

        for (int i = 0; i < tracks.Length; i++)
        {
            if (tracks[i] != null && tracks[i].sceneName == sceneName)
                return tracks[i].clip;
        }

        return null;
    }
}
