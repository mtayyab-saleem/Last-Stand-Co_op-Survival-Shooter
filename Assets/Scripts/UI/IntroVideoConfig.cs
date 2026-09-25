using UnityEngine;
using UnityEngine.Video;

/// <summary>Which clip IntroVideo plays. Lives in Resources so no scene has to reference it.</summary>
[CreateAssetMenu(menuName = "Last Stand/Intro Video Config")]
public class IntroVideoConfig : ScriptableObject
{
    public VideoClip clip;
}
