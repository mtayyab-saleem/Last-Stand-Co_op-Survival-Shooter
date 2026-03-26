using UnityEngine;
using Mirror;
using JUTPS;
using JUTPS.FX;

[RequireComponent(typeof(JUHealth))]
public class JU_NetworkManager : NetworkBehaviour
{
    private JUHealth _juHealth;

    [SyncVar(hook = nameof(OnServerHealthChanged))]
    private float netHealth = 100f;

    [SyncVar(hook = nameof(OnDeathStateChanged))]
    private bool netIsDead = false;

    void Awake()
    {
        _juHealth = GetComponent<JUHealth>();
    }

    public override void OnStartServer()
    {
        netHealth = _juHealth.Health;
        netIsDead = _juHealth.IsDead;
    }

    void Update()
    {
        // Detect damage applied locally by a bullet and report it to the server
        if (!isLocalPlayer && _juHealth.Health < netHealth)
        {
            float damage = netHealth - _juHealth.Health;

            // Reset local values to server authority to prevent visual glitch
            _juHealth.Health = netHealth;
            _juHealth.IsDead = netIsDead;

            var localPlayer = NetworkClient.connection?.identity?.GetComponent<JU_NetworkManager>();
            localPlayer?.CmdReportDamage(this.gameObject, damage);
        }

        // Same check for local player (fall damage, explosions, etc.)
        if (isLocalPlayer && _juHealth.Health < netHealth)
        {
            float damage = netHealth - _juHealth.Health;
            _juHealth.Health = netHealth;
            CmdReportDamage(this.gameObject, damage);
        }
    }

    [Command]
    public void CmdReportDamage(GameObject target, float amount)
    {
        target?.GetComponent<JU_NetworkManager>()?.ApplyDamageOnServer(amount);
    }

    [Server]
    public void ApplyDamageOnServer(float amount)
    {
        if (netIsDead) return;

        netHealth -= amount;
        if (netHealth <= 0f)
        {
            netHealth = 0f;
            netIsDead = true;
        }

        _juHealth.Health = netHealth;
        _juHealth.CheckHealthState();
    }

    // --- SYNCVAR HOOKS ---

    void OnServerHealthChanged(float oldHealth, float newHealth)
    {
        _juHealth.Health = newHealth;

        if (isLocalPlayer && newHealth < oldHealth && !netIsDead)
            BloodScreen.PlayerTakingDamaged();
    }

    void OnDeathStateChanged(bool oldState, bool newState)
    {
        _juHealth.IsDead = newState;

        if (newState)
        {
            _juHealth.Health = 0f;
            _juHealth.CheckHealthState(); // Triggers ragdoll / death anim in JU TPS
        }
    }
}