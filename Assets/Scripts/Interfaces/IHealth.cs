using System;

namespace ParaMoon
{
    public interface IHealth
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsAlive { get; }
        event Action<float, float> OnHealthChanged;
        event Action OnDeath;

        void Heal(float amount);
    }

    public interface IDamageable
    {
        void TakeDamage(DamageInfo damageInfo);
    }
}