using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ParaMoon
{
    public class InteractionUIController : MonoBehaviour
    {
        [SerializeField] GameObject _promptPanel;
        [SerializeField] TextMeshProUGUI _promptText;
        [SerializeField] Image _promptIcon;

        InteractionData _lastData;

        private void Start()
        {
            HideInteractionPrompt();
        }

        public void ShowInteractionPrompt(InteractionData data)
        {
            // Skip if data hasn't changed
            if (_lastData != null &&
                _lastData.PromptText == data.PromptText &&
                _lastData.PromptIcon == data.PromptIcon)
                return;

            if (_promptPanel != null)
            {
                _promptPanel.SetActive(true);

                if (_promptText != null)
                    _promptText.text = data.PromptText;

                if (_promptIcon != null && data.PromptIcon != null)
                {
                    _promptIcon.sprite = data.PromptIcon;
                    _promptIcon.gameObject.SetActive(true);
                }
                else if (_promptIcon != null)
                {
                    _promptIcon.gameObject.SetActive(false);
                }
            }

            _lastData = data;
        }

        public void HideInteractionPrompt()
        {
            if (_promptPanel != null)
                _promptPanel.SetActive(false);
        }
    }
}