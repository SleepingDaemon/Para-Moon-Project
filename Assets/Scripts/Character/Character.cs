using System;
using UnityEngine;

namespace ParaMoon
{
    /// <summary>
    /// Base class for all characters in the game
    /// </summary>
    [RequireComponent(typeof(HealthSystem))]
    public abstract class Character : HighlightableBase
    {
        [Header("Identity")]
        [SerializeField] protected string _characterName;
        [SerializeField] protected int _characterID;

        protected HealthSystem _healthSystem;

        public HealthSystem HealthSystem => _healthSystem;

        private void Awake()
        {
            _characterID = $"{gameObject.name}_{Guid.NewGuid()}".GetHashCode();

            _healthSystem = GetComponent<HealthSystem>();
        }
    }
}
