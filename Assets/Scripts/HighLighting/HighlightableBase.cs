using UnityEngine;

namespace ParaMoon
{
    /**
     * Base class for highlightable objects in the game.
     * This class implements the IHighlightable interface and provides basic functionality.
     *
     * Usage:
     * - Extend this class to create custom highlightable objects
     * - Configure highlightable properties in inspector
     */
    public class HighlightableBase : MonoBehaviour, IHighlightable
    {
        [SerializeField] protected string _displayName;
        [SerializeField] protected Color _highlightColor = Color.clear; // Default to clear color
        [SerializeField] protected HighlightableType _highlightType = HighlightableType.Item; // Default type
        [SerializeField] protected bool _useAllRenderers = true;
        [SerializeField] protected Renderer[] _customRenderers;

        public HighlightableType GetHighlightableType()
        {
            return _highlightType;
        }

        public virtual Color GetHighlightColor()
        {
            return _highlightColor;
        }

        /*
         * Returns additional data to display in the highlight UI.
         * For example, health percentage for enemies.
         * This method can be overridden in derived classes to provide custom data.
         */
        public virtual HighlightData[] GetHighlightData()
        {
            return System.Array.Empty<HighlightData>();
        }

        public virtual string GetHighlightName()
        {
            return _displayName;
        }

        /*
         * Returns the renderers used for highlighting.
         * If _useAllRenderers is true, it returns all child renderers.
         * Otherwise, it returns the custom renderers specified in the inspector.
         */
        public Renderer[] GetHighlightRenderers()
        {
            if (!_useAllRenderers && _customRenderers != null && _customRenderers.Length > 0)
                return _customRenderers;

            return GetComponentsInChildren<Renderer>();
        }

        /*
         * Unity method called when the script is loaded or a value is changed in the inspector.
         * This method ensures that if _useAllRenderers is true, _customRenderers is set to null.
         */
        private void OnValidate()
        {
            if (_useAllRenderers)
                _customRenderers = null;
        }
    }
}