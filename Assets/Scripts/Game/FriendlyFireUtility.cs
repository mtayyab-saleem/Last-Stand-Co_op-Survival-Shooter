using UnityEngine;

/// <summary>
/// Small shared helper used by weapon hit systems.
/// It only blocks hits when both objects are network players
/// with the same valid team ID.
/// </summary>
public static class FriendlyFireUtility
{
    public static bool IsSameTeam(GameObject attackerObject, GameObject targetObject)
    {
        if (attackerObject == null || targetObject == null)
            return false;

        LSPlayer attacker = attackerObject.GetComponentInParent<LSPlayer>();
        LSPlayer target = targetObject.GetComponentInParent<LSPlayer>();

        // Non-player objects keep their normal JUTPS behaviour.
        if (attacker == null || target == null)
            return false;

        // Ignore self hits too.
        if (attacker == target)
            return true;

        // -1 means a team has not been assigned yet.
        if (attacker.teamID < 0 || target.teamID < 0)
            return false;

        return attacker.teamID == target.teamID;
    }
}
