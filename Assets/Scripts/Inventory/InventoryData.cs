using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace ParaMoon
{
    /**
     * Represents a container for inventory items, including its properties and behavior.
     * This class is responsible for managing the container's state, such as dimensions,
     * item stacking, and item type restrictions.
     *
     * Usage:
     * - Create an instance of InventoryContainerData to represent a container in the inventory
     * - Use methods to manage container properties and behavior
     */
    [CreateAssetMenu(fileName = "New Inventory", menuName = "Para Moon/Inventory/Container")]
    public class InventoryData : ScriptableObject
    {
        [SerializeField] ContainerType _containerType = ContainerType.None;
        [SerializeField] string _containerName;

        [Header("Grid Settings")]
        [SerializeField, Min(1)] int _width = 5; // Number of columns
        [SerializeField, Min(1)] int _height = 5; // Number of rows

        [Header("Visual Settings")]
        [SerializeField] Vector2 _cellSize = new(32, 32);
        [SerializeField] Vector2 _spacing = new(2, 2);

        [Header("Functionality")]
        [SerializeField] bool _allowRotation = false;
        [SerializeField] bool _allowItemStacking = true;

        [Header("Restrictions")]
        [SerializeField] bool _restrictItemTypes = false;
        [SerializeField] List<ItemType> _acceptedItemTypes;

        public Vector2Int Size => new(_width, _height);
        public Vector2 CellSize => _cellSize;
        public Vector2 Spacing => _spacing;
        public bool AllowRotation => _allowRotation;
        public IReadOnlyList<ItemType> AcceptedItemTypes => _restrictItemTypes ? _acceptedItemTypes : null;
        public bool AllowStackingItems => _allowItemStacking;
    }

    public enum ContainerType
    {
        None,
        Player,
        Vendor,
        Loot,
        Storage,
        Implant,
        Armor,
        Constructing,
    }
}