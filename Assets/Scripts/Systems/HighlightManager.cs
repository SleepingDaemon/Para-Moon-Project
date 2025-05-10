using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ParaMoon
{
    /*
     * HighlightManager is responsible for managing the highlighting of objects in the game.
     * It provides visual feedback when objects are under the player's cursor.
     * 
     * Usage:
     * - Attach this script to a GameObject in the scene.
     * - Assign the UI elements and settings in the inspector.
     */
    [Injectable]
    public class HighlightManager : ServiceBehaviour<HighlightManager>
    {
        static HighlightManager _instance;
        public static HighlightManager Instance => _instance;

        [Header("References")]
        [SerializeField] Canvas _uiCanvas;
        [SerializeField] RectTransform _highlightContainer;
        [SerializeField] Image _outlinePrefab;
        [SerializeField] Image _fillPrefab;
        [SerializeField] TextMeshProUGUI _nameTextPrefab;
        [SerializeField] GameObject _dataRowPrefab;

        [Header("Highlighting Settings")]
        [SerializeField] float _outlineThickness = 2f;
        [SerializeField] float _nameVerticalOffset = 10f;
        [SerializeField] float _dataRowSpacing = 20f;
        [SerializeField] float _minHighlightSize = 50f;
        [SerializeField] float _maxDistanceToHighlight = 12f;
        [SerializeField] bool _showBackgroundFill = true;
        [SerializeField] Color _fillColor = new(0.1f, 0.1f, 0.1f, 0.5f);
        [SerializeField] private int _highlightUpdateFrequency = 2; // Update every 2 frames

        [Header("Type Colors")]
        [SerializeField] Color _itemColor = new(0.2f, 0.8f, 0.2f);
        [SerializeField] Color _npcColor = new(0.2f, 0.6f, 1f);
        [SerializeField] Color _enemyColor = new(1f, 0.2f, 0.2f);
        [SerializeField] Color _defaultColor = new(0.8f, 0.8f, 0.8f);

        // Active highlight elements
        Camera _mainCamera;
        IHighlightable _currentHighlighted;
        Image _outlineImage;
        Image _fillImage;
        TextMeshProUGUI _nameText;
        List<GameObject> _dataRows = new();
        Dictionary<HighlightableType, Color> _highlightColors = new();
        List<GameObject> _dataRowPool = new List<GameObject>();
        RectTransform _highlightValuesContainer;
        private int _frameCount;

        #region Unity Methods

        protected override void Awake()
        {
            _outlineImage = _outlinePrefab.GetComponent<Image>();
            _nameText = _nameTextPrefab.GetComponent<TextMeshProUGUI>();
            _highlightValuesContainer = (RectTransform)_highlightContainer.transform.Find("HightlightValues");
        }
        private void Start()
        {
            if (_instance == null)
            {
                _instance = this;

                // Find camera if not already set
                if (_mainCamera == null)
                {
                    _mainCamera = Camera.main;
                }

                // Set up highlight colors
                _highlightColors[HighlightableType.Item] = _itemColor;
                _highlightColors[HighlightableType.NPC] = _npcColor;
                _highlightColors[HighlightableType.Enemy] = _enemyColor;
                _highlightColors[HighlightableType.Container] = _defaultColor;

                // Set up UI if not already set
                ValidateUIElements();

                // Create or update UI elements
                CreateOrUpdateUIElements();

                // Hide elements initially
                ClearHighlight();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /**
         * Update highlight position to follow moving objects.
         */
        private void LateUpdate()
        {
            _frameCount++;

            if (_currentHighlighted != null)
            {
                // Check if object is still in range
                bool isInRange = IsInRange(_currentHighlighted);

                if (!isInRange || !IsVisibleOnScreen(_currentHighlighted))
                {
                    ClearHighlight();
                    return;
                }

                // Update highlight position and information
                if (_frameCount % _highlightUpdateFrequency == 0)
                {
                    UpdateHighlightDisplay();
                }
            }
        }

        #endregion

        /**
         * Creates or updates UI elements for highlighting.
         * This can be called when switching scenes or when UI needs to be recreated.
         */
        public void CreateOrUpdateUIElements()
        {
            // Ensure UI container exists and is set up
            if (_highlightContainer == null)
            {
                if (_uiCanvas != null)
                {
                    // Try to find or create container
                    _highlightContainer = _uiCanvas.transform.Find("Highlight Container") as RectTransform;
                    if (_highlightContainer == null)
                    {
                        GameObject containerObj = new GameObject("HighlightContainer");
                        _highlightContainer = containerObj.AddComponent<RectTransform>();
                        _highlightContainer.SetParent(_uiCanvas.transform, false);
                        _highlightContainer.anchorMin = Vector2.zero;
                        _highlightContainer.anchorMax = Vector2.one;
                        _highlightContainer.offsetMin = Vector2.zero;
                        _highlightContainer.offsetMax = Vector2.zero;
                    }
                }
                else
                {
                    Debug.LogError("UI Canvas is missing. Cannot create highlight UI elements.");
                    return;
                }
            }

            // Create or update outline
            if (_outlineImage == null || !_outlineImage.gameObject.scene.IsValid())
            {
                Image outlineInstance = Instantiate(_outlinePrefab);
                //outlineInstance.transform.SetParent(_highlightContainer, false);
                _outlineImage = outlineInstance;
                _outlineImage.gameObject.name = "HighlightOutline";
            }

            // Create or update fill
            if (_showBackgroundFill)
            {
                if (_fillImage == null || !_fillImage.gameObject.scene.IsValid())
                {
                    Image fillInstance = Instantiate(_fillPrefab);
                    //fillInstance.transform.SetParent(_highlightContainer, false);
                    _fillImage = fillInstance;
                    _fillImage.gameObject.name = "HighlightFill";
                }
                _fillImage.color = _fillColor;
            }
            else if (_fillImage != null)
            {
                Destroy(_fillImage.gameObject);
                _fillImage = null;
            }

            // Create or update text
            if (_nameText == null || !_nameText.gameObject.scene.IsValid())
            {
                TextMeshProUGUI nameInstance = Instantiate(_nameTextPrefab);
                //nameInstance.transform.SetParent(_highlightContainer, false);
                _nameText = nameInstance;
                _nameText.gameObject.name = "HighlightNameText";
            }
        }

        private void ValidateUIElements()
        {
            if (_uiCanvas == null)
                Debug.LogError("UI Canvas is not set in HighlightManager!");

            if (_highlightContainer == null)
                Debug.LogError("Highlight container is not set in HighlightManager!");

            if (_outlinePrefab == null)
                Debug.LogError("Outline prefab is not set in HighlightManager!");

            if (_fillPrefab == null)
                Debug.LogError("Fill prefab is not set in HighlightManager!");

            if (_nameTextPrefab == null)
                Debug.LogError("Name text prefab is not set in HighlightManager!");

            if (_dataRowPrefab == null)
                Debug.LogError("Data row prefab is not set in HighlightManager!");
        }

        /**
         * Highlights an object by drawing a box around it in screen space.
         * @param highlightable The object to highlight
         */
        public void HighlightObject(IHighlightable highlightable)
        {
            if (highlightable == null || !IsInRange(highlightable))
            {
                ClearHighlight();
                return;
            }

            _currentHighlighted = highlightable;

            // Create UI elements if needed
            if (_outlineImage == null)
            {
                _outlineImage = Instantiate(_outlinePrefab, _highlightContainer);
            }

            if (_fillImage == null && _showBackgroundFill)
            {
                _fillImage = Instantiate(_fillPrefab, _highlightContainer);
                _fillImage.color = _fillColor;
            }

            if (_nameText == null)
            {
                _nameText = Instantiate(_nameTextPrefab, _highlightContainer);
            }

            // Set highlight color based on type or custom color
            Color highlightColor = GetHighlightColorForType(_currentHighlighted.GetHighlightableType());
            if (_currentHighlighted.GetHighlightColor() != Color.clear)
            {
                highlightColor = _currentHighlighted.GetHighlightColor();
            }

            _outlineImage.color = highlightColor;
            //_nameText.color = highlightColor;

            // Set name text
            _nameText.text = _currentHighlighted.GetHighlightName();

            // Show UI elements
            _outlineImage.gameObject.SetActive(true);
            if (_fillImage != null)
            {
                _fillImage.gameObject.SetActive(_showBackgroundFill);
            }
            _nameText.gameObject.SetActive(true);

            // Set initial positions
            UpdateHighlightDisplay();

            // Set up data rows
            UpdateHighlightData();
        }

        /**
         * Updates the highlight data rows with the current highlighted object's data.
         */
        private void UpdateHighlightData()
        {
            // Clean up any destroyed rows in the data rows list
            for (int i = _dataRows.Count - 1; i >= 0; i--)
            {
                if (_dataRows[i] == null)
                {
                    _dataRows.RemoveAt(i);
                }
                else
                {
                    _dataRows[i].SetActive(false);
                }
            }

            _dataRows.Clear();

            if (_currentHighlighted == null || _nameText == null)
                return;

            // Get highlight data
            HighlightData[] dataEntries = _currentHighlighted.GetHighlightData();
            if (dataEntries == null || dataEntries.Length == 0)
                return;

            // Clean up destroyed objects in the pool
            for (int i = _dataRowPool.Count - 1; i >= 0; i--)
            {
                if (_dataRowPool[i] == null)
                {
                    _dataRowPool.RemoveAt(i);
                }
            }

            // Ensure pool has enough rows
            while (_dataRowPool.Count < dataEntries.Length)
            {
                GameObject rowObj = Instantiate(_dataRowPrefab, _highlightValuesContainer.transform);
                rowObj.transform.SetSiblingIndex(1);
                rowObj.SetActive(false);
                _dataRowPool.Add(rowObj);
            }

            // Reuse or create rows as needed
            for (int i = 0; i < dataEntries.Length; i++)
            {
                // Skip if we've run out of valid pool objects
                if (i >= _dataRowPool.Count)
                    break;

                GameObject rowObj = _dataRowPool[i];
                if (rowObj == null)
                    continue;

                // Make sure parent is set correctly (in case it was changed)
                //if (rowObj.transform.parent != _nameText.transform)
                //    rowObj.transform.SetParent(_nameText.transform, false);

                rowObj.SetActive(true);

                // Get and update text components
                Transform labelTransform = rowObj.transform.GetChild(0);
                Transform valueTransform = rowObj.transform.GetChild(1);

                if (labelTransform == null || valueTransform == null)
                    continue;

                TextMeshProUGUI labelText = labelTransform.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI valueText = valueTransform.GetComponent<TextMeshProUGUI>();

                if (labelText == null || valueText == null)
                    continue;

                labelText.text = dataEntries[i].Label + ":";
                valueText.text = dataEntries[i].Value;
                valueText.color = dataEntries[i].ValueColor;

                _dataRows.Add(rowObj);
            }
        }

        /**
         * Updates the position and size of the highlight UI elements.
         */
        private void UpdateHighlightDisplay()
        {
            if (_currentHighlighted == null || _mainCamera == null)
            {
                return;
            }

            // Calculate screen-space bounds
            Rect screenBounds = CalculateScreenBounds(_currentHighlighted);

            // Check if bounds are valid
            if (screenBounds.width < 1 || screenBounds.height < 1)
            {
                ClearHighlight();
                return;
            }

            // Apply minimum size if needed
            if (screenBounds.width < _minHighlightSize || screenBounds.height < _minHighlightSize)
            {
                float centerX = screenBounds.x + screenBounds.width / 2;
                float centerY = screenBounds.y + screenBounds.height / 2;

                float newWidth = Mathf.Max(screenBounds.width, _minHighlightSize);
                float newHeight = Mathf.Max(screenBounds.height, _minHighlightSize);

                screenBounds.x = centerX - newWidth / 2;
                screenBounds.y = centerY - newHeight / 2;
                screenBounds.width = newWidth;
                screenBounds.height = newHeight;
            }

            // Position outline
            RectTransform outlineRect = _outlineImage.rectTransform;
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.zero;
            outlineRect.pivot = Vector2.zero;

            // Add some padding for the outline
            float padding = _outlineThickness;
            outlineRect.sizeDelta = new Vector2(screenBounds.width + padding * 2, screenBounds.height + padding * 2);
            outlineRect.anchoredPosition = new Vector2(screenBounds.x - padding, screenBounds.y - padding);

            // Position fill (if enabled)
            if (_fillImage != null && _showBackgroundFill)
            {
                RectTransform fillRect = _fillImage.rectTransform;
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.zero;
                fillRect.pivot = Vector2.zero;
                fillRect.sizeDelta = new Vector2(screenBounds.width, screenBounds.height);
                fillRect.anchoredPosition = new Vector2(screenBounds.x, screenBounds.y);
            }

            // Position name text
            RectTransform nameRect = _nameText.rectTransform;
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = Vector2.zero;
            nameRect.pivot = new Vector2(0.5f, 0);

            // Match width to outline (accounting for outline thickness)
            float outlineWidth = screenBounds.width + (_outlineThickness * 2);
            nameRect.sizeDelta = new Vector2(outlineWidth, 30);

            // Center perfectly above outline
            nameRect.anchoredPosition = new Vector2(
                screenBounds.x + screenBounds.width / 2,
                screenBounds.y + screenBounds.height + _nameVerticalOffset
                        );

            // Position data rows
            for (int i = 0; i < _dataRows.Count; i++)
            {
                RectTransform rowRect = _dataRows[i].GetComponent<RectTransform>();
                rowRect.anchorMin = Vector2.zero;
                rowRect.anchorMax = Vector2.zero;
                rowRect.pivot = new Vector2(0.5f, 1);
                rowRect.sizeDelta = new Vector2(screenBounds.width, 20);
                rowRect.anchoredPosition = new Vector2(
                    screenBounds.x + screenBounds.width / 2,
                    screenBounds.y - (i + 1) * _dataRowSpacing
                );
            }
        }

        /**
         * Calculates screen-space bounds for the highlightable object.
         * @param highlightable The object to calculate bounds for
         * @return Screen-space rectangle encompassing the object
         */
        private Rect CalculateScreenBounds(IHighlightable highlightable)
        {
            Renderer[] renderers = highlightable.GetHighlightRenderers();
            if (renderers == null || renderers.Length == 0)
            {
                return new Rect(0, 0, 0, 0);
            }

            // Initialize with values that will be overwritten
            Vector3 min = new(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new(float.MinValue, float.MinValue, float.MinValue);
            bool validBoundsFound = false;

            // Calculate combined bounds in world space
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                Bounds bounds = renderer.bounds;

                // Skip empty bounds
                if (bounds.size == Vector3.zero)
                {
                    continue;
                }

                // Update min/max
                min = Vector3.Min(min, bounds.min);
                max = Vector3.Max(max, bounds.max);
                validBoundsFound = true;
            }

            if (!validBoundsFound)
            {
                return new Rect(0, 0, 0, 0);
            }

            // Convert world bounds corners to screen space
            Vector3[] corners = new Vector3[8];
            corners[0] = new Vector3(min.x, min.y, min.z); // bottom-left-back
            corners[1] = new Vector3(max.x, min.y, min.z); // bottom-right-back
            corners[2] = new Vector3(max.x, min.y, max.z); // bottom-right-front
            corners[3] = new Vector3(min.x, min.y, max.z); // bottom-left-front
            corners[4] = new Vector3(min.x, max.y, min.z); // top-left-back
            corners[5] = new Vector3(max.x, max.y, min.z); // top-right-back
            corners[6] = new Vector3(max.x, max.y, max.z); // top-right-front
            corners[7] = new Vector3(min.x, max.y, max.z); // top-left-front

            // Find screen bounds
            Vector2 screenMin = new(float.MaxValue, float.MaxValue);
            Vector2 screenMax = new(float.MinValue, float.MinValue);

            foreach (Vector3 corner in corners)
            {
                Vector3 screenPoint = _mainCamera.WorldToScreenPoint(corner);

                // Skip points behind the camera
                if (screenPoint.z < 0)
                {
                    continue;
                }

                // Update screen bounds
                screenMin.x = Mathf.Min(screenMin.x, screenPoint.x);
                screenMin.y = Mathf.Min(screenMin.y, screenPoint.y);
                screenMax.x = Mathf.Max(screenMax.x, screenPoint.x);
                screenMax.y = Mathf.Max(screenMax.y, screenPoint.y);
            }

            // Create screen-space rect
            return new Rect(
                screenMin.x,
                screenMin.y,
                screenMax.x - screenMin.x,
                screenMax.y - screenMin.y
            );
        }

        /**
         * Clears any active highlights.
         */
        public void ClearHighlight()
        {
            _currentHighlighted = null;

            if (_outlineImage != null)
            {
                _outlineImage.gameObject.SetActive(false);
            }

            if (_fillImage != null)
            {
                _fillImage.gameObject.SetActive(false);
            }

            if (_nameText != null)
            {
                _nameText.gameObject.SetActive(false);
            }

            foreach (var row in _dataRows)
            {
                if (row != null)
                {
                    Destroy(row);
                }
            }

            _dataRows.Clear();
        }

        /**
         * Checks if an object is within highlighting range.
         * @param highlightable The object to check
         * @return True if the object is in range, false otherwise
         */
        private bool IsInRange(IHighlightable highlightable)
        {
            if (highlightable == null || _mainCamera == null)
            {
                return false;
            }

            // Get renderers
            Renderer[] renderers = highlightable.GetHighlightRenderers();
            if (renderers == null || renderers.Length == 0)
            {
                return false;
            }

            // Check if any renderer is within range
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                float distance = Vector3.Distance(_mainCamera.transform.position, renderer.bounds.center);
                if (distance <= _maxDistanceToHighlight)
                {
                    return true;
                }
            }

            return false;
        }

        /**
         * Checks if an object is visible on screen.
         * @param highlightable The object to check
         * @return True if the object is visible, false otherwise
         */
        private bool IsVisibleOnScreen(IHighlightable highlightable)
        {
            if (highlightable == null)
            {
                return false;
            }

            // Get renderers
            Renderer[] renderers = highlightable.GetHighlightRenderers();
            if (renderers == null || renderers.Length == 0)
            {
                return false;
            }

            // Check if any renderer is visible
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.isVisible)
                {
                    continue;
                }

                // Check if renderer bounds are in camera view
                Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
                if (GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                {
                    return true;
                }
            }

            return false;
        }

        /**
         * Gets the appropriate highlight color based on object type.
         * @param type The type of object to get color for
         * @return The color to use for highlighting
         */
        private Color GetHighlightColorForType(HighlightableType type)
        {
            if (_highlightColors.TryGetValue(type, out Color color))
            {
                return color;
            }
            return _defaultColor;
        }
    }
}