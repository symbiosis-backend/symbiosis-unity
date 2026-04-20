namespace VoidSurvivor
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(DamageInfo damage);
    }
}
