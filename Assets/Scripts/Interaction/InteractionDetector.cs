using System;
using UnityEngine;

namespace ParaMoon
{
    /**
     * Handles detection of interactable objects using raycasting.
     * Also integrates with the highlighting system to highlight objects in view.
     *
     * Dependencies:
     * - HighlightManager for object highlighting
     *
     * Usage:
     * - Created by interactors to detect objects in their field of view
     * - Configured with source transform, max distance, and layer mask
     */
    public class InteractionDetector
    {
        readonly Transform _source;
        readonly float _maxInteractDistance;
        readonly float _maxHighlightDistance;
        readonly LayerMask _interactableMask;
        readonly RaycastHit[] _hitResults = new RaycastHit[1];
        IHighlightable _lastHighlighted;

        public InteractionDetector(Transform source, float interactDistance, float highlightDistance, LayerMask interactableMask)
        {
            _source = source;
            _maxInteractDistance = interactDistance;
            _maxHighlightDistance = highlightDistance;
            _interactableMask = interactableMask;
        }

        /**
     * Detects interactable objects in view via raycasting.
     * Also handles highlighting of detected objects.
     * 
     * @return The detected interactable object, or null if none found
     */
        public IInteractable GetInteractableInView()
        {
            Ray ray = new(_source.position, _source.forward);
            IInteractable interactable = null;

            int hitCount = Physics.RaycastNonAlloc(ray, _hitResults, _maxHighlightDistance, _interactableMask);

            // First perform highlight detection (longer distance)
            if (hitCount > 0)
            {
                // Use cached hit result
                RaycastHit highlightHit = _hitResults[0];

                // Try to find highlightable component
                IHighlightable highlightable = highlightHit.collider.GetComponent<IHighlightable>() ??
                                              highlightHit.collider.GetComponentInParent<IHighlightable>();

                // Handle highlighting
                // If we have a highlightable object and it's not the last highlighted one
                if (highlightable != null && highlightable != _lastHighlighted)
                {
                    if (HighlightManager.Instance != null)
                        HighlightManager.Instance.HighlightObject(highlightable);

                    _lastHighlighted = highlightable;
                }

                // Check for interactable objects - but only if they're within interaction distance

                if (highlightHit.distance <= _maxInteractDistance)
                {
                    interactable = highlightHit.collider.GetComponent<IInteractable>() ??
                                  highlightHit.collider.GetComponentInParent<IInteractable>();
                }
            }
            else
            {
                // Clear highlighting if nothing hit
                if (_lastHighlighted != null && HighlightManager.Instance != null)
                {
                    HighlightManager.Instance.ClearHighlight();
                    _lastHighlighted = null;
                }
            }

            return interactable;
        }
    }
}