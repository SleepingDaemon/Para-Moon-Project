using System;
using UnityEngine;

namespace ParaMoon
{
    /**
     * Abstract base class for item effects.
     * Extend this class to create different types of effects that items can have.
     *
     * Usage:
     * - Create subclasses for specific effect types
     * - Assign effect instances to items in the inspector
     */
    [System.Serializable]
    public abstract class ItemEffect : ScriptableObject
    {
        /**
         * Apply the effect to the specified user.
         * This method should be implemented to define what happens when the effect is applied.
         *
         * @param user The GameObject using the item (e.g., player)
         */
        public abstract void ApplyEffect(GameObject user);
    }

    /**
     * Example subclass of ItemEffect for healing effects.
     * This class should define how the healing effect is applied to the user.
     *
     * Usage:
     * - Create a new ScriptableObject in Unity and assign it to this class
     * - Define properties like healing amount, duration, etc.
     */
    [CreateAssetMenu(fileName = "New Healing Effect", menuName = "Para Moon/Inventory/Effects/Healing")]
    public class HealingEffect : ItemEffect
    {
        [SerializeField] int _healingAmount;

        public override void ApplyEffect(GameObject user)
        {
            var health = user.GetComponent<IHealth>();
            if (health != null)
                health.Heal(_healingAmount);
        }
    }
}