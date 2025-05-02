namespace ParaMoon
{
    /**
     * Enum representing different types of highlightable objects in the game.
     * This is used to categorize objects for highlighting purposes.
     *
     * Usage:
     * - Use this enum to define the type of object in the IHighlightable interface
     * - Helps in managing different highlight behaviors based on object type
     */
    public enum HighlightableType
    {
        Item,
        Inventory,
        NPC,
        Enemy,
        Ally,
        Container,
        Interactable,
    }
}