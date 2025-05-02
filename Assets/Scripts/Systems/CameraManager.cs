using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace ParaMoon
{
    /**
     * CameraManager is responsible for managing the camera settings in the game.
     * It handles camera depth, post-processing settings, and UI camera configuration.
     * 
     * Usage:
     * - Attach this script to a GameObject in the scene.
     * - Assign the main camera and UI camera in the inspector.
     * - Configure camera depth settings as needed.
     */
    public class CameraManager : ServiceBehaviour<CameraManager>
    {
        [System.Serializable]
        public class CameraDepthConfig
        {
            public string tag;
            public int depth;
        }

        [Header("Cameras")]
        [SerializeField] Camera _mainCamera;
        [SerializeField] Camera _uiCamera;

        [Header("Camera Depth Configuration")]
        [SerializeField]
        private List<CameraDepthConfig> _cameraConfigs = new()
        {
            new CameraDepthConfig { tag = "UICamera", depth = -1 },
            new CameraDepthConfig { tag = "MainCamera", depth = 0 },
        };

        [SerializeField] private bool _applyOnStart = true;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            if (_applyOnStart)
                SetupCameras();
        }

        private void SetupCameras()
        {
            // Find cameras if not assigned in inspector
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_uiCamera == null)
                _uiCamera = FindObjectsByType<Camera>(FindObjectsSortMode.None)
                    .FirstOrDefault(cam => cam.CompareTag("UICamera"));

            ApplyAllCameraDepths();

            if (_mainCamera != null)
            {
                _mainCamera.depthTextureMode = DepthTextureMode.Depth;
                _mainCamera.depth = 0;

                var mainCameraData = _mainCamera.GetComponent<HDAdditionalCameraData>();
                if (mainCameraData != null)
                {
                    // Configure for post-processing
                    mainCameraData.volumeLayerMask = -1;
                    mainCameraData.customRenderingSettings = false;

                    // Set as a regular camera
                    mainCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;
                    mainCameraData.backgroundColorHDR = Color.black;
                    mainCameraData.probeLayerMask = -1;
                }
                else
                {
                    Debug.LogWarning("Main camera does not have HDAdditionalCameraData component.");
                }
            }

            // Set up UI camera
            if (_uiCamera != null)
            {
                _uiCamera.depth = -1;

                var uiCameraData = _uiCamera.GetComponent<HDAdditionalCameraData>();
                if (uiCameraData != null)
                {
                    uiCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.None; // Don't clear, just overlay
                    uiCameraData.volumeLayerMask = 0; // Don't process volumes in UI camera

                    // Set as overlay camera
                    uiCameraData.fullscreenPassthrough = true;
                    uiCameraData.xrRendering = false;
                    
                    uiCameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
                    uiCameraData.allowDynamicResolution = false;
                }
                else
                {
                    Debug.LogWarning("UI camera does not have HDAdditionalCameraData component.");
                }
            }
        }

        /// <summary>
        /// Applies depth settings to all cameras based on configured tags
        /// </summary>
        public void ApplyAllCameraDepths()
        {
            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

            foreach (Camera cam in allCameras)
            {
                SetCameraDepth(cam);
            }
        }

        /// <summary>
        /// Sets camera depth based on tag configuration
        /// </summary>
        public void SetCameraDepth(Camera camera)
        {
            foreach (CameraDepthConfig config in _cameraConfigs)
            {
                if (camera.CompareTag(config.tag))
                {
                    camera.depth = config.depth;
                    break;
                }
            }
        }

        /// <summary>
        /// Static helper to set camera depth based on tag
        /// </summary>
        public static void ConfigureCameraDepth(Camera camera, string tag, int depth)
        {
            if (camera.CompareTag(tag))
            {
                camera.depth = depth;
                Debug.Log($"Set camera {camera.name} depth to {depth}");
            }
        }

        /// <summary>
        /// Get the configured depth value for a specific camera tag
        /// </summary>
        public int GetDepthForTag(string tag)
        {
            foreach (CameraDepthConfig config in _cameraConfigs)
            {
                if (config.tag == tag)
                    return config.depth;
            }

            return 0; // Default depth
        }
    }
}
