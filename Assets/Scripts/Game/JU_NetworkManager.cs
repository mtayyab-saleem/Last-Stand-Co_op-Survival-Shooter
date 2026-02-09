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
        // Agar main Local Player nahi hoon (Dushman hoon) aur Bullet ne mujhe locally damage diya hai
        if (!isLocalPlayer)
        {
            // Check agar health kam hui hai
            if (_juHealth.Health < netHealth)
            {
                float damageAmount = netHealth - _juHealth.Health;

                // CRITICAL FIX: Health ko wapis reset karein taake glitch na ho
                _juHealth.Health = netHealth;
                _juHealth.IsDead = netIsDead; // Death state bhi reset karein

                // Server ko batayen
                var myPlayer = NetworkClient.connection.identity.GetComponent<JU_NetworkManager>();
                if (myPlayer != null)
                {
                    myPlayer.CmdReportDamage(this.gameObject, damageAmount);
                }
            }
        }

        // Agar main Local Player hoon (Fall damage etc)
        if (isLocalPlayer)
        {
            if (_juHealth.Health < netHealth)
            {
                float damageAmount = netHealth - _juHealth.Health;
                _juHealth.Health = netHealth;
                CmdReportDamage(this.gameObject, damageAmount);
            }
        }
    }

    [Command]
    public void CmdReportDamage(GameObject target, float amount)
    {
        if (target != null)
        {
            var targetScript = target.GetComponent<JU_NetworkManager>();
            if (targetScript != null)
            {
                targetScript.ApplyDamageOnServer(amount);
            }
        }
    }

    [Server]
    public void ApplyDamageOnServer(float amount)
    {
        if (netIsDead) return; // Agar pehle se dead hai to kuch na karein

        netHealth -= amount;
        if (netHealth <= 0)
        {
            netHealth = 0;
            netIsDead = true; // Server par death confirm
        }

        // Server par JUHealth update
        _juHealth.Health = netHealth;
        _juHealth.CheckHealthState();
    }

    // --- HOOKS ---

    void OnServerHealthChanged(float oldHealth, float newHealth)
    {
        _juHealth.Health = newHealth;

        // Sirf Local Player (Victim) ko Blood dikhayein
        if (isLocalPlayer && newHealth < oldHealth && !netIsDead)
        {
            BloodScreen.PlayerTakingDamaged();
        }
    }

    void OnDeathStateChanged(bool oldState, bool newState)
    {
        _juHealth.IsDead = newState;

        if (newState == true) // Agar Dead hua hai
        {
            _juHealth.Health = 0;
            _juHealth.CheckHealthState(); // Ye JU TPS ki death logic (Ragdoll/Anim) chalayega
        }
        else // Agar Respawn hua hai
        {
            // Respawn logic agar chahiye ho to yahan ayegi
        }
    }
}