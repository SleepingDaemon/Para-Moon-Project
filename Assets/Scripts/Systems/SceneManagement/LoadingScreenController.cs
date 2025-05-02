using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ParaMoon
{
    public class LoadingScreenController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Image _fadePanel;
        [SerializeField] Slider _progressBar;
        [SerializeField] TextMeshProUGUI _loadingText;
        [SerializeField] TextMeshProUGUI _progressText;
        [SerializeField] CanvasGroup _canvasGroup;

        [Header("Settings")]
        [SerializeField] float _fadeSpeed = 1.5f;
        [SerializeField] string[] _loadingTips;

        Coroutine _updateTextCoroutine;
        bool _isVisible = false;

        private void Awake()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            // Initialize invisible
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;

            if (_fadePanel != null)
                _fadePanel.color = new(_fadePanel.color.r, _fadePanel.color.g, _fadePanel.color.b, 0);

            // Reset progress
            SetProgress(0);
        }

        /// <summary>
        /// Show the loading screen with a fade transition
        /// </summary>
        /// <param name="color">Fade color</param>
        /// <param name="duration">Fade duration</param>
        /// <returns>Coroutine for awaiting completion</returns>
        public IEnumerator ShowLoadingScreen(float duration)
        {
            // Stop existing transitions
            StopAllCoroutines();

            // Set the fade panel color
            if (_fadePanel != null)
            {
                _fadePanel.color = Color.black;
            }

            // Show the loading screen
            _canvasGroup.blocksRaycasts = true;
            _isVisible = true;

            // Display random tip if we have any
            if (_loadingTips != null && _loadingTips.Length > 0 && _loadingText != null)
            {
                _loadingText.text = _loadingTips[UnityEngine.Random.Range(0, _loadingTips.Length)];
            }

            // Start cycling tips if there are multiple
            if (_loadingTips != null && _loadingTips.Length > 1 && _loadingText != null)
            {
                if (_updateTextCoroutine != null)
                    StopCoroutine(_updateTextCoroutine);

                _updateTextCoroutine = StartCoroutine(CycleTips());
            }

            // Fade in
            float elapsedTime = 0;
            while (elapsedTime < duration)
            {
                float normTime = elapsedTime / duration;

                // Fade canvas group
                _canvasGroup.alpha = Mathf.Lerp(0, 1, normTime);

                // Fade panel if available
                if (_fadePanel != null)
                {
                    Color panelColor = _fadePanel.color;
                    _fadePanel.color = new Color(panelColor.r, panelColor.g, panelColor.b, Mathf.Lerp(0, 1, normTime));
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure final state
            _canvasGroup.alpha = 1;
            if (_fadePanel != null)
            {
                Color panelColor = _fadePanel.color;
                _fadePanel.color = new Color(panelColor.r, panelColor.g, panelColor.b, 1);
            }
        }

        /// <summary>
        /// Hide the loading screen with a fade transition
        /// </summary>
        /// <param name="duration">Fade duration</param>
        /// <returns>Coroutine for awaiting completion</returns>
        public IEnumerator HideLoadingScreen(float duration)
        {
            // Stop cycling tips
            if (_updateTextCoroutine != null)
            {
                StopCoroutine(_updateTextCoroutine);
                _updateTextCoroutine = null;
            }

            // Fade out
            float elapsedTime = 0;
            while (elapsedTime < duration)
            {
                float normTime = elapsedTime / duration;

                // Fade canvas group
                _canvasGroup.alpha = Mathf.Lerp(1, 0, normTime);

                // Fade panel if available
                if (_fadePanel != null)
                {
                    Color panelColor = _fadePanel.color;
                    _fadePanel.color = new Color(panelColor.r, panelColor.g, panelColor.b, Mathf.Lerp(1, 0, normTime));
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure final state
            _canvasGroup.alpha = 0;
            if (_fadePanel != null)
            {
                Color panelColor = _fadePanel.color;
                _fadePanel.color = new Color(panelColor.r, panelColor.g, panelColor.b, 0);
            }

            _canvasGroup.blocksRaycasts = false;
            _isVisible = false;
        }

        /// <summary>
        /// Update the progress bar and text
        /// </summary>
        /// <param name="progress">Progress value (0-1)</param>
        public void SetProgress(float progress)
        {
            // Clamp progress between 0 and 1
            progress = Mathf.Clamp01(progress);

            // Update progress bar if available
            if (_progressBar != null)
                _progressBar.value = progress;

            // Update progress text if available
            if (_progressText != null)
                _progressText.text = $"{Mathf.Round(progress * 100)}%";
        }

        private IEnumerator CycleTips()
        {
            int currentTip = 0;

            while (_isVisible)
            {
                // Display the current tip
                _loadingText.text = _loadingTips[currentTip];

                // Wait before showing the next tip
                yield return new WaitForSeconds(5f);

                // Fade out current tip
                float fadeTime = 0.5f;
                float elapsed = 0;

                while (elapsed < fadeTime)
                {
                    _loadingText.alpha = Mathf.Lerp(1, 0, elapsed / fadeTime);
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                // Move to next tip
                currentTip = (currentTip + 1) % _loadingTips.Length;

                // Fade in new tip
                elapsed = 0;
                while (elapsed < fadeTime)
                {
                    _loadingText.alpha = Mathf.Lerp(0, 1, elapsed / fadeTime);
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                _loadingText.alpha = 1;
            }
        }
    }
}