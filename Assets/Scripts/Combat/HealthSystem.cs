using System;
using UnityEngine;

namespace ParaMoon
{
    /**
     * Class representing the health system of an entity.
     * Implements IHealth and IDamageable interfaces.
     *
     * Usage:
     * - Used to manage health, damage, and healing of entities
     * - Can be extended for different types of health systems (e.g., player, enemy)
     */
    public class HealthSystem : MonoBehaviour, IHealth, IDamageable
    {
        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;

        [SerializeField] float _currentHealth;
        [SerializeField] float _maxHealth;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public bool IsAlive => _currentHealth > 0;

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }
        public void Heal(float amount)
        {
            throw new NotImplementedException();
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            _currentHealth -= damageInfo.DamageAmount;

            if (_currentHealth <= 0)
            {
                Dead();
            }
        }

        private void Dead()
        {
            _currentHealth = 0;
            gameObject.SetActive(false);    
            OnDeath?.Invoke();
        }

        public float GetHealthPercentage(float percent)
        {
            return (_currentHealth / _maxHealth) * percent;
        }
    }
}