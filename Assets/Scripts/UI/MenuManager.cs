using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ParaMoon
{
    public class MenuManager : MonoBehaviour
    {
        [System.Serializable]
        public class TabWindowPair
        {
            public Button TabButton;
            public GameObject Window;
            public Vector2 DefaultPosition;
        }

        [SerializeField] List<TabWindowPair> _tabWindows = new();
        [SerializeField] bool _allowMultipleWindows = true;

        private void Start()
        {
            InitializeMenu();
        }

        private void InitializeMenu()
        {
            // Close all windows initially
            foreach (var window in _tabWindows)
            {
                // Store the default position for each window
                window.DefaultPosition = window.Window.GetComponent<RectTransform>().anchoredPosition;

                // Initially hide all windows
                window.Window.SetActive(false);

                // Add listener to the tab button
                window.TabButton.onClick.AddListener(() => ToggleWindow(window));
            }
        }

        private void ToggleWindow(TabWindowPair window)
        {
            // Toggle the visibility of the window
            bool isActive = window.Window.activeSelf;

            if (!_allowMultipleWindows && !isActive)
            {
                // Close all other windows if multiple windows are not allowed
                foreach (var otherWindow in _tabWindows)
                {
                    if (otherWindow != window)
                        otherWindow.Window.SetActive(false);
                }
            }

            // Toggle this window
            window.Window.SetActive(!isActive);

            // Reset position when opening
            if (!isActive)
            {
                window.Window.GetComponent<RectTransform>().anchoredPosition = window.DefaultPosition;
                window.Window.transform.SetAsLastSibling(); // Bring the window to the front
            }
        }
    }
}