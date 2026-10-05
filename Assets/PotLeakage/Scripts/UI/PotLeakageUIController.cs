using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PotLeakage.Configuration;
using PotLeakage.VFX;
using PotLeakage.Interaction;
using PotLeakage.Core;

namespace PotLeakage.UI
{
    /// <summary>
    /// UI Controller for Vedanta Aluminium Pot Leakage Visualization.
    /// Manages the single reusable explanation panel inside the Main Camera view,
    /// performs direct material swap highlighting for tripo_node_cf158416,
    /// and controls Cube.010 magma drool animation toward TransformPoints/drop.
    /// </summary>
    public class PotLeakageUIController : MonoBehaviour
    {
        [Header("Configuration")]
        public PotLeakageConfig config;

        [Header("UI Panels")]
        [Tooltip("Optional Header bar")]
        public GameObject headerPanel;

        [Tooltip("Reusable Explanation / Welcome Panel UI")]
        public GameObject welcomePanel;

        // Backward compatibility alias
        public GameObject explanationPanel { get => welcomePanel; set => welcomePanel = value; }
        public GameObject informationPanel { get => welcomePanel; set => welcomePanel = value; }

        [Header("Texts")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI progressText;

        [Header("Company Logo")]
        [Tooltip("Empty image slot for company logo at top-right of panel (user assigns manually)")]
        public Image companyLogo;

        [Header("Buttons")]
        public Button nextButton;

        [Header("Value Display Helper")]
        public PotLeakageValueDisplay valueDisplay;

        [Header("Highlight Targets")]
        [Tooltip("Full Pot Machine mesh GameObject (tripo_node_cf158416)")]
        public GameObject potMachineHighlightTarget;

        // Backward compatibility fields
        public GameObject cbtHighlightTarget;
        public GameObject fireEffect;
        public void EnableFireEffect() {}
        public void DisableFireEffect() {}

        [Header("Pot Machine Material Swap (tripo_node_cf158416)")]
        [Tooltip("Designated pot highlight material (M_PotLeakage_Highlight.mat)")]
        public Material potHighlightMaterial;
        [Tooltip("Designated yellow highlight material (M_PotLeakage_Highlight_Yellow.mat)")]
        public Material yellowHighlightMaterial;
        private Material potOriginalMaterial;
        private MeshRenderer potMeshRenderer;

        [Header("Pot Machine Blink Settings")]
        [Tooltip("Blink interval in seconds (default 0.35s)")]
        public float blinkInterval = 0.35f;
        private Coroutine potBlinkCoroutine;
        private bool isBlinking;
        public bool IsBlinking => isBlinking;

        [Header("Magma Drool Controller (Cube.010 -> TransformPoints/drop)")]
        public MoltenAluminiumVFXController magmaDroolController;

        [Header("Walkie-Talkie Interaction (Task 05)")]
        public WalkieTalkieInteractionController walkieTalkieController;

        [Header("Pot Voltage Wall Panel (Task 06)")]
        [Tooltip("Wall-mounted information panel in front of PotMachine_LookTarget")]
        public GameObject potVoltageWallPanel;
        public TextMeshProUGUI wallPanelTitle;
        public TextMeshProUGUI wallPanelStatusText;
        public TextMeshProUGUI wallPanelValuesText;
        public TextMeshProUGUI wallPanelDescriptionText;

        [Header("CBT Look Target Panel (Task 08)")]
        [Tooltip("Wall/hanging panel in front of CBT_LookTarget")]
        public GameObject cbtLookTargetPanel;
        public TextMeshProUGUI cbtPanelTitle;
        public TextMeshProUGUI cbtPanelStatusText;
        public TextMeshProUGUI cbtPanelDescriptionText;
        [SerializeField] private Image processImage;
        public Image ProcessImage { get => processImage; set => processImage = value; }

        [Header("Tool Presentation (Task 08 Tools Area)")]
        public ToolPresentationController toolPresentationController;

        [Header("Low Leakage Presentation")]
        public LowLeakagePresentationController lowLeakageController;

        [Header("Audio SFX Clips")]
        public AudioClip clickAudioClip;
        public AudioClip cameraTransitionSFX;
        public AudioSource sfxAudioSource;

        [Header("Pot Control Display Controller")]
        public PotControlDisplayController potControlDisplayController;

        [Header("Pot Control Sequence Voice-Over Clips (Quest Packaged)")]
        public AudioClip task05Clip;
        public AudioClip task06Clip;
        public AudioClip task07Clip;
        public AudioClip task08Clip;
        public AudioClip warningAudioClip;
        public AudioClip task09Clip;
        public AudioClip task09SuccessClip;
        public AudioClip task10Clip;
        public AudioClip task11Clip;

        [Header("Walkie-Talkie 2 (Task 11)")]
        public GameObject walkieTalkie2Target;
        private readonly System.Collections.Generic.List<WalkieTalkieBlinkSlot> walkieTalkie2BlinkSlots = new System.Collections.Generic.List<WalkieTalkieBlinkSlot>();
        private Coroutine walkieTalkie2BlinkCoroutine;
        public bool IsWalkieTalkie2Blinking { get; private set; }

        [Header("Task 11 Manual Mode & Anode Down Interaction")]
        [Tooltip("Anode Down button GameObject (Cube.016 under crane )")]
        public GameObject anodeDownButtonTarget;
        private MeshRenderer anodeDownRenderer;
        private Material anodeDownOriginalMaterial;
        public bool isAnodeDownInteractable = false;
        public int anodeDownClickCount = 0;
        public bool isWalkieTalkie2Clicked = false;
        private Coroutine manualModeRoutine;

        private static PotLeakageUIController _instance;
        public static PotLeakageUIController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<PotLeakageUIController>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            set => _instance = value;
        }

        [Header("Side Breaking Tool (Task 08 & 09)")]
        public GameObject sideBreakingToolTarget;
        private MeshRenderer sideBreakingToolRenderer;
        private Material sideBreakingToolOriginalMaterial;
        public bool isSideBreakingToolInteractable = false;
        public bool isSideBreakingToolConfirmed = false;

        [Header("Safety Gloves & Stopper Application")]
        public GameObject glovesTarget;
        public bool isGlovesInteractable = false;
        public bool isGlovesConfirmed = false;
        public GameObject sideBreakingTool4Target;
        public GameObject sideBreakingTool5Target;
        public GameObject sideBreakingTool6Target;
        public bool isStopperToolInteractable = false;
        public bool isStopperToolConfirmed = false;

        [System.Serializable]
        public struct ToolBlinkSlot
        {
            public Renderer renderer;
            public int slotIndex;
            public Material originalMaterial;
            public string targetName;
        }

        private readonly System.Collections.Generic.List<ToolBlinkSlot> sideBreakingToolSlots = new System.Collections.Generic.List<ToolBlinkSlot>();
        private Coroutine sideBreakingToolBlinkCoroutine;
        public bool IsSideBreakingToolBlinking { get; private set; }

        public System.Collections.Generic.List<ToolBlinkSlot> GetSideBreakingToolSlots() => sideBreakingToolSlots;

        [Header("Task 09 Verification State")]
        public bool isTask09Verified = false;

        [Header("Pot Controller Repeated Announcement (Task 04)")]
        public int potControllerAnnouncementRepeatCount = 4;
        public float potControllerAnnouncementInterval = 1.2f;
        private Coroutine potControllerAnnouncementCoroutine;

        // Backward compatibility aliases
        public AudioClip nextButtonSFX { get => clickAudioClip; set => clickAudioClip = value; }
        public AudioClip clickAudio { get => clickAudioClip; set => clickAudioClip = value; }

        private void Awake()
        {
            Instance = this;
            InitializeReferences();
        }

        private void OnEnable()
        {
            Instance = this;
        }

        private void Start()
        {
            InitializeReferences();

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(OnNextButtonClicked);
                nextButton.onClick.AddListener(OnNextButtonClicked);
            }
        }

        private static bool IsPersistentObject(Object obj)
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.IsPersistent(obj);
#else
            return false;
#endif
        }

        public void InitializeReferences()
        {
            if (config == null)
            {
                config = Resources.Load<PotLeakageConfig>("PotLeakageConfig");
#if UNITY_EDITOR
                if (config == null)
                {
                    config = UnityEditor.AssetDatabase.LoadAssetAtPath<PotLeakageConfig>("Assets/PotLeakage/Data/PotLeakageConfig.asset");
                }
#endif
            }

            if (statusText == null)
            {
                var stGo = GameObject.Find("UI/Canvas/Welcome Panel/Content/StatusText");
                if (stGo != null) statusText = stGo.GetComponent<TextMeshProUGUI>();
            }

            if (descriptionText == null)
            {
                var dtGo = GameObject.Find("UI/Canvas/Welcome Panel/Content/Description");
                if (dtGo != null) descriptionText = dtGo.GetComponent<TextMeshProUGUI>();
            }

            if (titleText == null)
            {
                var ttGo = GameObject.Find("UI/Canvas/Welcome Panel/Header/Title");
                if (ttGo != null) titleText = ttGo.GetComponent<TextMeshProUGUI>();
            }

            if (progressText == null)
            {
                var ptGo = GameObject.Find("UI/Canvas/Welcome Panel/Footer/ProgressText");
                if (ptGo != null) progressText = ptGo.GetComponent<TextMeshProUGUI>();
            }

            if (potMachineHighlightTarget == null)
            {
                potMachineHighlightTarget = GameObject.Find("tripo_node_cf158416");
            }
            if (potMachineHighlightTarget != null && potMeshRenderer == null)
            {
                potMeshRenderer = potMachineHighlightTarget.GetComponent<MeshRenderer>();
                if (potMeshRenderer != null && potOriginalMaterial == null)
                {
                    potOriginalMaterial = potMeshRenderer.sharedMaterial;
                    if (potOriginalMaterial != null && potOriginalMaterial.name.Contains("Highlight"))
                    {
#if UNITY_EDITOR
                        potOriginalMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Model/Materials/metal.mat");
#endif
                    }
                }
            }

            if (potHighlightMaterial == null)
            {
                potHighlightMaterial = Resources.Load<Material>("M_PotLeakage_Highlight");
#if UNITY_EDITOR
                if (potHighlightMaterial == null)
                {
                    potHighlightMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/PotLeakage/Materials/M_PotLeakage_Highlight.mat");
                }
#endif
            }

            if (yellowHighlightMaterial == null)
            {
                yellowHighlightMaterial = Resources.Load<Material>("M_PotLeakage_Highlight_Yellow");
#if UNITY_EDITOR
                if (yellowHighlightMaterial == null)
                {
                    yellowHighlightMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/PotLeakage/Materials/M_PotLeakage_Highlight_Yellow.mat");
                }
#endif
            }

            if (magmaDroolController == null)
            {
                magmaDroolController = Object.FindFirstObjectByType<MoltenAluminiumVFXController>(FindObjectsInactive.Include);
                if (magmaDroolController == null)
                {
                    var allVfx = Resources.FindObjectsOfTypeAll<MoltenAluminiumVFXController>();
                    foreach (var v in allVfx)
                    {
                        if (v.gameObject.scene.isLoaded && !IsPersistentObject(v))
                        {
                            magmaDroolController = v;
                            break;
                        }
                    }
                }
            }

            if (walkieTalkieController == null)
            {
                walkieTalkieController = Object.FindAnyObjectByType<WalkieTalkieInteractionController>();
                if (walkieTalkieController == null)
                {
                    var wt = GameObject.Find("defaultMaterial.007");
                    if (wt != null)
                    {
                        walkieTalkieController = wt.GetComponent<WalkieTalkieInteractionController>();
                        if (walkieTalkieController == null)
                        {
                            walkieTalkieController = wt.AddComponent<WalkieTalkieInteractionController>();
                        }
                    }
                }
            }
            if (walkieTalkieTarget == null)
            {
                var wt = GameObject.Find("walkietalkie");
                if (wt == null)
                {
                    var all = Resources.FindObjectsOfTypeAll<GameObject>();
                    foreach (var g in all)
                    {
                        if (g.name == "walkietalkie" && !IsPersistentObject(g) && g.transform.parent == null)
                        {
                            wt = g;
                            break;
                        }
                    }
                }
                if (wt != null) walkieTalkieTarget = wt;
            }

            if (personnelImages == null)
            {
                personnelImages = GameObject.Find("UI/Canvas/IdealCBTTemperatureUI/PersonnelImages")
                               ?? GameObject.Find("PersonnelImages");
                if (personnelImages == null)
                {
                    var all = Resources.FindObjectsOfTypeAll<GameObject>();
                    foreach (var g in all)
                    {
                        if (g.name == "PersonnelImages" && !IsPersistentObject(g))
                        {
                            personnelImages = g;
                            break;
                        }
                    }
                }
            }

            if (walkieTalkieController != null)
            {
                walkieTalkieController.InitializeReferences();
            }

            if (toolPresentationController == null)
            {
                toolPresentationController = GetComponent<PotLeakage.Interaction.ToolPresentationController>();
                if (toolPresentationController == null)
                {
                    toolPresentationController = Object.FindAnyObjectByType<PotLeakage.Interaction.ToolPresentationController>();
                }
            }
            if (toolPresentationController != null)
            {
                toolPresentationController.InitializeReferences();
            }

            if (potVoltageWallPanel == null)
            {
                potVoltageWallPanel = GameObject.Find("PotMachine_LookTarget Panel");
                if (potVoltageWallPanel == null)
                {
                    var parentTP = GameObject.Find("CameraSystem/TransformPoints/PotMachine_LookTarget Panel");
                    if (parentTP != null) potVoltageWallPanel = parentTP;
                }
            }

            if (potVoltageWallPanel != null)
            {
                if (wallPanelTitle == null)
                {
                    var t = potVoltageWallPanel.transform.Find("Panel/Title");
                    if (t != null) wallPanelTitle = t.GetComponent<TextMeshProUGUI>();
                }
                if (wallPanelStatusText == null)
                {
                    var s = potVoltageWallPanel.transform.Find("Panel/StatusText");
                    if (s != null) wallPanelStatusText = s.GetComponent<TextMeshProUGUI>();
                }
                if (wallPanelValuesText == null)
                {
                    var v = potVoltageWallPanel.transform.Find("Panel/VoltageDetails/ValuesText");
                    if (v != null) wallPanelValuesText = v.GetComponent<TextMeshProUGUI>();
                }
                if (wallPanelDescriptionText == null)
                {
                    var d = potVoltageWallPanel.transform.Find("Panel/Description");
                    if (d != null) wallPanelDescriptionText = d.GetComponent<TextMeshProUGUI>();
                }
            }

            if (cbtLookTargetPanel == null)
            {
                cbtLookTargetPanel = GameObject.Find("CBT_LookTarget Panel");
                if (cbtLookTargetPanel == null)
                {
                    var parentTP = GameObject.Find("CameraSystem/TransformPoints/CBT_LookTarget Panel");
                    if (parentTP != null) cbtLookTargetPanel = parentTP;
                }
            }

            if (cbtLookTargetPanel != null)
            {
                if (cbtPanelTitle == null)
                {
                    var t = cbtLookTargetPanel.transform.Find("Panel/Title");
                    if (t != null) cbtPanelTitle = t.GetComponent<TextMeshProUGUI>();
                }
                if (cbtPanelStatusText == null)
                {
                    var s = cbtLookTargetPanel.transform.Find("Panel/StatusText");
                    if (s != null) cbtPanelStatusText = s.GetComponent<TextMeshProUGUI>();
                }
                if (cbtPanelDescriptionText == null)
                {
                    var d = cbtLookTargetPanel.transform.Find("Panel/Description");
                    if (d != null) cbtPanelDescriptionText = d.GetComponent<TextMeshProUGUI>();
                }
                if (processImage == null)
                {
                    var img = cbtLookTargetPanel.transform.Find("Panel/ProcessImage");
                    if (img != null) processImage = img.GetComponent<Image>();
                }
            }

            if (toolPresentationController == null)
            {
                toolPresentationController = Object.FindAnyObjectByType<ToolPresentationController>();
            }

            if (lowLeakageController == null)
            {
                lowLeakageController = Object.FindAnyObjectByType<LowLeakagePresentationController>();
            }
            if (lowLeakageController != null)
            {
                lowLeakageController.InitializeReferences();
            }

            if (clickAudioClip == null)
            {
#if UNITY_EDITOR
                clickAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/click.mp3");
#endif
            }

            if (cameraTransitionSFX == null)
            {
#if UNITY_EDITOR
                cameraTransitionSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/swap.mp3");
#endif
            }

            if (potControlDisplayController == null)
            {
                potControlDisplayController = Object.FindAnyObjectByType<PotControlDisplayController>();
            }

            if (task05Clip == null)
            {
#if UNITY_EDITOR
                task05Clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/PotLeakage/Audio/TTS/PotLeakage_Task05_DuctEndSafety.wav");
#endif
            }
            if (task06Clip == null)
            {
#if UNITY_EDITOR
                task06Clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/PotLeakage/Audio/TTS/PotLeakage_Task06_EmergencyTrip.wav");
#endif
            }
            if (task07Clip == null)
            {
#if UNITY_EDITOR
                task07Clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/PotLeakage/Audio/TTS/PotLeakage_Task07_CheckSafeLimits.wav");
#endif
            }
            if (task08Clip == null)
            {
#if UNITY_EDITOR
                task08Clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/PotLeakage/Audio/TTS/PotLeakage_Task08_ActiveDigitsWarning.wav");
#endif
            }
#if UNITY_EDITOR
            task09Clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/PotLeakage/Audio/TTS/PotLeakage_Task09_EquipGloves.wav");
#else
            task09Clip = null;
#endif
            if (warningAudioClip == null)
            {
#if UNITY_EDITOR
                warningAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/warning.mp3");
#endif
            }

            if (anodeDownButtonTarget == null || anodeDownButtonTarget.name != "Cube.016" || (anodeDownButtonTarget.transform.parent != null && anodeDownButtonTarget.transform.parent.name.ToLower().Contains("crane")))
            {
                GameObject c16 = null;
                var machine = GameObject.Find("Machine") ?? GameObject.Find("machine");
                if (machine != null)
                {
                    var child = machine.transform.Find("Cube.016");
                    if (child != null) c16 = child.gameObject;
                }
                if (c16 == null)
                {
                    var all = Resources.FindObjectsOfTypeAll<GameObject>();
                    foreach (var g in all)
                    {
                        if (g.name == "Cube.016" && g.transform.parent != null && g.transform.parent.name.ToLower().Contains("machine") && !IsPersistentObject(g))
                        {
                            c16 = g;
                            break;
                        }
                    }
                }
                if (c16 != null) anodeDownButtonTarget = c16;
            }

            if (anodeDownButtonTarget != null)
            {
                if (anodeDownRenderer == null || anodeDownRenderer.gameObject != anodeDownButtonTarget)
                {
                    anodeDownRenderer = anodeDownButtonTarget.GetComponent<MeshRenderer>();
                    if (anodeDownRenderer != null)
                    {
                        anodeDownOriginalMaterial = anodeDownRenderer.sharedMaterial;
                    }
                }

                var col = anodeDownButtonTarget.GetComponent<Collider>();
                if (col == null)
                {
                    anodeDownButtonTarget.AddComponent<BoxCollider>();
                }

                var interaction = anodeDownButtonTarget.GetComponent<AnodeDownButtonInteraction>();
                if (interaction == null)
                {
                    interaction = anodeDownButtonTarget.AddComponent<AnodeDownButtonInteraction>();
                }
                interaction.uiController = this;
            }

            if (sideBreakingToolTarget == null)
            {
                var tb = GameObject.Find("ToolBox");
                if (tb != null)
                {
                    var child = tb.transform.Find("SIDEREAKING TOOL");
                    if (child != null) sideBreakingToolTarget = child.gameObject;
                }
                if (sideBreakingToolTarget == null)
                {
                    var all = Resources.FindObjectsOfTypeAll<GameObject>();
                    foreach (var g in all)
                    {
                        if (g.name == "SIDEREAKING TOOL" && !IsPersistentObject(g))
                        {
                            sideBreakingToolTarget = g;
                            break;
                        }
                    }
                }
            }

            if (sideBreakingToolTarget != null)
            {
                sideBreakingToolRenderer = sideBreakingToolTarget.GetComponent<MeshRenderer>();
                if (sideBreakingToolRenderer != null && sideBreakingToolOriginalMaterial == null)
                {
                    var mat = sideBreakingToolRenderer.sharedMaterial;
                    if (mat != null && !mat.name.Contains("Highlight"))
                    {
                        sideBreakingToolOriginalMaterial = mat;
                    }
                }

                var col = sideBreakingToolTarget.GetComponent<Collider>();
                if (col == null)
                {
                    col = sideBreakingToolTarget.AddComponent<BoxCollider>();
                }
                col.enabled = true;

                var interaction = sideBreakingToolTarget.GetComponent<SideBreakingToolInteraction>();
                if (interaction == null)
                {
                    interaction = sideBreakingToolTarget.AddComponent<SideBreakingToolInteraction>();
                }
                interaction.uiController = this;
            }
        }

        private void OnDisable()
        {
            StopPotBlink(false);
            StopWalkieTalkieBlink();
            StopWalkieTalkie2Blink();
            ResetManualModeInteraction();
            var beamScale = UnityEngine.Object.FindFirstObjectByType<PotBeamScaleController>();
            if (beamScale != null) beamScale.StopNeedleAnimation();
            if (walkieTalkie2Target != null) walkieTalkie2Target.SetActive(false);
            SequenceHelperFunctions.OnInterceptTaskCompletion = null;
            if (leakageIncidentCoroutine != null)
            {
                StopCoroutine(leakageIncidentCoroutine);
                leakageIncidentCoroutine = null;
            }
            if (potControllerAnnouncementCoroutine != null)
            {
                StopCoroutine(potControllerAnnouncementCoroutine);
                potControllerAnnouncementCoroutine = null;
            }
            if (magmaDroolController != null)
            {
                magmaDroolController.StopMagmaDrool(true);
            }
            if (walkieTalkieController != null)
            {
                walkieTalkieController.ResetInteraction();
            }
            if (toolPresentationController != null)
            {
                toolPresentationController.ClearAllToolHighlights();
            }
            if (lowLeakageController != null)
            {
                lowLeakageController.ResetPresentation();
            }
            if (potVoltageWallPanel != null)
            {
                potVoltageWallPanel.SetActive(true);
            }
            if (cbtLookTargetPanel != null)
            {
                cbtLookTargetPanel.SetActive(true);
            }
        }

        public void OnNextButtonClicked()
        {
            var clip = clickAudioClip != null ? clickAudioClip : nextButtonSFX;
            PlaySFX(clip);
            // NOTE: Do NOT call CompleteCurrentTask(), TaskCompleted(), NextTask(), or NextSequence() here!
            // The persistent UnityEvent on UI/Canvas/Welcome Panel/Footer/NextButton
            // is the authoritative event calling SequenceHelperFunctions.CompleteCurrentTask().
        }

        /// <summary>
        /// Speaks the description text using LocalTTS (Manager.Instance.SpeakText).
        /// Strips rich-text tags before speaking and cancels any previous active speech.
        /// </summary>
        public void SpeakDescriptionText(string text, string key = "")
        {
            if (string.IsNullOrEmpty(text)) return;
            string clean = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty).Trim();
            var mgr = TruckTyreReplacement.Core.Manager.Instance;
            if (mgr != null)
            {
                mgr.SpeakText(clean, clean);
            }
        }

        /// <summary>
        /// Plays training voice-over clip via SequenceHelperFunctions / Manager / LocalTTS fallback.
        /// Guaranteed to play on both PC and Meta Quest standalone.
        /// </summary>
        public void PlayTrainingVoiceOver(AudioClip clip, string text, string key = "")
        {
            if (clip != null)
            {
                if (SequenceHelperFunctions.instance != null)
                {
                    SequenceHelperFunctions.instance.VoiceOverCall(clip);
                    return;
                }

                var mgr = TruckTyreReplacement.Core.Manager.Instance;
                if (mgr != null)
                {
                    var src = mgr.GetVoiceAudioSource();
                    if (src != null)
                    {
                        src.Stop();
                        src.clip = clip;
                        src.Play();
                        return;
                    }
                }
            }

            if (!string.IsNullOrEmpty(text))
            {
                SpeakDescriptionText(text, key);
            }
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;

            if (sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(clip);
            }
            else if (SequenceHelperFunctions.instance != null)
            {
                SequenceHelperFunctions.instance.PlaySFX(clip);
            }
        }

        /// <summary>
        /// Direct material swap on tripo_node_cf158416 to highlight material.
        /// Preserves original material reference and does NOT modify transform.
        /// </summary>
        public void SwapPotHighlightMaterial()
        {
            InitializeReferences();

            if (potMeshRenderer != null)
            {
                if (potOriginalMaterial == null)
                {
                    potOriginalMaterial = potMeshRenderer.sharedMaterial;
                }

                if (potHighlightMaterial != null)
                {
                    potMeshRenderer.material = potHighlightMaterial;
                }
            }
        }

        public void StartPotBlink()
        {
            InitializeReferences();

            if (potBlinkCoroutine != null)
            {
                StopCoroutine(potBlinkCoroutine);
                potBlinkCoroutine = null;
            }

            SwapPotHighlightMaterial();
            isBlinking = true;
            if (Application.isPlaying)
            {
                potBlinkCoroutine = StartCoroutine(PotBlinkRoutine());
            }
        }

        public void StopPotBlink(bool keepHighlight = true)
        {
            if (potBlinkCoroutine != null)
            {
                StopCoroutine(potBlinkCoroutine);
                potBlinkCoroutine = null;
            }
            isBlinking = false;

            if (keepHighlight)
            {
                SwapPotHighlightMaterial();
            }
            else
            {
                RestorePotOriginalMaterial();
            }
        }

        private IEnumerator PotBlinkRoutine()
        {
            isBlinking = true;
            while (isBlinking)
            {
                if (potMeshRenderer != null && potHighlightMaterial != null)
                {
                    potMeshRenderer.material = potHighlightMaterial;
                }
                yield return new WaitForSeconds(blinkInterval);

                if (potMeshRenderer != null && potOriginalMaterial != null)
                {
                    potMeshRenderer.material = potOriginalMaterial;
                }
                yield return new WaitForSeconds(blinkInterval);
            }
            potBlinkCoroutine = null;
        }

        public void RestorePotOriginalMaterial()
        {
            if (potBlinkCoroutine != null)
            {
                StopCoroutine(potBlinkCoroutine);
                potBlinkCoroutine = null;
            }
            isBlinking = false;

            if (potMeshRenderer != null && potOriginalMaterial != null)
            {
                potMeshRenderer.material = potOriginalMaterial;
            }
        }

        private string GetConfiguredVoltageString()
        {
            float v = (config != null) ? config.normalProductionPotVoltage : 0f;
            if (v <= 0f) return "VALUE NOT CONFIGURED";
            float round1 = Mathf.Round(v * 10f) / 10f;
            return Mathf.Abs(v - round1) < 0.001f ? $"{v:0.0} V" : $"{v:0.00} V";
        }

        private string GetConfiguredCBTString()
        {
            float cbt = (config != null) ? config.normalProductionCBTTemperature : 0f;
            if (cbt <= 0f) return "VALUE NOT CONFIGURED";
            return $"{cbt:F0} °C";
        }

        private string GetConfiguredObservationVoltageString()
        {
            float v = (config != null) ? ((config.potVoltageObservationValue > 0f) ? config.potVoltageObservationValue : config.currentPotVoltage) : 4.2f;
            if (v <= 0f) v = 4.2f;
            float round1 = Mathf.Round(v * 10f) / 10f;
            return Mathf.Abs(v - round1) < 0.001f ? $"{v:0.0} V" : $"{v:0.00} V";
        }

        private string GetConfiguredDuctEndLimitString()
        {
            float v = (config != null) ? config.maximumDuctEndVoltage : 4.5f;
            if (v <= 0f) v = 4.5f;
            float round1 = Mathf.Round(v * 10f) / 10f;
            return Mathf.Abs(v - round1) < 0.001f ? $"≤ {v:0.0} V" : $"≤ {v:0.00} V";
        }

        public void UpdateWallPanelContent()
        {
            InitializeReferences();

            string currentVoltageStr = GetConfiguredObservationVoltageString();
            string normalVoltageStr = GetConfiguredVoltageString();
            string ductEndLimitStr = GetConfiguredDuctEndLimitString();

            if (wallPanelTitle != null)
            {
                wallPanelTitle.text = "POT VOLTAGE";
            }

            if (wallPanelStatusText != null)
            {
                wallPanelStatusText.text = $"POT VOLTAGE: {currentVoltageStr}";
            }

            if (wallPanelValuesText != null)
            {
                wallPanelValuesText.text = $"<b>Current Pot Voltage:</b>\n<color=#FFD54F>{currentVoltageStr}</color>\n\n<b>Normal Production Voltage:</b>\n<color=#00E5FF>{normalVoltageStr}</color>\n\n<b>Duct-End Voltage Limit:</b>\n<color=#FF5252>{ductEndLimitStr}</color>";
            }

            if (wallPanelDescriptionText != null)
            {
                wallPanelDescriptionText.text = "During leakage response, the Technical In-charge continuously monitors the duct-end pot voltage.\n\nMaintain the voltage at not more than 4.5 V by lowering the anode beam in steps of 2–3 pulses as required.";
            }
        }

        public void UpdateCBTPanelContent()
        {
            InitializeReferences();

            if (cbtPanelTitle != null)
            {
                cbtPanelTitle.text = "ESTABLISH ACCESS & PREPARE THE WORK AREA";
            }

            if (cbtPanelStatusText != null)
            {
                cbtPanelStatusText.text = "PREPARATION BEFORE TAP-OUT";
            }

            if (cbtPanelDescriptionText != null)
            {
                cbtPanelDescriptionText.text = "Team goes to the lower level to prevent metal pool formation.\n\nTake at least 4 channel-making tools.\n\nRemove the -3 m mesh to provide access.\n\nArrange the bath/alumina tanker near the tap-out.";
            }

            if (processImage != null)
            {
                processImage.preserveAspect = true;
                processImage.gameObject.SetActive(true);
            }
        }

        // ── TASK SPECIFIC UI METHODS ──────────────────────────────────────────

        // ── TASK SPECIFIC UI METHODS ──────────────────────────────────────────

        public void SetTask01Default() => SetTask01UI();
        public void SetTask02Default() => SetTask02UI();
        public void SetTask03Default() => SetTask03UI();
        public void SetTask04Default() => SetTask04UI();
        public void SetTask05Default() => SetTask05PotControlMachineUI();
        public void SetTask06Default() => SetTask06AnodeDownUI();
        public void SetTask07Default() => SetTask07ToolBoxUI();
        public void SetTask08Default() => SetTask08NormalPotUI();
        public void SetTask09Default() => SetTask09VoltageAppropriateUI();
        public void SetTask10Default() => SetTask10ToolBoxUI();
        public void SetTask11Default() => SetTask11NormalPotUI();

        // Backward compatibility aliases
        public void SetTask02Part1Default() => SetTask02UI();
        public void SetTask02Part2Default() => SetTask03UI();
        public void SetTask02Part1UI() => SetTask02UI();
        public void SetTask02Part2UI() => SetTask03UI();

        /// <summary>
        /// Unified dispatcher to trigger UI updates for any task index.
        /// Supports both 8-task canonical flow and legacy 11-task setup.
        /// </summary>
        public void SetTaskByIndex(int taskIndex)
        {
            var seq = SequenceHandler.instance != null && SequenceHandler.instance.sequenceList != null && SequenceHandler.instance.sequenceList.Count > 0
                ? SequenceHandler.instance.sequenceList[0] : UnityEngine.Object.FindFirstObjectByType<Sequence>();
            int taskCount = (seq != null && seq.TaskList != null) ? seq.TaskList.Count : 8;

            if (taskCount == 10)
            {
                switch (taskIndex)
                {
                    case 0: SetTask01UI(); break;
                    case 1: SetTask02UI(); break;
                    case 2: SetTask03UI(); break;
                    case 3: SetTask04UI(); break;
                    case 4: SetTask05PotControlMachineUI(); break;
                    case 5: SetTask06PotControlWarningUI(); break;
                    case 6: SetTask07BeamScaleVerificationUI(); break;
                    case 7: SetTask08AnodeDownUI(); break;
                    case 8: SetTask09ToolBoxUI(); break;
                    case 9: SetTask10NormalPotUI(); break;
                    default:
                        Debug.LogWarning($"[PotLeakageUIController] Invalid task index: {taskIndex}");
                        break;
                }
            }
            else if (taskCount == 8)
            {
                switch (taskIndex)
                {
                    case 0: SetTask01UI(); break;
                    case 1: SetTask02UI(); break;
                    case 2: SetTask03UI(); break;
                    case 3: SetTask04UI(); break;
                    case 4: SetTask05PotControlMachineUI(); break;
                    case 5: SetTask06AnodeDownUI(); break;
                    case 6: SetTask07ToolBoxUI(); break;
                    case 7: SetTask08NormalPotUI(); break;
                    default:
                        Debug.LogWarning($"[PotLeakageUIController] Invalid task index: {taskIndex}");
                        break;
                }
            }
            else
            {
                switch (taskIndex)
                {
                    case 0: SetTask01UI(); break;
                    case 1: SetTask02UI(); break;
                    case 2: SetTask03UI(); break;
                    case 3: SetTask04UI(); break;
                    case 4: SetTask05PotControlMachineUI(); break;
                    case 5: SetTask06PotInspectionUI(); break;
                    case 6: SetTask07ManualModeUI(); break;
                    case 7: SetTask06AnodeDownUI(); break;
                    case 8: SetTask09VoltageAppropriateUI(); break;
                    case 9: SetTask10ToolBoxUI(); break;
                    case 10: SetTask11NormalPotUI(); break;
                    default:
                        Debug.LogWarning($"[PotLeakageUIController] Invalid task index: {taskIndex}");
                        break;
                }
            }
        }

        [Header("Opening Flow References")]
        [Tooltip("Authoritative walkietalkie GameObject (active only in Task 04)")]
        public GameObject walkieTalkieTarget;

        [Tooltip("PersonnelImages UI panel under IdealCBTTemperatureUI")]
        public GameObject personnelImages;

        [System.Serializable]
        public struct WalkieTalkieBlinkSlot
        {
            public Renderer renderer;
            public int slotIndex;
            public Material originalMaterial;
            public string targetName;
        }

        [Header("Walkie-Talkie Highlight Management")]
        private readonly System.Collections.Generic.List<WalkieTalkieBlinkSlot> walkieTalkieBlinkSlots = new System.Collections.Generic.List<WalkieTalkieBlinkSlot>();
        private Coroutine walkieTalkieBlinkCoroutine;
        public bool IsWalkieTalkieBlinking { get; private set; }

        public static readonly string[] WalkieTalkieTargetNames = new string[]
        {
            "defaultMaterial.001",
            "defaultMaterial.002",
            "defaultMaterial.003",
            "defaultMaterial.004",
            "defaultMaterial.005",
            "defaultMaterial.006",
            "defaultMaterial.007",
            "defaultMaterial.008",
            "defaultMaterial.009"
        };

        public System.Collections.Generic.List<WalkieTalkieBlinkSlot> GetWalkieTalkieBlinkSlots() => walkieTalkieBlinkSlots;

        public void DisablePersonnelImages()
        {
            if (personnelImages != null) personnelImages.SetActive(false);
            var piGo = GameObject.Find("PersonnelImages");
            if (piGo != null) piGo.SetActive(false);
            var cbtUI = GameObject.Find("UI/Canvas/IdealCBTTemperatureUI") ?? GameObject.Find("IdealCBTTemperatureUI");
            if (cbtUI != null)
            {
                var pi = cbtUI.transform.Find("PersonnelImages");
                if (pi != null) pi.gameObject.SetActive(false);
            }
        }

        public void DisableWalkieTalkie()
        {
            StopWalkieTalkieBlink();
            if (walkieTalkieTarget != null) walkieTalkieTarget.SetActive(false);
            var wt = GameObject.Find("walkietalkie");
            if (wt != null) wt.SetActive(false);
        }

        public System.Collections.Generic.List<Renderer> CollectWalkieTalkieBlinkTargets()
        {
            InitializeReferences();
            var targetRenderers = new System.Collections.Generic.List<Renderer>();
            walkieTalkieBlinkSlots.Clear();

            if (walkieTalkieTarget == null)
            {
                var wt = GameObject.Find("walkietalkie");
                if (wt == null)
                {
                    var all = Resources.FindObjectsOfTypeAll<GameObject>();
                    foreach (var g in all)
                    {
                        if (g.name == "walkietalkie" && !IsPersistentObject(g) && g.transform.parent == null)
                        {
                            wt = g;
                            break;
                        }
                    }
                }
                if (wt != null) walkieTalkieTarget = wt;
            }

            if (walkieTalkieTarget != null)
            {
                var renderers = walkieTalkieTarget.GetComponentsInChildren<Renderer>(true);
                foreach (var targetName in WalkieTalkieTargetNames)
                {
                    foreach (var r in renderers)
                    {
                        if (r == null) continue;
                        string goName = r.gameObject.name.Trim();
                        Material[] mats = r.sharedMaterials;
                        for (int s = 0; s < mats.Length; s++)
                        {
                            var m = mats[s];
                            string matName = m != null ? m.name.Trim() : "";
                            if (goName.Equals(targetName, System.StringComparison.OrdinalIgnoreCase) ||
                                matName.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
                            {
                                Material origMat = m;
                                if (origMat != null && origMat.name.Contains("Highlight"))
                                {
#if UNITY_EDITOR
                                    origMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Model/Materials/Image_0.mat");
#endif
                                }
                                walkieTalkieBlinkSlots.Add(new WalkieTalkieBlinkSlot
                                {
                                    renderer = r,
                                    slotIndex = s,
                                    originalMaterial = origMat,
                                    targetName = targetName
                                });
                                if (!targetRenderers.Contains(r)) targetRenderers.Add(r);
                                break;
                            }
                        }
                    }
                }
            }
            return targetRenderers;
        }

        public void StartWalkieTalkieBlink()
        {
            InitializeReferences();

            if (walkieTalkieBlinkCoroutine != null)
            {
                StopCoroutine(walkieTalkieBlinkCoroutine);
                walkieTalkieBlinkCoroutine = null;
            }

            if (potHighlightMaterial == null)
            {
#if UNITY_EDITOR
                potHighlightMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/PotLeakage/Materials/M_PotLeakage_Highlight.mat");
#endif
            }

            CollectWalkieTalkieBlinkTargets();

            if (walkieTalkieTarget != null)
            {
                walkieTalkieTarget.SetActive(true);
            }

            Debug.Log("[PotLeakage] Emergency Communication blink starting");
            Debug.Log($"[PotLeakage] Walkie blink target count = {walkieTalkieBlinkSlots.Count}");
            foreach (var slot in walkieTalkieBlinkSlots)
            {
                Debug.Log($"[PotLeakage] Blink target = {slot.targetName}");
            }

            IsWalkieTalkieBlinking = true;
            if (Application.isPlaying)
            {
                walkieTalkieBlinkCoroutine = StartCoroutine(WalkieTalkieBlinkRoutine());
            }
            else
            {
                ApplyWalkieTalkieHighlightState(true);
            }
        }

        private IEnumerator WalkieTalkieBlinkRoutine()
        {
            IsWalkieTalkieBlinking = true;
            bool showHighlight = true;
            while (IsWalkieTalkieBlinking)
            {
                ApplyWalkieTalkieHighlightState(showHighlight);
                yield return new WaitForSeconds(0.35f);
                showHighlight = !showHighlight;
            }
            walkieTalkieBlinkCoroutine = null;
        }

        private void ApplyWalkieTalkieHighlightState(bool highlight)
        {
            for (int i = 0; i < walkieTalkieBlinkSlots.Count; i++)
            {
                var slot = walkieTalkieBlinkSlots[i];
                if (slot.renderer != null)
                {
                    Material[] mats = slot.renderer.sharedMaterials;
                    if (slot.slotIndex >= 0 && slot.slotIndex < mats.Length)
                    {
                        mats[slot.slotIndex] = highlight ? potHighlightMaterial : slot.originalMaterial;
                        slot.renderer.sharedMaterials = mats;
                    }
                }
            }
        }

        public void StopWalkieTalkieBlink()
        {
            if (walkieTalkieBlinkCoroutine != null)
            {
                StopCoroutine(walkieTalkieBlinkCoroutine);
                walkieTalkieBlinkCoroutine = null;
            }
            IsWalkieTalkieBlinking = false;

            int restoredCount = 0;
            for (int i = 0; i < walkieTalkieBlinkSlots.Count; i++)
            {
                var slot = walkieTalkieBlinkSlots[i];
                if (slot.renderer != null && slot.originalMaterial != null)
                {
                    Material[] mats = slot.renderer.sharedMaterials;
                    if (slot.slotIndex >= 0 && slot.slotIndex < mats.Length)
                    {
                        mats[slot.slotIndex] = slot.originalMaterial;
                        slot.renderer.sharedMaterials = mats;
                        restoredCount++;
                    }
                }
            }

            Debug.Log("[PotLeakage] Emergency Communication blink stopped");
            Debug.Log($"[PotLeakage] Walkie-Talkie materials restored: {restoredCount}");

            walkieTalkieBlinkSlots.Clear();
        }

        public void HighlightWalkieTalkie() => StartWalkieTalkieBlink();
        public void RestoreWalkieTalkieMaterials() => StopWalkieTalkieBlink();

        public void DisableWalkieTalkie2()
        {
            StopWalkieTalkie2Blink();
            if (walkieTalkie2Target != null) walkieTalkie2Target.SetActive(false);
            var wt = GameObject.Find("walkietalkie_2");
            if (wt != null) wt.SetActive(false);
        }

        public void StartWalkieTalkie2Blink()
        {
            InitializeReferences();

            if (walkieTalkie2BlinkCoroutine != null)
            {
                StopCoroutine(walkieTalkie2BlinkCoroutine);
                walkieTalkie2BlinkCoroutine = null;
            }

            if (walkieTalkie2Target == null)
            {
                var wt = GameObject.Find("walkietalkie_2");
                if (wt == null)
                {
                    var all = Resources.FindObjectsOfTypeAll<GameObject>();
                    foreach (var g in all)
                    {
                        if (g.name == "walkietalkie_2" && !IsPersistentObject(g))
                        {
                            wt = g;
                            break;
                        }
                    }
                }
                if (wt != null) walkieTalkie2Target = wt;
            }

            if (walkieTalkie2Target != null)
            {
                walkieTalkie2Target.SetActive(true);

                var box = walkieTalkie2Target.GetComponent<Collider>();
                if (box == null) box = walkieTalkie2Target.AddComponent<BoxCollider>();
                box.enabled = true;

                var interaction = walkieTalkie2Target.GetComponent<WalkieTalkie2Interaction>();
                if (interaction == null) interaction = walkieTalkie2Target.AddComponent<WalkieTalkie2Interaction>();
                interaction.enabled = true;
                interaction.uiController = this;

                // Ensure child defaultMaterial.007 also has WalkieTalkie2Interaction and enabled collider
                var defMat = walkieTalkie2Target.transform.Find("Collada visual scene group/Part_5_L/defaultMaterial.007");
                if (defMat == null)
                {
                    var transforms = walkieTalkie2Target.GetComponentsInChildren<Transform>(true);
                    foreach (var t in transforms)
                    {
                        if (t.name == "defaultMaterial.007")
                        {
                            defMat = t;
                            break;
                        }
                    }
                }
                if (defMat != null)
                {
                    var childInteraction = defMat.GetComponent<WalkieTalkie2Interaction>();
                    if (childInteraction == null) childInteraction = defMat.gameObject.AddComponent<WalkieTalkie2Interaction>();
                    childInteraction.enabled = true;
                    childInteraction.uiController = this;
                }

                Debug.Log("[POT LEAKAGE] walkietalkie_2 ACTIVE");
                Debug.Log($"[POT LEAKAGE] collider enabled = {(box != null && box.enabled ? "TRUE" : "FALSE")}");
                Debug.Log($"[POT LEAKAGE] interaction enabled = {(interaction != null && interaction.enabled ? "TRUE" : "FALSE")}");

                if (potHighlightMaterial == null)
                {
#if UNITY_EDITOR
                    potHighlightMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/PotLeakage/Materials/M_PotLeakage_Highlight.mat");
#endif
                }

                walkieTalkie2BlinkSlots.Clear();

                var renderers = walkieTalkie2Target.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    Material[] mats = r.sharedMaterials;
                    for (int s = 0; s < mats.Length; s++)
                    {
                        var m = mats[s];
                        walkieTalkie2BlinkSlots.Add(new WalkieTalkieBlinkSlot
                        {
                            renderer = r,
                            slotIndex = s,
                            originalMaterial = m
                        });
                    }
                }

                IsWalkieTalkie2Blinking = true;
                if (Application.isPlaying)
                {
                    walkieTalkie2BlinkCoroutine = StartCoroutine(WalkieTalkie2BlinkRoutine());
                }
                else
                {
                    ApplyWalkieTalkie2HighlightState(true);
                }
            }
        }

        private IEnumerator WalkieTalkie2BlinkRoutine()
        {
            IsWalkieTalkie2Blinking = true;
            bool showHighlight = true;
            while (IsWalkieTalkie2Blinking)
            {
                ApplyWalkieTalkie2HighlightState(showHighlight);
                yield return new WaitForSeconds(0.35f);
                showHighlight = !showHighlight;
            }
            walkieTalkie2BlinkCoroutine = null;
        }

        private void ApplyWalkieTalkie2HighlightState(bool highlight)
        {
            for (int i = 0; i < walkieTalkie2BlinkSlots.Count; i++)
            {
                var slot = walkieTalkie2BlinkSlots[i];
                if (slot.renderer != null)
                {
                    Material[] mats = slot.renderer.sharedMaterials;
                    if (slot.slotIndex >= 0 && slot.slotIndex < mats.Length)
                    {
                        mats[slot.slotIndex] = highlight ? potHighlightMaterial : slot.originalMaterial;
                        slot.renderer.sharedMaterials = mats;
                    }
                }
            }
        }

        public void StopWalkieTalkie2Blink()
        {
            if (walkieTalkie2BlinkCoroutine != null)
            {
                StopCoroutine(walkieTalkie2BlinkCoroutine);
                walkieTalkie2BlinkCoroutine = null;
            }
            IsWalkieTalkie2Blinking = false;

            for (int i = 0; i < walkieTalkie2BlinkSlots.Count; i++)
            {
                var slot = walkieTalkie2BlinkSlots[i];
                if (slot.renderer != null && slot.originalMaterial != null)
                {
                    Material[] mats = slot.renderer.sharedMaterials;
                    if (slot.slotIndex >= 0 && slot.slotIndex < mats.Length)
                    {
                        mats[slot.slotIndex] = slot.originalMaterial;
                        slot.renderer.sharedMaterials = mats;
                    }
                }
            }
            walkieTalkie2BlinkSlots.Clear();
        }

        public const string ManualModeInstructionText = "Now I have changed from Auto Mode to Manual Mode. As you can see, the display has changed from Auto to Manual and the MAN light is glowing. Since the pot leakage is severe, we need to reduce the voltage. Find the Anode Down button.";

        public void OnWalkieTalkie2Clicked()
        {
            if (isWalkieTalkie2Clicked) return;
            isWalkieTalkie2Clicked = true;

            Debug.Log("[POT LEAKAGE] walkietalkie_2 CLICK RECEIVED");

            // 1. Stop walkietalkie_2 blinking
            // 2. Remove its temporary highlight (restores original materials)
            StopWalkieTalkie2Blink();

            // 3. Keep walkietalkie_2 active
            if (walkieTalkie2Target != null) walkieTalkie2Target.SetActive(true);

            // 4. DO NOT complete the Task yet.
            SequenceHelperFunctions.OnInterceptTaskCompletion = () => true;

            // 5. Change the ModeIndicator from its default ● AUTO to ● MAN
            if (potControlDisplayController != null)
            {
                potControlDisplayController.SetModeIndicatorMan();
            }

            PlaySFX(clickAudioClip);

            // Hide/block Next button until the 4 Anode Down clicks are completed
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(false);
            }

            if (manualModeRoutine != null) StopCoroutine(manualModeRoutine);
            if (Application.isPlaying)
            {
                manualModeRoutine = StartCoroutine(ManualModeInteractionRoutine());
            }
            else
            {
                // In Edit Mode, update description UI, validate speech, and trigger visual state
                if (descriptionText != null)
                {
                    descriptionText.gameObject.SetActive(true);
                    descriptionText.text = ManualModeInstructionText;
                }
                Debug.Log("[POT LEAKAGE] Manual Mode description shown");
                Debug.Log($"[POT LEAKAGE TTS]\nManualModeInstruction\nSpeechText = {ManualModeInstructionText}\nAudioStarted = TRUE");
                Debug.Log("[POT LEAKAGE] Manual Mode TTS STARTED");
                SpeakDescriptionText(ManualModeInstructionText, "TASK_11_MANUAL_MODE");
                Debug.Log("[POT LEAKAGE] Manual Mode TTS COMPLETED");
                ActivateManualModeVisualState();
            }
        }

        private IEnumerator ManualModeInteractionRoutine()
        {
            isAnodeDownInteractable = false;

            // 1. Update description text
            // 2. Show the description UI
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = ManualModeInstructionText;
            }
            if (welcomePanel != null) welcomePanel.SetActive(true);
            Debug.Log("[POT LEAKAGE] Manual Mode description shown");

            // 3. Start the existing LocalTTS voice-over
            Debug.Log($"[POT LEAKAGE TTS]\nManualModeInstruction\nSpeechText = {ManualModeInstructionText}\nAudioStarted = TRUE");
            Debug.Log("[POT LEAKAGE] Manual Mode TTS STARTED");
            SpeakDescriptionText(ManualModeInstructionText, "TASK_11_MANUAL_MODE");

            // 4. Wait until the voice-over finishes
            var mgr = TruckTyreReplacement.Core.Manager.Instance;
            if (mgr != null)
            {
                var src = mgr.GetVoiceAudioSource();
                float waitStart = Time.realtimeSinceStartup;
                // Wait for playback to begin (or timeout after 1.5s if offline/headless)
                while (!mgr.IsSpeaking && (src == null || !src.isPlaying) && (Time.realtimeSinceStartup - waitStart) < 1.5f)
                {
                    yield return null;
                }
                // Wait while speaking
                while (mgr.IsSpeaking || (src != null && src.isPlaying))
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(4.0f);
            }

            Debug.Log("[POT LEAKAGE] Manual Mode TTS COMPLETED");

            // 5. ONLY AFTER the voice-over finishes, enable the next visual interaction
            ActivateManualModeVisualState();
            manualModeRoutine = null;
        }

        public void ActivateManualModeVisualState()
        {
            InitializeReferences();

            // MAN_Lamp glowing #FFEB26, AUTO_Lamp #FFFFFF not glowing, ModeIndicator ● MAN
            if (potControlDisplayController != null)
            {
                potControlDisplayController.SetManualModeLamps();
                // Ensure initial value 4.454 if not already set
                if (anodeDownClickCount == 0 && Mathf.Abs(potControlDisplayController.voltage - 4.454f) > 0.001f)
                {
                    potControlDisplayController.SetVoltage(4.454f);
                }
            }

            // Highlight Cube.016 using fluorescent green
            StartAnodeDownHighlight();

            // Enable Cube.016 interaction
            isAnodeDownInteractable = true;
            anodeDownClickCount = 0;

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(false);
            }

            Debug.Log("[POT LEAKAGE] Cube.016 INTERACTION ENABLED");
        }

        public void StartAnodeDownHighlight() => StartAnodeDownBlink();
        public void StopAnodeDownHighlight() => StopAnodeDownBlink();

        private Coroutine anodeDownBlinkCoroutine;
        public bool IsAnodeDownBlinking { get; private set; }

        public void StartAnodeDownBlink()
        {
            if (anodeDownButtonTarget == null || anodeDownButtonTarget.name != "Cube.016" || (anodeDownButtonTarget.transform.parent != null && anodeDownButtonTarget.transform.parent.name.ToLower().Contains("crane")))
            {
                InitializeReferences();
            }

            if (potHighlightMaterial == null)
            {
#if UNITY_EDITOR
                potHighlightMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/PotLeakage/Materials/M_PotLeakage_Highlight.mat");
#endif
            }

            if (anodeDownBlinkCoroutine != null)
            {
                StopCoroutine(anodeDownBlinkCoroutine);
                anodeDownBlinkCoroutine = null;
            }

            if (anodeDownRenderer != null && potHighlightMaterial != null)
            {
                IsAnodeDownBlinking = true;
                if (Application.isPlaying)
                {
                    anodeDownBlinkCoroutine = StartCoroutine(AnodeDownBlinkRoutine());
                }
                else
                {
                    anodeDownRenderer.material = potHighlightMaterial;
                }
            }
        }

        private IEnumerator AnodeDownBlinkRoutine()
        {
            IsAnodeDownBlinking = true;
            bool showHighlight = true;
            while (IsAnodeDownBlinking)
            {
                if (anodeDownRenderer != null)
                {
                    anodeDownRenderer.material = showHighlight ? potHighlightMaterial : anodeDownOriginalMaterial;
                }
                yield return new WaitForSeconds(0.35f);
                showHighlight = !showHighlight;
            }
            anodeDownBlinkCoroutine = null;
        }

        public void StopAnodeDownBlink()
        {
            if (anodeDownBlinkCoroutine != null)
            {
                StopCoroutine(anodeDownBlinkCoroutine);
                anodeDownBlinkCoroutine = null;
            }
            IsAnodeDownBlinking = false;
            if (anodeDownRenderer != null && anodeDownOriginalMaterial != null)
            {
                anodeDownRenderer.material = anodeDownOriginalMaterial;
            }
        }

        public void OnAnodeDownButtonClicked()
        {
            if (SequenceHelperFunctions.instance != null)
            {
                SequenceHelperFunctions.instance.HandleAnodeDownClick();
                return;
            }

            if (!isAnodeDownInteractable)
            {
                Debug.Log("[AnodeDown] Click ignored: button is not currently interactable.");
                return;
            }

            if (anodeDownClickCount >= 4)
            {
                Debug.Log("[AnodeDown] Click ignored: 4 clicks already completed.");
                return;
            }

            anodeDownClickCount++;
            PlaySFX(clickAudioClip);

            switch (anodeDownClickCount)
            {
                case 1:
                    // CLICK 1: 4.544 -> 4.444
                    if (potControlDisplayController != null) potControlDisplayController.SetVoltage(4.444f);
                    Debug.Log("[PotLeakage] Anode Down clicked: 1/4");
                    Debug.Log("[PotLeakage] Voltage = 4.444 V");
                    break;
                case 2:
                    // CLICK 2: 4.444 -> 4.344
                    if (potControlDisplayController != null) potControlDisplayController.SetVoltage(4.344f);
                    Debug.Log("[PotLeakage] Anode Down clicked: 2/4");
                    Debug.Log("[PotLeakage] Voltage = 4.344 V");
                    break;
                case 3:
                    // CLICK 3: 4.344 -> 4.244
                    if (potControlDisplayController != null) potControlDisplayController.SetVoltage(4.244f);
                    Debug.Log("[PotLeakage] Anode Down clicked: 3/4");
                    Debug.Log("[PotLeakage] Voltage = 4.244 V");
                    break;
                case 4:
                    // CLICK 4: 4.244 -> 4.144 and safe limit reached
                    if (potControlDisplayController != null)
                    {
                        potControlDisplayController.SetVoltage(4.144f);
                        potControlDisplayController.suppressWarningBlink = true;
                        potControlDisplayController.StopWarningBlink();
                        if (potControlDisplayController.voltageDisplayText != null)
                        {
                            potControlDisplayController.voltageDisplayText.enabled = true;
                        }
                        if (potControlDisplayController.kaDataDisplayText != null)
                        {
                            potControlDisplayController.kaDataDisplayText.enabled = true;
                        }
                    }
                    Debug.Log("[PotLeakage] Anode Down clicked: 4/4");
                    Debug.Log("[PotLeakage] Voltage = 4.144 V");
                    Debug.Log("[PotLeakage] Safe voltage reached");
                    Debug.Log("[PotLeakage] Awaiting user Continue");
                    CompleteAnodeDownSequence();
                    break;
            }
        }

        public void CompleteAnodeDownSequence()
        {
            // 1. Set VoltageDisplay ActiveDigits = 4.144
            if (potControlDisplayController != null)
            {
                potControlDisplayController.SetVoltage(4.144f);
                // 2. STOP ActiveDigits blinking behavior permanently
                potControlDisplayController.suppressWarningBlink = true;
                potControlDisplayController.StopWarningBlink();
                if (potControlDisplayController.voltageDisplayText != null)
                {
                    potControlDisplayController.voltageDisplayText.enabled = true;
                }
                if (potControlDisplayController.kaDataDisplayText != null)
                {
                    potControlDisplayController.kaDataDisplayText.enabled = true;
                }
            }

            // 3. Stop Cube.016 fluorescent-green highlight and blink
            StopAnodeDownBlink();

            // 4. Disable Cube.016 interaction for this task
            isAnodeDownInteractable = false;

            // 5. Keep MAN_Lamp highlighted #FFEB26, AUTO_Lamp #FFFFFF, ModeIndicator ● MAN
            if (potControlDisplayController != null)
            {
                potControlDisplayController.SetManualModeLamps();
            }

            // 6. Unblock task progression so Next/Continue button advances
            SequenceHelperFunctions.OnInterceptTaskCompletion = null;

            // 7. Update UI with the required completion text
            string safeMsg = "The voltage is now at an appropriate level. We can proceed with leakage control.";
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = safeMsg;
            }
            if (titleText != null)
            {
                titleText.text = "Safe Limits Reached";
            }
            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "VOLTAGE AT SAFE LEVEL (< 4.2 V)";
            }

            // 8. Ensure Continue / Next button is active and visible
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(true);
            }

            // 9. Play voice-over: Voice completion NEVER auto-advances
            SpeakDescriptionText(safeMsg, "TASK_06_SAFE_VOLTAGE");

            Debug.Log("[POT LEAKAGE] AnodeDown sequence COMPLETE - 4.144 V reached, awaiting manual Next click.");
        }

        public void ResetManualModeInteraction()
        {
            if (manualModeRoutine != null)
            {
                StopCoroutine(manualModeRoutine);
                manualModeRoutine = null;
            }

            isAnodeDownInteractable = false;
            anodeDownClickCount = 0;
            isWalkieTalkie2Clicked = false;

            StopAnodeDownHighlight();

            if (potControlDisplayController != null)
            {
                potControlDisplayController.RestoreDefaultModeAndLamps();
            }
        }

        // Opening Flow Task 01 - 04 aliases
        public void SetOpeningTask01UI() => SetTask01UI();
        public void SetOpeningTask02UI() => SetTask02UI();
        public void SetOpeningTask03UI() => SetTask03UI();
        public void SetOpeningTask04UI() => SetTask04UI();

        public void ResetWalkieCommunicationSystem()
        {
            var commSys = UnityEngine.Object.FindFirstObjectByType<WalkieCommunicationSystem>();
            if (commSys != null) commSys.ResetSystem();
        }

        /// <summary>
        /// TASK 01 — Welcome: Introduces pot leakage training module.
        /// Camera: TransformPoints/Welcome
        /// Panel visible, PersonnelImages disabled, LocalTTS speaks welcome text. Waits for Continue button.
        /// </summary>
        public void SetTask01UI()
        {
            InitializeReferences();
            ResetOpeningToolStates();

            DisableWalkieTalkie();
            DisableWalkieTalkie2();
            ResetManualModeInteraction();
            DisableWalkieTalkie2();
            var beamScale = UnityEngine.Object.FindFirstObjectByType<PotBeamScaleController>();
            if (beamScale != null) beamScale.StopNeedleAnimation();
            if (potControlDisplayController != null)
            {
                potControlDisplayController.RestoreNormalLamps();
            }
            isTask09Verified = false;
            SequenceHelperFunctions.OnInterceptTaskCompletion = null;

            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<PotLeakage.Camera.PotLeakageCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.MoveToWelcome();
                }
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(true);
            }
            DisablePersonnelImages();
            StopPotBlink(false);
            RestorePotOriginalMaterial();
            ResetWalkieCommunicationSystem();
            if (magmaDroolController != null)
            {
                magmaDroolController.StopMagmaDrool(true);
                magmaDroolController.StopMoltenMetalOverflow();
                magmaDroolController.ResetFloorSpill();
                if (magmaDroolController.flowMesh != null)
                {
                    magmaDroolController.flowMesh.gameObject.SetActive(false);
                }
                magmaDroolController.gameObject.SetActive(false);
            }

            // Ensure old machine model is disabled
            var machine = GameObject.Find("Machine");
            if (machine == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "Machine" && !IsPersistentObject(g) && g.transform.parent == null)
                    {
                        machine = g;
                        break;
                    }
                }
            }
            if (machine != null) machine.SetActive(false);

            if (leakageIncidentCoroutine != null)
            {
                StopCoroutine(leakageIncidentCoroutine);
                leakageIncidentCoroutine = null;
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "Welcome";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
                statusText.text = "";
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = "Welcome to the Vedanta Pot Leakage Training Visualization.\nThis module covers the Emergency Response Procedure for Side Shell Leakage at Upper Location.";
                Debug.Log("[PotLeakage TTS] Task 01 Welcome requested");
                SpeakDescriptionText(descriptionText.text, "TASK_01_WELCOME");
            }

            int totalTasks = (SequenceHandler.instance != null && SequenceHandler.instance.sequenceList != null && SequenceHandler.instance.sequenceList.Count > 0 && SequenceHandler.instance.sequenceList[0].TaskList != null)
                ? SequenceHandler.instance.sequenceList[0].TaskList.Count : 8;

            if (progressText != null)
            {
                progressText.text = $"<color=#00E5FF>TASK 01 / {totalTasks:D2}</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress($"TASK 01 / {totalTasks:D2}");
                valueDisplay.SetTitle("Welcome");
                valueDisplay.HideStatus();
                valueDisplay.HideValueDisplay();
            }
        }

        /// <summary>
        /// TASK 02 — Normal Pot Operation: Displays normal potline environment (ware_house + LINE).
        /// Camera: TransformPoints/Normal Pot Operation
        /// Molten leakage mesh ready at start transform. Panel visible, PersonnelImages disabled. Auto advances after voice-over.
        /// </summary>
        public void SetTask02UI()
        {
            InitializeReferences();
            ResetOpeningToolStates();

            DisableWalkieTalkie();
            DisablePersonnelImages();
            StopPotBlink(false);
            RestorePotOriginalMaterial();
            ResetWalkieCommunicationSystem();

            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<PotLeakage.Camera.PotLeakageCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.MoveToNormalPotOperation();
                }
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(true);
            }

            if (leakageIncidentCoroutine != null)
            {
                StopCoroutine(leakageIncidentCoroutine);
                leakageIncidentCoroutine = null;
            }

            // MoltenMetalFlowMesh must remain DISABLED during Task 02 (enabled only when entering Task 03)
            if (magmaDroolController != null)
            {
                magmaDroolController.StopMoltenMetalOverflow();
                if (magmaDroolController.flowMesh != null)
                {
                    magmaDroolController.flowMesh.gameObject.SetActive(false);
                }
                magmaDroolController.gameObject.SetActive(false);
            }

            // Ensure old machine model is disabled
            var machine = GameObject.Find("Machine");
            if (machine == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "Machine" && !IsPersistentObject(g) && g.transform.parent == null)
                    {
                        machine = g;
                        break;
                    }
                }
            }
            if (machine != null) machine.SetActive(false);

            if (welcomePanel != null) welcomePanel.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "Normal Pot Operation";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "NORMAL POTLINE OPERATION";
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = "The reduction pots operate continuously under high temperature and molten bath conditions. Proper monitoring and rapid response to abnormalities are critical to operational safety.";
                SpeakDescriptionText(descriptionText.text, descriptionText.text);
            }

            int totalTasks = (SequenceHandler.instance != null && SequenceHandler.instance.sequenceList != null && SequenceHandler.instance.sequenceList.Count > 0 && SequenceHandler.instance.sequenceList[0].TaskList != null)
                ? SequenceHandler.instance.sequenceList[0].TaskList.Count : 8;

            if (progressText != null)
            {
                progressText.text = $"<color=#00E5FF>TASK 02 / {totalTasks:D2}</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress($"TASK 02 / {totalTasks:D2}");
                valueDisplay.SetTitle("Normal Pot Operation");
                valueDisplay.DisplayNormalStatus("NORMAL POTLINE OPERATION");
                valueDisplay.HideValueDisplay();
            }
        }

        private Coroutine leakageIncidentCoroutine;

        /// <summary>
        /// TASK 03 — Pot Leakage: MoltenMetalFlowMesh animates downward from START to TARGET.
        /// Description UI panel and Next button remain visible for dual-path progression.
        /// Voice-over speaks: "Pot leakage has occurred in Pot 69."
        /// Advances automatically to Task 04 when voice-over completes or user clicks Next.
        /// </summary>
        public void SetTask03UI()
        {
            InitializeReferences();
            ResetOpeningToolStates();

            DisableWalkieTalkie();
            DisablePersonnelImages();
            StopPotBlink(false);
            RestorePotOriginalMaterial();
            ResetWalkieCommunicationSystem();

            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<PotLeakage.Camera.PotLeakageCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.MoveToNormalPotOperation();
                }
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);
            if (nextButton != null) nextButton.gameObject.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "Pot Leakage Incident";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "POT LEAKAGE DETECTED";
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = "Pot leakage has occurred in Pot 69.";
            }

            int totalTasks = (SequenceHandler.instance != null && SequenceHandler.instance.sequenceList != null && SequenceHandler.instance.sequenceList.Count > 0 && SequenceHandler.instance.sequenceList[0].TaskList != null)
                ? SequenceHandler.instance.sequenceList[0].TaskList.Count : 8;

            if (progressText != null)
            {
                progressText.text = $"<color=#00E5FF>TASK 03 / {totalTasks:D2}</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress($"TASK 03 / {totalTasks:D2}");
                valueDisplay.SetTitle("Pot Leakage Incident");
                valueDisplay.DisplayNormalStatus("POT LEAKAGE DETECTED");
                valueDisplay.HideValueDisplay();
            }

            if (leakageIncidentCoroutine != null)
            {
                StopCoroutine(leakageIncidentCoroutine);
                leakageIncidentCoroutine = null;
            }

            // Ensure droplets and floor spill are disabled
            if (magmaDroolController != null)
            {
                if (magmaDroolController.moltenMetalDroplets != null)
                {
                    magmaDroolController.moltenMetalDroplets.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    magmaDroolController.moltenMetalDroplets.gameObject.SetActive(false);
                }
                if (magmaDroolController.floorSpill != null)
                {
                    magmaDroolController.floorSpill.StopSpill();
                    magmaDroolController.floorSpill.gameObject.SetActive(false);
                }
            }

            if (magmaDroolController != null)
            {
                magmaDroolController.gameObject.SetActive(true);
                magmaDroolController.PlayMoltenMetalOverflow();
            }

            string speechText = "Pot leakage has occurred in Pot 69.";
            var mgr = TruckTyreReplacement.Core.Manager.Instance;
            if (mgr == null) mgr = Object.FindFirstObjectByType<TruckTyreReplacement.Core.Manager>();

            if (mgr != null)
            {
                mgr.SpeakText(speechText, speechText);
            }
        }

        /// <summary>
        /// TASK 04 — Call Superintendent / Walkie-Talkie:
        /// Snaps camera to TransformPoints/Normal Pot BEFORE displaying description UI.
        /// Walkie-talkie activated and highlighted fluorescent green.
        /// Next button visible for dual-path progression.
        /// </summary>
        public void SetTask04UI()
        {
            InitializeReferences();

            DisablePersonnelImages();
            ResetWalkieCommunicationSystem();

            // 1. FIRST snap camera instantly to TransformPoints/Normal Pot BEFORE showing UI
            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<PotLeakage.Camera.PotLeakageCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.MoveToNormalPot();
                }
            }

            // 2. Description panel and Next button remain active
            if (welcomePanel != null) welcomePanel.SetActive(true);
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(true);
            }

            // Ensure flow mesh remains visible at target position with full flow
            if (magmaDroolController != null)
            {
                magmaDroolController.gameObject.SetActive(true);
                if (magmaDroolController.flowMesh != null)
                {
                    magmaDroolController.flowMesh.gameObject.SetActive(true);
                }
                if (magmaDroolController.flowMeshRenderer != null)
                {
                    magmaDroolController.flowMeshRenderer.enabled = true;
                }
            }

            if (titleText != null)
            {
                titleText.text = "Emergency Communication";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "REPORT LEAKAGE IMMEDIATELY";
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = "Let us call the Superintendent in such situation by picking up the walkie-talkie.";
                SpeakDescriptionText("Let us call the Superintendent in such situation by picking up the walkie-talkie.", "TASK_04");
            }

            int totalTasks = (SequenceHandler.instance != null && SequenceHandler.instance.sequenceList != null && SequenceHandler.instance.sequenceList.Count > 0 && SequenceHandler.instance.sequenceList[0].TaskList != null)
                ? SequenceHandler.instance.sequenceList[0].TaskList.Count : 8;

            if (progressText != null)
            {
                progressText.text = $"<color=#00E5FF>TASK 04 / {totalTasks:D2}</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress($"TASK 04 / {totalTasks:D2}");
                valueDisplay.SetTitle("Emergency Communication");
                valueDisplay.DisplayNormalStatus("REPORT LEAKAGE IMMEDIATELY");
                valueDisplay.HideValueDisplay();
            }

            // Start fluorescent-green blinking across all 10 requested material slots
            StartWalkieTalkieBlink();
        }

        /// <summary>
        /// TASK 05 — Pot Control Machine:
        /// Snaps camera to TransformPoints/PotControlMachine.
        /// Mode AUTO, lamps normal.
        /// "If it is not safe to reach the duct end, we must call the Pot Controller team to check the voltage on the main server."
        /// Next button visible for dual-path progression.
        /// <summary>
        /// TASK 05 (Stage 1 — Observation):
        /// Snaps camera immediately to TransformPoints/PotControlMachine.
        /// ActiveDigits MUST NOT blink yet (normal state). Voltmeter needle normal. Initial voltage = 4.544 V.
        /// Description & LocalTTS VO: "Observe the voltage value. The safe limit is below 4.5 V, with current below 350 kA."
        /// Next button visible for progression.
        /// </summary>
        public void SetTask05PotControlMachineUI()
        {
            InitializeReferences();
            ResetOpeningToolStates();
            StopPotBlink(false);
            StopWalkieTalkieBlink();
            // WalkieCommunicationCanvas remains active until after Normal Pot snap in Task 09/10

            // Snap camera to Pot Control Machine
            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<PotLeakage.Camera.PotLeakageCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.MoveToPotControlMachine();
                }
            }

            if (potControlDisplayController == null)
            {
                potControlDisplayController = Object.FindAnyObjectByType<PotControlDisplayController>();
            }
            if (potControlDisplayController != null)
            {
                potControlDisplayController.SetModeIndicatorAuto();
                potControlDisplayController.RestoreNormalLamps();
                potControlDisplayController.SetVoltage(4.544f);
                potControlDisplayController.suppressWarningBlink = true;
                potControlDisplayController.StopWarningBlink();
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(true);
            }

            if (titleText != null)
            {
                titleText.text = "Pot Control Machine";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "OBSERVE VOLTAGE AND CURRENT";
            }

            string desc = "Observe the voltage value. The safe limit is below 4.5 V, with current below 350 kA.";
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = desc;
            }

            int totalTasks = (SequenceHandler.instance != null && SequenceHandler.instance.sequenceList != null && SequenceHandler.instance.sequenceList.Count > 0 && SequenceHandler.instance.sequenceList[0].TaskList != null)
                ? SequenceHandler.instance.sequenceList[0].TaskList.Count : 10;

            if (progressText != null)
            {
                progressText.text = $"<color=#00E5FF>TASK 05 / {totalTasks:D2}</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress($"TASK 05 / {totalTasks:D2}");
                valueDisplay.SetTitle("Pot Control Machine");
                valueDisplay.DisplayNormalStatus("OBSERVE VOLTAGE AND CURRENT");
                valueDisplay.HideValueDisplay();
            }

            SequenceHelperFunctions.OnInterceptTaskCompletion = null;
            SpeakDescriptionText(desc, "TASK_05_POT_CONTROL_OBSERVATION");
        }

        /// <summary>
        /// TASK 06 (Stage 2 — Active Digits & Needle Tremble):
        /// Camera stays at TransformPoints/PotControlMachine.
        /// ActiveDigits of VoltageDisplay and KADataDisplay blink (0.5s ON / 0.5s OFF).
        /// Voltmeter needle moves to Tick_Minor_-42 and trembles subtly.
        /// Warning description & VO play. Voice finish does NOT advance.
        /// User clicks NEXT.
        /// </summary>
        public void SetTask06PotControlWarningUI()
        {
            InitializeReferences();
            StopPotBlink(false);
            StopWalkieTalkieBlink();

            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<PotLeakage.Camera.PotLeakageCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.MoveToPotControlMachine();
                }
            }

            if (potControlDisplayController == null)
            {
                potControlDisplayController = Object.FindAnyObjectByType<PotControlDisplayController>();
            }
            if (potControlDisplayController != null)
            {
                potControlDisplayController.suppressWarningBlink = false;
                potControlDisplayController.StartWarningBlink();
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);
            if (nextButton != null) nextButton.gameObject.SetActive(true);

            if (titleText != null) titleText.text = "Pot Control Warning";
            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "SAFE LIMIT EXCEEDED — EMERGENCY TRIP";
            }

            string desc = "Active digits on Voltage and Current displays are blinking. Beam level exceeds safe limits (> 350). Pot voltage ceiling is 4.5 V. Request emergency trip.";
            string spokenVO = "Active digits on the voltage and current displays are blinking. The beam level exceeds the safe limit of 350, and the pot voltage is at the 4.5 volt ceiling. Request an emergency trip.";

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = desc;
            }

            int totalTasks = (SequenceHandler.instance != null && SequenceHandler.instance.sequenceList != null && SequenceHandler.instance.sequenceList.Count > 0 && SequenceHandler.instance.sequenceList[0].TaskList != null)
                ? SequenceHandler.instance.sequenceList[0].TaskList.Count : 10;

            if (progressText != null)
            {
                progressText.text = $"<color=#00E5FF>TASK 06 / {totalTasks:D2}</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress($"TASK 06 / {totalTasks:D2}");
                valueDisplay.SetTitle("Pot Control Warning");
                valueDisplay.DisplayNormalStatus("SAFE LIMIT EXCEEDED");
                valueDisplay.HideValueDisplay();
            }

            SequenceHelperFunctions.OnInterceptTaskCompletion = null;
            PlayTrainingVoiceOver(task08Clip, spokenVO, "TASK_06_POT_CONTROL_WARNING");
        }

        /// <summary>
        /// TASK 07 (Stage 3 — Beam Scale Verification):
        /// Stops Stage 2 warning effects (ActiveDigits blink OFF, needle tremble OFF).
        /// Camera snaps directly to TransformPoints/PotInspection.
        /// Starts needle warning animation on PotBeamScaleController.
        /// Displays description: "Verify the beam level on both the Pot Controller and the Beam Scale."
        /// Speaks exact text through LocalTTS. Voice finish does NOT advance. User clicks NEXT.
        /// </summary>
        public void SetTask07BeamScaleVerificationUI()
        {
            InitializeReferences();
            ResetOpeningToolStates();
            StopPotBlink(false);
            StopWalkieTalkieBlink();

            // Stop Stage 2 warning effects on PotControlMachine
            if (potControlDisplayController == null)
            {
                potControlDisplayController = Object.FindAnyObjectByType<PotControlDisplayController>();
            }
            if (potControlDisplayController != null)
            {
                potControlDisplayController.suppressWarningBlink = true;
                potControlDisplayController.StopWarningBlink();
            }

            // Snap camera directly to TransformPoints/PotInspection
            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<PotLeakage.Camera.PotLeakageCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.MoveToPotInspection();
                }
                else
                {
                    var tp = GameObject.Find("CameraSystem/TransformPoints/PotInspection") ?? GameObject.Find("PotInspection");
                    if (tp != null && SequenceHelperFunctions.instance != null)
                    {
                        SequenceHelperFunctions.instance.SnapCameraToTransform(tp.transform);
                    }
                }
            }

            var beamScale = UnityEngine.Object.FindAnyObjectByType<PotBeamScaleController>(FindObjectsInactive.Include);
            if (beamScale != null)
            {
                beamScale.StartNeedleWarningAnimation();
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);
            if (nextButton != null) nextButton.gameObject.SetActive(true);

            if (titleText != null) titleText.text = "Verify Beam Level";
            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "BEAM SCALE VERIFICATION";
            }

            string desc = "Verify the beam level on both the Pot Controller and the Beam Scale.";
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = desc;
            }

            int totalTasks = (SequenceHandler.instance != null && SequenceHandler.instance.sequenceList != null && SequenceHandler.instance.sequenceList.Count > 0 && SequenceHandler.instance.sequenceList[0].TaskList != null)
                ? SequenceHandler.instance.sequenceList[0].TaskList.Count : 10;

            if (progressText != null)
            {
                progressText.text = $"<color=#00E5FF>TASK 07 / {totalTasks:D2}</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress($"TASK 07 / {totalTasks:D2}");
                valueDisplay.SetTitle("Verify Beam Level");
                valueDisplay.DisplayNormalStatus("BEAM SCALE VERIFICATION");
                valueDisplay.HideValueDisplay();
            }

            SequenceHelperFunctions.OnInterceptTaskCompletion = null;
            SpeakDescriptionText(desc, "TASK_07_BEAM_SCALE_VERIFICATION");
        }

        public void SetTask05DuctEndSafetyUI() => SetTask05PotControlMachineUI();
        public void SetTask05UI() => SetTask05PotControlMachineUI();

        /// <summary>
        /// TASK 06 — Emergency Trip Criteria / Pot Control System:
        /// Camera snaps to Pot Control Machine position.
        /// "If either condition is met — the voltage is above 4.5 volts or the beam level is above 350 — you should call for an emergency trip.
        /// If the voltage is increasing slowly while the beam level is above 350, we can ask the rectifier team to reduce the current to 0 kA within a few minutes."
        /// </summary>
        public void SetTask06EmergencyTripUI()
        {
            InitializeReferences();
            StopPotBlink(false);
            StopWalkieTalkieBlink();
            ResetWalkieCommunicationSystem();

            // Snap camera directly to Pot Control Machine
            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<PotLeakage.Camera.PotLeakageCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.MoveToPotControlMachine();
                }
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "Pot Control System";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "EMERGENCY TRIP CRITERIA";
            }

            string desc = "If either condition is met — the voltage is above 4.5 volts or the beam level is above 350 — you should call for an emergency trip. If the voltage is increasing slowly while the beam level is above 350, we can ask the rectifier team to reduce the current to 0 kA within a few minutes.";
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = desc;
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 06 / 08</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 06 / 08");
                valueDisplay.SetTitle("Pot Control System");
                valueDisplay.DisplayNormalStatus("EMERGENCY TRIP CRITERIA");
                valueDisplay.HideValueDisplay();
            }

            string spokenVO = "If either condition is met — the voltage is above 4.5 volts or the beam level is above 350 — you should call for an emergency trip. If the voltage is increasing slowly while the beam level is above 350, we can ask the rectifier team to reduce the current to zero kA within a few minutes.";
            PlayTrainingVoiceOver(task06Clip, spokenVO, "TASK_06_EMERGENCY_TRIP");
        }

        /// <summary>
        /// TASK 07 — Check Safe Limits:
        /// "Check whether the displayed values are above the safe limits."
        /// </summary>
        public void SetTask07CheckSafeLimitsUI()
        {
            InitializeReferences();
            if (welcomePanel != null) welcomePanel.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "Check Safe Limits";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "SAFE LIMIT VERIFICATION";
            }

            string desc = "Check whether the displayed values are above the safe limits.";
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = desc;
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 07 / 08</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 07 / 08");
                valueDisplay.SetTitle("Check Safe Limits");
                valueDisplay.DisplayNormalStatus("SAFE LIMIT VERIFICATION");
                valueDisplay.HideValueDisplay();
            }

            PlayTrainingVoiceOver(task07Clip, desc, "TASK_07_CHECK_LIMITS");
        }

        /// <summary>
        /// TASK 08 — Warning State:
        /// Triggers ActiveDigits blinking on VoltageDisplay and KADataDisplay, plays warning SFX once,
        /// animates NeedlePivot from normal position toward Tick_Minor_-42 with subtle vibration,
        /// and plays Task 08 training VO once.
        /// </summary>
        public void SetTask08WarningStateUI()
        {
            InitializeReferences();
            if (welcomePanel != null) welcomePanel.SetActive(true);

            // 1. Set title
            if (titleText != null)
            {
                titleText.text = "Abnormal Value Warning";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "SAFE LIMIT EXCEEDED — EMERGENCY TRIP";
            }

            // 2. Set description (Single authoritative description, no duplicated Task 07 text)
            string desc = "Active digits on Voltage and Current displays are blinking. Beam level exceeds safe limits (> 350). Pot voltage ceiling is 4.5 V. Request emergency trip.";
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = desc;
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 08 / 08</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 08 / 08");
                valueDisplay.SetTitle("Abnormal Value Warning");
                valueDisplay.DisplayNormalStatus("SAFE LIMIT EXCEEDED");
                valueDisplay.HideValueDisplay();
            }

            // 3. Start Task 08 VO ONCE
            string spokenVO = "Active digits on the voltage and current displays are blinking. The beam level exceeds the safe limit of 350, and the pot voltage is at the 4.5 volt ceiling. Request an emergency trip.";
            PlayTrainingVoiceOver(task08Clip, spokenVO, "TASK_08_ACTIVE_DIGITS");

            // 4. Start ActiveDigits warning blinking + warning SFX ONCE + needle travel/vibration to Tick_Minor_-42
            if (potControlDisplayController == null)
            {
                potControlDisplayController = Object.FindAnyObjectByType<PotControlDisplayController>();
            }
            if (potControlDisplayController != null)
            {
                potControlDisplayController.TriggerWarningState();
            }
        }

        /// <summary>
        /// TASK 06 — Pot Inspection:
        /// Snaps camera to TransformPoints/PotInspection.
        /// Starts NeedlePivot warning animation on VoltageScale.
        /// Next button visible for dual-path progression.
        /// </summary>
        public void SetTask06PotInspectionUI()
        {
            InitializeReferences();
            StopPotBlink(false);
            StopWalkieTalkieBlink();
            DisableWalkieTalkie2();

            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<PotLeakage.Camera.PotLeakageCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.MoveToPotInspection();
                }
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);
            if (nextButton != null) nextButton.gameObject.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "Pot Inspection";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "VERIFY BEAM LEVEL SCALE";
            }

            string desc = "Look at the beam pointer and confirm that the reading on the beam scale matches the beam level shown on the Pot Control panel.";
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = desc;
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 06 / 11</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 06 / 11");
                valueDisplay.SetTitle("Pot Inspection");
                valueDisplay.DisplayNormalStatus("VERIFY BEAM LEVEL SCALE");
                valueDisplay.HideValueDisplay();
            }

            SequenceHelperFunctions.OnInterceptTaskCompletion = null;

            // Start NeedlePivot warning animation from Tick_0 to MinorTick_460 with tremble
            var beamScale = UnityEngine.Object.FindFirstObjectByType<PotBeamScaleController>();
            if (beamScale != null)
            {
                beamScale.StartNeedleWarningAnimation();
            }

            SpeakDescriptionText(desc, desc);
        }

        public void SetTask09PotInspectionUI() => SetTask06PotInspectionUI();

        public bool OnTask09InterceptVerification()
        {
            if (isTask09Verified)
            {
                // Already successfully verified, do not intercept; advance task!
                SequenceHelperFunctions.OnInterceptTaskCompletion = null;
                return false;
            }

            bool matches = false;
            if (potControlDisplayController == null)
            {
                potControlDisplayController = UnityEngine.Object.FindFirstObjectByType<PotControlDisplayController>();
            }
            if (potControlDisplayController != null)
            {
                matches = potControlDisplayController.VerifyBeamLevelAgainstPotController();
            }
            else
            {
                var beamScale = UnityEngine.Object.FindFirstObjectByType<PotBeamScaleController>();
                if (beamScale != null)
                {
                    matches = beamScale.VerifyBeamLevelAgainstPotController();
                }
            }

            if (matches)
            {
                isTask09Verified = true;
                if (statusText != null)
                {
                    statusText.text = "SCALE VERIFIED — MATCH CONFIRMED";
                }
                string successVO = "Good work. You have confirmed that the beam scale matches the Pot Control panel.";
                PlayTrainingVoiceOver(task09SuccessClip, successVO, "TASK_09_SUCCESS");
                SequenceHelperFunctions.OnInterceptTaskCompletion = null;
                return true; // Intercept this click so user hears success VO, next click will advance!
            }
            else
            {
                if (statusText != null)
                {
                    statusText.text = "CHECK THE BEAM POINTER";
                }
                if (descriptionText != null)
                {
                    descriptionText.text = "Check the beam pointer against the Pot Control panel.";
                }
                return true; // Intercept, do not advance
            }
        }

        public void OnVerifyBeamScaleClicked()
        {
            OnTask09InterceptVerification();
        }

        /// <summary>
        /// TASK 10 — Pot Control Machine:
        /// Snaps camera to TransformPoints/PotControlMachine.
        /// AUTO_Lamp glows yellow, MAN_Lamp is active #FFFFFF (no yellow glow).
        /// Voltage > 4.5 V or beamLevel > 350.
        /// </summary>
        public void SetTask10PotControlMachineUI()
        {
            InitializeReferences();
            StopPotBlink(false);
            StopWalkieTalkieBlink();
            DisableWalkieTalkie2();

            var beamScale = UnityEngine.Object.FindFirstObjectByType<PotBeamScaleController>();
            if (beamScale != null)
            {
                beamScale.StopNeedleAnimation();
            }

            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<PotLeakage.Camera.PotLeakageCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.MoveToPotControlMachine();
                }
            }

            if (potControlDisplayController == null)
            {
                potControlDisplayController = UnityEngine.Object.FindFirstObjectByType<PotControlDisplayController>();
            }
            if (potControlDisplayController != null)
            {
                potControlDisplayController.SetTask10Lamps();
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);
            if (nextButton != null) nextButton.gameObject.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "Pot Control Machine";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "POT VOLTAGE CEILING EXCEEDED";
            }

            string desc = "The pot voltage is above the 4.5-volt ceiling. Let us contact the Pot Tending Machine operator team.";
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = desc;
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 10 / 11</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 10 / 11");
                valueDisplay.SetTitle("Pot Control Machine");
                valueDisplay.DisplayNormalStatus("POT VOLTAGE CEILING EXCEEDED");
                valueDisplay.HideValueDisplay();
            }

            SequenceHelperFunctions.OnInterceptTaskCompletion = null;
            PlayTrainingVoiceOver(task10Clip, desc, "TASK_10_POT_CONTROL_MACHINE");
        }

        /// <summary>
        /// TASK 11 — Emergency Communication / Walkie-Talkie 2:
        /// Camera at PotControlMachine.
        /// walkietalkie_2 active and blinking fluorescent green.
        /// Clicking walkietalkie_2 stops blink, plays confirmation VO, and enables CONTINUE button.
        /// </summary>
        public void SetTask11EmergencyCommunicationUI()
        {
            InitializeReferences();
            StopPotBlink(false);
            StopWalkieTalkieBlink();
            ResetManualModeInteraction();

            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<PotLeakage.Camera.PotLeakageCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.MoveToPotControlMachine();
                }
            }

            if (potControlDisplayController == null)
            {
                potControlDisplayController = UnityEngine.Object.FindFirstObjectByType<PotControlDisplayController>();
            }
            if (potControlDisplayController != null)
            {
                potControlDisplayController.SetTask10Lamps();
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "Emergency Communication";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "CONTACT POT CONTROLLER TEAM";
            }

            string desc = "Pick up the walkie-talkie to contact the Pot Controller team.";
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = desc;
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 11 / 11</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 11 / 11");
                valueDisplay.SetTitle("Emergency Communication");
                valueDisplay.DisplayNormalStatus("CONTACT POT CONTROLLER TEAM");
                valueDisplay.HideValueDisplay();
            }

            // Start walkietalkie_2 blinking and hide/block Next button until clicked
            StartWalkieTalkie2Blink();

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(false);
            }
            SequenceHelperFunctions.OnInterceptTaskCompletion = () =>
            {
                // Block advance if not yet clicked
                return true;
            };
        }

        // ── FLOW ARCHITECTURE: TASK 07 TO TASK 11 ────────────────────────────────────────

        /// <summary>
        /// TASK 07 — Manual Mode:
        /// 1. Immediately ModeIndicator -> ● MAN, MAN_Lamp ON (#FFEB26), AUTO_Lamp OFF (#FFFFFF).
        /// 2. Display description: "Now I have changed from Auto Mode to Manual Mode..."
        /// 3. Start LocalTTS voice-over.
        /// 4. Next button VISIBLE for dual-path progression (VO finishes or user clicks Next).
        /// </summary>
        public void SetTask07ManualModeUI()
        {
            InitializeReferences();

            DisableWalkieTalkie();
            DisableWalkieTalkie2();

            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<PotLeakage.Camera.PotLeakageCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.MoveToPotControlMachine();
                }
            }

            var beamScale = UnityEngine.Object.FindFirstObjectByType<PotBeamScaleController>();
            if (beamScale != null) beamScale.StopNeedleAnimation();

            // 1. Change ModeIndicator from AUTO to MAN and set lamps
            if (potControlDisplayController != null)
            {
                potControlDisplayController.SetModeIndicatorMan();
                potControlDisplayController.SetManualModeLamps();
            }

            // 2. Next button visible for dual-path progression
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(true);
            }

            // 3. Update description UI
            if (welcomePanel != null) welcomePanel.SetActive(true);
            if (titleText != null) titleText.text = "Manual Mode Selection";
            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "SWITCH TO MANUAL OPERATION";
            }
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = ManualModeInstructionText;
            }
            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 07 / 11</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 07 / 11");
                valueDisplay.SetTitle("Manual Mode Selection");
                valueDisplay.DisplayNormalStatus("SWITCH TO MANUAL OPERATION");
                valueDisplay.HideValueDisplay();
            }

            // 4. Voice-over via LocalTTS
            SpeakDescriptionText(ManualModeInstructionText, ManualModeInstructionText);

            Debug.Log("[POT LEAKAGE] Task 07 Manual Mode active: ModeIndicator -> MAN, MAN lamp ON, VO started.");
        }

        public void SetTask06ManualModeUI() => SetTask07ManualModeUI();
        public void SetTask07UI() => SetTask07ManualModeUI();

        public const string AnodeDownInstructionText = "Click the highlighted Anode Down button to lower the anode beam in steps until the voltage reaches safe limits. The voltage should be below 4.2 V.";

        /// <summary>
        /// TASK 08 (or 06 in 8-task mode) — Lower Anode Beam / Anode Down Interaction:
        /// 1. Camera snaps back to TransformPoints/PotControlMachine.
        /// 2. ModeIndicator MAN, MAN lamp glowing #FFEB26, AUTO lamp #FFFFFF not glowing.
        /// 3. Cube.016 highlighted fluorescent green & blinks.
        /// 4. Initial voltage: 4.544 V.
        /// 5. 4 clicks on Cube.016: 4.544 -> 4.444 -> 4.344 -> 4.244 -> 4.144 V.
        /// 6. After 4th click & 4.144 V: stops blinking, shows "The voltage is now at an appropriate level...", plays VO.
        /// 7. Continue/Next button requires manual click to advance.
        /// </summary>
        public void SetTask06AnodeDownUI()
        {
            InitializeReferences();

            DisableWalkieTalkie();
            DisableWalkieTalkie2();

            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<PotLeakage.Camera.PotLeakageCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.MoveToPotControlMachine();
                }
                else
                {
                    var tp = GameObject.Find("CameraSystem/TransformPoints/PotControlMachine") ?? GameObject.Find("PotControlMachine");
                    if (tp != null && SequenceHelperFunctions.instance != null)
                    {
                        SequenceHelperFunctions.instance.SnapCameraToTransform(tp.transform);
                    }
                }
            }

            var beamScale = UnityEngine.Object.FindFirstObjectByType<PotBeamScaleController>();
            if (beamScale != null) beamScale.StopNeedleAnimation();

            if (potControlDisplayController != null)
            {
                potControlDisplayController.SetModeIndicatorMan();
                potControlDisplayController.SetManualModeLamps();
                potControlDisplayController.SetVoltage(4.544f);
            }

            anodeDownClickCount = 0;
            isAnodeDownInteractable = true;

            StartAnodeDownBlink();

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(true);
            }
            SequenceHelperFunctions.OnInterceptTaskCompletion = () => anodeDownClickCount < 4;

            if (welcomePanel != null) welcomePanel.SetActive(true);
            if (titleText != null) titleText.text = "Lower Anode Beam";
            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "VOLTAGE REDUCTION PROCEDURE";
            }
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = AnodeDownInstructionText;
            }

            int totalTasks = (SequenceHandler.instance != null && SequenceHandler.instance.sequenceList != null && SequenceHandler.instance.sequenceList.Count > 0 && SequenceHandler.instance.sequenceList[0].TaskList != null)
                ? SequenceHandler.instance.sequenceList[0].TaskList.Count : 10;
            int taskNum = totalTasks == 8 ? 6 : (totalTasks == 10 ? 8 : 6);

            if (progressText != null)
            {
                progressText.text = $"<color=#00E5FF>TASK {taskNum:D2} / {totalTasks:D2}</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress($"TASK {taskNum:D2} / {totalTasks:D2}");
                valueDisplay.SetTitle("Lower Anode Beam");
                valueDisplay.DisplayNormalStatus("VOLTAGE REDUCTION PROCEDURE");
                valueDisplay.HideValueDisplay();
            }

            SpeakDescriptionText(AnodeDownInstructionText, "TASK_08_LOWER_ANODE_BEAM");
            Debug.Log("[POT LEAKAGE] Lower Anode Beam active: Cube.016 highlighted and blinking, awaiting 4 clicks.");
        }

        public void SetTask08AnodeDownUI() => SetTask06AnodeDownUI();
        public void SetTask07AnodeDownUI() => SetTask06AnodeDownUI();
        public void SetTask06UI() => SetTask06AnodeDownUI();

        public const string VoltageAppropriateDescription = "The voltage is now at an appropriate level. We can proceed with leakage control.";

        /// <summary>
        /// TASK 09 — Voltage Appropriate:
        /// 1. Description: "The voltage is now at an appropriate level. We can proceed with leakage control."
        /// 2. Voice-over speaks description.
        /// 3. Next button visible for dual-path progression (VO finish or Next click advances to Task 10).
        /// </summary>
        public void SetTask09VoltageAppropriateUI()
        {
            InitializeReferences();

            if (welcomePanel != null) welcomePanel.SetActive(true);
            if (titleText != null) titleText.text = "Voltage Level Appropriate";
            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "VOLTAGE AT SAFE LEVEL";
            }
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = VoltageAppropriateDescription;
            }
            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 09 / 11</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 09 / 11");
                valueDisplay.SetTitle("Voltage Level Appropriate");
                valueDisplay.DisplayNormalStatus("VOLTAGE AT SAFE LEVEL");
                valueDisplay.HideValueDisplay();
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(true);
            }

            SpeakDescriptionText(VoltageAppropriateDescription, VoltageAppropriateDescription);

            Debug.Log("[POT LEAKAGE] Task 09 Voltage Appropriate active: VO started.");
        }

        public void SetTask09UI() => SetTask09VoltageAppropriateUI();

        public const string SideBreakingToolVOText = "Excellent choice. You have selected the correct tool for side breaking.";

        /// <summary>
        /// TASK 10 (or 09 in 10-task mode) — Toolbox Selection:
        /// 1. Snap camera to TransformPoints/ToolBoxSelection.
        /// 2. Highlight SIDEREAKING TOOL inside ToolBox fluorescent green & interactable.
        /// 3. Next button VISIBLE for progression.
        /// 4. Trainee clicks tool (or Next): VO plays, advances to final task.
        /// </summary>
        public void OnGlovesClicked()
        {
            if (SequenceHelperFunctions.instance != null)
            {
                SequenceHelperFunctions.instance.HandleGlovesClicked();
            }
        }

        public void OnStopperToolClicked()
        {
            if (SequenceHelperFunctions.instance != null)
            {
                SequenceHelperFunctions.instance.HandleStopperToolClicked();
            }
        }

        /// <summary>
        /// TASK 10 (or 09 in 10-task mode) — Toolbox Selection:
        /// 1. Snap camera to TransformPoints/ToolBoxSelection.
        /// 2. Highlight gloves inside ToolBox fluorescent green & interactable.
        /// 3. Trainee clicks gloves -> switches to Side Breaking Tool highlight.
        /// 4. Trainee clicks Side Breaking Tool -> transitions to Normal Pot stopper application.
        /// </summary>
        public void SetTask10ToolBoxUI()
        {
            InitializeReferences();
            ResetOpeningToolStates();

            if (SequenceHelperFunctions.instance != null)
            {
                SequenceHelperFunctions.instance.BeginToolboxGloveInteraction();
                return;
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);
            if (titleText != null) titleText.text = "Toolbox Selection";
            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "EQUIP SAFETY GLOVES";
            }
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = SequenceHelperFunctions.GlovesInstructionText;
            }
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(true);
            }

            int totalTasks = (SequenceHandler.instance != null && SequenceHandler.instance.sequenceList != null && SequenceHandler.instance.sequenceList.Count > 0 && SequenceHandler.instance.sequenceList[0].TaskList != null)
                ? SequenceHandler.instance.sequenceList[0].TaskList.Count : 10;
            int taskNum = totalTasks == 8 ? 7 : (totalTasks == 10 ? 9 : 10);

            if (progressText != null)
            {
                progressText.text = $"<color=#00E5FF>TASK {taskNum:D2} / {totalTasks:D2}</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress($"TASK {taskNum:D2} / {totalTasks:D2}");
                valueDisplay.SetTitle("Toolbox Selection");
                valueDisplay.DisplayNormalStatus("EQUIP SAFETY GLOVES");
                valueDisplay.HideValueDisplay();
            }

            SpeakDescriptionText(SequenceHelperFunctions.GlovesInstructionText);

            Debug.Log("[POT LEAKAGE] Toolbox Selection active: gloves equip stage.");
        }

        /// <summary>
        /// Safety reset for tools (4), (5), and (6).
        /// Guarantees that neither tool remains active during opening stages or from previous runs.
        /// Does NOT touch SIDEREAKING TOOL (1), (2), or (3).
        /// </summary>
        public void ResetOpeningToolStates()
        {
            if (SequenceHelperFunctions.instance != null)
            {
                SequenceHelperFunctions.instance.ResetOpeningToolStates();
                return;
            }

            if (sideBreakingTool4Target == null)
            {
                var tb = GameObject.Find("ToolBox");
                if (tb != null)
                {
                    var child = tb.transform.Find("SIDEREAKING TOOL (4)");
                    if (child != null) sideBreakingTool4Target = child.gameObject;
                }
            }
            if (sideBreakingTool4Target != null) sideBreakingTool4Target.SetActive(false);

            if (sideBreakingTool5Target == null)
            {
                var tp = GameObject.Find("CameraSystem/TransformPoints") ?? GameObject.Find("TransformPoints");
                if (tp != null)
                {
                    var child = tp.transform.Find("SIDEREAKING TOOL (5)");
                    if (child != null) sideBreakingTool5Target = child.gameObject;
                }
            }
            if (sideBreakingTool5Target != null) sideBreakingTool5Target.SetActive(false);

            if (sideBreakingTool6Target == null)
            {
                var tp = GameObject.Find("CameraSystem/TransformPoints") ?? GameObject.Find("TransformPoints");
                if (tp != null)
                {
                    var child = tp.transform.Find("SIDEREAKING TOOL (6)");
                    if (child != null) sideBreakingTool6Target = child.gameObject;
                }
            }
            if (sideBreakingTool6Target != null) sideBreakingTool6Target.SetActive(false);

            isStopperToolInteractable = false;
            isStopperToolConfirmed = false;
        }

        public void SetTask07ToolBoxUI() => SetTask10ToolBoxUI();
        public void SetTask08ToolBoxUI() => SetTask10ToolBoxUI();
        public void SetTask09ToolBoxUI() => SetTask10ToolBoxUI();
        public void SetTask09ToolConfirmedUI() => SetTask10ToolBoxUI();
        public void SetTask10UI() => SetTask10ToolBoxUI();

        public const string NormalPotTask11Description = SequenceHelperFunctions.StopperInstructionText;

        /// <summary>
        /// TASK 10 (or 08 in 8-task mode / 11 in 11-task mode) — Return to Normal Pot View:
        /// 1. Snap camera to TransformPoints/Normal Pot.
        /// 2. Ready for physical leakage response / stopper application.
        /// </summary>
        public void SetTask11NormalPotUI()
        {
            InitializeReferences();

            StopSideBreakingToolHighlight();

            if (SequenceHelperFunctions.instance != null)
            {
                SequenceHelperFunctions.instance.SnapToNormalPot();
                SequenceHelperFunctions.instance.BeginNormalPotStopperInteraction();
                return;
            }

            Debug.Log("[PotLeakage] Snapping camera to TransformPoints/Normal Pot");
            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<PotLeakage.Camera.PotLeakageCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.MoveToNormalPot();
                }
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);
            if (titleText != null) titleText.text = "Upper Side Shell Response";
            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "APPLY STOPPER TO LEAKAGE POINT";
            }
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = SequenceHelperFunctions.StopperInstructionText;
            }

            int totalTasks = (SequenceHandler.instance != null && SequenceHandler.instance.sequenceList != null && SequenceHandler.instance.sequenceList.Count > 0 && SequenceHandler.instance.sequenceList[0].TaskList != null)
                ? SequenceHandler.instance.sequenceList[0].TaskList.Count : 10;
            int taskNum = totalTasks == 8 ? 8 : (totalTasks == 10 ? 10 : 11);

            if (progressText != null)
            {
                progressText.text = $"<color=#00E5FF>TASK {taskNum:D2} / {totalTasks:D2}</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress($"TASK {taskNum:D2} / {totalTasks:D2}");
                valueDisplay.SetTitle("Upper Side Shell Response");
                valueDisplay.DisplayNormalStatus("APPLY STOPPER TO LEAKAGE POINT");
                valueDisplay.HideValueDisplay();
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(true);
            }

            SpeakDescriptionText(SequenceHelperFunctions.StopperInstructionText, "TASK_10_APPLY_STOPPER");

            Debug.Log("[POT LEAKAGE] Return to Normal Pot active: stopper application stage.");
        }

        public void SetTask08NormalPotUI() => SetTask11NormalPotUI();
        public void SetTask10NormalPotUI() => SetTask11NormalPotUI();
        public void SetTask11UI() => SetTask11NormalPotUI();

        public System.Collections.Generic.List<ToolBlinkSlot> CollectSideBreakingToolSlots()
        {
            InitializeReferences();
            sideBreakingToolSlots.Clear();

            if (sideBreakingToolTarget == null)
            {
                var tb = GameObject.Find("ToolBox");
                if (tb != null)
                {
                    var child = tb.transform.Find("SIDEREAKING TOOL");
                    if (child != null) sideBreakingToolTarget = child.gameObject;
                }
                if (sideBreakingToolTarget == null)
                {
                    var all = Resources.FindObjectsOfTypeAll<GameObject>();
                    foreach (var g in all)
                    {
                        if (g.name == "SIDEREAKING TOOL" && !IsPersistentObject(g))
                        {
                            sideBreakingToolTarget = g;
                            break;
                        }
                    }
                }
            }

            if (sideBreakingToolTarget != null)
            {
                var renderers = sideBreakingToolTarget.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    Material[] mats = r.sharedMaterials;
                    for (int s = 0; s < mats.Length; s++)
                    {
                        var m = mats[s];
                        if (m == null) continue;
                        string matName = m.name;
                        bool isToolSlot = matName.Contains("Stylized_MetalGrid_01_basecolor") ||
                                          matName.Contains("plane_divided_DefaultMaterial_BaseColor") ||
                                          matName.Contains("Highlight");

                        if (isToolSlot)
                        {
                            Material origMat = m;
                            if (origMat != null && origMat.name.Contains("Highlight"))
                            {
#if UNITY_EDITOR
                                if (s == 0 || matName.Contains("plane_divided"))
                                    origMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Model/Materials/plane_divided_DefaultMaterial_BaseColor.mat");
                                else
                                    origMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Model/Materials/Stylized_MetalGrid_01_basecolor.mat");
#endif
                            }
                            sideBreakingToolSlots.Add(new ToolBlinkSlot
                            {
                                renderer = r,
                                slotIndex = s,
                                originalMaterial = origMat,
                                targetName = origMat != null ? origMat.name : matName
                            });
                        }
                    }
                }
            }

            return sideBreakingToolSlots;
        }

        public void StartSideBreakingToolHighlight()
        {
            InitializeReferences();
            if (potHighlightMaterial == null)
            {
#if UNITY_EDITOR
                potHighlightMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/PotLeakage/Materials/M_PotLeakage_Highlight.mat");
#endif
                if (potHighlightMaterial == null)
                {
                    potHighlightMaterial = Resources.Load<Material>("M_PotLeakage_Highlight");
                }
            }

            if (sideBreakingToolBlinkCoroutine != null)
            {
                StopCoroutine(sideBreakingToolBlinkCoroutine);
                sideBreakingToolBlinkCoroutine = null;
            }

            CollectSideBreakingToolSlots();

            Debug.Log("[PotLeakage] Sidebreaking Tool highlight targets found:\nStylized_MetalGrid_01_basecolor\nplane_divided_DefaultMaterial_BaseColor");

            isSideBreakingToolConfirmed = false;
            isSideBreakingToolInteractable = true;
            IsSideBreakingToolBlinking = true;

            // Block task progression until tool is confirmed
            SequenceHelperFunctions.OnInterceptTaskCompletion = () => !isSideBreakingToolConfirmed;

            if (Application.isPlaying)
            {
                sideBreakingToolBlinkCoroutine = StartCoroutine(SideBreakingToolBlinkRoutine());
            }
            else
            {
                ApplySideBreakingToolHighlightState(true);
            }
        }

        private IEnumerator SideBreakingToolBlinkRoutine()
        {
            IsSideBreakingToolBlinking = true;
            bool showHighlight = true;
            while (IsSideBreakingToolBlinking)
            {
                ApplySideBreakingToolHighlightState(showHighlight);
                yield return new WaitForSeconds(blinkInterval);
                showHighlight = !showHighlight;
            }
            sideBreakingToolBlinkCoroutine = null;
        }

        private void ApplySideBreakingToolHighlightState(bool highlight)
        {
            Material hlMat = potHighlightMaterial != null ? potHighlightMaterial : yellowHighlightMaterial;
            for (int i = 0; i < sideBreakingToolSlots.Count; i++)
            {
                var slot = sideBreakingToolSlots[i];
                if (slot.renderer != null)
                {
                    Material[] mats = slot.renderer.sharedMaterials;
                    if (slot.slotIndex >= 0 && slot.slotIndex < mats.Length)
                    {
                        mats[slot.slotIndex] = highlight ? hlMat : slot.originalMaterial;
                        slot.renderer.sharedMaterials = mats;
                    }
                }
            }
        }

        public void StopSideBreakingToolHighlight()
        {
            if (sideBreakingToolBlinkCoroutine != null)
            {
                StopCoroutine(sideBreakingToolBlinkCoroutine);
                sideBreakingToolBlinkCoroutine = null;
            }
            IsSideBreakingToolBlinking = false;
            isSideBreakingToolInteractable = false;

            for (int i = 0; i < sideBreakingToolSlots.Count; i++)
            {
                var slot = sideBreakingToolSlots[i];
                if (slot.renderer != null && slot.originalMaterial != null)
                {
                    Material[] mats = slot.renderer.sharedMaterials;
                    if (slot.slotIndex >= 0 && slot.slotIndex < mats.Length)
                    {
                        mats[slot.slotIndex] = slot.originalMaterial;
                        slot.renderer.sharedMaterials = mats;
                    }
                }
            }
        }

        public void OnSideBreakingToolClicked()
        {
            if (SequenceHelperFunctions.instance != null)
            {
                SequenceHelperFunctions.instance.HandleSideBreakingToolClicked();
                return;
            }

            if (!isSideBreakingToolInteractable) return;
            isSideBreakingToolInteractable = false;
            isSideBreakingToolConfirmed = true;

            PlaySFX(clickAudioClip);
            StopSideBreakingToolHighlight();

            if (sideBreakingToolTarget != null) sideBreakingToolTarget.SetActive(false);
            if (sideBreakingTool4Target != null) sideBreakingTool4Target.SetActive(true);

            Debug.Log("[PotLeakage] Sidebreaking Tool confirmed");
        }

        // =========================================================================
        // [FUTURE QUESTION SLOT 1]
        // Extension point between Task 04 (Pot Controller Notification) and
        // Task 05 (Identify Upper Side Shell Leakage) for future interactive knowledge check.
        // =========================================================================
        public virtual void OnFutureQuestionSlot1Reached()
        {
            // Reserved for future question/quiz UI modal integration
        }

        /// <summary>
        /// TASK 05 — Identify Upper Side Shell Leakage: Camera snaps to Normal Pot Operation,
        /// upper side shell highlighted on tripo_node_cf158416, narrow continuous molten metal stream begins.
        /// </summary>
        public void SetLegacyTask05UpperSideShellUI()
        {
            InitializeReferences();

            StopPotBlink(false);
            SwapPotHighlightMaterial();
            ResetWalkieCommunicationSystem();
            if (walkieTalkieController != null)
            {
                walkieTalkieController.ResetInteraction();
            }
            if (toolPresentationController != null)
            {
                toolPresentationController.ClearAllToolHighlights();
            }
            if (potVoltageWallPanel != null)
            {
                potVoltageWallPanel.SetActive(true);
            }
            if (cbtLookTargetPanel != null)
            {
                cbtLookTargetPanel.SetActive(true);
            }

            if (magmaDroolController != null)
            {
                magmaDroolController.PlayMoltenMetalOverflow();
                magmaDroolController.SetControlledLeakage(false, 0f);
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "IDENTIFY UPPER SIDE SHELL LEAKAGE";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "SIDE SHELL LEAKAGE — UPPER LOCATION";
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = "Identify Upper Side Shell Leakage.\n\nInspect the upper side shell area of Pot 69. A continuous narrow stream of molten metal is escaping from the upper shell.\n\nImmediate physical containment actions are required.";
                SpeakDescriptionText(descriptionText.text, "TASK_05");
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 05 / 07</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 05 / 07");
                valueDisplay.SetTitle("IDENTIFY UPPER SIDE SHELL LEAKAGE");
                valueDisplay.DisplayNormalStatus("SIDE SHELL LEAKAGE — UPPER LOCATION");
                valueDisplay.HideValueDisplay();
            }
        }

        /// <summary>
        /// TASK 06 — Upper Side Shell Response: Documented physical steps per WI-POT/OPR/019:
        /// 1. Side breaking & remove anode
        /// 2. Add crust bath in small pieces + fused alumina (NO cover bath)
        /// Molten stream gradually reduces.
        /// </summary>
        public void SetLegacyTask06UpperSideShellResponseUI()
        {
            InitializeReferences();

            StopPotBlink(false);
            RestorePotOriginalMaterial();
            if (walkieTalkieController != null)
            {
                walkieTalkieController.ResetInteraction();
            }
            if (toolPresentationController != null)
            {
                toolPresentationController.ClearAllToolHighlights();
            }
            if (potVoltageWallPanel != null)
            {
                potVoltageWallPanel.SetActive(true);
                UpdateWallPanelContent();
            }
            if (cbtLookTargetPanel != null)
            {
                cbtLookTargetPanel.SetActive(true);
            }

            // Stream gradually reduces / narrows
            if (magmaDroolController != null)
            {
                magmaDroolController.SetControlledLeakage(true, 1.5f);
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "UPPER SIDE SHELL RESPONSE";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "SIDE BREAKING & BATH / ALUMINA ADDITION";
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = "Upper Side Shell Response Procedure (WI-POT/OPR/019):\n\n1. Carry out side breaking and remove the affected anode.\n2. Add crust bath in small pieces along with fused alumina to choke and seal the leakage path.\n\nObserve gradual reduction in the metal flow.";
                SpeakDescriptionText(descriptionText.text, "TASK_06");
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 06 / 07</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 06 / 07");
                valueDisplay.SetTitle("UPPER SIDE SHELL RESPONSE");
                valueDisplay.DisplayNormalStatus("SIDE BREAKING & BATH / ALUMINA ADDITION");
                valueDisplay.HideValueDisplay();
            }
        }

        /// <summary>
        /// TASK 07 — Partial Leakage Arrest / Cut-Out Planning:
        /// Status: LEAKAGE PARTIALLY ARRESTED -> PLAN FOR POT CUT-OUT
        /// Small controlled residual leakage.
        /// </summary>
        public void SetLegacyTask07PartialLeakageUI()
        {
            InitializeReferences();

            StopPotBlink(false);
            RestorePotOriginalMaterial();
            if (walkieTalkieController != null)
            {
                walkieTalkieController.ResetInteraction();
            }
            if (toolPresentationController != null)
            {
                toolPresentationController.ClearAllToolHighlights();
            }
            if (potVoltageWallPanel != null)
            {
                potVoltageWallPanel.SetActive(true);
            }
            if (cbtLookTargetPanel != null)
            {
                cbtLookTargetPanel.SetActive(true);
            }

            // Residual controlled stream remains active
            if (magmaDroolController != null)
            {
                magmaDroolController.SetControlledLeakage(true, 0f);
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "PARTIAL LEAKAGE ARREST / CUT-OUT PLANNING";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "LEAKAGE PARTIALLY ARRESTED — PLAN FOR POT CUT-OUT";
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = "Partial Leakage Arrest & Cut-Out Planning.\n\nStatus: LEAKAGE PARTIALLY ARRESTED.\n\nResidual flow is controlled and minimal. Proceed with planning for pot cut-out according to the approved potline isolation procedure.";
                SpeakDescriptionText(descriptionText.text, "TASK_07");
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 07 / 07</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 07 / 07");
                valueDisplay.SetTitle("PARTIAL LEAKAGE ARREST / CUT-OUT PLANNING");
                valueDisplay.DisplayNormalStatus("LEAKAGE PARTIALLY ARRESTED — PLAN FOR POT CUT-OUT");
                valueDisplay.HideValueDisplay();
            }
        }

        // =========================================================================
        // [FUTURE QUESTION SLOT 2]
        // Extension point after Task 07 (Partial Leakage Arrest / Cut-Out Planning)
        // for future evaluation / quiz UI modal integration.
        // =========================================================================
        public virtual void OnFutureQuestionSlot2Reached()
        {
            // Reserved for future question/quiz UI modal integration
        }

        /// <summary>
        /// TASK 08 — Establish Access & Prepare Work Area: Lower level access, tools, tanker.
        /// Camera: TransformPoints/CBT_LookTarget
        /// Magma Drool: ACTIVE. Walkie-Talkie: Inactive. Pot Voltage Wall Panel: Inactive. CBT Wall Panel: ACTIVE.
        /// </summary>
        public void SetTask08UI() => SetLegacyTask08EstablishAccessUI();
        public void SetLegacyTask08EstablishAccessUI()
        {
            InitializeReferences();

            StopPotBlink(false);
            RestorePotOriginalMaterial();
            if (magmaDroolController != null)
            {
                magmaDroolController.StartMagmaDrool();
            }
            if (walkieTalkieController != null)
            {
                walkieTalkieController.ResetInteraction();
            }
            if (toolPresentationController != null)
            {
                toolPresentationController.ClearAllToolHighlights();
            }
            if (potVoltageWallPanel != null)
            {
                potVoltageWallPanel.SetActive(true);
            }
            if (cbtLookTargetPanel != null)
            {
                cbtLookTargetPanel.SetActive(true);
                UpdateCBTPanelContent();
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "ESTABLISH ACCESS & PREPARE THE WORK AREA";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "PREPARATION BEFORE TAP-OUT";
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = "Team goes to the lower level to prevent metal pool formation.\n\nTake at least 4 channel-making tools.\n\nRemove the -3 m mesh to provide access.\n\nArrange the bath/alumina tanker near the tap-out.";
                SpeakDescriptionText(descriptionText.text, "TASK_08");
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 08 / 10</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 08 / 10");
                valueDisplay.SetTitle("ESTABLISH ACCESS & PREPARE THE WORK AREA");
                valueDisplay.DisplayNormalStatus("PREPARATION BEFORE TAP-OUT");
                valueDisplay.HideValueDisplay();
            }
        }

        /// <summary>
        /// Explicit event trigger to start the pinned tool presentation sequence on tool board.
        /// Snaps camera to TransformPoints/Tools and highlights tools one by one.
        /// </summary>
        public void StartToolPresentation()
        {
            InitializeReferences();
            if (toolPresentationController != null)
            {
                toolPresentationController.StartPresentation();
            }
        }

        /// <summary>
        /// TASK 09 — Low Leakage Process (Internal 4-Stage Presentation inside Task 09):
        /// Snaps camera to TransformPoints/Ideal Pot Voltage,
        /// and starts the 4-step Low Leakage presentation (Container -> Shovel -> Crowbar -> Controlled Leakage).
        /// </summary>
        public void SetTaskToolsUI()
        {
            InitializeReferences();

            StopPotBlink(false);
            RestorePotOriginalMaterial();
            if (magmaDroolController != null)
            {
                magmaDroolController.StartMagmaDrool();
            }
            if (walkieTalkieController != null)
            {
                walkieTalkieController.ResetInteraction();
            }
            if (cbtLookTargetPanel != null)
            {
                cbtLookTargetPanel.SetActive(true);
            }
            if (potVoltageWallPanel != null)
            {
                potVoltageWallPanel.SetActive(true);
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 09 / 10</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 09 / 10");
                valueDisplay.SetTitle("ESTABLISH ACCESS & PREPARE THE WORK AREA");
                valueDisplay.DisplayNormalStatus("CHANNEL-MAKING TOOLS");
                valueDisplay.HideValueDisplay();
            }

            if (toolPresentationController != null)
            {
                toolPresentationController.ClearAllToolHighlights();
            }

            if (lowLeakageController != null)
            {
                lowLeakageController.StartPresentation();
            }
        }

        /// <summary>
        /// Backward-compatible or direct call for Task 09 (Channel-Making Tools).
        /// </summary>
        public void SetLegacyTask09ToolsUI()
        {
            SetTaskToolsUI();
        }

        /// <summary>
        /// TASK 10 — Return to Ideal Pot Voltage: Replay Molten Metal Overflow VFX and gradual floor spill.
        /// Camera: TransformPoints/Ideal Pot Voltage
        /// Pot machine: Retain highlight, stop blink. Molten Metal VFX: RESTART + Floor Spill.
        /// </summary>
        public void SetLegacyTask10IdealPotVoltageUI()
        {
            InitializeReferences();

            StopPotBlink(false);
            RestorePotOriginalMaterial();
            if (magmaDroolController != null)
            {
                magmaDroolController.StopMagmaDrool(true);
                magmaDroolController.PlayMoltenMetalOverflow();
                magmaDroolController.StartFloorSpill();
            }
            if (walkieTalkieController != null)
            {
                walkieTalkieController.ResetInteraction();
            }
            if (toolPresentationController != null)
            {
                toolPresentationController.ClearAllToolHighlights();
            }
            if (potVoltageWallPanel != null)
            {
                potVoltageWallPanel.SetActive(true);
            }
            if (cbtLookTargetPanel != null)
            {
                cbtLookTargetPanel.SetActive(true);
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "IDEAL POT VOLTAGE";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "LEAKAGE CONDITION — METAL SPILL OBSERVED";
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = "Metal leakage has progressed beyond the affected area and is forming a pool on the floor. The flow must be controlled and directed through an established channel to prevent uncontrolled metal accumulation.";
                SpeakDescriptionText(descriptionText.text, "TASK_10");
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 10 / 10</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 10 / 10");
                valueDisplay.SetTitle("IDEAL POT VOLTAGE");
                valueDisplay.DisplayNormalStatus("LEAKAGE CONDITION — METAL SPILL OBSERVED");
                valueDisplay.HideValueDisplay();
            }
        }

        /// <summary>
        /// Starts the 4-stage low-leakage explanation sequence at CameraSystem/TransformPoints/Ideal Pot Voltage:
        /// Container -> Shovel -> Crowbar -> Controlled Leakage Stream.
        /// </summary>
        public void StartLowLeakagePresentation()
        {
            InitializeReferences();
            if (lowLeakageController != null)
            {
                lowLeakageController.StartPresentation();
            }
        }

        public void SetTaskLowLeakageUI()
        {
            StartLowLeakagePresentation();
        }
    }
}
