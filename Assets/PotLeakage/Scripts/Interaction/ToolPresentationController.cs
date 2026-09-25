using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PotLeakage.Camera;
using PotLeakage.UI;
using PotLeakage.VFX;

namespace PotLeakage.Interaction
{
    /// <summary>
    /// Coordinates the presentation and material-swap highlighting of the pinned tools
    /// on the existing 'tool board' at CameraSystem/TransformPoints/Tools.
    /// Strictly preserves existing transforms, hierarchy, and geometry.
    /// </summary>
    public class ToolPresentationController : MonoBehaviour
    {
        [Header("Pinned Tool Transforms on 'tool board'")]
        [SerializeField] public Transform crowbar;
        [SerializeField] public Transform lTool;
        [SerializeField] public Transform shovel;

        [SerializeField] public Transform crowbarHandle;
        [SerializeField] public Transform lToolHandle;
        [SerializeField] public Transform shovelHandle;

        [Header("Highlight Material")]
        [SerializeField] public Material highlightMaterial;

        [Header("UI Controller")]
        [SerializeField] public PotLeakageUIController uiController;

        [Header("Camera & Waypoints")]
        public Transform toolsTransformPoint;
        public PotLeakageCameraController cameraController;

        [Header("UI Text References (Optional / Fallback to UI Controller)")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI descriptionText;

        [Header("Presentation State")]
        [SerializeField] private int currentHighlightedIndex = 0; // 0 = Crowbar, 1 = L_TOOL, 2 = SHOVEL
        public int CurrentHighlightedIndex => currentHighlightedIndex;
        public bool IsPresentationActive { get; private set; }
        public bool IsPresentationCompleted { get; private set; }

        // Public accessors
        public Transform Crowbar { get => crowbar; set => crowbar = value; }
        public Transform LTool { get => lTool; set => lTool = value; }
        public Transform Shovel { get => shovel; set => shovel = value; }
        public Transform CrowbarHandle { get => crowbarHandle; set => crowbarHandle = value; }
        public Transform LToolHandle { get => lToolHandle; set => lToolHandle = value; }
        public Transform ShovelHandle { get => shovelHandle; set => shovelHandle = value; }
        public Material HighlightMaterial { get => highlightMaterial; set => highlightMaterial = value; }
        public PotLeakageUIController UIController { get => uiController; set => uiController = value; }

        // Backward-compatibility properties
        public GameObject crowbarBody { get => crowbar != null ? crowbar.gameObject : null; set => crowbar = value != null ? value.transform : null; }
        public GameObject lToolBody { get => lTool != null ? lTool.gameObject : null; set => lTool = value != null ? value.transform : null; }
        public GameObject shovelBody { get => shovel != null ? shovel.gameObject : null; set => shovel = value != null ? value.transform : null; }

        private struct RendererMaterialCache
        {
            public Renderer renderer;
            public Material[] originalMaterials;
        }

        private List<RendererMaterialCache> crowbarCache = new List<RendererMaterialCache>();
        private List<RendererMaterialCache> lToolCache = new List<RendererMaterialCache>();
        private List<RendererMaterialCache> shovelCache = new List<RendererMaterialCache>();
        [System.NonSerialized] private bool isCacheInitialized = false;

        private void Awake()
        {
            InitializeReferences();
        }

        private void Start()
        {
            InitializeReferences();
        }

        public void InitializeReferences()
        {
            if (crowbar == null)
            {
                var go = GameObject.Find("tool board/Crowbar");
                if (go != null) crowbar = go.transform;
            }
            if (crowbarHandle == null)
            {
                var go = GameObject.Find("tool board/Crowbar_HANDAL");
                if (go != null) crowbarHandle = go.transform;
            }

            if (lTool == null)
            {
                var go = GameObject.Find("tool board/L_TOOL");
                if (go != null) lTool = go.transform;
            }
            if (lToolHandle == null)
            {
                var go = GameObject.Find("tool board/L_TOOL_HANDAL");
                if (go != null) lToolHandle = go.transform;
            }

            if (shovel == null)
            {
                var go = GameObject.Find("tool board/SHOVEL");
                if (go != null) shovel = go.transform;
            }
            if (shovelHandle == null)
            {
                var go = GameObject.Find("tool board/SHOVEL_HANDAL");
                if (go != null) shovelHandle = go.transform;
            }

            if (highlightMaterial == null)
            {
#if UNITY_EDITOR
                highlightMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/PotLeakage/Materials/M_PotLeakage_Highlight.mat");
#endif
            }

            if (uiController == null)
            {
                uiController = Object.FindAnyObjectByType<PotLeakageUIController>();
            }

            if (toolsTransformPoint == null)
            {
                var tp = GameObject.Find("CameraSystem/TransformPoints/Tools") ?? GameObject.Find("TransformPoints/Tools");
                if (tp != null) toolsTransformPoint = tp.transform;
            }

            if (cameraController == null)
            {
                cameraController = Object.FindAnyObjectByType<PotLeakageCameraController>();
            }

            ResolveUIReferences();
            CacheOriginalMaterials();
        }

        private void ResolveUIReferences()
        {
            if (uiController == null)
            {
                uiController = Object.FindAnyObjectByType<PotLeakageUIController>();
            }

            if (uiController != null)
            {
                if (uiController.titleText != null) titleText = uiController.titleText;
                if (uiController.statusText != null) statusText = uiController.statusText;
                if (uiController.descriptionText != null) descriptionText = uiController.descriptionText;
            }

            if (titleText == null)
            {
                var go = GameObject.Find("UI/Canvas/Welcome Panel/Header/Title")
                      ?? GameObject.Find("CameraSystem/TransformPoints/CBT_LookTarget Panel/Panel/Title");
                if (go != null) titleText = go.GetComponent<TextMeshProUGUI>();
            }
            if (statusText == null)
            {
                var go = GameObject.Find("UI/Canvas/Welcome Panel/Content/StatusText")
                      ?? GameObject.Find("CameraSystem/TransformPoints/CBT_LookTarget Panel/Panel/StatusText");
                if (go != null) statusText = go.GetComponent<TextMeshProUGUI>();
            }
            if (descriptionText == null)
            {
                var go = GameObject.Find("UI/Canvas/Welcome Panel/Content/Description")
                      ?? GameObject.Find("CameraSystem/TransformPoints/CBT_LookTarget Panel/Panel/Description");
                if (go != null) descriptionText = go.GetComponent<TextMeshProUGUI>();
            }
        }

        private void CacheGroupMaterials(Transform body, Transform handle, List<RendererMaterialCache> cache)
        {
            if (cache.Count > 0 && IsCacheValid(cache))
            {
                return;
            }

            cache.Clear();
            AddTransformToCache(body, cache);
            AddTransformToCache(handle, cache);
        }

        private bool IsCacheValid(List<RendererMaterialCache> cache)
        {
            if (cache == null || cache.Count == 0) return false;
            foreach (var item in cache)
            {
                if (item.renderer == null || item.originalMaterials == null || item.originalMaterials.Length == 0)
                    return false;
                foreach (var m in item.originalMaterials)
                {
                    if (m == null || m == highlightMaterial)
                        return false;
                }
            }
            return true;
        }

        private void AddTransformToCache(Transform t, List<RendererMaterialCache> cache)
        {
            if (t == null) return;
            var renderers = t.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r == null || r.sharedMaterials == null) continue;

                var mats = (Material[])r.sharedMaterials.Clone();
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == highlightMaterial || mats[i] == null)
                    {
                        mats[i] = GetFallbackMaterialForTool(r.gameObject.name);
                    }
                }

                cache.Add(new RendererMaterialCache { renderer = r, originalMaterials = mats });
            }
        }

        private Material GetFallbackMaterialForTool(string toolName)
        {
#if UNITY_EDITOR
            if (toolName.Contains("Crowbar_HANDAL"))
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Tools/Materials/Black_colour.mat");
            if (toolName.Contains("Crowbar") || (toolName.Contains("L_TOOL") && !toolName.Contains("HANDAL")))
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Tools/Materials/evren-metal-aluminium-3d-texture-pbr-material-high-resolution-free-downlox-hd-4k-5.mat");
            if (toolName.Contains("L_TOOL_HANDAL"))
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Tools/Materials/006400.mat");
            if (toolName.Contains("SHOVEL"))
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Tools/Materials/Rusty_Metal_Image-diffuse.mat");
#endif
            return null;
        }

        private void CacheOriginalMaterials()
        {
            if (isCacheInitialized && crowbarCache.Count > 0 && lToolCache.Count > 0 && shovelCache.Count > 0)
            {
                return;
            }

            CacheGroupMaterials(crowbar, crowbarHandle, crowbarCache);
            CacheGroupMaterials(lTool, lToolHandle, lToolCache);
            CacheGroupMaterials(shovel, shovelHandle, shovelCache);

            if (crowbarCache.Count > 0 && lToolCache.Count > 0 && shovelCache.Count > 0)
            {
                isCacheInitialized = true;
            }
        }

        private void ApplyHighlightToGroup(List<RendererMaterialCache> cache)
        {
            if (highlightMaterial == null) return;

            foreach (var item in cache)
            {
                if (item.renderer != null && item.originalMaterials != null)
                {
                    Material[] highlightArray = new Material[item.originalMaterials.Length];
                    for (int i = 0; i < highlightArray.Length; i++)
                    {
                        highlightArray[i] = highlightMaterial;
                    }
                    item.renderer.sharedMaterials = highlightArray;
                }
            }
        }

        private void RestoreGroupMaterials(List<RendererMaterialCache> cache)
        {
            foreach (var item in cache)
            {
                if (item.renderer != null && item.originalMaterials != null)
                {
                    item.renderer.sharedMaterials = (Material[])item.originalMaterials.Clone();
                }
            }
        }

        /// <summary>
        /// Starts the pinned tool presentation sequence:
        /// Resets previous state, restores all original materials, highlights Crowbar, and updates UI.
        /// </summary>
        public void StartPresentation()
        {
            InitializeReferences();
            ClearAllToolHighlights();

            currentHighlightedIndex = 0;
            IsPresentationActive = true;
            IsPresentationCompleted = false;

            if (toolsTransformPoint != null)
            {
                if (cameraController != null)
                {
                    cameraController.SnapCameraToTransform(toolsTransformPoint);
                }
                else
                {
                    var mainCam = UnityEngine.Camera.main;
                    if (mainCam != null)
                    {
                        mainCam.transform.position = toolsTransformPoint.position;
                        mainCam.transform.rotation = toolsTransformPoint.rotation;
                    }
                }
            }

            HighlightCrowbar();
        }

        public void StartToolPresentation() => StartPresentation();

        /// <summary>
        /// Advances tool presentation through: Crowbar -> L_TOOL -> SHOVEL -> Completion.
        /// Returns TRUE if advanced to another tool (SequenceHandler must NOT complete task yet).
        /// Returns FALSE if presentation completed (SequenceHandler may now complete task).
        /// </summary>
        public bool AdvancePresentation()
        {
            InitializeReferences();

            if (!IsPresentationActive)
            {
                return false;
            }

            switch (currentHighlightedIndex)
            {
                case 0:
                    // Step 1 (Crowbar) -> Step 2 (L_TOOL)
                    RestoreGroupMaterials(crowbarCache);
                    HighlightLTool();
                    currentHighlightedIndex = 1;
                    return true;

                case 1:
                    // Step 2 (L_TOOL) -> Step 3 (SHOVEL)
                    RestoreGroupMaterials(lToolCache);
                    HighlightShovel();
                    currentHighlightedIndex = 2;
                    return true;

                case 2:
                    // Step 3 (SHOVEL) -> Complete
                    RestoreGroupMaterials(shovelCache);
                    ClearAllToolHighlights();
                    IsPresentationActive = false;
                    IsPresentationCompleted = true;
                    return false;

                default:
                    ClearAllToolHighlights();
                    IsPresentationActive = false;
                    IsPresentationCompleted = true;
                    return false;
            }
        }

        public void HighlightCrowbar()
        {
            InitializeReferences();
            RestoreGroupMaterials(lToolCache);
            RestoreGroupMaterials(shovelCache);

            currentHighlightedIndex = 0;
            ApplyHighlightToGroup(crowbarCache);

            UpdateUI("ESTABLISH ACCESS & PREPARE THE WORK AREA",
                     "CHANNEL-MAKING TOOLS: CROWBAR",
                     "Used to break and open compacted material around the affected area to establish a passage for the leakage.");
        }

        public void HighlightLTool()
        {
            InitializeReferences();
            RestoreGroupMaterials(crowbarCache);
            RestoreGroupMaterials(shovelCache);

            currentHighlightedIndex = 1;
            ApplyHighlightToGroup(lToolCache);

            UpdateUI("LOW LEAKAGE — REQUIRED CHANNEL-MAKING TOOL",
                     "L-TOOL REQUIRED",
                     "Use the L-TOOL to break and push material in the affected area to establish a channel for controlled metal flow during the low-leakage response.");
        }

        public void HighlightShovel()
        {
            InitializeReferences();
            RestoreGroupMaterials(crowbarCache);
            RestoreGroupMaterials(lToolCache);

            currentHighlightedIndex = 2;
            ApplyHighlightToGroup(shovelCache);

            UpdateUI("ESTABLISH ACCESS & PREPARE THE WORK AREA",
                     "CHANNEL-MAKING TOOLS: SHOVEL",
                     "Used to move loose material and assist in maintaining the channel around the affected area.");
        }

        public void CompletePresentation()
        {
            ClearAllToolHighlights();
            IsPresentationActive = false;
            IsPresentationCompleted = true;
        }

        /// <summary>
        /// Restores all original materials on all 3 tools without leaving any highlighted.
        /// </summary>
        public void ClearAllToolHighlights()
        {
            RestoreGroupMaterials(crowbarCache);
            RestoreGroupMaterials(lToolCache);
            RestoreGroupMaterials(shovelCache);
            currentHighlightedIndex = -1;
            IsPresentationActive = false;
        }

        /// <summary>
        /// Cleanly resets presentation state for replay.
        /// </summary>
        public void ResetPresentation()
        {
            ClearAllToolHighlights();
            currentHighlightedIndex = 0;
            IsPresentationActive = false;
            IsPresentationCompleted = false;
        }

        private void UpdateUI(string title, string status, string description)
        {
            ResolveUIReferences();

            if (uiController != null)
            {
                if (uiController.titleText != null) uiController.titleText.text = title;
                if (uiController.statusText != null)
                {
                    uiController.statusText.gameObject.SetActive(true);
                    uiController.statusText.text = status;
                }
                if (uiController.descriptionText != null)
                {
                    uiController.descriptionText.gameObject.SetActive(true);
                    uiController.descriptionText.text = description;
                }
                uiController.SpeakDescriptionText(description, status);
            }

            if (titleText != null && (uiController == null || titleText != uiController.titleText))
            {
                titleText.text = title;
            }
            if (statusText != null && (uiController == null || statusText != uiController.statusText))
            {
                statusText.gameObject.SetActive(true);
                statusText.text = status;
            }
            if (descriptionText != null && (uiController == null || descriptionText != uiController.descriptionText))
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = description;
            }
        }

        private void OnDisable()
        {
            ClearAllToolHighlights();
            IsPresentationActive = false;
        }
    }

    /// <summary>
    /// Coordinates the 4-stage low-leakage explanation sequence at CameraSystem/TransformPoints/Ideal Pot Voltage:
    /// Step 1: COVER BATH CONTAINER (Fused Alumina Container)
    /// Step 2: SHOVEL (Fused Alumina Handling/Transfer)
    /// Step 3: Crowbar (Side Breaking)
    /// Step 4: Controlled Leakage Visualization (Continuous thin molten metal stream, settled pool)
    ///
    /// Strictly preserves existing transforms, hierarchy, and geometry of Low Leakage GameObject.
    /// Operates decoupled from generic sequence progression; advances via Next clicks and releases to SequenceHandler upon completion.
    /// </summary>
    public class LowLeakagePresentationController : MonoBehaviour
    {
        [Header("Existing Low Leakage Hierarchy")]
        [Tooltip("Low Leakage/COVER BATH CONTAINER")]
        public GameObject container;

        [Tooltip("Low Leakage/SHOVEL")]
        public GameObject shovel;

        [Tooltip("Low Leakage/Crowbar")]
        public GameObject crowbar;

        [Header("Highlight Material")]
        public Material highlightMaterial;

        [Header("Camera & Waypoints")]
        public Transform toolsTransformPoint;
        public Transform idealPotVoltageTarget;
        public PotLeakageCameraController cameraController;

        [Header("Controllers")]
        public PotLeakageUIController uiController;
        public ToolPresentationController toolPresentationController;
        public MoltenAluminiumVFXController vfxController;

        [Header("UI Text References (Optional / Fallback to UI Controller)")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI descriptionText;

        [Header("Presentation State")]
        [SerializeField] private int currentStep = 0;
        public int CurrentStep => currentStep;
        public bool IsPresentationActive { get; private set; }
        public bool IsPresentationCompleted { get; private set; }

        private struct RendererMaterialCache
        {
            public Renderer renderer;
            public Material[] originalMaterials;
        }

        private List<RendererMaterialCache> containerCache = new List<RendererMaterialCache>();
        private List<RendererMaterialCache> shovelCache = new List<RendererMaterialCache>();
        private List<RendererMaterialCache> crowbarCache = new List<RendererMaterialCache>();
        [System.NonSerialized] private bool isCacheInitialized = false;

        private void Awake()
        {
            InitializeReferences();
        }

        private void Start()
        {
            InitializeReferences();
        }

        public void InitializeReferences()
        {
            if (container == null)
            {
                var go = GameObject.Find("Low Leakage/COVER BATH CONTAINER")
                      ?? GameObject.Find("COVER BATH CONTAINER");
                if (go != null) container = go;
            }

            if (shovel == null)
            {
                var go = GameObject.Find("Low Leakage/SHOVEL")
                      ?? GameObject.Find("SHOVEL");
                if (go != null) shovel = go;
            }

            if (crowbar == null)
            {
                var go = GameObject.Find("Low Leakage/Crowbar")
                      ?? GameObject.Find("Crowbar");
                if (go != null) crowbar = go;
            }

            if (highlightMaterial == null)
            {
#if UNITY_EDITOR
                highlightMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/PotLeakage/Materials/M_PotLeakage_Highlight.mat");
#endif
            }

            if (toolsTransformPoint == null)
            {
                var tp = GameObject.Find("CameraSystem/TransformPoints/Tools")
                      ?? GameObject.Find("TransformPoints/Tools");
                if (tp != null) toolsTransformPoint = tp.transform;
            }

            if (idealPotVoltageTarget == null)
            {
                var tp = GameObject.Find("CameraSystem/TransformPoints/Ideal Pot Voltage")
                      ?? GameObject.Find("TransformPoints/Ideal Pot Voltage");
                if (tp != null) idealPotVoltageTarget = tp.transform;
            }

            if (cameraController == null)
            {
                cameraController = Object.FindAnyObjectByType<PotLeakageCameraController>();
            }

            if (uiController == null)
            {
                uiController = Object.FindAnyObjectByType<PotLeakageUIController>();
            }

            if (toolPresentationController == null)
            {
                toolPresentationController = Object.FindAnyObjectByType<ToolPresentationController>();
            }
            if (toolPresentationController != null)
            {
                toolPresentationController.InitializeReferences();
            }

            if (vfxController == null)
            {
                vfxController = Object.FindAnyObjectByType<MoltenAluminiumVFXController>();
            }

            ResolveUIReferences();
            CacheOriginalMaterials();
        }

        private void ResolveUIReferences()
        {
            if (uiController != null)
            {
                if (uiController.titleText != null) titleText = uiController.titleText;
                if (uiController.statusText != null) statusText = uiController.statusText;
                if (uiController.descriptionText != null) descriptionText = uiController.descriptionText;
            }

            if (titleText == null)
            {
                var go = GameObject.Find("UI/Canvas/Welcome Panel/Header/Title");
                if (go != null) titleText = go.GetComponent<TextMeshProUGUI>();
            }
            if (statusText == null)
            {
                var go = GameObject.Find("UI/Canvas/Welcome Panel/Content/StatusText");
                if (go != null) statusText = go.GetComponent<TextMeshProUGUI>();
            }
            if (descriptionText == null)
            {
                var go = GameObject.Find("UI/Canvas/Welcome Panel/Content/Description");
                if (go != null) descriptionText = go.GetComponent<TextMeshProUGUI>();
            }
        }

        private void CacheOriginalMaterials()
        {
            if (isCacheInitialized && containerCache.Count > 0 && shovelCache.Count > 0 && crowbarCache.Count > 0)
            {
                return;
            }

            CacheObjectMaterials(container, containerCache);
            CacheObjectMaterials(shovel, shovelCache);
            CacheObjectMaterials(crowbar, crowbarCache);

            if (containerCache.Count > 0 && shovelCache.Count > 0 && crowbarCache.Count > 0)
            {
                isCacheInitialized = true;
            }
        }

        private void CacheObjectMaterials(GameObject target, List<RendererMaterialCache> cache)
        {
            if (target == null) return;
            cache.Clear();
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r != null && r.sharedMaterials != null)
                {
                    cache.Add(new RendererMaterialCache
                    {
                        renderer = r,
                        originalMaterials = (Material[])r.sharedMaterials.Clone()
                    });
                }
            }
        }

        private void ApplyHighlightToGroup(List<RendererMaterialCache> cache)
        {
            if (highlightMaterial == null) return;

            foreach (var item in cache)
            {
                if (item.renderer != null && item.originalMaterials != null)
                {
                    Material[] highlightArray = new Material[item.originalMaterials.Length];
                    for (int i = 0; i < highlightArray.Length; i++)
                    {
                        highlightArray[i] = highlightMaterial;
                    }
                    item.renderer.sharedMaterials = highlightArray;
                }
            }
        }

        private void RestoreGroupMaterials(List<RendererMaterialCache> cache)
        {
            foreach (var item in cache)
            {
                if (item.renderer != null && item.originalMaterials != null)
                {
                    item.renderer.sharedMaterials = (Material[])item.originalMaterials.Clone();
                }
            }
        }

        public void RestoreAllMaterials()
        {
            RestoreGroupMaterials(containerCache);
            RestoreGroupMaterials(shovelCache);
            RestoreGroupMaterials(crowbarCache);
        }

        public void StartPresentation()
        {
            InitializeReferences();
            RestoreAllMaterials();
            if (toolPresentationController != null)
            {
                toolPresentationController.ClearAllToolHighlights();
            }

            SnapCameraToTools();

            SequenceHelperFunctions.OnInterceptTaskCompletion = AdvancePresentation;

            currentStep = 0;
            IsPresentationActive = true;
            IsPresentationCompleted = false;

            ShowToolBoardStep1Crowbar();
        }

        public void StartLowLeakagePresentation()
        {
            InitializeReferences();
            RestoreAllMaterials();
            if (toolPresentationController != null)
            {
                toolPresentationController.ClearAllToolHighlights();
            }

            SnapCameraToIdealPotVoltage();

            SequenceHelperFunctions.OnInterceptTaskCompletion = AdvancePresentation;

            currentStep = 3;
            IsPresentationActive = true;
            IsPresentationCompleted = false;

            ShowStep1Container();
        }

        public void SnapCameraToTools()
        {
            if (toolsTransformPoint != null)
            {
                if (cameraController != null)
                {
                    cameraController.SnapCameraToTransform(toolsTransformPoint);
                }
                else
                {
                    var mainCam = UnityEngine.Camera.main;
                    if (mainCam != null)
                    {
                        mainCam.transform.position = toolsTransformPoint.position;
                        mainCam.transform.rotation = toolsTransformPoint.rotation;
                    }
                }
            }
            Debug.Log("[TOOLS] Camera snapped to Tools");
        }

        public void SnapCameraToIdealPotVoltage()
        {
            if (idealPotVoltageTarget != null)
            {
                if (cameraController != null)
                {
                    cameraController.SnapCameraToTransform(idealPotVoltageTarget);
                }
                else
                {
                    var mainCam = UnityEngine.Camera.main;
                    if (mainCam != null)
                    {
                        mainCam.transform.position = idealPotVoltageTarget.position;
                        mainCam.transform.rotation = idealPotVoltageTarget.rotation;
                    }
                }
            }
            Debug.Log("[TOOLS] Camera snapped to Ideal Pot Voltage");
        }

        public void ShowToolBoardStep1Crowbar()
        {
            InitializeReferences();
            RestoreAllMaterials();

            if (toolPresentationController != null)
            {
                toolPresentationController.HighlightCrowbar();
            }

            Debug.Log("[TOOLS] Crowbar presentation");

            UpdateUI("ESTABLISH ACCESS & PREPARE THE WORK AREA",
                     "CHANNEL-MAKING TOOLS",
                     "CROWBAR\n\nThe crowbar is used to break and open compacted material around the affected area. This helps establish a passage for the leakage response.",
                     "TOOL_BOARD_CROWBAR");
        }

        public void ShowToolBoardStep2LTool()
        {
            InitializeReferences();
            RestoreAllMaterials();

            if (toolPresentationController != null)
            {
                toolPresentationController.HighlightLTool();
            }

            Debug.Log("[TOOLS] L_TOOL presentation");

            UpdateUI("ESTABLISH ACCESS & PREPARE THE WORK AREA",
                     "CHANNEL-MAKING TOOLS",
                     "L-TOOL\n\nThe L_TOOL is included in this training visualization as a channel-making tool for working around the affected area.",
                     "TOOL_BOARD_L_TOOL");
        }

        public void ShowToolBoardStep3Shovel()
        {
            InitializeReferences();
            RestoreAllMaterials();

            if (toolPresentationController != null)
            {
                toolPresentationController.HighlightShovel();
            }

            Debug.Log("[TOOLS] SHOVEL presentation");

            UpdateUI("ESTABLISH ACCESS & PREPARE THE WORK AREA",
                     "CHANNEL-MAKING TOOLS",
                     "SHOVEL\n\nThe shovel is included in this training visualization to assist with handling loose material around the affected area.",
                     "TOOL_BOARD_SHOVEL");
        }

        public void ShowStep1Container()
        {
            InitializeReferences();
            RestoreAllMaterials();
            if (toolPresentationController != null)
            {
                toolPresentationController.ClearAllToolHighlights();
            }

            if (container != null) container.SetActive(true);
            if (shovel != null) shovel.SetActive(false);
            if (crowbar != null) crowbar.SetActive(false);

            ApplyHighlightToGroup(containerCache);

            Debug.Log("[LOW LEAKAGE] COVER BATH CONTAINER");

            UpdateUI("LOW LEAKAGE — FUSED ALUMINA",
                     "FUSED ALUMINA PREPARATION",
                     "COVER BATH CONTAINER\n\nThis container holds fused alumina used during the low-intensity collector-bar leakage response. Fused alumina is pushed into the broken area so that it reaches the bottom and helps control the leakage path.",
                     "LOW_LEAKAGE_STEP_1");
        }

        public void ShowStep2Shovel()
        {
            InitializeReferences();
            RestoreAllMaterials();

            if (container != null) container.SetActive(false);
            if (shovel != null) shovel.SetActive(true);
            if (crowbar != null) crowbar.SetActive(false);

            ApplyHighlightToGroup(shovelCache);

            Debug.Log("[LOW LEAKAGE] SHOVEL");

            UpdateUI("LOW LEAKAGE — FUSED ALUMINA",
                     "FUSED ALUMINA HANDLING",
                     "SHOVEL\n\nIn this training visualization, the shovel is used to take fused alumina from the prepared container and bring it toward the affected leakage area. The fused alumina is then pushed into the broken area so that it reaches the bottom and helps control the leakage path.",
                     "LOW_LEAKAGE_STEP_2");
        }

        public void ShowStep3Crowbar()
        {
            InitializeReferences();
            RestoreAllMaterials();

            if (container != null) container.SetActive(false);
            if (shovel != null) shovel.SetActive(false);
            if (crowbar != null) crowbar.SetActive(true);

            ApplyHighlightToGroup(crowbarCache);

            Debug.Log("[LOW LEAKAGE] CROWBAR");

            UpdateUI("LOW LEAKAGE — SIDE BREAKING",
                     "SIDE BREAKING / CHANNEL PREPARATION",
                     "CROWBAR\n\nUse the crowbar to break and open the compacted material around the affected area. This creates a passage for the fused alumina and helps establish a controlled path for the leakage.",
                     "LOW_LEAKAGE_STEP_3");
        }

        public void ShowStep4ControlledLeakage()
        {
            InitializeReferences();
            RestoreAllMaterials();

            if (container != null) container.SetActive(false);
            if (shovel != null) shovel.SetActive(false);
            if (crowbar != null) crowbar.SetActive(false);

            if (vfxController != null)
            {
                vfxController.SetControlledLeakage(true);
            }

            Debug.Log("[LOW LEAKAGE] CONTROLLED LEAKAGE");
            Debug.Log("[LOW LEAKAGE] Molten flow reduced to controlled thin stream");

            UpdateUI("LOW LEAKAGE — CONTROLLED",
                     "CONTROLLED LEAKAGE",
                     "CONTROLLED LEAKAGE\n\nSide breaking and fused alumina pushing help establish a controlled leakage path. The leakage is reduced to a thin controlled stream rather than an uncontrolled flow.",
                     "LOW_LEAKAGE_CONTROLLED");
        }

        public bool AdvancePresentation()
        {
            InitializeReferences();

            if (!IsPresentationActive)
            {
                return false;
            }

            switch (currentStep)
            {
                case 0:
                    // Step A1 (Crowbar) -> Step A2 (L_TOOL)
                    ShowToolBoardStep2LTool();
                    currentStep = 1;
                    return true;

                case 1:
                    // Step A2 (L_TOOL) -> Step A3 (SHOVEL)
                    ShowToolBoardStep3Shovel();
                    currentStep = 2;
                    return true;

                case 2:
                    // Step A3 (SHOVEL) -> Tool Board Complete -> Snap to Ideal Pot Voltage -> Step B1 (COVER BATH CONTAINER)
                    if (toolPresentationController != null)
                    {
                        toolPresentationController.ClearAllToolHighlights();
                    }
                    Debug.Log("[TOOLS] Tool board completed");
                    SnapCameraToIdealPotVoltage();
                    ShowStep1Container();
                    currentStep = 3;
                    return true;

                case 3:
                    // Step B1 (Container) -> Step B2 (SHOVEL)
                    ShowStep2Shovel();
                    currentStep = 4;
                    return true;

                case 4:
                    // Step B2 (SHOVEL) -> Step B3 (CROWBAR)
                    ShowStep3Crowbar();
                    currentStep = 5;
                    return true;

                case 5:
                    // Step B3 (CROWBAR) -> Step B4 (CONTROLLED LEAKAGE)
                    ShowStep4ControlledLeakage();
                    currentStep = 6;
                    return true;

                case 6:
                    // Step B4 (CONTROLLED LEAKAGE) -> Complete Task 09
                    Debug.Log("[TASK] Task 09 completed");
                    CompletePresentation();
                    currentStep = 7;
                    return false;

                default:
                    CompletePresentation();
                    return false;
            }
        }

        public void CompletePresentation()
        {
            RestoreAllMaterials();
            if (container != null) container.SetActive(false);
            if (shovel != null) shovel.SetActive(false);
            if (crowbar != null) crowbar.SetActive(false);

            if (toolPresentationController != null)
            {
                toolPresentationController.ClearAllToolHighlights();
            }

            IsPresentationActive = false;
            IsPresentationCompleted = true;
            SequenceHelperFunctions.OnInterceptTaskCompletion = null;
        }

        public void ResetPresentation()
        {
            InitializeReferences();
            RestoreAllMaterials();

            if (container != null) container.SetActive(false);
            if (shovel != null) shovel.SetActive(false);
            if (crowbar != null) crowbar.SetActive(false);

            if (toolPresentationController != null)
            {
                toolPresentationController.ResetPresentation();
            }

            if (vfxController != null)
            {
                vfxController.SetControlledLeakage(false, 0f);
                vfxController.ResetFloorSpill();
            }

            var mgr = TruckTyreReplacement.Core.Manager.Instance;
            if (mgr != null)
            {
                mgr.StopSpeech();
            }

            currentStep = 0;
            IsPresentationActive = false;
            IsPresentationCompleted = false;
            SequenceHelperFunctions.OnInterceptTaskCompletion = null;
        }

        private void UpdateUI(string title, string status, string description, string speechKey = "")
        {
            ResolveUIReferences();

            if (uiController != null)
            {
                if (uiController.welcomePanel != null) uiController.welcomePanel.SetActive(true);
                if (uiController.progressText != null) uiController.progressText.text = "<color=#00E5FF>TASK 09 / 10</color>";
                uiController.SpeakDescriptionText(description, speechKey);
            }

            if (titleText != null)
            {
                titleText.text = title;
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = status;
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = description;
            }
        }

        private void OnDisable()
        {
            if (SequenceHelperFunctions.OnInterceptTaskCompletion == AdvancePresentation)
            {
                SequenceHelperFunctions.OnInterceptTaskCompletion = null;
            }
        }
    }
}
