using UnityEngine;

namespace ParaMoon
{
    /**
     * Interface defining objects that can be highlighted in the game world.
     * Implements visual feedback when objects are under the player's cursor.
     *
     * Usage:
     * - Implement on objects that should show highlighting and name display
     * - Typically implemented alongside IInteractable
     */
    public interface IHighlightable
    {
        /**
         * Gets the name to display when highlighting the object.
         * 
         * @return The display name
         */
        string GetHighlightName();

        /**
         * Gets the color used for highlighting the object.
         * 
         * @return The highlight color, or Color.clear to use default
         */
        Color GetHighlightColor();

        /**
         * Gets the renderer(s) to use for calculating screen bounds.
         * @return Array of renderers to use for highlighting
         */
        Renderer[] GetHighlightRenderers();

        /**
         * Gets additional data to display in the highlight UI.
         * For example, health percentage for enemies.
         * @return Array of key-value pairs to display
         */
        HighlightData[] GetHighlightData();

        /**
         * Gets the type of highlightable object.
         * 
         * @return The highlightable type
         */
        HighlightableType GetHighlightableType();
    }
}