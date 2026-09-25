using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PotLeakage.Configuration;
using PotLeakage.VFX;
using PotLeakage.Interaction;

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

        // Backward compatibility aliases
        public AudioClip nextButtonSFX { get => clickAudioClip; set => clickAudioClip = value; }
        public AudioClip clickAudio { get => clickAudioClip; set => clickAudioClip = value; }

        private void Awake()
        {
            InitializeReferences();
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

            if (magmaDroolController == null)
            {
                var vfxGo = GameObject.Find("Machine/MoltenMetalOverflowVFX") ?? GameObject.Find("MoltenMetalOverflowVFX");
                if (vfxGo != null)
                {
                    magmaDroolController = vfxGo.GetComponent<MoltenAluminiumVFXController>();
                }
                if (magmaDroolController == null)
                {
                    magmaDroolController = Object.FindAnyObjectByType<MoltenAluminiumVFXController>();
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
        }

        private void OnDisable()
        {
            StopPotBlink(false);
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
                mgr.SpeakText(clean, key);
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

        public void SetTask01Default() => SetTask01UI();
        public void SetTask02Default() => SetTask02UI();
        public void SetTask03Default() => SetTask03UI();
        public void SetTask04Default() => SetTask04UI();
        public void SetTask05Default() => SetTask05UI();
        public void SetTask06Default() => SetTask06UI();
        public void SetTask07Default() => SetTask07UI();
        public void SetTask08Default() => SetTask08UI();
        public void SetTask09Default() => SetTask09UI();

        // Backward compatibility aliases
        public void SetTask02Part1Default() => SetTask02UI();
        public void SetTask02Part2Default() => SetTask03UI();
        public void SetTask02Part1UI() => SetTask02UI();
        public void SetTask02Part2UI() => SetTask03UI();

        /// <summary>
        /// Unified dispatcher to trigger UI updates for any task index (0 to 9).
        /// Cleanly integrates with SequenceHandler.currentTask.
        /// </summary>
        public void SetTaskByIndex(int taskIndex)
        {
            switch (taskIndex)
            {
                case 0: SetTask01UI(); break;
                case 1: SetTask02UI(); break;
                case 2: SetTask03UI(); break;
                case 3: SetTask04UI(); break;
                case 4: SetTask05UI(); break;
                case 5: SetTask06UI(); break;
                case 6: SetTask07UI(); break;
                case 7: SetTask08UI(); break;
                case 8: SetTaskToolsUI(); break;
                case 9: SetTask10UI(); break;
                default:
                    Debug.LogWarning($"[PotLeakageUIController] Invalid task index: {taskIndex}");
                    break;
            }
        }

        /// <summary>
        /// TASK 01 — Welcome: Introduces pot leakage training module.
        /// Camera: TransformPoints/Welcome
        /// Highlights: NONE. Blinking: OFF. Magma Drool: OFF. Molten Metal VFX: OFF.
        /// </summary>
        public void SetTask01UI()
        {
            InitializeReferences();

            StopPotBlink(false);
            RestorePotOriginalMaterial();
            if (magmaDroolController != null)
            {
                magmaDroolController.StopMagmaDrool(true);
                magmaDroolController.StopMoltenMetalOverflow();
                magmaDroolController.ResetFloorSpill();
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

            if (welcomePanel != null) welcomePanel.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "POT LEAKAGE";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
                statusText.text = "";
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = "Welcome to the Pot Leakage training module.\n\nThis module introduces normal pot operation and the important parameters used to identify abnormal pot conditions.";
                SpeakDescriptionText(descriptionText.text, "TASK_01");
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 01 / 10</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 01 / 10");
                valueDisplay.SetTitle("POT LEAKAGE");
                valueDisplay.HideStatus();
                valueDisplay.HideValueDisplay();
            }
        }

        /// <summary>
        /// TASK 02 — Normal Pot Operation: Introduces normal operating condition.
        /// Camera: TransformPoints/Normal Pot Operation
        /// Pot machine: Material swap to M_PotLeakage_Highlight.mat + BLINK. Magma Drool: OFF. Molten Metal VFX: OFF.
        /// </summary>
        public void SetTask02UI()
        {
            InitializeReferences();

            SwapPotHighlightMaterial();
            StartPotBlink();
            if (magmaDroolController != null)
            {
                magmaDroolController.StopMagmaDrool(true);
                magmaDroolController.StopMoltenMetalOverflow();
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
                titleText.text = "NORMAL POT OPERATION";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "NORMAL OPERATING CONDITION";
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = "This section introduces the normal operating condition of the pot before discussing abnormal conditions such as pot leakage.\n\nDuring normal operation, key electrical and thermal parameters are monitored to ensure process stability.";
                SpeakDescriptionText(descriptionText.text, "TASK_02");
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 02 / 10</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 02 / 10");
                valueDisplay.SetTitle("NORMAL POT OPERATION");
                valueDisplay.DisplayNormalStatus("NORMAL OPERATING CONDITION");
                valueDisplay.HideValueDisplay();
            }
        }

        /// <summary>
        /// TASK 03 — Normal Operating Parameters: Key electrical and thermal parameters.
        /// Camera: TransformPoints/Normal Pot Operation
        /// Pot machine: Retain highlight + BLINK. Magma Drool: OFF.
        /// </summary>
        /// <summary>
        /// TASK 03 — Ideal Pot Voltage: Normal operating voltage range.
        /// Camera: TransformPoints/Ideal Pot Voltage
        /// Pot machine: Retain highlight, stop blink. Molten Metal VFX: PLAY/RESTART.
        /// </summary>
        public void SetTask03UI()
        {
            InitializeReferences();

            StopPotBlink(true); // Keep highlight, stop blinking
            if (magmaDroolController != null)
            {
                magmaDroolController.StopMagmaDrool(true);
                magmaDroolController.PlayMoltenMetalOverflow();
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
                statusText.text = "NORMAL OPERATING RANGE";
            }

            string voltageStr = GetConfiguredVoltageString();

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = $"Pot voltage is continuously monitored during operation.\n\nThe pot should remain within its approved operating range.\n\nNormal Production Pot Voltage:\n<b>{voltageStr}</b>\n\nStable operating voltage is an important indicator of normal pot operation.";
                SpeakDescriptionText(descriptionText.text, "TASK_03");
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 03 / 10</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 03 / 10");
                valueDisplay.SetTitle("IDEAL POT VOLTAGE");
                valueDisplay.DisplayNormalStatus("NORMAL OPERATING RANGE");
                valueDisplay.HideValueDisplay();
            }
        }

        /// <summary>
        /// TASK 04 — Ideal CBT Temperature / Leakage Observed: Transition to abnormal condition.
        /// Camera: TransformPoints/Ideal CBT Temperature
        /// Magma Drool: START. Pot machine: Restore original material.
        /// Walkie-Talkie starts blinking.
        /// </summary>
        public void SetTask04UI()
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
                walkieTalkieController.StartWalkieTalkieBlink();
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
                titleText.text = "IDEAL CBT TEMPERATURE";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "LEAKAGE CONDITION OBSERVED";
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = "Collector bar temperature is monitored during operation.\n\nA significant rise in CBT temperature or an abnormal cathode condition can indicate possible leakage.\n\nWhen leakage is observed, immediately inform the Shift Superintendent and Technical In-charge.";
                SpeakDescriptionText(descriptionText.text, "TASK_04");
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 04 / 10</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 04 / 10");
                valueDisplay.SetTitle("IDEAL CBT TEMPERATURE");
                valueDisplay.DisplayNormalStatus("LEAKAGE CONDITION OBSERVED");
                valueDisplay.HideValueDisplay();
            }
        }

        /// <summary>
        /// TASK 05 — Emergency Communication: Two-stage walkie-talkie communication (Shift then Technical).
        /// Camera: TransformPoints/Ideal CBT Temperature
        /// Magma Drool: ACTIVE. Walkie-Talkie & Personnel Images: ACTIVE.
        /// </summary>
        public void SetTask05UI()
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
                walkieTalkieController.ActivateInteraction();
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
                titleText.text = "EMERGENCY COMMUNICATION";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "IMMEDIATE ACTION REQUIRED";
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = "Immediately inform the Shift Superintendent and Technical In-charge about the pot leakage.\n\n(Initial communication stage: subsequent plant emergency protocol involves notifications to Pot Control Room, Senior Operation In-charge, Fire, First Aid, and Combat Team.)";
                SpeakDescriptionText(descriptionText.text, "TASK_05");
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 05 / 10</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 05 / 10");
                valueDisplay.SetTitle("EMERGENCY COMMUNICATION");
                valueDisplay.DisplayNormalStatus("IMMEDIATE ACTION REQUIRED");
                valueDisplay.HideValueDisplay();
            }
        }

        /// <summary>
        /// TASK 06 — Pot Voltage / Duct-End Voltage Control: Wall panel active, 2-3 pulse anode lowering.
        /// Camera: TransformPoints/PotMachine_LookTarget
        /// Magma Drool: ACTIVE. Walkie-Talkie: Inactive. Wall Panel: ACTIVE.
        /// </summary>
        public void SetTask06UI()
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
                UpdateWallPanelContent();
            }
            if (cbtLookTargetPanel != null)
            {
                cbtLookTargetPanel.SetActive(true);
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "POT VOLTAGE";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "DUCT-END VOLTAGE CONTROL";
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = "During leakage response, the Technical In-charge continuously monitors the duct-end pot voltage.\n\nMaintain the voltage at ≤ 4.5 V by lowering the anode beam in 2–3 pulse steps as required.";
                SpeakDescriptionText(descriptionText.text, "TASK_06");
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 06 / 10</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 06 / 10");
                valueDisplay.SetTitle("POT VOLTAGE");
                valueDisplay.DisplayNormalStatus("DUCT-END VOLTAGE CONTROL");
                valueDisplay.HideValueDisplay();
            }
        }

        /// <summary>
        /// TASK 07 — Leakage-Specific Response: Comprehensive collector bar leakage procedure per WI-POT/OPR/019.
        /// Camera: TransformPoints/PotMachine_LookTarget
        /// Magma Drool: ACTIVE. Walkie-Talkie: Inactive. Wall Panel: Inactive.
        /// </summary>
        public void SetTask07UI()
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
            }

            if (welcomePanel != null) welcomePanel.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "LEAKAGE-SPECIFIC RESPONSE";
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "COLLECTOR BAR LEAKAGE";
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = "Reduce amperage to 300 kA initially.\n\nFor low leakage intensity, use side breaking / fused alumina pushing and ensure fused alumina reaches the bottom.\n\nFor high leakage intensity, lower the anode to the bottom.\n\nIf leakage continues, proceed with the documented cover-bath, crust-breaking and pot cut-out procedure.\n\nIf the leakage is not arrested, stop the pot according to the approved procedure.";
                SpeakDescriptionText(descriptionText.text, "TASK_07");
            }

            if (progressText != null)
            {
                progressText.text = "<color=#00E5FF>TASK 07 / 10</color>";
            }

            if (valueDisplay != null)
            {
                valueDisplay.SetProgress("TASK 07 / 10");
                valueDisplay.SetTitle("LEAKAGE-SPECIFIC RESPONSE");
                valueDisplay.DisplayNormalStatus("COLLECTOR BAR LEAKAGE");
                valueDisplay.HideValueDisplay();
            }
        }

        /// <summary>
        /// TASK 08 — Establish Access & Prepare Work Area: Lower level access, tools, tanker.
        /// Camera: TransformPoints/CBT_LookTarget
        /// Magma Drool: ACTIVE. Walkie-Talkie: Inactive. Pot Voltage Wall Panel: Inactive. CBT Wall Panel: ACTIVE.
        /// </summary>
        public void SetTask08UI()
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
        public void SetTask09UI()
        {
            SetTaskToolsUI();
        }

        /// <summary>
        /// TASK 10 — Return to Ideal Pot Voltage: Replay Molten Metal Overflow VFX and gradual floor spill.
        /// Camera: TransformPoints/Ideal Pot Voltage
        /// Pot machine: Retain highlight, stop blink. Molten Metal VFX: RESTART + Floor Spill.
        /// </summary>
        public void SetTask10UI()
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
