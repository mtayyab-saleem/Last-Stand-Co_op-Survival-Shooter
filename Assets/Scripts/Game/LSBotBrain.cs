using System.Collections.Generic;
using JUTPS;
using JUTPS.ItemSystem;
using JUTPS.WeaponSystem;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Drives one AI player. Runs on the server only; clients see the bot through the same
/// network sync as any other player.
///
/// Priorities, highest first:
///   1. Stay in the safe zone - head inwards once near the edge or outside it.
///   2. Fight a visible enemy - react, then shoot in bursts with aim that starts off and
///      settles in, while strafing and holding a comfortable distance.
///   3. Roam to random points inside the zone.
///
/// Movement uses the NavMesh when the scene has one baked, and falls back to steering
/// straight at the goal with simple obstacle avoidance when it does not.
/// </summary>
[DisallowMultipleComponent]
public class LSBotBrain : MonoBehaviour
{
    private const float ThinkInterval = 0.25f;
    private const float EyeHeight = 1.6f;
    private const float GoalReachedDistance = 2.5f;
    private const float PathRefreshInterval = 1.5f;
    private const float StuckCheckInterval = 1f;
    // Distance per stuck check under which a bot counts as blocked: about a third of
    // its speed, so a walking bot (~1.2 m/s) is not mistaken for a stuck one.
    private const float StuckDistanceRunning = 1.2f;
    private const float StuckDistanceWalking = 0.4f;

    // Obstacles are felt with a sphere about as wide as the body. A single ray from the
    // chest slipped past thin tree trunks while the shoulders still hit them, so a bot
    // would keep running on the spot against a tree.
    private const float BodyProbeRadius = 0.35f;
    private const float DetourHoldTime = 0.7f;
    private const float LowerWeaponAfter = 2.5f;

    private LSBotSettings settings;
    private LSPlayer self;
    private PlayerHealthManager health;
    private JUCharacterController character;
    private Animator animator;
    private SafeZoneController zone;

    private int obstacleMask;
    private bool navMeshAvailable;
    private int framesAlive;
    private bool equipped;
    private Weapon weapon;

    private float nextThinkTime;

    // Target
    private LSPlayer target;
    private float targetLastSeen;
    private bool targetVisible;
    private float reactionLeft;

    // Aim
    private Vector3 aimJitter;
    private float nextJitterTime;
    private float aimErrorScale = 1f;

    // Burst fire
    private bool burstFiring;
    private float burstTimeLeft;
    private float noTargetTime;

    // Movement
    private Vector3 goal;
    private bool hasGoal;
    private bool goalIsZone;
    private bool running;
    private readonly List<Vector3> pathCorners = new List<Vector3>();
    private int cornerIndex;
    private float nextPathTime;
    private NavMeshPath navPath;

    private float strafeSide = 1f;
    private float nextStrafeFlip;

    // Once a way round an obstacle is chosen it is kept for a moment; re-deciding every
    // frame turned the bot back into the tree as soon as the trunk left the probe.
    private Vector3 detourDirection;
    private float detourUntil;

    // Stuck handling
    private Vector3 lastCheckedPosition;
    private float nextStuckCheck;
    private int stuckCount;
    private Vector3 unstickDirection;
    private float unstickUntil;

    public void Initialise(LSBotSettings botSettings)
    {
        settings = botSettings ?? new LSBotSettings();
    }

    private void Awake()
    {
        self = GetComponent<LSPlayer>();
        health = GetComponent<PlayerHealthManager>();
        character = GetComponent<JUCharacterController>();
        animator = GetComponent<Animator>();

        obstacleMask = LayerMask.GetMask("Default", "Terrain", "Walls");
        navPath = new NavMeshPath();

        // The character ran its Awake as a player and grabbed the scene camera. JUTPS
        // drops it for an AI the first time it is asked for its forward orientation -
        // but only while MyPivotCamera is still set, so it must not be cleared by hand.
        // Asking now, before anything else runs, keeps every move and every shot from
        // being measured from the host's camera (which made bots walk the wrong way
        // and fire from the host's point of view).
        if (character != null && character.IsArtificialIntelligence)
            character.GetForwardOrientation();

        if (settings == null)
            settings = new LSBotSettings();
    }

    private void Start()
    {
        lastCheckedPosition = transform.position;
        strafeSide = Random.value < 0.5f ? -1f : 1f;

        // Only true if a NavMesh has been baked under this part of the map.
        navMeshAvailable = NavMesh.SamplePosition(transform.position, out _, 4f, NavMesh.AllAreas);
    }

    /// <summary>Called by the server when this bot is hit, so it turns on its attacker.</summary>
    public void OnHitBy(LSPlayer attacker)
    {
        if (attacker == null || attacker == self || !attacker.isAlive || IsTeammate(attacker))
            return;

        if (target != attacker)
        {
            target = attacker;
            reactionLeft = Random.Range(settings.reactionTime.x, settings.reactionTime.y) * 0.5f;
            aimErrorScale = 1f;
        }

        targetLastSeen = Time.time;
        UpdateAnimatorCulling();
    }

    private void Update()
    {
        if (!NetworkServer.active || character == null)
        {
            enabled = false;
            return;
        }

        if (IsOutOfPlay())
        {
            StandDown();
            enabled = false;
            return;
        }

        // Two frames for the inventory to finish its own setup before equipping.
        framesAlive++;
        if (framesAlive < 3)
            return;

        if (!equipped)
            TryEquip();

        if (Time.time >= nextThinkTime)
        {
            nextThinkTime = Time.time + ThinkInterval;
            Think();
        }

        float deltaTime = Time.deltaTime;
        Fight(deltaTime);
        Move();
    }

    private bool IsOutOfPlay()
    {
        if (health != null && health.netIsDead)
            return true;

        if (character.IsDead)
            return true;

        MatchTracker tracker = MatchTracker.Instance;
        return tracker != null && tracker.MatchEnded;
    }

    /// <summary>Everything off: used on death and when the match is decided.</summary>
    private void StandDown()
    {
        if (character == null)
            return;

        character._Move(0f, 0f, false);
        character.DefaultUseOfAllItems(false, false);
        character.FiringMode = false;
        character.IsAiming = false;
    }

    // -------------------------
    // Loadout
    // -------------------------

    private void TryEquip()
    {
        var inventory = character.Inventory;

        if (inventory == null || inventory.HoldableItensRightHand == null)
        {
            equipped = true;   // nothing to equip; fight unarmed rather than retry forever
            return;
        }

        JUHoldableItem chosen = null;

        foreach (string wanted in settings.preferredWeapons)
        {
            foreach (JUHoldableItem item in inventory.HoldableItensRightHand)
            {
                if (item == null || !item.Unlocked || !(item is Weapon))
                    continue;

                if (NameMatches(item, wanted))
                {
                    chosen = item;
                    break;
                }
            }

            if (chosen != null)
                break;
        }

        // Anything with a trigger will do.
        if (chosen == null)
        {
            foreach (JUHoldableItem item in inventory.HoldableItensRightHand)
            {
                if (item != null && item.Unlocked && item is Weapon)
                {
                    chosen = item;
                    break;
                }
            }
        }

        if (chosen == null)
        {
            equipped = true;
            return;
        }

        character.SwitchToItem(chosen.ItemSwitchID, true);

        if (character.WeaponInUseRightHand == null)
            return;   // switching was refused this frame (e.g. mid-animation); try again

        weapon = character.WeaponInUseRightHand;
        weapon.TotalBullets = Mathf.Max(weapon.TotalBullets, settings.reserveAmmo);

        // The weapon grabbed the scene camera for recoil before this bot was marked as
        // AI. Left in place, every bot shot would shake the host's camera.
        weapon.CamPivot = null;

        equipped = true;
    }

    private static bool NameMatches(JUHoldableItem item, string wanted)
    {
        if (string.IsNullOrWhiteSpace(wanted))
            return false;

        return string.Equals(item.ItemName, wanted, System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(item.gameObject.name, wanted, System.StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------
    // Decisions (a few times a second)
    // -------------------------

    private void Think()
    {
        if (zone == null)
            zone = FindFirstObjectByType<SafeZoneController>();

        // Something took the gun away (an item switch, a scene script): pick it up again
        // rather than fighting empty-handed.
        if (equipped && weapon != null && character.WeaponInUseRightHand != weapon)
        {
            equipped = false;
            weapon = null;
        }

        UpdateTarget();
        UpdateAnimatorCulling();
        ChooseGoal();
        CheckStuck();
    }

    /// <summary>
    /// The gun is held by the hand bone, so while the bot fights its bones must update
    /// even off the host's screen: with culling, a bot out of view froze its arm about
    /// 90 degrees off and every shot went sideways. Out of a fight nothing reads them,
    /// so an off-screen bot skips writing its bones like any other character - most
    /// bots, most of the time, are roaming somewhere the host cannot see.
    /// </summary>
    private void UpdateAnimatorCulling()
    {
        if (animator == null)
            return;

        AnimatorCullingMode mode = target != null
            ? AnimatorCullingMode.AlwaysAnimate
            : AnimatorCullingMode.CullUpdateTransforms;

        if (animator.cullingMode != mode)
            animator.cullingMode = mode;
    }

    private void UpdateTarget()
    {
        if (target != null && !IsValidEnemy(target))
            target = null;

        if (target != null)
        {
            targetVisible = CanSee(target);

            if (targetVisible)
                targetLastSeen = Time.time;
            else if (Time.time - targetLastSeen > settings.targetMemory)
                target = null;

            if (target != null && Distance(target.transform.position) > settings.detectRange * 1.25f)
                target = null;
        }

        if (target != null)
            return;

        // Nearest enemy in plain sight.
        LSPlayer best = null;
        float bestDistance = settings.detectRange;

        LSMatchManager match = LSMatchManager.Instance;
        if (match == null)
            return;

        foreach (LSPlayer candidate in match.players)
        {
            if (!IsValidEnemy(candidate))
                continue;

            float distance = Distance(candidate.transform.position);

            if (distance >= bestDistance || !CanSee(candidate))
                continue;

            best = candidate;
            bestDistance = distance;
        }

        if (best != null)
        {
            target = best;
            targetVisible = true;
            targetLastSeen = Time.time;
            reactionLeft = Random.Range(settings.reactionTime.x, settings.reactionTime.y);
            aimErrorScale = 1f;
        }
    }

    private bool IsValidEnemy(LSPlayer candidate)
    {
        if (candidate == null || candidate == self || !candidate.isAlive)
            return false;

        if (IsTeammate(candidate))
            return false;

        return !candidate.TryGetComponent(out PlayerHealthManager candidateHealth) || !candidateHealth.netIsDead;
    }

    private bool IsTeammate(LSPlayer other)
    {
        return self != null && self.teamID > 0 && other.teamID == self.teamID;
    }

    private bool CanSee(LSPlayer other)
    {
        Vector3 eye = transform.position + Vector3.up * EyeHeight;

        // Only the world blocks sight. A collider that belongs to either character
        // (a gun, a prop on the same layer) does not count as cover.
        if (!Physics.Linecast(eye, AimPoint(other), out RaycastHit hit, obstacleMask, QueryTriggerInteraction.Ignore))
            return true;

        LSPlayer owner = hit.collider.GetComponentInParent<LSPlayer>();
        return owner == other || owner == self;
    }

    private static Vector3 AimPoint(LSPlayer other)
    {
        if (other.TryGetComponent(out JUCharacterController otherCharacter) && otherCharacter.HumanoidSpine != null)
            return otherCharacter.HumanoidSpine.position;

        return other.transform.position + Vector3.up * 1.2f;
    }

    private void ChooseGoal()
    {
        running = false;

        // 1. The zone comes first.
        if (NeedsToMoveIntoZone())
        {
            if (!hasGoal || !goalIsZone || !zone.IsInsideZone(goal))
                SetGoal(RandomPointInZone(settings.zoneTargetDepth), true);

            running = true;
            return;
        }

        if (goalIsZone)
            hasGoal = false;   // made it in; the zone goal is done

        // 2. A fight in progress.
        if (target != null)
        {
            float distance = Distance(target.transform.position);

            if (distance > settings.engageRange || !targetVisible)
            {
                // Close in on where the target is, or was last seen.
                SetGoal(target.transform.position, false);
                running = distance > settings.preferredDistance * 1.5f;
            }
            else
            {
                hasGoal = false;   // hold position and strafe (see Move)
            }

            return;
        }

        // 3. Nothing going on: wander inside the zone.
        if (!hasGoal || Distance(goal) < GoalReachedDistance)
            SetGoal(RandomPointInZone(settings.roamDepth), false);

        running = Distance(goal) > 25f;
    }

    private bool NeedsToMoveIntoZone()
    {
        if (!ZoneActive())
            return false;

        float fromCentre = FlatDistance(transform.position, zone.ZoneCenter);
        return fromCentre > zone.CurrentRadius * settings.zoneComfort;
    }

    private bool ZoneActive()
    {
        if (zone == null)
            return false;

        SafeZoneController.ZonePhase phase = zone.Phase;
        return phase != SafeZoneController.ZonePhase.Idle && phase != SafeZoneController.ZonePhase.Ended;
    }

    private Vector3 RandomPointInZone(float depth)
    {
        if (zone == null || zone.Phase == SafeZoneController.ZonePhase.Ended)
        {
            // No zone to follow: wander around where we are.
            Vector2 wander = Random.insideUnitCircle * 30f;
            return transform.position + new Vector3(wander.x, 0f, wander.y);
        }

        // sqrt spreads the points evenly over the disc instead of bunching at the centre.
        Vector2 offset = Random.insideUnitCircle.normalized * (zone.CurrentRadius * depth * Mathf.Sqrt(Random.value));
        Vector3 centre = zone.ZoneCenter;

        return new Vector3(centre.x + offset.x, transform.position.y, centre.z + offset.y);
    }

    private void SetGoal(Vector3 newGoal, bool isZoneGoal)
    {
        // Small changes to a chasing goal should not throw the path away every tick.
        if (hasGoal && goalIsZone == isZoneGoal && FlatDistance(goal, newGoal) < 3f)
            return;

        goal = newGoal;
        hasGoal = true;
        goalIsZone = isZoneGoal;
        stuckCount = 0;
        nextPathTime = 0f;
    }

    private void CheckStuck()
    {
        if (Time.time < nextStuckCheck)
            return;

        nextStuckCheck = Time.time + StuckCheckInterval;

        bool tryingToMove = hasGoal || target != null;
        float moved = FlatDistance(transform.position, lastCheckedPosition);
        lastCheckedPosition = transform.position;

        float stuckDistance = running ? StuckDistanceRunning : StuckDistanceWalking;

        if (!tryingToMove || moved > stuckDistance || Time.time < unstickUntil)
            return;

        stuckCount++;

        // Back off and step sideways, which gets round trunks and corners. Alternate the
        // side on repeated attempts so a bot does not retry the same blocked way.
        Vector3 toGoal = hasGoal ? Flat(goal - transform.position) : transform.forward;
        float sideSign = stuckCount % 2 == 0 ? -1f : 1f;
        Vector3 side = Vector3.Cross(Vector3.up, toGoal.normalized) * sideSign;
        unstickDirection = (side - toGoal.normalized * 0.5f).normalized;
        unstickUntil = Time.time + 1.1f;
        detourUntil = 0f;

        // A hop only helps over something low; into a trunk or a wall it is wasted.
        Vector3 chest = transform.position + Vector3.up * 1.2f;
        if (character.IsGrounded && !IsWall(chest, toGoal.normalized, 1.5f))
            character._Jump();

        // Repeatedly stuck on the same goal: that goal is unreachable, pick another.
        if (stuckCount >= 3 && !goalIsZone)
            hasGoal = false;
    }

    // -------------------------
    // Every frame
    // -------------------------

    private void Fight(float deltaTime)
    {
        // Only with a gun actually in hand: fire mode with empty hands just walks the bot
        // sideways at its target.
        bool canShoot = weapon != null && character.WeaponInUseRightHand == weapon &&
                        target != null && targetVisible &&
                        Distance(target.transform.position) <= settings.engageRange;

        if (!canShoot)
        {
            character.DefaultUseOfAllItems(false, false);
            noTargetTime += deltaTime;

            if (noTargetTime > LowerWeaponAfter)
                character.FiringMode = false;

            return;
        }

        noTargetTime = 0f;

        character.FiringMode = true;
        character.FiringModeIK = true;
        character.IsAiming = false;

        // Aim: start off target, settle in, never quite perfect.
        aimErrorScale = Mathf.MoveTowards(aimErrorScale, settings.minimumAimError, settings.aimSettleSpeed * deltaTime);

        if (Time.time >= nextJitterTime)
        {
            nextJitterTime = Time.time + Random.Range(0.2f, 0.4f);
            aimJitter = Random.insideUnitSphere;
        }

        Vector3 aimPoint = AimPoint(target);
        float distanceScale = Mathf.Max(0.5f, Distance(aimPoint) / 20f);
        character.LookAtPosition = aimPoint + aimJitter * (settings.aimError * distanceScale * aimErrorScale);

        if (reactionLeft > 0f)
        {
            reactionLeft -= deltaTime;
            character.DefaultUseOfAllItems(false, false);
            return;
        }

        // Out of rounds: reload instead of dry-firing.
        if (weapon != null && weapon.BulletsAmounts <= 0)
        {
            character.DefaultUseOfAllItems(false, false, true);
            return;
        }

        burstTimeLeft -= deltaTime;

        if (burstTimeLeft <= 0f)
        {
            burstFiring = !burstFiring;
            burstTimeLeft = burstFiring
                ? Random.Range(settings.burstDuration.x, settings.burstDuration.y)
                : Random.Range(settings.burstPause.x, settings.burstPause.y);
        }

        character.DefaultUseOfAllItems(burstFiring, burstFiring);
    }

    private void Move()
    {
        Vector3 direction;

        if (Time.time < unstickUntil)
        {
            direction = unstickDirection;
        }
        else if (hasGoal)
        {
            direction = SteerAroundObstacles(DirectionToGoal());
        }
        else if (target != null)
        {
            direction = SteerAroundObstacles(CombatDirection());
        }
        else
        {
            direction = Vector3.zero;
        }

        if (direction.sqrMagnitude < 0.01f)
        {
            character._Move(0f, 0f, false);

            if (target == null)
                LookAhead(transform.forward);

            return;
        }

        direction.Normalize();
        character._Move(direction.x, direction.z, running);

        if (target == null || !targetVisible)
            LookAhead(direction);
    }

    /// <summary>Head and gun point where the bot is going when it is not fighting.</summary>
    private void LookAhead(Vector3 direction)
    {
        character.LookAtPosition = transform.position + Vector3.up * EyeHeight + Flat(direction).normalized * 10f;
    }

    /// <summary>Holds a comfortable distance from the target and strafes side to side.</summary>
    private Vector3 CombatDirection()
    {
        Vector3 toTarget = Flat(target.transform.position - transform.position);
        float distance = toTarget.magnitude;

        if (distance < 0.01f)
            return Vector3.zero;

        toTarget /= distance;

        if (Time.time >= nextStrafeFlip)
        {
            nextStrafeFlip = Time.time + Random.Range(1f, 2.5f);
            strafeSide = -strafeSide;
        }

        Vector3 strafe = Vector3.Cross(Vector3.up, toTarget) * strafeSide;

        float approach = 0f;
        if (distance < settings.tooCloseDistance) approach = -1f;
        else if (distance > settings.preferredDistance + 4f) approach = 0.6f;
        else if (distance < settings.preferredDistance - 4f) approach = -0.4f;

        return strafe + toTarget * approach;
    }

    private Vector3 DirectionToGoal()
    {
        if (FlatDistance(transform.position, goal) < GoalReachedDistance)
        {
            if (!goalIsZone)
                hasGoal = false;

            return Vector3.zero;
        }

        if (navMeshAvailable)
        {
            if (Time.time >= nextPathTime)
            {
                nextPathTime = Time.time + PathRefreshInterval;
                RebuildPath();
            }

            // Next corner of the NavMesh path, skipping the ones already reached.
            while (cornerIndex < pathCorners.Count && FlatDistance(transform.position, pathCorners[cornerIndex]) < 1.5f)
                cornerIndex++;

            if (cornerIndex < pathCorners.Count)
                return Flat(pathCorners[cornerIndex] - transform.position);
        }

        return Flat(goal - transform.position);
    }

    private void RebuildPath()
    {
        pathCorners.Clear();
        cornerIndex = 0;

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit from, 4f, NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(goal, out NavMeshHit to, 8f, NavMesh.AllAreas))
            return;

        if (NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, navPath) &&
            navPath.status != NavMeshPathStatus.PathInvalid)
        {
            pathCorners.AddRange(navPath.corners);
        }
    }

    /// <summary>
    /// Without a NavMesh the bot walks straight at its goal, so it feels ahead for walls
    /// and turns towards whichever side is open. A low obstacle is jumped instead.
    /// </summary>
    private Vector3 SteerAroundObstacles(Vector3 wanted)
    {
        wanted = Flat(wanted);

        if (wanted.sqrMagnitude < 0.01f)
            return wanted;

        wanted.Normalize();

        Vector3 chest = transform.position + Vector3.up * 1.2f;
        Vector3 knee = transform.position + Vector3.up * 0.4f;
        const float lookAhead = 2.5f;

        // Keep going round the obstacle already being avoided while that way is open.
        if (Time.time < detourUntil && !IsWall(chest, detourDirection, lookAhead))
            return detourDirection;

        bool chestBlocked = IsWall(chest, wanted, lookAhead);

        if (!chestBlocked)
        {
            // Something knee-high only: hop over it. A thin ray here, not the body-wide
            // probe - that one grazed every bump in the ground and kept the bot hopping.
            if (character.IsGrounded &&
                Physics.Raycast(knee, wanted, out RaycastHit low, 1.2f, obstacleMask, QueryTriggerInteraction.Ignore) &&
                low.normal.y < 0.6f)
                character._Jump();

            return wanted;
        }

        // Try progressively wider turns, preferring the side that stays closer to the goal.
        float preferred = strafeSide;
        float[] angles = { 45f, 90f, 135f };

        foreach (float angle in angles)
        {
            for (int side = 0; side < 2; side++)
            {
                float signed = angle * (side == 0 ? preferred : -preferred);
                Vector3 candidate = Quaternion.Euler(0f, signed, 0f) * wanted;

                if (!IsWall(chest, candidate, lookAhead))
                {
                    detourDirection = candidate;
                    detourUntil = Time.time + DetourHoldTime;
                    return candidate;
                }
            }
        }

        return -wanted;   // boxed in: back out
    }

    /// <summary>
    /// True for something the bot cannot walk up. Rising ground ahead is hit by the same
    /// rays, and treating a hillside as a wall made bots hop and turn on every slope.
    /// </summary>
    private bool IsWall(Vector3 origin, Vector3 direction, float distance)
    {
        // Start half a metre back: a cast ignores anything it starts inside, so a bot
        // already touching a trunk would otherwise never see it.
        const float backOff = 0.5f;

        if (!Physics.SphereCast(origin - direction * backOff, BodyProbeRadius, direction, out RaycastHit hit,
                                distance + backOff, obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        return hit.normal.y < 0.6f;   // steeper than about 53 degrees
    }

    // -------------------------
    // Helpers
    // -------------------------

    private float Distance(Vector3 point) => Vector3.Distance(transform.position, point);

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }
}
