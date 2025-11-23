using UnityEngine;

public class UnarmedWeapon : BaseWeapon
{
    // Unarmed doesn't need a physical GameObject usually, but BaseWeapon requires it.
    // For simplicity, we can assign the player's root GameObject or simply leave it null
    // if the BaseWeapon.WeaponGameObject getter can handle null safely.
    // For now, we'll assign the current GameObject.
    [field:SerializeField] public override GameObject WeaponGameObject { get; protected set; }
    [field:SerializeField] public override string WeaponName { get; protected set; } = "Unarmed";
    [field:SerializeField] public override int WeaponID { get; protected set; } = 0; // ID 0 for unarmed
    [field:SerializeField] public override SlotType Type { get; protected set; } = SlotType.unarmed;


    private void Awake()
    {
        if (WeaponGameObject == null)
        {
            WeaponGameObject = this.gameObject; // Assign the player's root or a dummy GameObject
        }
    }

    public override void Equip()
    {
        // Unarmed doesn't have a model to activate/deactivate,
        // or its "model" is the player's bare hands.
        // So, this might do nothing or trigger unarmed animations.
        Debug.Log("Equipping Unarmed");
        // base.Equip(); // Don't call base.Equip() if no model to set active
    }

    public override void Unequip()
    {
        // Unarmed doesn't have a model to activate/deactivate.
        Debug.Log("Unequipping Unarmed");
        // base.Unequip(); // Don't call base.Unequip() if no model to set active
    }

    public override void Attack()
    {
        Debug.Log("[UnarmedWeapon] Punching!");
        // Play unarmed attack animation, maybe a small hit detection.
        // This will be handled by the PlayerCombatController.
    }

    public override void Drop()
    {
        // Cannot drop unarmed
        Debug.Log("[UnarmedWeapon] Cannot drop unarmed state.");
    }
}