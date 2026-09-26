using UnityEngine;

public enum BotDifficulty { Easy, Medium, Hard }

/// <summary>
/// The AI difficulty picked in the Settings panel. Every player has their own, but only
/// the host's is used: it is read once when the host starts the match.
/// </summary>
public static class LSBotDifficulty
{
    public const string Key = "LSBotDifficulty";

    public static BotDifficulty Current
    {
        get { return (BotDifficulty)Mathf.Clamp(PlayerPrefs.GetInt(Key, (int)BotDifficulty.Hard), 0, 2); }
        set { PlayerPrefs.SetInt(Key, (int)value); PlayerPrefs.Save(); }
    }
}

/// <summary>
/// Tuning for the AI players. Lives on LSMatchManager, so it can be adjusted in the
/// Inspector of the persistent match object in LobbyScene.
///
/// The defaults aim for bots that feel like average players: they react, they miss
/// their first shots and settle in, and they fire in bursts rather than holding the
/// trigger down with perfect aim.
/// </summary>
[System.Serializable]
public class LSBotSettings
{
    [Header("Senses")]
    [Tooltip("How far away an enemy can be noticed, in metres.")]
    public float detectRange = 50f;

    [Tooltip("An enemy that stays out of sight this long is forgotten.")]
    public float targetMemory = 3f;

    [Header("Fighting")]
    [Tooltip("Shoots at enemies closer than this, in metres.")]
    public float engageRange = 35f;

    [Tooltip("Distance the bot tries to keep from its target while fighting.")]
    public float preferredDistance = 14f;

    [Tooltip("Closer than this, the bot backs off.")]
    public float tooCloseDistance = 6f;

    [Tooltip("Seconds between seeing an enemy and the first shot.")]
    public Vector2 reactionTime = new Vector2(0.45f, 0.9f);

    [Tooltip("How far off the first shots land, in metres, for a target 20 m away.")]
    public float aimError = 1.4f;

    [Tooltip("How quickly the aim settles onto the target.")]
    public float aimSettleSpeed = 1.2f;

    [Tooltip("Part of the aim error that never goes away. 0 = perfect aim once settled.")]
    [Range(0f, 1f)] public float minimumAimError = 0.25f;

    [Tooltip("Seconds of shooting per burst.")]
    public Vector2 burstDuration = new Vector2(0.35f, 0.9f);

    [Tooltip("Seconds between bursts.")]
    public Vector2 burstPause = new Vector2(0.6f, 1.4f);

    [Header("Movement")]
    [Tooltip("How far into the safe zone the bot heads when it has to move there (0 = centre, 1 = edge).")]
    [Range(0f, 1f)] public float zoneTargetDepth = 0.55f;

    [Tooltip("Roams to points within this part of the safe zone when nothing is happening.")]
    [Range(0f, 1f)] public float roamDepth = 0.75f;

    [Tooltip("Starts heading inwards once it is further out than this part of the zone radius.")]
    [Range(0f, 1f)] public float zoneComfort = 0.85f;

    [Header("Loadout")]
    [Tooltip("Guns the bots pick from, by item name. The first one found in the inventory is used.")]
    public string[] preferredWeapons = { "UMP", "P226", "SNIPER M82" };

    [Tooltip("Reserve ammo given to a bot's gun, so it never runs dry mid-match.")]
    public int reserveAmmo = 999;

    /// <summary>
    /// A copy tuned for <paramref name="difficulty"/>. Hard is exactly the values set in
    /// the Inspector; Medium and Easy notice enemies later, react slower, aim worse and
    /// pause longer between bursts.
    /// </summary>
    public LSBotSettings ForDifficulty(BotDifficulty difficulty)
    {
        var tuned = (LSBotSettings)MemberwiseClone();

        if (difficulty == BotDifficulty.Hard)
            return tuned;

        bool easy = difficulty == BotDifficulty.Easy;

        tuned.detectRange *= easy ? 0.7f : 0.85f;
        tuned.engageRange *= easy ? 0.7f : 0.85f;
        tuned.reactionTime *= easy ? 2f : 1.4f;
        tuned.aimError *= easy ? 2.2f : 1.5f;
        tuned.aimSettleSpeed *= easy ? 0.5f : 0.75f;
        tuned.minimumAimError = Mathf.Min(1f, minimumAimError + (easy ? 0.35f : 0.15f));
        tuned.burstPause *= easy ? 1.8f : 1.3f;
        return tuned;
    }

    [Header("Names")]
    public string[] names =
    {
        "Viper", "Ghost", "Raven", "Blaze", "Hunter", "Nova", "Shadow", "Titan",
        "Reaper", "Falcon", "Storm", "Wolf", "Cobra", "Phantom", "Striker", "Onyx"
    };
}
