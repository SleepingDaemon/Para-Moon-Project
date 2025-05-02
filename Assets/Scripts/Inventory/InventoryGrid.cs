using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParaMoon
{
    public class InventoryGrid : IInventory, IInventoryProvider
    {
        readonly Vector2Int _gridSize;
        readonly Dictionary<Vector2Int, Item> _itemGrid = new();
        readonly Dictionary<Item, Vector2Int> _itemPositions = new();

        public Vector2Int GridSize => _gridSize;

        public IInventory Inventory => this;

        public event Action<IItem, Vector2Int> OnItemAdded;
        public event Action<IItem, Vector2Int> OnItemRemoved;
        public event Action<IItem, Vector2Int, Vector2Int> OnItemMoved;

        public InventoryGrid(InventoryData data)
        {
            _gridSize = data.Size;
        }

        public IItem GetItemAt(Vector2Int position)
        {
            if (_itemGrid.TryGetValue(position, out Item item))
                return item;

            return null;
        }

        public bool IsPositionFree(Vector2Int position, Vector2Int size)
        {
            // Check if the position is valid
            if (!IsPositionValid(position, size))
                return false;

            // Check if all cells are free
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    Vector2Int cell = position + new Vector2Int(x, y);
                    if (_itemGrid.ContainsKey(cell))
                        return false; // Cell is occupied
                }
            }

            return true; // All cells are free
        }

        public bool IsPositionValid(Vector2Int position, Vector2Int size)
        {
            // Check if the position is within the grid bounds
            if (position.x < 0 || position.y < 0 ||     // Negative indices
                position.x + size.x > _gridSize.x ||    // Exceeds grid width
                position.y + size.y > _gridSize.y)      // Exceeds grid height
                return false;

            return true;
        }

        public bool TryAddItem(IItem item, Vector2Int position)
        {
            if (item == null || !IsPositionValid(position, item.Size))
                return false;

            if (!IsPositionFree(position, item.Size))
                return false;

            if (!(item is Item inventoryItem))
                return false;

            // Add item to grid
            for (int y = 0; y < item.Size.y; y++)
            {
                for (int x = 0; x < item.Size.x; x++)
                {
                    Vector2Int cellPosition = position + new Vector2Int(x, y);
                    _itemGrid[cellPosition] = inventoryItem;
                }
            }

            // Track item position
            _itemPositions[inventoryItem] = position;

            OnItemAdded?.Invoke(item, position);
            return true;
        }

        public bool TryAddItem(IItem item, out Vector2Int position)
        {
            position = Vector2Int.zero;

            if (item == null)
                return false;

            // Find first available position
            for (int y = 0; y <= _gridSize.y - item.Size.y; y++)
            {
                for (int x = 0; x <= _gridSize.x - item.Size.x; x++)
                {
                    Vector2Int testPosition = new Vector2Int(x, y);

                    if (IsPositionFree(testPosition, item.Size))
                    {
                        position = testPosition;
                        return TryAddItem(item, position);
                    }
                }
            }

            return false; // No available position found
        }

        public bool TryMoveItem(Vector2Int fromPosition, Vector2Int toPosition)
        {
            if (!_itemGrid.TryGetValue(fromPosition, out Item item))
                return false;

            // Find the item's origin position
            Vector2Int fromOrigin = FindItemOriginPosition(fromPosition, item);

            // Check if the new position is valid and free
            if (!IsPositionValid(toPosition, item.Size))
                return false;

            // Check if the new position is free (ignoring the current item)
            if (!IsPositionFreeExcept(toPosition, item.Size, item))
                return false;

            // Remove item from grid but keep a reference
            for (int y = 0; y < item.Size.y; y++)
            {
                for (int x = 0; x < item.Size.x; x++)
                {
                    Vector2Int cellPosition = fromOrigin + new Vector2Int(x, y);
                    _itemGrid.Remove(cellPosition);
                }
            }

            // Add item at new position
            for (int y = 0; y < item.Size.y; y++)
            {
                for (int x = 0; x < item.Size.x; x++)
                {
                    Vector2Int cellPosition = toPosition + new Vector2Int(x, y);
                    _itemGrid[cellPosition] = item;
                }
            }

            // Update item position tracking
            _itemPositions[item] = toPosition;

            OnItemMoved?.Invoke(item, fromOrigin, toPosition);
            return true;
        }

        public bool IsPositionFreeExcept(Vector2Int toPosition, Vector2Int size, IItem exceptItem)
        {
            // Check if the position is valid
            if (!IsPositionValid(toPosition, size))
                return false;

            // Check if all cells are free or occupied by the excepted item
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    Vector2Int cell = toPosition + new Vector2Int(x, y);

                    if (_itemGrid.TryGetValue(cell, out Item occupyingItem))
                    {
                        if (occupyingItem != exceptItem)
                            return false; // Cell is occupied by a different item
                    }
                }
            }

            return true; // All cells are free or occupied by the excepted item
        }

        public bool TryRemoveItem(Vector2Int position, out IItem item)
        {
            item = null;

            if (!_itemGrid.TryGetValue(position, out Item inventoryItem))
                return false;

            // Find the item's origin position
            Vector2Int itemPosition = FindItemOriginPosition(position, inventoryItem);

            // Remove from all cells
            for (int y = 0; y < inventoryItem.Size.y; y++)
            {
                for (int x = 0; x < inventoryItem.Size.x; x++)
                {
                    Vector2Int cellPosition = itemPosition + new Vector2Int(x, y);
                    _itemGrid.Remove(cellPosition);
                }
            }

            // Remove from position tracking
            _itemPositions.Remove(inventoryItem);

            // Return item
            item = inventoryItem;

            OnItemRemoved?.Invoke(item, itemPosition);
            return true;
        }

        private Vector2Int FindItemOriginPosition(Vector2Int position, Item item)
        {
            if (_itemPositions.TryGetValue(item, out Vector2Int originPosition))
                return originPosition;

            // Fallback search method if position tracking fails
            for (int y = 0; y < item.Size.y; y++)
            {
                for (int x = 0; x < item.Size.x; x++)
                {
                    Vector2Int testPosition = new Vector2Int(x, y);

                    // Skip invalid positions
                    if (testPosition.x < 0 || testPosition.y < 0)
                        continue;

                    bool isOrigin = true;

                    // Check if all cells in the item's area are the same item
                    for (int j = 0; j < item.Size.y && isOrigin; j++)
                    {
                        for (int i = 0; i < item.Size.x && isOrigin; i++)
                        {
                            Vector2Int checkPosition = testPosition + new Vector2Int(i, j);

                            if (!_itemGrid.TryGetValue(checkPosition, out Item checkItem) || checkItem != item)
                                isOrigin = false;
                        }
                    }

                    if (isOrigin)
                        return testPosition; // Found the origin position
                }
            }

            return position; // Fallback to the provided position if not found
        }

        public IEnumerable<(Item item, Vector2Int position)> GetAllItems()
        {
            HashSet<Item> processedItems = new HashSet<Item>();

            foreach (var kvp in _itemGrid)
            {
                Item item = kvp.Value;

                if (!processedItems.Contains(item))
                {
                    Vector2Int position = FindItemOriginPosition(kvp.Key, item);
                    yield return (item, position);
                    processedItems.Add(item);
                }
            }
        }

        public void Clear()
        {
            _itemGrid.Clear();
            _itemPositions.Clear();
        }
    }
}