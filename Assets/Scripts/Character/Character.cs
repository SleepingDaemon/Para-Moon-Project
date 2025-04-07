using System;
using UnityEngine;

namespace ParaMoon
{
    /// <summary>
    /// Base class for all characters in the game
    /// </summary>
    public abstract class Character : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] protected string _characterName;
        [SerializeField] protected int _characterID;

        private void OnEnable()
        {
            _characterID = $"{gameObject.name}_{Guid.NewGuid()}".GetHashCode();
        }
    }
}
