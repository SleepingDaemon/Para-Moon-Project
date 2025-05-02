using UnityEngine;
using UnityEngine.UI;

namespace ParaMoon
{
    /*
     * HelmetVisorEffects.cs
     * This script handles the visor effects for the helmet in the game.
     * It includes fogging and light reflection effects based on player actions.
     */
    public class HelmetVisorEffects : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image visorOverlay;
        [SerializeField] private Light[] environmentLights;

        [Header("Reflection Settings")]
        [SerializeField] private float reflectionIntensity = 0.2f;
        [SerializeField] private float maxReflectionIntensity = 0.5f; // Maximum allowed intensity
        [SerializeField] private float minLightDistance = 1.0f; // Minimum distance to prevent division by very small numbers
        [SerializeField] private Color reflectionTint = Color.white;

        [Header("Fogging")]
        [SerializeField] private float fogRate = 0.1f;
        [SerializeField] private float maxFog = 0.3f;
        [SerializeField] private float defogRate = 0.5f;

        private float currentFog = 0f;
        private Material visorMaterial;

        void Start()
        {
            if (visorOverlay == null)
            {
                Debug.LogError("Visor Overlay reference is missing. Please assign it in the inspector.");
                enabled = false; // Disable this component to prevent further errors
                return;
            }

            if (visorOverlay.material == null)
            {
                Debug.LogError("Visor Overlay material is null. Make sure the Image has a material assigned.");
                enabled = false;
                return;
            }

            visorMaterial = new Material(visorOverlay.material);
            visorOverlay.material = visorMaterial;

            // Initialize reflection intensity to 0
            visorMaterial.SetFloat("_ReflectionIntensity", 0);
        }

        void Update()
        {
            // Update visor fog based on player exertion (could tie to sprinting)
            if (ServiceLocator.Instance.TryGetService<InputManager>(out var inputManager) &&
                inputManager.Sprint)
            {
                currentFog = Mathf.Min(currentFog + fogRate * Time.deltaTime, maxFog);
            }
            else
            {
                currentFog = Mathf.Max(currentFog - defogRate * Time.deltaTime, 0);
            }

            // Apply fog to visor
            Color fogColor = new Color(1, 1, 1, currentFog);
            visorMaterial.SetColor("_FogOverlay", fogColor);

            // Reset reflection intensity before checking lights
            float highestIntensity = 0f;
            Color dominantLightColor = Color.white;

            // Check for lights in player's view and find the most significant one
            foreach (Light light in environmentLights)
            {
                if (IsLightVisible(light))
                {
                    float intensity = CalculateLightReflection(light);

                    // Keep track of the light with highest contribution
                    if (intensity > highestIntensity)
                    {
                        highestIntensity = intensity;
                        dominantLightColor = light.color;
                    }
                }
            }

            // Apply only the most significant light's reflection
            if (highestIntensity > 0)
            {
                visorMaterial.SetFloat("_ReflectionIntensity", highestIntensity);
                visorMaterial.SetColor("_ReflectionTint", reflectionTint * dominantLightColor);
            }
            else
            {
                // No lights visible, fade out reflection
                float currentIntensity = visorMaterial.GetFloat("_ReflectionIntensity");
                float newIntensity = Mathf.Lerp(currentIntensity, 0, Time.deltaTime * 5f);
                visorMaterial.SetFloat("_ReflectionIntensity", newIntensity);
            }
        }

        bool IsLightVisible(Light light)
        {
            // Skip inactive lights
            if (!light.isActiveAndEnabled)
                return false;

            Vector3 directionToLight = light.transform.position - Camera.main.transform.position;
            float angle = Vector3.Angle(Camera.main.transform.forward, directionToLight);

            // Only apply reflection if light is in view
            return angle < 60f && !Physics.Linecast(
                Camera.main.transform.position,
                light.transform.position,
                LayerMask.GetMask("Environment"));
        }

        float CalculateLightReflection(Light light)
        {
            // Calculate reflection intensity based on light properties and angle
            float distance = Vector3.Distance(light.transform.position, Camera.main.transform.position);

            // Prevent division by very small numbers by enforcing minimum distance
            distance = Mathf.Max(distance, minLightDistance);

            // Calculate base intensity with smoother falloff
            float intensity = light.intensity / (distance * distance) * reflectionIntensity;

            // Add angle factor - stronger when looking more directly at light
            Vector3 directionToLight = light.transform.position - Camera.main.transform.position;
            float angleFactor = Vector3.Dot(Camera.main.transform.forward.normalized, directionToLight.normalized);
            angleFactor = Mathf.Clamp01(angleFactor); // Only positive values (in front of player)

            intensity *= angleFactor;

            // Clamp intensity to prevent extreme values
            intensity = Mathf.Clamp(intensity, 0, maxReflectionIntensity);

            return intensity;
        }
    }
}