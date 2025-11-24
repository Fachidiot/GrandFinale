using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class MeleeWeapon : BaseWeapon
{
    [Header("Weapon Stats")]
    public float attackRange = 1.5f; // Used for visualization or AI, the collider is the authority

    [Header("Internal References")]
    [SerializeField] private Collider damageCollider;

    // A list to track which enemies have been hit during the current swing
    // to prevent a single swing from hitting the same enemy multiple times.
    private List<Collider> hitTargets;

    private void Awake()
    {
        if (damageCollider == null)
        {
            damageCollider = GetComponent<Collider>();
        }
        damageCollider.isTrigger = true;
        damageCollider.enabled = false; // The collider should be disabled by default.

        hitTargets = new List<Collider>();
    }

    /// <summary>
    /// Called by an Animation Event at the start of the weapon's swing.
    /// Enables the damage collider and clears the list of targets hit in the previous swing.
    /// </summary>
    public override bool Attack()
    {
        hitTargets.Clear();
        damageCollider.enabled = true;
        Debug.Log($"[{gameObject.name}] Attack Begun. Collider enabled.");
        return true;
    }

    /// <summary>
    /// Called by an Animation Event at the end of the weapon's swing.
    /// Disables the damage collider.
    /// </summary>
    public void EndAttack()
    {
        damageCollider.enabled = false;
        Debug.Log($"[{gameObject.name}] Attack Ended. Collider disabled.");
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = GetComponentInParent<IPlayerControllable>();
        if (player == null || !player.IsMine)
        {
            return;
        }

        // Check if we've already hit this target during this swing.
        if (hitTargets.Contains(other))
        {
            return;
        }

        // Check if the hit object is a monster.
        var monsterHealth = other.GetComponent<MonsterHealth>();
        if (monsterHealth == null) return;

        hitTargets.Add(other); // Add to the list of hit targets for this swing.

        var networkMonster = other.GetComponent<NetworkMonster>();
        if (networkMonster != null && NetworkManager.Instance != null)
        {
            Debug.Log($"[{gameObject.name}] Hit monster {networkMonster.MonsterId} for {PlayerDamage} damage. Sending damage report.");

            // If we are the host, apply damage directly.
            if (NetworkManager.Instance.Mode == NetworkMode.Host)
            {
                monsterHealth.TakeDamage(PlayerDamage);
            }
            // If we are a client, send a message to the host.
            else
            {
                JObject damageData = new JObject
                {
                    ["type"] = "player_dealt_damage",
                    ["monsterId"] = networkMonster.MonsterId,
                    ["damage"] = PlayerDamage
                };
                NetworkManager.Instance.SendJsonMessage(NetworkManager.Instance.LobbyHostID, damageData);
            }
        }
    }
}
