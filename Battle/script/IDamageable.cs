public interface IDamageable
{
    void TakeDamage(int amount, object source = null);
    bool IsDead { get; }
}
