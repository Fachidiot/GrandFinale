public interface ICombatStats
{
    float MaxHP { get; }
    float CurrentHP { get; }
    bool IsDead { get; }

    void TakeDamage(float amount);
    void Heal(float amount);
    void RestoreFullHP();
}
