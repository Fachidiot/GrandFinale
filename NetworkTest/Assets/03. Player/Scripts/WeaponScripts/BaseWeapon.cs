using UnityEngine;

public abstract class BaseWeapon : MonoBehaviour, IWeapon
{
    public enum SlotType
    {
        unarmed = 0,
        rifle = 1,
        smg = 2,
        pistol = 3,
        melee = 4
    }

    public virtual GameObject WeaponGameObject { get; protected set; }
    public virtual string WeaponName { get; protected set; }
    public virtual int WeaponID { get; protected set; }
    public virtual SlotType Type { get; protected set; }
    public int WeaponSlotIndex { get; set; }

    public virtual void Equip()
    {
        if (WeaponGameObject != null)
        {
            WeaponGameObject.SetActive(true);
        }
        // Additional common equip logic here
    }

    public virtual void Unequip()
    {
        if (WeaponGameObject != null)
        {
            WeaponGameObject.SetActive(false);
        }
        // Additional common unequip logic here
    }

    public abstract void Attack();

    public virtual void Drop()
    {
        // Default drop logic. This will likely be handled by a manager,
        // but the weapon can have some inherent drop behavior or data.
        Debug.Log($"Dropping {WeaponName}");
        // This will be replaced by InventoryManager logic later.
    }

    // Common properties or methods for all weapons can go here.
}