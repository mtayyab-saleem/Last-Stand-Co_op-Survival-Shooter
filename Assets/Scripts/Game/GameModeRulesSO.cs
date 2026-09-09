using UnityEngine;

[CreateAssetMenu(
    fileName = "GameModeRules",
    menuName = "Last Stand/Game Mode Rules")]
public class GameModeRulesSO : ScriptableObject
{
    [System.Serializable]
    public class ModeRule
    {
        public LSMatchManager.GameMode mode;

        [Min(1)]
        [Tooltip("Minimum total players required before the host can start the match.")]
        public int minimumPlayersToStart = 1;

        [Min(1)]
        [Tooltip("Maximum number of players allowed inside one team.")]
        public int maximumPlayersPerTeam = 1;
    }

    [Header("Mode Rules")]
    public ModeRule[] rules;

    public ModeRule GetRule(LSMatchManager.GameMode mode)
    {
        if (rules == null)
            return null;

        foreach (ModeRule rule in rules)
        {
            if (rule != null && rule.mode == mode)
                return rule;
        }

        return null;
    }
}
