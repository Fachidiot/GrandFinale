using UnityEngine;

public abstract class BaseWeapon : MonoBehaviour, IWeapon
{
    [Header("Weapon Settings")]
    [SerializeField] protected IWeapon.SlotType slotType;
    public IWeapon.SlotType Type => slotType;

    [SerializeField] protected int playerDamage = 10;
    public int PlayerDamage => playerDamage;

    [Header("Components")]
    [SerializeField] protected Transform aimPoint;
    public Transform AimPoint => aimPoint;

    [Header("View Resistance")]
    [SerializeField] private float resistanceForce; // view offset rotation
    public float ResistanceForce => resistanceForce;

    [SerializeField] private float resistanceSmoothing; // view offset rotation speed
    public float ResistanceSmoothing => resistanceSmoothing;

    [SerializeField] private bool isMale;
    public bool IsMale => isMale;

    public abstract bool Attack();

    public void SetOwnerGender(bool isMale)
    {
        this.isMale = isMale;
    }

}
