using UnityEngine;

public interface IWeapon
{
    public enum SlotType
    {
        unarmed = 0,
        rifle = 1,
        smg = 2,
        pistol = 3,
        melee = 4,
    }

    SlotType Type { get; }
    int PlayerDamage { get; }
    GameObject gameObject { get; }
    Transform AimPoint { get; }
    float ResistanceForce { get; }
    float ResistanceSmoothing { get; }
    bool Attack();
}
