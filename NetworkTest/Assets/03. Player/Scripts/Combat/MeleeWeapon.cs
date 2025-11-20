using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// This component is attached to a melee weapon prefab.
/// It holds the weapon's stats and handles collision detection during an attack swing.
/// </summary>
public class MeleeWeapon : MonoBehaviour
{
    [Header("Weapon Stats")]
    public float damage = 15f;
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
    public void BeginAttack()
    {
        hitTargets.Clear();
        damageCollider.enabled = true;
        Debug.Log("[MeleeWeapon] Attack Begun. Collider enabled.");
    }

    /// <summary>
    /// Called by an Animation Event at the end of the weapon's swing.
    /// Disables the damage collider.
    /// </summary>
    public void EndAttack()
    {
        damageCollider.enabled = false;
        Debug.Log("[MeleeWeapon] Attack Ended. Collider disabled.");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore triggers that are not from the local player's authority.
        // This check assumes the CombatManager is on the same root object as the NetworkPlayer component.
        var networkPlayer = GetComponentInParent<NetworkPlayer>();
        if (networkPlayer != null && !networkPlayer.IsMine)
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
        if (monsterHealth != null)
        {
            hitTargets.Add(other); // Add to the list of hit targets for this swing.
            
            var networkMonster = other.GetComponent<NetworkMonster>();
            if (networkMonster != null && NetworkManager.Instance != null)
            {
                Debug.Log($"[MeleeWeapon] Hit monster {networkMonster.MonsterId}. Sending damage report.");

                // If we are the host, apply damage directly.
                if (NetworkManager.Instance.Mode == NetworkMode.Host)
                {
                    monsterHealth.TakeDamage(damage);
                }
                // If we are a client, send a message to the host.
                else
                {
                    JObject damageData = new JObject
                    {
                        ["type"] = "player_dealt_damage",
                        ["monsterId"] = networkMonster.MonsterId,
                        ["damage"] = damage
                    };
                    NetworkManager.Instance.SendJsonMessage(NetworkManager.Instance.LobbyHostID, damageData);
                }
            }
        }
    }
}
