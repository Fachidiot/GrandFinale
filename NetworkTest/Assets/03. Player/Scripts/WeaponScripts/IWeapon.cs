using UnityEngine;

public interface IWeapon
{
    GameObject WeaponGameObject { get; }
    string WeaponName { get; }
    int WeaponID { get; } // A unique identifier for the weapon type (e.g., for icons, stats lookup)
    BaseWeapon.SlotType Type { get; }
    int WeaponSlotIndex { get; set; } // The slot index this weapon currently occupies

    // Methods for equipping/unequipping animations and logic
    void Equip();
    void Unequip();

    // Method for attacking
    void Attack();

    // Method for dropping the weapon
    void Drop();

    // Method for handling UI interactions (e.g., display name, icon)
    // void GetUIInfo();
}