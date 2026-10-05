using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Switch;
using TruckTyreReplacement.Core;
using PotLeakage.UI;
using PotLeakage.Camera;
using PotLeakage.Interaction;

// ─────────────────────────────────────────────────────────────────────────────
// ObjectMovementMapping
// Maps a string key to a source GameObject + destination Transform.
// Used by MoveObjectToDestination(key) for Inspector-configurable object movement.
// ─────────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class ObjectMovementMapping
{
    [Tooltip("Unique key used to call MoveObjectToDestination(key) from a UnityEvent.")]
    public string key;
    [Tooltip("The object to move.")]
    public GameObject sourceObject;
    [Tooltip("The destination transform.")]
    public Transform destination;
}

// ─────────────────────────────────────────────────────────────────────────────
// GrabSnapMapping
// ─────────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class GrabSnapMapping
{
    public string Name;
    public GameObject GrabbableObject;
    public GameObject GhostTargetObject;

    [HideInInspector] public Vector3 InitialPosition;
    [HideInInspector] public Quaternion InitialRotation;
    [HideInInspector] public bool IsInsideTargetCollider;
}

// ─────────────────────────────────────────────────────────────────────────────
// SequenceAnimation
// ─────────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class SequenceAnimation
{
    public string animationName = "Describe Animation Here";
    public Animator targetAnimator;
    public string triggerName;
    public float animationDuration = 2.0f;
}

// ─────────────────────────────────────────────────────────────────────────────
// SequenceHelperFunctions
// Generic reusable operations for the Sequence framework.
// ALL methods are Inspector-callable via UnityEvent.
// No module-specific logic (no tyre, wheel, AIRBUM, pipe references).
// ─────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(SequenceHandler))]
public class SequenceHelperFunctions : MonoBehaviour
{
    public static SequenceHelperFunctions instance;
    [SerializeField] SequenceHandler handler;

    private static bool IsPersistentObject(UnityEngine.Object obj)
    {
#if UNITY_EDITOR
        return UnityEditor.EditorUtility.IsPersistent(obj);
#else
        return false;
#endif
    }

    // ── AUDIO ─────────────────────────────────────────────────────────────────
    [Header("Audio Sources")]
    [SerializeField] AudioSource BG_Audio;
    [SerializeField] AudioSource Voice_Audio;
    [SerializeField] private Coroutine currentAudioCoroutine;

    // ── ANIMATION ─────────────────────────────────────────────────────────────
    [Header("Animations Configurations")]
    public List<SequenceAnimation> sequenceAnimations = new List<SequenceAnimation>();

    // ── FADE ──────────────────────────────────────────────────────────────────
    [Space(4)]
    [Header("Fade Controller")]
    [SerializeField] GameObject _fadeobj;
    [SerializeField] Material fadeMaterial;
    [SerializeField] float fadeDuration = 1f;

    // ── PLAYER ────────────────────────────────────────────────────────────────
    [Space(4)]
    [Header("Player (XR Origin)")]
    [Tooltip("Assign the XR Origin (VR) GameObject here for TeleportPlayer().")]
    [SerializeField] GameObject player;
    [SerializeField] private Transform _nextPlayerTransform;

    // ── UI ────────────────────────────────────────────────────────────────────
    [Space(4)]
    [Header("Canvas")]
    [SerializeField] GameObject UI_Canvas;
    [SerializeField] TextMeshProUGUI TitleText;
    [SerializeField] private string titleString;
    [SerializeField] TextMeshProUGUI DescriptionText;
    [SerializeField] private string desString;
    [SerializeField] private Transform _nextCanvasTransform;

    public Button nextButton, previousButton;

    // ── HIGHLIGHT ─────────────────────────────────────────────────────────────
    [SerializeField] Material transparentMaterial;
    public Material HighlightMaterial { get => transparentMaterial; set => transparentMaterial = value; }
    public bool _isTransparent;
    private Coroutine transparencyCoroutine;
    private Dictionary<Renderer, Material[]> originalMaterialsDict = new Dictionary<Renderer, Material[]>();

    private List<GameObject> activeHighlightedObjects = new List<GameObject>();
    private Coroutine blinkCoroutine;
    private bool highlightBlinkState = true;

    // ── OBJECT MOVEMENT ───────────────────────────────────────────────────────
    [Space(4)]
    [Header("Object Movement Mappings")]
    [Tooltip("Pre-configure named source->destination pairs. Call MoveObjectToDestination(key) from UnityEvent.")]
    public List<ObjectMovementMapping> movementMappings = new List<ObjectMovementMapping>();

    // ── CONTROLLER ────────────────────────────────────────────────────────────
    [Header("Controller")]
    public GameObject currentTarget;

    // ── PPE ───────────────────────────────────────────────────────────────────
    [Header("PPE Management")]
    private Dictionary<GameObject, Vector3> ppeInitialPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Quaternion> ppeInitialRotations = new Dictionary<GameObject, Quaternion>();
    private bool isInsidePPEZone = false;
    public GameObject ppeObjectToEnable;

    // ── MULTI GRAB ────────────────────────────────────────────────────────────
    [Header("Multi-Object Grab Logic")]
    public List<GrabSnapMapping> multiGrabMappings = new List<GrabSnapMapping>();

    // ─────────────────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        instance = this;
        if (activeHighlightedObjects == null) activeHighlightedObjects = new List<GameObject>();
        if (originalMaterialsDict == null) originalMaterialsDict = new Dictionary<Renderer, Material[]>();
        activeHighlightedObjects.Clear();
        originalMaterialsDict.Clear();
    }

    private void OnEnable()
    {
        instance = this;
        if (activeHighlightedObjects == null) activeHighlightedObjects = new List<GameObject>();
        if (originalMaterialsDict == null) originalMaterialsDict = new Dictionary<Renderer, Material[]>();
        activeHighlightedObjects.Clear();
        originalMaterialsDict.Clear();
    }

    private void Start()
    {
        ResolveHandler();
        ResetOpeningToolStates();
        if (BG_Audio != null) BG_Audio.Play();
        RegisterAllMultiGrabObjects();

        if (transparentMaterial == null)
        {
            transparentMaterial = Resources.Load<Material>("M_Highlight_FluorescentGreen");
#if UNITY_EDITOR
            if (transparentMaterial == null)
                transparentMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckTyreReplacement/Materials/M_Highlight_FluorescentGreen.mat");
#endif
        }
    }

    private void ResolveHandler()
    {
        if (handler != null && handler.sequenceList != null && handler.sequenceList.Count > 0)
        {
            return;
        }

        if (SequenceHandler.instance != null && SequenceHandler.instance.sequenceList != null && SequenceHandler.instance.sequenceList.Count > 0)
        {
            handler = SequenceHandler.instance;
            return;
        }

        var allHandlers = UnityEngine.Object.FindObjectsByType<SequenceHandler>(FindObjectsSortMode.None);
        foreach (var h in allHandlers)
        {
            if (h != null && h.sequenceList != null && h.sequenceList.Count > 0)
            {
                handler = h;
                return;
            }
        }

        if (handler == null)
        {
            handler = this.GetComponent<SequenceHandler>();
        }
    }

    private void OnDisable()
    {
        if (handler != null) handler.StopAllCoroutines();
        this.StopAllCoroutines();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SEQUENCE — Task Completion
    // ─────────────────────────────────────────────────────────────────────────

    private int lastCompletedFrame = -1;

    public void ResetDebounce() => lastCompletedFrame = -1;

    /// <summary>
    /// Generic decoupled interceptor delegate for active interactive presentations.
    /// When assigned and returning true, standard task completion is deferred until the presentation finishes.
    /// </summary>
    public static System.Func<bool> OnInterceptTaskCompletion;

    /// <summary>Complete the current task. Wire from OnCompleted, OnTargetReached, OnGrabbed, etc.</summary>
    public void CompleteCurrentTask()
    {
        if (Application.isPlaying && Time.frameCount == lastCompletedFrame && Time.frameCount > 0)
        {
            Debug.LogWarning($"[SequenceHelperFunctions] Debounce: CompleteCurrentTask already called in frame {Time.frameCount}. Ignoring duplicate.");
            return;
        }
        lastCompletedFrame = Time.frameCount;

        ResolveHandler();
        if (handler != null)
        {
            if (OnInterceptTaskCompletion != null && OnInterceptTaskCompletion.Invoke())
            {
                return;
            }

            var tpc = UnityEngine.Object.FindAnyObjectByType<PotLeakage.Interaction.ToolPresentationController>();
            if (tpc != null && tpc.IsPresentationActive)
            {
                bool advanced = tpc.AdvancePresentation();
                if (advanced)
                {
                    return;
                }
            }

            Debug.Log("[SequenceHelperFunctions] CompleteCurrentTask() called.");
            handler.TaskCompleted();
        }
        else
        {
            Debug.LogError("[SequenceHelperFunctions] CompleteCurrentTask: SequenceHandler not found.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PLAYER — Teleportation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Teleport the XR Origin (player) to a destination transform.</summary>
    public void TeleportPlayer(Transform destination)
    {
        if (player == null)
        {
            var go = GameObject.Find("XR Origin (VR)");
            if (go == null) go = GameObject.Find("XR Origin");
            if (go != null) player = go;
        }
        if (player != null && destination != null)
        {
            player.transform.position = destination.position;
            player.transform.rotation = destination.rotation;
            Debug.Log($"[SequenceHelperFunctions] TeleportPlayer to '{destination.name}'");
        }
        else
        {
            Debug.LogWarning("[SequenceHelperFunctions] TeleportPlayer: player or destination is null.");
        }
    }

    // Legacy alias
    public void PlayerPosChange(Transform t)
    {
        if (player != null) { player.transform.localPosition = t.localPosition; player.transform.localRotation = t.localRotation; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CAMERA — Instant Snapping (Zero Panning, Zero Interpolation)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Instantly snaps the camera to the target transform's position and rotation with zero delay or interpolation.
    /// Prefers the configured Pot Leakage camera or camera controller, falling back to Camera.main.
    /// </summary>
    public void SnapCameraToTransform(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("[SequenceHelperFunctions] SnapCameraToTransform called with null target.");
            return;
        }

        Transform camTransform = null;

        var camGo = GameObject.Find("CameraSystem/Main Camera") ?? GameObject.Find("Main Camera");
        if (camGo != null)
        {
            camTransform = camGo.transform;
            var camController = camGo.GetComponent<PotLeakage.Camera.PotLeakageCameraController>();
            if (camController != null)
            {
                camController.SnapCameraToTransform(target);
                return;
            }
        }
        else if (UnityEngine.Camera.main != null)
        {
            camTransform = UnityEngine.Camera.main.transform;
        }

        if (camTransform != null)
        {
            camTransform.position = target.position;
            camTransform.rotation = target.rotation;
        }
        else
        {
            Debug.LogWarning("[SequenceHelperFunctions] SnapCameraToTransform: Active camera not found.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POT CONTROL MACHINE STAGES & SEQUENCE HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private PotLeakageUIController potLeakageUI;
    private PotControlDisplayController potControlDisplay;
    private PotLeakageCameraController potLeakageCam;

    public int anodeDownClickCount = 0;
    public bool isAnodeDownInteractable = false;
    private float lastAnodeDownClickTime = -1f;
    private int lastAnodeDownClickFrame = -1;

    public PotLeakageUIController ResolvePotLeakageUI()
    {
        if (potLeakageUI == null)
        {
            potLeakageUI = PotLeakageUIController.Instance;
            if (potLeakageUI == null)
            {
                potLeakageUI = UnityEngine.Object.FindAnyObjectByType<PotLeakageUIController>(FindObjectsInactive.Include);
            }
        }
        return potLeakageUI;
    }

    public PotControlDisplayController ResolvePotControlDisplay()
    {
        if (potControlDisplay == null)
        {
            potControlDisplay = UnityEngine.Object.FindAnyObjectByType<PotControlDisplayController>(FindObjectsInactive.Include);
        }
        return potControlDisplay;
    }

    public PotLeakageCameraController ResolveCameraController()
    {
        if (potLeakageCam == null)
        {
            var cam = UnityEngine.Camera.main;
            potLeakageCam = cam != null ? cam.GetComponent<PotLeakageCameraController>() : null;
            if (potLeakageCam == null)
            {
                potLeakageCam = UnityEngine.Object.FindAnyObjectByType<PotLeakageCameraController>(FindObjectsInactive.Include);
            }
        }
        return potLeakageCam;
    }

    public void SnapToPotControlMachine()
    {
        var camCtrl = ResolveCameraController();
        if (camCtrl != null)
        {
            camCtrl.MoveToPotControlMachine();
        }
        else
        {
            var tp = GameObject.Find("CameraSystem/TransformPoints/PotControlMachine") 
                ?? GameObject.Find("TransformPoints/PotControlMachine") 
                ?? GameObject.Find("PotControlMachine");
            if (tp != null) SnapCameraToTransform(tp.transform);
        }
    }

    public void SnapToPotInspection()
    {
        var camCtrl = ResolveCameraController();
        if (camCtrl != null)
        {
            camCtrl.MoveToPotInspection();
        }
        else
        {
            var tp = GameObject.Find("CameraSystem/TransformPoints/PotInspection") 
                ?? GameObject.Find("TransformPoints/PotInspection") 
                ?? GameObject.Find("PotInspection");
            if (tp != null) SnapCameraToTransform(tp.transform);
        }
    }

    public void ReturnToPotControlMachine()
    {
        SnapToPotControlMachine();
    }

    /// <summary>
    /// STAGE 1 — POT CONTROL MACHINE (Observation):
    /// 1. Camera snaps immediately to TransformPoints/PotControlMachine.
    /// 2. ActiveDigits MUST NOT blink yet (normal state).
    /// 3. Voltmeter needle remains at normal position.
    /// 4. Initial voltage = 4.544 V.
    /// 5. Shows description: "Observe the voltage value. The safe limit is below 4.5 V, with current below 350 kA."
    /// 6. LocalTTS speaks exact description. Voice finish does NOT advance.
    /// 7. User clicks NEXT.
    /// </summary>
    public void BeginPotControlObservation()
    {
        ResetOpeningToolStates();
        SnapToPotControlMachine();

        StopPotControlWarning();

        var pcm = ResolvePotControlDisplay();
        if (pcm != null)
        {
            pcm.SetModeIndicatorAuto();
            pcm.RestoreNormalLamps();
            pcm.SetVoltage(4.544f);
            pcm.suppressWarningBlink = true;
            pcm.StopWarningBlink();
        }

        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            ui.StopPotBlink(false);
            ui.StopWalkieTalkieBlink();
            ui.DisableWalkieTalkie();
            ui.DisableWalkieTalkie2();
            ui.StopAnodeDownBlink();
            ui.isAnodeDownInteractable = false;
        }

        OnInterceptTaskCompletion = null;
        ShowPotControlObservationDescription();
    }

    public void ShowPotControlObservationDescription()
    {
        string desc = "Observe the voltage value. The safe limit is below 4.5 V, with current below 350 kA.";
        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            if (ui.welcomePanel != null) ui.welcomePanel.SetActive(true);
            if (ui.titleText != null) ui.titleText.text = "Pot Control Machine";
            if (ui.statusText != null)
            {
                ui.statusText.gameObject.SetActive(true);
                ui.statusText.text = "OBSERVE VOLTAGE AND CURRENT";
            }
            if (ui.descriptionText != null)
            {
                ui.descriptionText.gameObject.SetActive(true);
                ui.descriptionText.text = desc;
            }
            if (ui.nextButton != null) ui.nextButton.gameObject.SetActive(true);

            int totalTasks = (handler != null && handler.sequenceList != null && handler.sequenceList.Count > 0 && handler.sequenceList[0].TaskList != null)
                ? handler.sequenceList[0].TaskList.Count : 10;
            if (ui.progressText != null)
            {
                ui.progressText.text = $"<color=#00E5FF>TASK 05 / {totalTasks:D2}</color>";
            }
            if (ui.valueDisplay != null)
            {
                ui.valueDisplay.SetProgress($"TASK 05 / {totalTasks:D2}");
                ui.valueDisplay.SetTitle("Pot Control Machine");
                ui.valueDisplay.DisplayNormalStatus("OBSERVE VOLTAGE AND CURRENT");
                ui.valueDisplay.HideValueDisplay();
            }

            ui.SpeakDescriptionText(desc, "TASK_05_POT_CONTROL_OBSERVATION");
        }
        else
        {
            Manager.Instance?.SpeakText(desc, "TASK_05_POT_CONTROL_OBSERVATION");
        }

        Debug.Log("[PotLeakage] Stage 1: Pot Control Observation active. ActiveDigits blinking = OFF, Voltage = 4.544 V.");
    }

    /// <summary>
    /// STAGE 2 — POT CONTROL MACHINE WARNING / ACTIVE DIGITS:
    /// 1. Camera stays at TransformPoints/PotControlMachine (no movement).
    /// 2. ActiveDigits of VoltageDisplay and KADataDisplay blink (0.5s ON / 0.5s OFF).
    /// 3. NeedlePivot smoothly moves to Tick_Minor_-42 and trembles subtly.
    /// 4. Shows warning description and plays voice over.
    /// 5. Voice finish does NOT advance.
    /// 6. User clicks NEXT.
    /// </summary>
    public void StartPotControlWarning()
    {
        StartPotControlNeedleWarning();

        string desc = "Active digits on Voltage and Current displays are blinking. Beam level exceeds safe limits (> 350). Pot voltage ceiling is 4.5 V. Request emergency trip.";
        string spokenVO = "Active digits on the voltage and current displays are blinking. The beam level exceeds the safe limit of 350, and the pot voltage is at the 4.5 volt ceiling. Request an emergency trip.";

        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            if (ui.welcomePanel != null) ui.welcomePanel.SetActive(true);
            if (ui.titleText != null) ui.titleText.text = "Pot Control Warning";
            if (ui.statusText != null)
            {
                ui.statusText.gameObject.SetActive(true);
                ui.statusText.text = "SAFE LIMIT EXCEEDED — EMERGENCY TRIP";
            }
            if (ui.descriptionText != null)
            {
                ui.descriptionText.gameObject.SetActive(true);
                ui.descriptionText.text = desc;
            }
            if (ui.nextButton != null) ui.nextButton.gameObject.SetActive(true);

            int totalTasks = (handler != null && handler.sequenceList != null && handler.sequenceList.Count > 0 && handler.sequenceList[0].TaskList != null)
                ? handler.sequenceList[0].TaskList.Count : 10;
            if (ui.progressText != null)
            {
                ui.progressText.text = $"<color=#00E5FF>TASK 06 / {totalTasks:D2}</color>";
            }
            if (ui.valueDisplay != null)
            {
                ui.valueDisplay.SetProgress($"TASK 06 / {totalTasks:D2}");
                ui.valueDisplay.SetTitle("Pot Control Warning");
                ui.valueDisplay.DisplayNormalStatus("SAFE LIMIT EXCEEDED");
                ui.valueDisplay.HideValueDisplay();
            }

            ui.PlayTrainingVoiceOver(ui.task08Clip, spokenVO, "TASK_06_POT_CONTROL_WARNING");
        }
        else
        {
            Manager.Instance?.SpeakText(spokenVO, "TASK_06_POT_CONTROL_WARNING");
        }

        OnInterceptTaskCompletion = null;
        Debug.Log("[PotLeakage] Stage 2: Pot Control Warning active. ActiveDigits blinking = ON, Needle target = Tick_Minor_-42.");
    }

    public void StartPotControlNeedleWarning()
    {
        var pcm = ResolvePotControlDisplay();
        if (pcm != null)
        {
            pcm.suppressWarningBlink = false;
            pcm.StartWarningBlink();
        }
    }

    public void StopPotControlWarning()
    {
        var pcm = ResolvePotControlDisplay();
        if (pcm != null)
        {
            pcm.suppressWarningBlink = true;
            pcm.StopWarningBlink();
        }
    }

    /// <summary>
    /// STAGE 3 — VERIFY BEAM LEVEL (Pot Inspection / Beam Scale):
    /// 1. Stops Stage 2 warning effects (ActiveDigits blink OFF, needle tremble OFF).
    /// 2. Snaps camera directly to TransformPoints/PotInspection.
    /// 3. Shows description: "Verify the beam level on both the Pot Controller and the Beam Scale."
    /// 4. Speaks exact text through LocalTTS.
    /// 5. Voice finish does NOT advance.
    /// 6. User clicks NEXT.
    /// </summary>
    public void BeginBeamScaleVerification()
    {
        ResetOpeningToolStates();
        StopPotControlWarning();
        SnapToPotInspection();

        var beamScale = UnityEngine.Object.FindAnyObjectByType<PotBeamScaleController>(FindObjectsInactive.Include);
        if (beamScale != null)
        {
            beamScale.StartNeedleWarningAnimation();
        }

        OnInterceptTaskCompletion = null;
        ShowBeamScaleVerificationDescription();
    }

    public void ShowBeamScaleVerificationDescription()
    {
        string desc = "Verify the beam level on both the Pot Controller and the Beam Scale.";
        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            if (ui.welcomePanel != null) ui.welcomePanel.SetActive(true);
            if (ui.titleText != null) ui.titleText.text = "Verify Beam Level";
            if (ui.statusText != null)
            {
                ui.statusText.gameObject.SetActive(true);
                ui.statusText.text = "BEAM SCALE VERIFICATION";
            }
            if (ui.descriptionText != null)
            {
                ui.descriptionText.gameObject.SetActive(true);
                ui.descriptionText.text = desc;
            }
            if (ui.nextButton != null) ui.nextButton.gameObject.SetActive(true);

            int totalTasks = (handler != null && handler.sequenceList != null && handler.sequenceList.Count > 0 && handler.sequenceList[0].TaskList != null)
                ? handler.sequenceList[0].TaskList.Count : 10;
            if (ui.progressText != null)
            {
                ui.progressText.text = $"<color=#00E5FF>TASK 07 / {totalTasks:D2}</color>";
            }
            if (ui.valueDisplay != null)
            {
                ui.valueDisplay.SetProgress($"TASK 07 / {totalTasks:D2}");
                ui.valueDisplay.SetTitle("Verify Beam Level");
                ui.valueDisplay.DisplayNormalStatus("BEAM SCALE VERIFICATION");
                ui.valueDisplay.HideValueDisplay();
            }

            ui.SpeakDescriptionText(desc, "TASK_07_BEAM_SCALE_VERIFICATION");
        }
        else
        {
            Manager.Instance?.SpeakText(desc, "TASK_07_BEAM_SCALE_VERIFICATION");
        }

        Debug.Log("[PotLeakage] Stage 3: Beam Scale Verification active. Camera snapped to PotInspection.");
    }

    /// <summary>
    /// STAGE 4 — POT CONTROL MACHINE — ANODE DOWN:
    /// 1. Snaps camera BACK to TransformPoints/PotControlMachine.
    /// 2. ONLY AFTER camera snaps back, shows Anode Down description & plays VO.
    /// 3. Cube.016 highlighted, blinking, and interactable.
    /// 4. 4 discrete clicks: 4.544 -> 4.444 -> 4.344 -> 4.244 -> 4.144 V.
    /// 5. After 4th click (4.144 V): stops Cube.016 blink, shows safe limits text.
    /// 6. Voice finish does NOT advance.
    /// 7. User clicks NEXT.
    /// </summary>
    public void BeginAnodeDownInteraction()
    {
        ReturnToPotControlMachine();

        var beamScale = UnityEngine.Object.FindAnyObjectByType<PotBeamScaleController>(FindObjectsInactive.Include);
        if (beamScale != null) beamScale.StopNeedleAnimation();

        var pcm = ResolvePotControlDisplay();
        if (pcm != null)
        {
            pcm.SetModeIndicatorMan();
            pcm.SetManualModeLamps();
            pcm.SetVoltage(4.544f);
        }

        anodeDownClickCount = 0;
        isAnodeDownInteractable = true;
        OnInterceptTaskCompletion = () => anodeDownClickCount < 4;

        ShowAnodeDownDescription();
        ActivateAnodeDownButton();

        Debug.Log("[PotLeakage] Stage 4: Anode Down active. Camera snapped back to PotControlMachine.");
    }

    public void ShowAnodeDownDescription()
    {
        string desc = "Click the highlighted Anode Down button to lower the anode beam in steps until the voltage reaches safe limits. The voltage should be below 4.2 V.";
        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            if (ui.welcomePanel != null) ui.welcomePanel.SetActive(true);
            if (ui.titleText != null) ui.titleText.text = "Lower Anode Beam";
            if (ui.statusText != null)
            {
                ui.statusText.gameObject.SetActive(true);
                ui.statusText.text = "VOLTAGE REDUCTION PROCEDURE";
            }
            if (ui.descriptionText != null)
            {
                ui.descriptionText.gameObject.SetActive(true);
                ui.descriptionText.text = desc;
            }
            if (ui.nextButton != null) ui.nextButton.gameObject.SetActive(true);

            int totalTasks = (handler != null && handler.sequenceList != null && handler.sequenceList.Count > 0 && handler.sequenceList[0].TaskList != null)
                ? handler.sequenceList[0].TaskList.Count : 10;
            if (ui.progressText != null)
            {
                ui.progressText.text = $"<color=#00E5FF>TASK 08 / {totalTasks:D2}</color>";
            }
            if (ui.valueDisplay != null)
            {
                ui.valueDisplay.SetProgress($"TASK 08 / {totalTasks:D2}");
                ui.valueDisplay.SetTitle("Lower Anode Beam");
                ui.valueDisplay.DisplayNormalStatus("VOLTAGE REDUCTION PROCEDURE");
                ui.valueDisplay.HideValueDisplay();
            }

            ui.SpeakDescriptionText(desc, "TASK_08_LOWER_ANODE_BEAM");
        }
        else
        {
            Manager.Instance?.SpeakText(desc, "TASK_08_LOWER_ANODE_BEAM");
        }
    }

    public void ActivateAnodeDownButton()
    {
        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            ui.StartAnodeDownBlink();
            ui.isAnodeDownInteractable = true;
        }
    }

    public void ResetAnodeDownDebounce()
    {
        lastAnodeDownClickFrame = -1;
        lastAnodeDownClickTime = -1f;
    }

    public void HandleAnodeDownClick()
    {
        var ui = ResolvePotLeakageUI();
        if (!isAnodeDownInteractable && (ui == null || !ui.isAnodeDownInteractable)) return;

        if (Application.isPlaying && ((Time.frameCount == lastAnodeDownClickFrame && Time.frameCount > 0) || (Time.time - lastAnodeDownClickTime < 0.15f))) return;
        lastAnodeDownClickFrame = Time.frameCount;
        lastAnodeDownClickTime = Time.time;

        if (anodeDownClickCount >= 4) return;

        anodeDownClickCount++;
        if (ui != null) ui.anodeDownClickCount = anodeDownClickCount;

        var pcm = ResolvePotControlDisplay();

        switch (anodeDownClickCount)
        {
            case 1:
                if (pcm != null) pcm.SetVoltage(4.444f);
                Debug.Log("[PotLeakage] Anode Down clicked: 1/4");
                Debug.Log("[PotLeakage] Voltage = 4.444 V");
                break;
            case 2:
                if (pcm != null) pcm.SetVoltage(4.344f);
                Debug.Log("[PotLeakage] Anode Down clicked: 2/4");
                Debug.Log("[PotLeakage] Voltage = 4.344 V");
                break;
            case 3:
                if (pcm != null) pcm.SetVoltage(4.244f);
                Debug.Log("[PotLeakage] Anode Down clicked: 3/4");
                Debug.Log("[PotLeakage] Voltage = 4.244 V");
                break;
            case 4:
                if (pcm != null) pcm.SetVoltage(4.144f);
                Debug.Log("[PotLeakage] Anode Down clicked: 4/4");
                Debug.Log("[PotLeakage] Voltage = 4.144 V");
                Debug.Log("[PotLeakage] Safe voltage reached");
                Debug.Log("[PotLeakage] Awaiting user Continue");
                ShowSafeVoltageReachedDescription();
                break;
        }
    }

    public void ShowSafeVoltageReachedDescription()
    {
        isAnodeDownInteractable = false;
        OnInterceptTaskCompletion = null;

        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            ui.StopAnodeDownBlink();
            ui.isAnodeDownInteractable = false;
        }

        var pcm = ResolvePotControlDisplay();
        if (pcm != null)
        {
            pcm.SetVoltage(4.144f);
            pcm.suppressWarningBlink = true;
            pcm.StopWarningBlink();
            pcm.SetManualModeLamps();
        }

        string safeMsg = "The voltage is now at an appropriate level. We can proceed with leakage control.";
        if (ui != null)
        {
            if (ui.descriptionText != null)
            {
                ui.descriptionText.gameObject.SetActive(true);
                ui.descriptionText.text = safeMsg;
            }
            if (ui.titleText != null) ui.titleText.text = "Safe Limits Reached";
            if (ui.statusText != null)
            {
                ui.statusText.gameObject.SetActive(true);
                ui.statusText.text = "VOLTAGE AT SAFE LEVEL (< 4.2 V)";
            }
            if (ui.nextButton != null) ui.nextButton.gameObject.SetActive(true);

            ui.SpeakDescriptionText(safeMsg, "TASK_08_SAFE_VOLTAGE");
        }
        else
        {
            Manager.Instance?.SpeakText(safeMsg, "TASK_08_SAFE_VOLTAGE");
        }

        Debug.Log("[PotLeakage] Voltage appropriate message displayed (4.144 V reached). Awaiting manual Next click.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POT LEAKAGE — GLOVES, TOOL SELECTION, STOPPER & EMERGENCY COMMUNICATION HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Pot Leakage Tool & Stopper Interaction State")]
    public GameObject glovesTarget;
    public bool isGlovesInteractable = false;
    public bool isGlovesConfirmed = false;
    public bool isGlovesBlinking = false;
    private Material glovesOriginalMaterial;
    private Coroutine glovesBlinkCoroutine;
    private readonly Dictionary<Renderer, Material[]> glovesOriginalMaterials = new Dictionary<Renderer, Material[]>();
    private Material glovesHighlightMaterial;

    public GameObject sideBreakingToolTarget;
    public bool isSideBreakingToolInteractable = false;
    public bool isSideBreakingToolConfirmed = false;

    public GameObject sideBreakingTool4Target;
    public GameObject sideBreakingTool5Target;
    public GameObject sideBreakingTool6Target;
    public bool isStopperToolInteractable = false;
    public bool isStopperToolConfirmed = false;

    public GameObject walkieCommunicationCanvasTarget;
    private Material yellowHighlightMaterial;

    private readonly List<RendererSlotHighlight> leakagePointHighlightSlots = new List<RendererSlotHighlight>();
    private Coroutine leakagePointBlinkCoroutine;

    public struct RendererSlotHighlight
    {
        public Renderer renderer;
        public int slotIndex;
        public Material originalMaterial;
        public string targetMatName;
    }

    public Material ResolveGlovesHighlightMaterial()
    {
        if (glovesHighlightMaterial == null)
        {
#if UNITY_EDITOR
            glovesHighlightMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/PotLeakage/Materials/M_PotLeakage_Highlight.mat");
#endif
            if (glovesHighlightMaterial == null && potLeakageUI != null)
            {
                glovesHighlightMaterial = potLeakageUI.potHighlightMaterial;
            }
            if (glovesHighlightMaterial == null)
            {
                glovesHighlightMaterial = Resources.Load<Material>("M_PotLeakage_Highlight");
            }
            if (glovesHighlightMaterial == null)
            {
                glovesHighlightMaterial = HighlightMaterial;
            }
        }
        return glovesHighlightMaterial;
    }

    public void CacheGlovesOriginalMaterials()
    {
        if (glovesTarget == null)
        {
            var tb = GameObject.Find("ToolBox");
            if (tb != null)
            {
                var child = tb.transform.Find("gloves");
                if (child != null) glovesTarget = child.gameObject;
            }
        }
        if (glovesTarget == null) return;

        glovesOriginalMaterials.Clear();
        var renderers = glovesTarget.GetComponentsInChildren<Renderer>(true);

        Material fallbackGloveMat = null;
#if UNITY_EDITOR
        fallbackGloveMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Model/Materials/BaseColor.mat");
#endif
        if (fallbackGloveMat == null)
        {
            fallbackGloveMat = Resources.Load<Material>("BaseColor");
        }

        foreach (var r in renderers)
        {
            if (r != null && r.sharedMaterials != null && r.sharedMaterials.Length > 0)
            {
                var mats = (Material[])r.sharedMaterials.Clone();
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || mats[i].name.Contains("Highlight"))
                    {
                        if (fallbackGloveMat != null) mats[i] = fallbackGloveMat;
                    }
                }
                glovesOriginalMaterials[r] = mats;
                if (glovesOriginalMaterial == null && mats.Length > 0 && mats[0] != null)
                {
                    glovesOriginalMaterial = mats[0];
                }
            }
        }
    }

    public Material ResolveYellowHighlightMaterial()
    {
        if (yellowHighlightMaterial == null)
        {
#if UNITY_EDITOR
            yellowHighlightMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/PotLeakage/Materials/M_PotLeakage_Highlight_Yellow.mat");
#endif
            if (yellowHighlightMaterial == null)
            {
                var baseMat = HighlightMaterial ?? (potLeakageUI != null ? potLeakageUI.potHighlightMaterial : null);
                if (baseMat != null)
                {
                    yellowHighlightMaterial = new Material(baseMat);
                    yellowHighlightMaterial.name = "M_PotLeakage_Highlight_Yellow_Runtime";
                    yellowHighlightMaterial.SetColor("_BaseColor", new Color(1f, 0.95f, 0.1f, 0.35f));
                    yellowHighlightMaterial.SetColor("_Color", new Color(1f, 0.95f, 0.1f, 0.35f));
                    yellowHighlightMaterial.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.1f, 0.8f));
                }
            }
        }
        return yellowHighlightMaterial;
    }

    public GameObject ResolveGlovesTarget()
    {
        if (glovesTarget == null)
        {
            var tb = GameObject.Find("ToolBox");
            if (tb != null)
            {
                var child = tb.transform.Find("gloves");
                if (child != null) glovesTarget = child.gameObject;
            }
            if (glovesTarget == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "gloves" && !IsPersistentObject(g))
                    {
                        glovesTarget = g;
                        break;
                    }
                }
            }
        }

        if (glovesTarget != null)
        {
            if (glovesOriginalMaterials.Count == 0)
            {
                CacheGlovesOriginalMaterials();
            }

            var col = glovesTarget.GetComponent<Collider>();
            if (col == null) col = glovesTarget.AddComponent<BoxCollider>();
            col.enabled = true;

            var interaction = glovesTarget.GetComponent<SideBreakingToolInteraction>();
            if (interaction == null) interaction = glovesTarget.AddComponent<SideBreakingToolInteraction>();
            interaction.role = SideBreakingToolInteraction.ToolRole.Gloves;
            interaction.uiController = ResolvePotLeakageUI();
        }

        return glovesTarget;
    }

    public GameObject ResolveSideBreakingTool()
    {
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
            var col = sideBreakingToolTarget.GetComponent<Collider>();
            if (col == null) col = sideBreakingToolTarget.AddComponent<BoxCollider>();
            col.enabled = true;

            var interaction = sideBreakingToolTarget.GetComponent<SideBreakingToolInteraction>();
            if (interaction == null) interaction = sideBreakingToolTarget.AddComponent<SideBreakingToolInteraction>();
            interaction.role = SideBreakingToolInteraction.ToolRole.SideBreakingTool;
            interaction.uiController = ResolvePotLeakageUI();
        }

        return sideBreakingToolTarget;
    }

    public GameObject ResolveSideBreakingTool4()
    {
        if (sideBreakingTool4Target == null)
        {
            var tb = GameObject.Find("ToolBox");
            if (tb != null)
            {
                var child = tb.transform.Find("SIDEREAKING TOOL (4)");
                if (child != null) sideBreakingTool4Target = child.gameObject;
            }
            if (sideBreakingTool4Target == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "SIDEREAKING TOOL (4)" && !IsPersistentObject(g))
                    {
                        sideBreakingTool4Target = g;
                        break;
                    }
                }
            }
        }
        return sideBreakingTool4Target;
    }

    public GameObject ResolveSideBreakingTool5()
    {
        if (sideBreakingTool5Target == null)
        {
            var tp = GameObject.Find("CameraSystem/TransformPoints") ?? GameObject.Find("TransformPoints");
            if (tp != null)
            {
                var child = tp.transform.Find("SIDEREAKING TOOL (5)");
                if (child != null) sideBreakingTool5Target = child.gameObject;
            }
            if (sideBreakingTool5Target == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "SIDEREAKING TOOL (5)" && !IsPersistentObject(g))
                    {
                        sideBreakingTool5Target = g;
                        break;
                    }
                }
            }
        }

        if (sideBreakingTool5Target != null)
        {
            var col = sideBreakingTool5Target.GetComponent<Collider>();
            if (col == null) col = sideBreakingTool5Target.AddComponent<BoxCollider>();
            col.enabled = true;

            var interaction = sideBreakingTool5Target.GetComponent<SideBreakingToolInteraction>();
            if (interaction == null) interaction = sideBreakingTool5Target.AddComponent<SideBreakingToolInteraction>();
            interaction.role = SideBreakingToolInteraction.ToolRole.StopperTool;
            interaction.uiController = ResolvePotLeakageUI();
        }

        return sideBreakingTool5Target;
    }

    public GameObject ResolveSideBreakingTool6()
    {
        if (sideBreakingTool6Target == null)
        {
            var tp = GameObject.Find("CameraSystem/TransformPoints") ?? GameObject.Find("TransformPoints");
            if (tp != null)
            {
                var child = tp.transform.Find("SIDEREAKING TOOL (6)");
                if (child != null) sideBreakingTool6Target = child.gameObject;
            }
            if (sideBreakingTool6Target == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "SIDEREAKING TOOL (6)" && !IsPersistentObject(g))
                    {
                        sideBreakingTool6Target = g;
                        break;
                    }
                }
            }

            if (sideBreakingTool6Target == null)
            {
                var s5 = ResolveSideBreakingTool5();
                if (s5 != null)
                {
                    sideBreakingTool6Target = UnityEngine.Object.Instantiate(s5, s5.transform.parent);
                    sideBreakingTool6Target.name = "SIDEREAKING TOOL (6)";
                    sideBreakingTool6Target.transform.localPosition = s5.transform.localPosition;
                    sideBreakingTool6Target.transform.localRotation = s5.transform.localRotation;
                    sideBreakingTool6Target.transform.localScale = s5.transform.localScale;
                    sideBreakingTool6Target.SetActive(false);
                }
            }
        }

        if (sideBreakingTool6Target != null)
        {
            var col = sideBreakingTool6Target.GetComponent<Collider>();
            if (col == null) col = sideBreakingTool6Target.AddComponent<BoxCollider>();
            col.enabled = true;

            var interaction = sideBreakingTool6Target.GetComponent<SideBreakingToolInteraction>();
            if (interaction == null) interaction = sideBreakingTool6Target.AddComponent<SideBreakingToolInteraction>();
            interaction.role = SideBreakingToolInteraction.ToolRole.StopperTool;
            interaction.uiController = ResolvePotLeakageUI();
        }

        return sideBreakingTool6Target;
    }

    /// <summary>
    /// Safety reset for tools (4), (5), and (6).
    /// Guarantees that neither tool remains active during opening stages (Welcome, Normal Pot Operation,
    /// Normal Pot / Leakage, Pot Control Machine, Verify Beam Level) or from previous test runs.
    /// Does NOT touch SIDEREAKING TOOL (1), (2), or (3).
    /// </summary>
    public void ResetOpeningToolStates()
    {
        var s4 = ResolveSideBreakingTool4();
        if (s4 != null) s4.SetActive(false);

        var s5 = ResolveSideBreakingTool5();
        if (s5 != null) s5.SetActive(false);

        var s6 = ResolveSideBreakingTool6();
        if (s6 != null) s6.SetActive(false);

        isStopperToolInteractable = false;
        isStopperToolConfirmed = false;

        RestoreMoltenLeakageVFX();
    }

    public GameObject ResolveWalkieCommunicationCanvas()
    {
        if (walkieCommunicationCanvasTarget == null)
        {
            var canvas = GameObject.Find("WalkieCommunicationCanvas");
            if (canvas == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "WalkieCommunicationCanvas" && !IsPersistentObject(g))
                    {
                        canvas = g;
                        break;
                    }
                }
            }
            walkieCommunicationCanvasTarget = canvas;
        }
        return walkieCommunicationCanvasTarget;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. TOOLBOX CAMERA → SAFETY GLOVES
    // ─────────────────────────────────────────────────────────────────────────

    public const string GlovesInstructionText = "Equip safety gloves before picking up the side breaking tool.";
    public const string SideBreakingToolInstructionText = "Select the Side Breaking Tool from the toolbox to proceed with side breaking and crust removal.";
    public const string StopperInstructionText = "Now carefully apply the stopper to the leakage point on the pot. Look for the area where smoke is visible and molten metal is flowing. If the leakage is wider, you may use an additional stopper to help control it.";

    public void BeginToolboxGloveInteraction()
    {
        Debug.Log("[PotLeakage] BeginToolboxGloveInteraction: Snapping to ToolBoxSelection");
        ResetOpeningToolStates();

        var camCtrl = ResolveCameraController();
        if (camCtrl != null)
        {
            camCtrl.MoveToToolBoxSelection();
        }
        else
        {
            var tp = GameObject.Find("CameraSystem/TransformPoints/ToolBoxSelection") ?? GameObject.Find("ToolBoxSelection");
            if (tp != null) SnapCameraToTransform(tp.transform);
        }

        // During Toolbox Selection, SIDEREAKING TOOL is ON, SIDEREAKING TOOL (4) is OFF.
        var sideTool = ResolveSideBreakingTool();
        if (sideTool != null) sideTool.SetActive(true);

        isGlovesConfirmed = false;
        isGlovesInteractable = true;
        isSideBreakingToolInteractable = false;
        isStopperToolInteractable = false;

        var gloves = ResolveGlovesTarget();
        if (gloves != null)
        {
            gloves.SetActive(true);
            CacheGlovesOriginalMaterials();
            StartGlovesBlink();
        }

        OnInterceptTaskCompletion = () => !isGlovesConfirmed;

        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            if (ui.welcomePanel != null) ui.welcomePanel.SetActive(true);
            if (ui.titleText != null) ui.titleText.text = "Toolbox Selection";
            if (ui.statusText != null)
            {
                ui.statusText.gameObject.SetActive(true);
                ui.statusText.text = "EQUIP SAFETY GLOVES";
            }
            if (ui.descriptionText != null)
            {
                ui.descriptionText.gameObject.SetActive(true);
                ui.descriptionText.text = GlovesInstructionText;
            }
            if (ui.nextButton != null) ui.nextButton.gameObject.SetActive(true);

            ui.isGlovesInteractable = true;
            ui.isGlovesConfirmed = false;

            ui.SpeakDescriptionText(GlovesInstructionText);
        }
        else
        {
            TruckTyreReplacement.Core.Manager.Instance?.SpeakText(GlovesInstructionText, GlovesInstructionText);
        }
    }

    public void StartGlovesBlink()
    {
        var gloves = ResolveGlovesTarget();
        if (gloves == null) return;

        CacheGlovesOriginalMaterials();

        if (glovesBlinkCoroutine != null)
        {
            StopCoroutine(glovesBlinkCoroutine);
            glovesBlinkCoroutine = null;
        }

        isGlovesBlinking = true;

        if (Application.isPlaying)
        {
            glovesBlinkCoroutine = StartCoroutine(GlovesBlinkRoutine());
        }
        else
        {
            ApplyGlovesHighlightState(true);
        }
    }

    public void StopGlovesBlink()
    {
        isGlovesBlinking = false;
        if (glovesBlinkCoroutine != null)
        {
            StopCoroutine(glovesBlinkCoroutine);
            glovesBlinkCoroutine = null;
        }
        ApplyGlovesHighlightState(false);
    }

    private void ApplyGlovesHighlightState(bool highlight)
    {
        var gloves = ResolveGlovesTarget();
        if (gloves == null) return;

        if (glovesOriginalMaterials.Count == 0)
        {
            CacheGlovesOriginalMaterials();
        }

        Material hlMat = ResolveGlovesHighlightMaterial();
        foreach (var kvp in glovesOriginalMaterials)
        {
            var r = kvp.Key;
            var orig = kvp.Value;
            if (r == null || orig == null) continue;

            if (highlight && hlMat != null)
            {
                Material[] hlMats = new Material[orig.Length];
                for (int i = 0; i < hlMats.Length; i++) hlMats[i] = hlMat;
                r.sharedMaterials = hlMats;
            }
            else
            {
                r.sharedMaterials = orig;
            }
        }
    }

    private IEnumerator GlovesBlinkRoutine()
    {
        isGlovesBlinking = true;
        bool showHighlight = true;
        while (isGlovesBlinking && !isGlovesConfirmed)
        {
            ApplyGlovesHighlightState(showHighlight);
            yield return new WaitForSeconds(0.5f);
            showHighlight = !showHighlight;
        }
        ApplyGlovesHighlightState(false);
        glovesBlinkCoroutine = null;
        isGlovesBlinking = false;
    }

    public void HandleGlovesClicked()
    {
        if (!isGlovesInteractable && (potLeakageUI == null || !potLeakageUI.isGlovesInteractable)) return;

        isGlovesInteractable = false;
        isGlovesConfirmed = true;

        if (potLeakageUI != null)
        {
            potLeakageUI.isGlovesInteractable = false;
            potLeakageUI.isGlovesConfirmed = true;
            potLeakageUI.PlaySFX(potLeakageUI.clickAudioClip);
        }

        StopGlovesBlink();
        Debug.Log("[PotLeakage] Gloves clicked. Blink stopped, original materials restored.");

        // 1. Disable gloves
        var gloves = ResolveGlovesTarget();
        if (gloves != null) gloves.SetActive(false);

        // 2. Enable SIDEREAKING TOOL (Do NOT touch (1), (2), (3))
        var tool = ResolveSideBreakingTool();
        if (tool != null) tool.SetActive(true);

        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            if (ui.statusText != null)
            {
                ui.statusText.gameObject.SetActive(true);
                ui.statusText.text = "SELECT SIDE BREAKING TOOL";
            }
            if (ui.descriptionText != null)
            {
                ui.descriptionText.gameObject.SetActive(true);
                ui.descriptionText.text = SideBreakingToolInstructionText;
            }
            ui.SpeakDescriptionText(SideBreakingToolInstructionText);
        }
        else
        {
            TruckTyreReplacement.Core.Manager.Instance?.SpeakText(SideBreakingToolInstructionText, SideBreakingToolInstructionText);
        }

        StartSideBreakingToolSelection();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. SIDE BREAKING TOOL SELECTION
    // ─────────────────────────────────────────────────────────────────────────

    public void HighlightSideBreakingTool()
    {
        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            ui.StartSideBreakingToolHighlight();
        }
    }

    public void StopSideBreakingToolHighlight()
    {
        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            ui.StopSideBreakingToolHighlight();
        }
    }

    public void StartSideBreakingToolSelection()
    {
        var tool = ResolveSideBreakingTool();
        if (tool != null) tool.SetActive(true);

        isSideBreakingToolInteractable = true;
        isSideBreakingToolConfirmed = false;

        OnInterceptTaskCompletion = () => !isSideBreakingToolConfirmed;

        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            ui.isSideBreakingToolInteractable = true;
            ui.isSideBreakingToolConfirmed = false;
            ui.StartSideBreakingToolHighlight();
        }
        else
        {
            HighlightSideBreakingTool();
        }

        Debug.Log("[PotLeakage] Side Breaking Tool highlighted and awaiting player click.");
    }

    public void HandleSideBreakingToolClicked()
    {
        if (!isSideBreakingToolInteractable && (potLeakageUI == null || !potLeakageUI.isSideBreakingToolInteractable)) return;

        isSideBreakingToolInteractable = false;
        isSideBreakingToolConfirmed = true;

        if (potLeakageUI != null)
        {
            potLeakageUI.isSideBreakingToolInteractable = false;
            potLeakageUI.isSideBreakingToolConfirmed = true;
            potLeakageUI.PlaySFX(potLeakageUI.clickAudioClip);
            potLeakageUI.StopSideBreakingToolHighlight();
        }
        else
        {
            StopSideBreakingToolHighlight();
        }

        Debug.Log("[PotLeakage] SIDEREAKING TOOL confirmed: Disabling tool, enabling tool (4)");

        // 1. Stop its highlight/blink (done above)
        // 2. Restore original materials (done above)
        // 3. Disable SIDEREAKING TOOL
        var tool = ResolveSideBreakingTool();
        if (tool != null) tool.SetActive(false);

        // 4. Enable SIDEREAKING TOOL (4) (Do NOT touch (1), (2), (3))
        ActivateSideBreakingTool4();

        // 5. Instantly snap camera to TransformPoints/Normal Pot
        SnapToNormalPot();

        // 6. ONLY AFTER snapping to Normal Pot, disable WalkieCommunicationCanvas
        DisableWalkieCommunicationCanvas();

        // 7. Setup Normal Pot Stopper Interaction
        BeginNormalPotStopperInteraction();
    }

    public void ActivateSideBreakingTool4()
    {
        var tool4 = ResolveSideBreakingTool4();
        if (tool4 != null)
        {
            tool4.SetActive(true);
            Debug.Log("[PotLeakage] SIDEREAKING TOOL (4) enabled.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. NORMAL POT → STOPPER APPLICATION
    // ─────────────────────────────────────────────────────────────────────────

    public void SnapToNormalPot()
    {
        Debug.Log("[PotLeakage] Snapping camera to TransformPoints/Normal Pot");
        var camCtrl = ResolveCameraController();
        if (camCtrl != null)
        {
            camCtrl.MoveToNormalPot();
        }
        else
        {
            var tp = GameObject.Find("CameraSystem/TransformPoints") ?? GameObject.Find("TransformPoints");
            if (tp != null)
            {
                for (int i = 0; i < tp.transform.childCount; i++)
                {
                    var child = tp.transform.GetChild(i);
                    if (child.name.Trim() == "Normal Pot")
                    {
                        SnapCameraToTransform(child);
                        break;
                    }
                }
            }
        }
    }

    public void DisableWalkieCommunicationCanvas()
    {
        var canvas = ResolveWalkieCommunicationCanvas();
        if (canvas != null)
        {
            canvas.SetActive(false);
            Debug.Log("[PotLeakage] WalkieCommunicationCanvas disabled.");
        }
    }

    public void ActivateSideBreakingTool5()
    {
        var tool5 = ResolveSideBreakingTool5();
        if (tool5 != null)
        {
            tool5.SetActive(true);
            var r = tool5.GetComponentInChildren<Renderer>(true);
            if (r != null)
            {
#if UNITY_EDITOR
                var m0 = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Model/Materials/plane_divided_DefaultMaterial_BaseColor.mat");
                var m1 = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Model/Materials/Stylized_MetalGrid_01_basecolor.mat");
                if (m0 != null && m1 != null)
                {
                    r.sharedMaterials = new Material[] { m0, m1 };
                }
#endif
            }
            Debug.Log("[PotLeakage] SIDEREAKING TOOL (5) enabled with original materials.");
        }
    }

    public void ActivateSideBreakingTool6()
    {
        var tool6 = ResolveSideBreakingTool6();
        if (tool6 != null)
        {
            tool6.SetActive(true);
            Debug.Log("[PotLeakage] SIDEREAKING TOOL (6) enabled.");
        }
    }

    public void BeginNormalPotStopperInteraction()
    {
        DisableWalkieCommunicationCanvas();

        // SIDEREAKING TOOL (6) is active BEFORE the click!
        var tool5 = ResolveSideBreakingTool5();
        if (tool5 != null) tool5.SetActive(false);

        ActivateSideBreakingTool6();
        HighlightLeakagePointMaterials();

        isStopperToolInteractable = true;
        isStopperToolConfirmed = false;

        OnInterceptTaskCompletion = () => !isStopperToolConfirmed;

        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            if (ui.welcomePanel != null) ui.welcomePanel.SetActive(true);
            if (ui.titleText != null) ui.titleText.text = "Upper Side Shell Response";
            if (ui.statusText != null)
            {
                ui.statusText.gameObject.SetActive(true);
                ui.statusText.text = "APPLY STOPPER TO LEAKAGE POINT";
            }
            if (ui.descriptionText != null)
            {
                ui.descriptionText.gameObject.SetActive(true);
                ui.descriptionText.text = StopperInstructionText;
            }
            if (ui.nextButton != null) ui.nextButton.gameObject.SetActive(true);

            ui.isStopperToolInteractable = true;
            ui.isStopperToolConfirmed = false;
            ui.SpeakDescriptionText(StopperInstructionText, "TASK_10_APPLY_STOPPER");
        }
        else
        {
            TruckTyreReplacement.Core.Manager.Instance?.SpeakText(StopperInstructionText, "TASK_10_APPLY_STOPPER");
        }

        Debug.Log("[PotLeakage] Normal Pot Stopper Interaction active. Description displayed, SIDEREAKING TOOL (6) interactable.");
    }

    public void HighlightLeakagePointMaterials()
    {
        StopLeakagePointHighlight();

        var yellowMat = ResolveYellowHighlightMaterial();
        leakagePointHighlightSlots.Clear();

        // Scan scene for renderers that use Stylized_MetalGrid_01_basecolor or plane_divided_DefaultMaterial_BaseColor
        var tool6 = ResolveSideBreakingTool6();
        List<Renderer> candidateRenderers = new List<Renderer>();
        if (tool6 != null)
        {
            candidateRenderers.AddRange(tool6.GetComponentsInChildren<Renderer>(true));
        }
        var tool5 = ResolveSideBreakingTool5();
        if (tool5 != null)
        {
            candidateRenderers.AddRange(tool5.GetComponentsInChildren<Renderer>(true));
        }

        var allRenderers = Resources.FindObjectsOfTypeAll<Renderer>();
        foreach (var r in allRenderers)
        {
            if (r == null || IsPersistentObject(r.gameObject)) continue;
            if (r.gameObject.activeInHierarchy && !candidateRenderers.Contains(r))
            {
                string rName = r.gameObject.name;
                if (rName.Contains("SIDEREAKING") || rName.Contains("Leak") || rName.Contains("Pot"))
                {
                    candidateRenderers.Add(r);
                }
            }
        }

        foreach (var r in candidateRenderers)
        {
            if (r == null) continue;
            Material[] mats = r.sharedMaterials;
            for (int s = 0; s < mats.Length; s++)
            {
                var m = mats[s];
                if (m == null) continue;
                string mName = m.name;
                if (mName.Contains("Stylized_MetalGrid_01_basecolor") || mName.Contains("plane_divided_DefaultMaterial_BaseColor"))
                {
                    Material origMat = m;
                    if (origMat != null && origMat.name.Contains("Highlight"))
                    {
#if UNITY_EDITOR
                        if (mName.Contains("Stylized_MetalGrid_01_basecolor"))
                            origMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Model/Materials/Stylized_MetalGrid_01_basecolor.mat");
                        else
                            origMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Model/Materials/plane_divided_DefaultMaterial_BaseColor.mat");
#endif
                    }

                    leakagePointHighlightSlots.Add(new RendererSlotHighlight
                    {
                        renderer = r,
                        slotIndex = s,
                        originalMaterial = origMat,
                        targetMatName = mName
                    });
                }
            }
        }

        Debug.Log($"[PotLeakage] HighlightLeakagePointMaterials: {leakagePointHighlightSlots.Count} slots collected.");

        if (Application.isPlaying)
        {
            leakagePointBlinkCoroutine = StartCoroutine(LeakagePointBlinkRoutine());
        }
        else
        {
            ApplyLeakagePointHighlightState(true);
        }
    }

    private void ApplyLeakagePointHighlightState(bool highlight)
    {
        var yellowMat = ResolveYellowHighlightMaterial();
        for (int i = 0; i < leakagePointHighlightSlots.Count; i++)
        {
            var slot = leakagePointHighlightSlots[i];
            if (slot.renderer != null)
            {
                Material[] mats = slot.renderer.sharedMaterials;
                if (slot.slotIndex >= 0 && slot.slotIndex < mats.Length)
                {
                    mats[slot.slotIndex] = highlight ? (yellowMat != null ? yellowMat : slot.originalMaterial) : slot.originalMaterial;
                    slot.renderer.sharedMaterials = mats;
                }
            }
        }
    }

    private IEnumerator LeakagePointBlinkRoutine()
    {
        bool showHighlight = true;
        while (isStopperToolInteractable && !isStopperToolConfirmed)
        {
            ApplyLeakagePointHighlightState(showHighlight);
            yield return new WaitForSeconds(0.45f);
            showHighlight = !showHighlight;
        }
        ApplyLeakagePointHighlightState(false);
        leakagePointBlinkCoroutine = null;
    }

    public void StopLeakagePointHighlight()
    {
        if (leakagePointBlinkCoroutine != null)
        {
            StopCoroutine(leakagePointBlinkCoroutine);
            leakagePointBlinkCoroutine = null;
        }

        for (int i = 0; i < leakagePointHighlightSlots.Count; i++)
        {
            var slot = leakagePointHighlightSlots[i];
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
        leakagePointHighlightSlots.Clear();
        Debug.Log("[PotLeakage] Leakage point highlight stopped and original materials restored.");
    }

    public void HandleStopperToolClicked()
    {
        if (!isStopperToolInteractable && (potLeakageUI == null || !potLeakageUI.isStopperToolInteractable)) return;

        isStopperToolInteractable = false;
        isStopperToolConfirmed = true;

        if (potLeakageUI != null)
        {
            potLeakageUI.isStopperToolInteractable = false;
            potLeakageUI.isStopperToolConfirmed = true;
            potLeakageUI.PlaySFX(potLeakageUI.clickAudioClip);
        }

        // 1. Stop any highlight/blink associated with stopper interaction
        // 2. Restore original materials for Stylized_MetalGrid_01_basecolor & plane_divided_DefaultMaterial_BaseColor
        StopLeakagePointHighlight();

        // 3. Disable SIDEREAKING TOOL (6)
        var tool6 = ResolveSideBreakingTool6();
        if (tool6 != null) tool6.SetActive(false);

        // 4. Enable SIDEREAKING TOOL (5) with original materials
        ActivateSideBreakingTool5();

        // 5. Reduce MoltenAluminium_VFX intensity/visual size
        ReduceMoltenLeakageVFX();

        OnInterceptTaskCompletion = null;

        string completeDesc = "The stopper has been applied to the leakage point. Molten metal flow is noticeably reduced.";
        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            if (ui.statusText != null)
            {
                ui.statusText.gameObject.SetActive(true);
                ui.statusText.text = "STOPPER APPLIED — LEAKAGE REDUCED";
            }
            if (ui.descriptionText != null)
            {
                ui.descriptionText.gameObject.SetActive(true);
                ui.descriptionText.text = completeDesc;
            }
            if (ui.nextButton != null) ui.nextButton.gameObject.SetActive(true);
            ui.SpeakDescriptionText(completeDesc);
        }
        else
        {
            TruckTyreReplacement.Core.Manager.Instance?.SpeakText(completeDesc, completeDesc);
        }

        Debug.Log("[PotLeakage] Stopper applied: SIDEREAKING TOOL (6) disabled, SIDEREAKING TOOL (5) enabled with original materials, MoltenAluminium_VFX reduced.");
    }

    public void ReduceMoltenLeakageVFX()
    {
        var vfxGo = GameObject.Find("VFX/MoltenAluminium_VFX") ?? GameObject.Find("MoltenAluminium_VFX");
        if (vfxGo == null)
        {
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var g in all)
            {
                if (g.name == "MoltenAluminium_VFX" && !IsPersistentObject(g))
                {
                    vfxGo = g;
                    break;
                }
            }
        }

        if (vfxGo != null)
        {
            var controller = vfxGo.GetComponent<PotLeakage.VFX.MoltenAluminiumVFXController>();
            if (controller != null)
            {
                controller.SetControlledLeakage(true, 1.0f);
                if (controller.flowMesh != null)
                {
                    controller.flowMesh.SetControlled(true, 1.0f);
                }
                Debug.Log("[PotLeakage] MoltenAluminiumVFXController.SetControlledLeakage(true) called successfully.");
                return;
            }

            var flowMesh = vfxGo.GetComponentInChildren<PotLeakage.VFX.MoltenMetalFlowMesh>(true);
            if (flowMesh != null)
            {
                flowMesh.SetControlled(true, 1.0f);
                Debug.Log("[PotLeakage] MoltenMetalFlowMesh.SetControlled(true) called directly.");
                return;
            }
        }

        var standaloneFlowMesh = UnityEngine.Object.FindFirstObjectByType<PotLeakage.VFX.MoltenMetalFlowMesh>();
        if (standaloneFlowMesh != null)
        {
            standaloneFlowMesh.SetControlled(true, 1.0f);
            Debug.Log("[PotLeakage] Standalone MoltenMetalFlowMesh.SetControlled(true) called directly.");
            return;
        }

        Debug.LogWarning("[PotLeakage] MoltenAluminium_VFX not found to reduce intensity.");
    }

    public void RestoreMoltenLeakageVFX()
    {
        var vfxGo = GameObject.Find("VFX/MoltenAluminium_VFX") ?? GameObject.Find("MoltenAluminium_VFX");
        if (vfxGo == null)
        {
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var g in all)
            {
                if (g.name == "MoltenAluminium_VFX" && !IsPersistentObject(g))
                {
                    vfxGo = g;
                    break;
                }
            }
        }

        if (vfxGo != null)
        {
            var controller = vfxGo.GetComponent<PotLeakage.VFX.MoltenAluminiumVFXController>();
            if (controller != null)
            {
                controller.SetControlledLeakage(false, 0f);
                if (controller.flowMesh != null)
                {
                    controller.flowMesh.SetControlled(false, 0f);
                }
                Debug.Log("[PotLeakage] MoltenAluminiumVFXController.SetControlledLeakage(false) restored.");
                return;
            }

            var flowMesh = vfxGo.GetComponentInChildren<PotLeakage.VFX.MoltenMetalFlowMesh>(true);
            if (flowMesh != null)
            {
                flowMesh.SetControlled(false, 0f);
                Debug.Log("[PotLeakage] MoltenMetalFlowMesh.SetControlled(false) restored directly.");
                return;
            }
        }

        var standaloneFlowMesh = UnityEngine.Object.FindFirstObjectByType<PotLeakage.VFX.MoltenMetalFlowMesh>();
        if (standaloneFlowMesh != null)
        {
            standaloneFlowMesh.SetControlled(false, 0f);
            Debug.Log("[PotLeakage] Standalone MoltenMetalFlowMesh.SetControlled(false) restored directly.");
        }
    }

    public List<Renderer> CollectWalkieTalkieBlinkTargets()
    {
        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            return ui.CollectWalkieTalkieBlinkTargets();
        }
        return new List<Renderer>();
    }

    public void StartEmergencyCommunicationBlink()
    {
        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            ui.StartWalkieTalkieBlink();
        }
    }

    public void StopEmergencyCommunicationBlink()
    {
        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            ui.StopWalkieTalkieBlink();
        }
    }

    public void RestoreWalkieTalkieMaterials()
    {
        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            ui.RestoreWalkieTalkieMaterials();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GAMEOBJECT — SetActive, Move
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>SetActive(true) on a GameObject. Use from UnityEvent.</summary>
    public void SetActive_True(GameObject target) { if (target != null) target.SetActive(true); }

    /// <summary>SetActive(false) on a GameObject. Use from UnityEvent.</summary>
    public void SetActive_False(GameObject target) { if (target != null) target.SetActive(false); }

    /// <summary>Toggle active state of a GameObject.</summary>
    public void ToggleActive(GameObject target) { if (target != null) target.SetActive(!target.activeSelf); }

    /// <summary>
    /// Move a named object to its configured destination.
    /// Configure source+destination pairs in the 'Movement Mappings' Inspector field.
    /// Then call with the matching key from a UnityEvent.
    /// </summary>
    public void MoveObjectToDestination(string mappingKey)
    {
        var m = movementMappings.Find(x => x.key == mappingKey);
        if (m != null && m.sourceObject != null && m.destination != null)
        {
            m.sourceObject.transform.SetPositionAndRotation(m.destination.position, m.destination.rotation);
            Debug.Log($"[SequenceHelperFunctions] Moved '{m.sourceObject.name}' to '{m.destination.name}' via mapping '{mappingKey}'");
        }
        else
        {
            Debug.LogWarning($"[SequenceHelperFunctions] MoveObjectToDestination: mapping '{mappingKey}' not found or has null references.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HIGHLIGHT — Ghost Material Blinking System
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Apply blinking ghost highlight to a GameObject (preserves original materials).</summary>
    public void ApplySafeGhostHighlight(GameObject target)
    {
        if (target == null) return;
        if (transparentMaterial == null)
        {
            Debug.LogError("[SequenceHelperFunctions] HighlightObject: transparentMaterial is null. Assign M_Highlight_FluorescentGreen in Inspector.");
            return;
        }

        if (!activeHighlightedObjects.Contains(target)) activeHighlightedObjects.Add(target);

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        Material defMat = null;
        var airbum = UnityEngine.GameObject.Find("AIRBUM");
        if (airbum != null)
        {
            foreach (Transform child in airbum.transform)
            {
                var rnd = child.GetComponent<MeshRenderer>();
                if (rnd != null && rnd.sharedMaterial != null && !rnd.sharedMaterial.name.Contains("Highlight"))
                {
                    defMat = rnd.sharedMaterial;
                    break;
                }
            }
        }

        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            Material[] currentMats = r.sharedMaterials;
            Material[] cleanMats = new Material[currentMats.Length];

            for (int i = 0; i < currentMats.Length; i++)
            {
                if (currentMats[i] != null && currentMats[i].name.Contains("Highlight"))
                {
                    cleanMats[i] = defMat != null ? defMat : currentMats[i];
                }
                else
                {
                    cleanMats[i] = currentMats[i];
                }
            }

            if (!originalMaterialsDict.ContainsKey(r) || (originalMaterialsDict[r] != null && originalMaterialsDict[r].Length > 0 && originalMaterialsDict[r][0].name.Contains("Highlight")))
            {
                originalMaterialsDict[r] = cleanMats;
            }
        }

        ApplyGhostMaterials(target, true);

        if (blinkCoroutine == null)
            blinkCoroutine = StartCoroutine(BlinkHighlightRoutine());
    }

    /// <summary>Generic alias: highlight any object. Wire from Sequence EventsToFollow.</summary>
    public void HighlightObject(GameObject target) => ApplySafeGhostHighlight(target);

    /// <summary>Remove blinking ghost highlight, restoring original materials.</summary>
    public void RemoveSafeGhostHighlight(GameObject target)
    {
        if (target == null) return;
        activeHighlightedObjects.RemoveAll(x => x == null || x == target || x.name == target.name);

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            if (originalMaterialsDict.ContainsKey(r))
            {
                r.sharedMaterials = originalMaterialsDict[r];
                originalMaterialsDict.Remove(r);
            }
        }

        if (activeHighlightedObjects.Count == 0 && blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
    }

    /// <summary>Generic alias: remove highlight. Wire from Sequence EventsToFollow.</summary>
    public void RemoveHighlight(GameObject target) => RemoveSafeGhostHighlight(target);

    private void ApplyGhostMaterials(GameObject target, bool showGhost)
    {
        if (target == null) return;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            if (showGhost && originalMaterialsDict.ContainsKey(r))
            {
                Material[] ghostMats = new Material[originalMaterialsDict[r].Length];
                for (int i = 0; i < ghostMats.Length; i++) ghostMats[i] = transparentMaterial;
                r.sharedMaterials = ghostMats;
            }
            else if (!showGhost && originalMaterialsDict.ContainsKey(r))
            {
                r.sharedMaterials = originalMaterialsDict[r];
            }
        }
    }

    private IEnumerator BlinkHighlightRoutine()
    {
        while (activeHighlightedObjects.Count > 0)
        {
            yield return new WaitForSeconds(0.5f);
            highlightBlinkState = !highlightBlinkState;
            for (int i = activeHighlightedObjects.Count - 1; i >= 0; i--)
            {
                var obj = activeHighlightedObjects[i];
                if (obj != null && obj.activeInHierarchy)
                    ApplyGhostMaterials(obj, highlightBlinkState);
            }
        }
        blinkCoroutine = null;
    }

    private Material GetCleanDefaultMaterial()
    {
        var airbum = UnityEngine.GameObject.Find("AIRBUM");
        if (airbum != null)
        {
            var sel001 = airbum.transform.Find("select.001");
            if (sel001 != null)
            {
                var r = sel001.GetComponent<MeshRenderer>();
                if (r != null && r.sharedMaterial != null && !r.sharedMaterial.name.Contains("Highlight"))
                    return r.sharedMaterial;
            }
            var screen = airbum.transform.Find("screen");
            if (screen != null)
            {
                var r = screen.GetComponent<MeshRenderer>();
                if (r != null && r.sharedMaterial != null && !r.sharedMaterial.name.Contains("Highlight"))
                    return r.sharedMaterial;
            }
        }

        var nvc = UnityEngine.Object.FindFirstObjectByType<NumericVariableController>();
        if (nvc != null && nvc.defaultSelectMaterial != null && !nvc.defaultSelectMaterial.name.Contains("Highlight"))
            return nvc.defaultSelectMaterial;

        var allRenderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
        foreach (var r in allRenderers)
        {
            if (r != null && r.sharedMaterial != null && r.sharedMaterial.name.Contains("ChatGPT Image"))
                return r.sharedMaterial;
        }

        return null;
    }

    /// <summary>Stop all highlights and restore all original materials.</summary>
    public void ClearAllHighlights()
    {
        if (blinkCoroutine != null) { StopCoroutine(blinkCoroutine); blinkCoroutine = null; }
        var cleanMat = GetCleanDefaultMaterial();
        Debug.Log($"[ClearAllHighlights] cleanMat = '{(cleanMat != null ? cleanMat.name : "null")}', originalMaterialsDict count = {originalMaterialsDict.Count}");

        foreach (var kvp in originalMaterialsDict)
        {
            if (kvp.Key != null)
            {
                if (cleanMat != null)
                {
                    kvp.Key.sharedMaterials = new Material[] { cleanMat };
                    kvp.Key.materials = new Material[] { cleanMat };
                }
            }
        }
        originalMaterialsDict.Clear();
        activeHighlightedObjects.Clear();

        var airbum = UnityEngine.GameObject.Find("AIRBUM");
        if (airbum != null && cleanMat != null)
        {
            var plus = airbum.transform.Find("+_button")?.gameObject;
            var minus = airbum.transform.Find("-_button")?.gameObject;
            var select = airbum.transform.Find("select")?.gameObject;

            if (plus != null && plus.GetComponent<MeshRenderer>() != null)
            {
                plus.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { cleanMat };
                plus.GetComponent<MeshRenderer>().materials = new Material[] { cleanMat };
            }
            if (minus != null && minus.GetComponent<MeshRenderer>() != null)
            {
                minus.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { cleanMat };
                minus.GetComponent<MeshRenderer>().materials = new Material[] { cleanMat };
            }
            if (select != null && select.GetComponent<MeshRenderer>() != null)
            {
                select.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { cleanMat };
                select.GetComponent<MeshRenderer>().materials = new Material[] { cleanMat };
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI — Canvas, Title, Description
    // ─────────────────────────────────────────────────────────────────────────

    public void UIPosChange(Transform t)
    {
        _nextCanvasTransform = t;
        if (UI_Canvas != null)
        {
            UI_Canvas.transform.localPosition = _nextCanvasTransform.localPosition;
            UI_Canvas.transform.localRotation = _nextCanvasTransform.localRotation;
        }
    }

    public void SetTitle(string title)
    {
        titleString = title;
        if (TitleText != null) TitleText.text = titleString;
    }

    public void SetDescription(string des)
    {
        desString = des;
        if (DescriptionText != null) DescriptionText.text = desString;
    }

    /// <summary>Look up display text for key in Manager's translation database, set on UI.</summary>
    public void SetLocalTitle(string key)
    {
        if (Manager.Instance != null)
            SetTitle(Manager.Instance.GetDisplayText(key));
        else
            SetTitle(key);
    }

    /// <summary>Look up display text for key in Manager's translation database, set on UI and hologram.</summary>
    public void SetLocalDescription(string key)
    {
        if (Manager.Instance != null)
        {
            string localizedDes = Manager.Instance.GetDisplayText(key);
            SetDescription(localizedDes);

            // Update hologram anchor panel if present
            var hologram = UnityEngine.Object.FindFirstObjectByType<TruckTyreReplacement.UI.TrainingHologramAnchor>();
            if (hologram != null) hologram.SetDescription(localizedDes);
        }
        else
        {
            SetDescription(key);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AUDIO — Locale-aware playback
    // ─────────────────────────────────────────────────────────────────────────

    public void BGAudioCall(AudioClip clip) { BG_Audio.clip = clip; BG_Audio.Play(); }
    public void BGAudioStop() => BG_Audio?.Stop();
    public void SetBGVolume(float vol) { if (BG_Audio != null) BG_Audio.volume = vol; }
    private void EnsureVoiceAudio()
    {
        if (Voice_Audio == null)
        {
            Voice_Audio = GetComponent<AudioSource>();
            if (Voice_Audio == null) Voice_Audio = gameObject.AddComponent<AudioSource>();
            Voice_Audio.spatialBlend = 0f;
            Voice_Audio.playOnAwake = false;
            Voice_Audio.loop = false;
        }
    }

    public void VoiceOverCall(AudioClip clip)
    {
        if (clip == null) return;
        EnsureVoiceAudio();
        if (Voice_Audio != null)
        {
            Voice_Audio.Stop();
            Voice_Audio.clip = clip;
            Voice_Audio.Play();
        }
    }

    public void PlayVoiceAudioClipWithCallback(AudioClip clip, System.Action onComplete)
    {
        if (clip != null)
        {
            EnsureVoiceAudio();
            StartCoroutine(PlayVoiceClipRoutine(clip, onComplete));
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private IEnumerator PlayVoiceClipRoutine(AudioClip clip, System.Action onComplete)
    {
        EnsureVoiceAudio();
        Voice_Audio.Stop();
        Voice_Audio.clip = clip;
        yield return null;
        Voice_Audio.Play();

        float startWait = Time.realtimeSinceStartup;
        yield return new WaitUntil(() => Voice_Audio.isPlaying || (Time.realtimeSinceStartup - startWait) > 1.0f);

        if (Voice_Audio.isPlaying)
        {
            yield return new WaitUntil(() => !Voice_Audio.isPlaying);
        }
        yield return new WaitForSeconds(0.2f);
        onComplete?.Invoke();
    }

    public void PlayAudio_TriggerOnComplete(AudioClip clip) { if (clip != null) StartCoroutine(PlayAudioWhenReady(clip)); }
    private IEnumerator PlayAudioWhenReady(AudioClip clip)
    {
        EnsureVoiceAudio();
        Voice_Audio.Stop();
        Voice_Audio.clip = clip;
        yield return null;
        Voice_Audio.Play();
        float startWait = Time.realtimeSinceStartup;
        yield return new WaitUntil(() => Voice_Audio.isPlaying || (Time.realtimeSinceStartup - startWait) > 1.0f);
        if (Voice_Audio.isPlaying)
        {
            yield return new WaitUntil(() => !Voice_Audio.isPlaying);
        }
        yield return new WaitForSeconds(0.2f);
        handler.TaskCompleted();
    }

    public void PlayAudio_WithoutNextTask(AudioClip clip) { if (clip != null) StartCoroutine(PlayAudioWhenReady_WithoutNextTask(clip)); }
    private IEnumerator PlayAudioWhenReady_WithoutNextTask(AudioClip clip)
    {
        EnsureVoiceAudio();
        Voice_Audio.Stop();
        Voice_Audio.clip = clip;
        yield return null;
        Voice_Audio.Play();
        float startWait = Time.realtimeSinceStartup;
        yield return new WaitUntil(() => Voice_Audio.isPlaying || (Time.realtimeSinceStartup - startWait) > 1.0f);
        if (Voice_Audio.isPlaying)
        {
            yield return new WaitUntil(() => !Voice_Audio.isPlaying);
        }
        yield return new WaitForSeconds(0.2f);
        handler.CurrentTaskCompleted();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        var mgr = Manager.Instance != null ? Manager.Instance : UnityEngine.Object.FindFirstObjectByType<Manager>();
        if (mgr != null)
            mgr.PlaySFX(clip);
    }

    /// <summary>Play localized audio for key (fire-and-forget, no task completion).</summary>
    public void PlayLocaleAudio(string key)
    {
        var mgr = Manager.Instance != null ? Manager.Instance : UnityEngine.Object.FindFirstObjectByType<Manager>();
        if (mgr != null)
            mgr.Speak(key);
    }

    /// <summary>Play localized audio for key. Calls CompleteCurrentTask() when audio finishes.</summary>
    public void PlayLocaleAudio_TriggerOnComplete(string key)
    {
        StartCoroutine(PlayLocaleAudioRoutine(key, true));
    }

    /// <summary>Play localized audio for key. Marks task completed (no advance) when done.</summary>
    public void PlayLocaleAudio_WithoutNextTask(string key)
    {
        StartCoroutine(PlayLocaleAudioRoutine(key, false));
    }

    private IEnumerator PlayLocaleAudioRoutine(string key, bool triggerNextTask)
    {
        var mgr = Manager.Instance != null ? Manager.Instance : UnityEngine.Object.FindFirstObjectByType<Manager>();

        if (mgr == null)
        {
            // No localization/TTS system available at all - this is not a content
            // (missing/outdated audio) condition, so preserve the original fallback
            // rather than permanently blocking the task.
            Debug.LogWarning("[SequenceHelperFunctions][TTS] Manager instance unavailable — cannot play audio.");
            yield return new WaitForSeconds(1.0f);
            if (triggerNextTask) handler.TaskCompleted();
            else handler.CurrentTaskCompleted();
            yield break;
        }

        Debug.Log($"[SequenceHelperFunctions][TTS] PlayLocaleAudio key='{key}' language={mgr.CurrentLanguage}");
        mgr.Speak(key);

        AudioSource source = mgr.GetVoiceAudioSource();
        Debug.Log($"[SequenceHelperFunctions][TTS] AudioSource: {(source != null ? source.gameObject.name : "NULL")} enabled={source?.enabled} volume={source?.volume}");

        bool audioActuallyCompleted = false;

        if (source != null)
        {
            // Phase 1: wait up to 1s for Manager.IsSpeaking to become true (Speak() is async/queued)
            float waitStart = Time.realtimeSinceStartup;
            yield return new WaitUntil(() => mgr.IsSpeaking || (Time.realtimeSinceStartup - waitStart) > 1.0f);

            // Phase 2: wait up to 2.5s for AudioSource.isPlaying to become true
            waitStart = Time.realtimeSinceStartup;
            yield return new WaitUntil(() => source.isPlaying || (Time.realtimeSinceStartup - waitStart) > 2.5f);

            Debug.Log($"[SequenceHelperFunctions][TTS] isPlaying={source.isPlaying} clip={source.clip?.name} length={source.clip?.length}s");

            if (source.isPlaying)
            {
                // Phase 3: wait for audio to actually finish. This - not the mere
                // absence of isPlaying right after Play(), and not a fixed delay -
                // is the only condition that counts as real playback completion.
                yield return new WaitUntil(() => !source.isPlaying && !mgr.IsSpeaking);
                Debug.Log($"[TTS PLAY COMPLETE]\nKey = {key}\nLanguage = {mgr.CurrentLanguage}");
                audioActuallyCompleted = true;
            }
            else
            {
                // Audio never started - missing/outdated cached WAV, or generation
                // still pending.
                var status = mgr.GetSpeechCacheStatus(key);
                Debug.LogWarning($"[TTS BLOCKED]\nKey = {key}\nLanguage = {mgr.CurrentLanguage}\nReason = {status.ToString().ToUpperInvariant()}");
                yield return new WaitUntil(() => !mgr.IsSpeaking);
            }
        }

        if (!audioActuallyCompleted)
        {
            if (triggerNextTask)
            {
                Debug.LogWarning("[SequenceHelperFunctions][TTS] Audio did not report isPlaying for auto-advancing task. Waiting fallback duration before advancing.");
                yield return new WaitForSeconds(3.0f);
                handler.TaskCompleted();
            }
            yield break;
        }

        yield return new WaitForSeconds(0.3f);

        if (triggerNextTask) handler.TaskCompleted();
        else handler.CurrentTaskCompleted();
    }

    /// <summary>
    /// Executes Task 04 final completion audio sequence:
    /// 1. localized "air_filling" TTS -> wait for finish
    /// 2. "Air Filling Sound" SFX -> wait for finish
    /// 3. localized "complete" TTS -> wait for finish
    /// 4. Complete training task.
    /// </summary>
    public void PlayCompletionSequence_TriggerOnComplete()
    {
        StartCoroutine(CompletionSequenceRoutine());
    }

    private IEnumerator CompletionSequenceRoutine()
    {
        var mgr = Manager.Instance != null ? Manager.Instance : UnityEngine.Object.FindFirstObjectByType<Manager>();
        if (mgr != null) mgr.StopSpeech();

        AudioSource voiceSource = mgr != null ? mgr.GetVoiceAudioSource() : null;
        AudioSource sfxSource   = mgr != null ? mgr.GetSFXAudioSource()   : null;

        // 1. Air Filling Sound SFX (Only play Air Filling Sound on pipe grab, no instruction speech!)
        Debug.Log("[AUDIO FLOW] Air Filling Sound START");
        AudioClip airFillClip = Resources.Load<AudioClip>("Audio/Air Filling Sound");
        if (airFillClip == null) airFillClip = Resources.Load<AudioClip>("Air Filling Sound");
        if (airFillClip == null)
        {
            var nvc = UnityEngine.Object.FindFirstObjectByType<NumericVariableController>(UnityEngine.FindObjectsInactive.Include);
            if (nvc != null && nvc.targetConfirmedSFX != null) airFillClip = nvc.targetConfirmedSFX;
        }

        if (airFillClip != null && mgr != null)
        {
            mgr.PlaySFX(airFillClip);
            if (sfxSource != null)
            {
                float start = Time.realtimeSinceStartup;
                yield return new WaitUntil(() => sfxSource.isPlaying || (Time.realtimeSinceStartup - start) > 0.5f);
                if (sfxSource.isPlaying)
                {
                    yield return new WaitUntil(() => !sfxSource.isPlaying);
                }
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }
        Debug.Log("[AUDIO FLOW] Air Filling Sound COMPLETE");
        yield return new WaitForSeconds(0.3f);

        // 3. complete localized TTS
        Debug.Log("[AUDIO FLOW] complete TTS START");
        if (mgr != null)
        {
            string localizedComplete = mgr.GetDisplayText("complete");
            var anchor = UnityEngine.Object.FindFirstObjectByType<TruckTyreReplacement.UI.TrainingHologramAnchor>();
            if (anchor != null) anchor.SetDescription(localizedComplete);

            mgr.Speak("complete");
        }

        bool completeAudioFinished = false;
        if (voiceSource != null && mgr != null)
        {
            float start = Time.realtimeSinceStartup;
            yield return new WaitUntil(() => mgr.IsSpeaking || (Time.realtimeSinceStartup - start) > 1.0f);
            yield return new WaitUntil(() => voiceSource.isPlaying || (Time.realtimeSinceStartup - start) > 1.5f);
            if (voiceSource.isPlaying)
            {
                yield return new WaitUntil(() => !voiceSource.isPlaying && !mgr.IsSpeaking);
                Debug.Log("[TTS PLAY COMPLETE]\nKey = complete\nLanguage = " + mgr.CurrentLanguage);
                completeAudioFinished = true;
            }
            else
            {
                var status = mgr.GetSpeechCacheStatus("complete");
                Debug.LogWarning($"[TTS BLOCKED]\nKey = complete\nLanguage = {mgr.CurrentLanguage}\nReason = {status.ToString().ToUpperInvariant()}");
                yield return new WaitUntil(() => !mgr.IsSpeaking);
            }
        }
        else
        {
            // No Manager/voice source at all - genuinely different failure mode from
            // missing/outdated content, preserve prior fallback so training doesn't
            // hard-lock with no TTS system present.
            yield return new WaitForSeconds(2.0f);
            completeAudioFinished = true;
        }

        if (!completeAudioFinished)
        {
            // Missing/outdated "complete" audio must not silently finish training.
            yield break;
        }

        Debug.Log("[AUDIO FLOW] complete TTS COMPLETE -> TRAINING COMPLETE");

        if (handler == null) handler = SequenceHandler.instance != null ? SequenceHandler.instance : GetComponent<SequenceHandler>();
        if (handler != null) handler.TaskCompleted();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // INTERACTION — Enable/Disable XR interactables
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Enable XRBaseInteractable and Collider on a GameObject.</summary>
    public void EnableInteractable(GameObject target)
    {
        if (target == null) return;
        var interactable = target.GetComponent<XRBaseInteractable>();
        if (interactable != null) interactable.enabled = true;
        var col = target.GetComponent<Collider>();
        if (col != null) col.enabled = true;
        Debug.Log($"[SequenceHelperFunctions] EnableInteractable: '{target.name}'");
    }

    /// <summary>Disable XRBaseInteractable and Collider on a GameObject.</summary>
    public void DisableInteractable(GameObject target)
    {
        if (target == null) return;
        var interactable = target.GetComponent<XRBaseInteractable>();
        if (interactable != null) interactable.enabled = false;
        var col = target.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    /// <summary>Activate a GrabDetect and XRGrabInteractable on a GameObject.</summary>
    public void EnableGrabObject(GameObject obj)
    {
        if (obj == null) return;

        var interactable = obj.GetComponent<XRBaseInteractable>();
        if (interactable != null) interactable.enabled = true;

        var grab = obj.GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            grab.enabled = true;
            if (grab.interactionManager == null)
            {
                grab.interactionManager = UnityEngine.Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
            }
            var col = obj.GetComponent<Collider>();
            if (col != null && !grab.colliders.Contains(col))
                grab.colliders.Add(col);
        }

        var colObj = obj.GetComponent<Collider>();
        if (colObj != null) colObj.enabled = true;

        if (obj.name == "Curves pipe" || obj.name.Contains("Pipe"))
        {
            EnablePipeInteraction();
        }

        GrabDetect gd = obj.GetComponent<GrabDetect>();
        if (gd != null) gd.ActivateGrab();

        Debug.Log($"[SequenceHelperFunctions] EnableGrabObject: '{obj.name}' activated and enabled for XR grab!");
    }

    /// <summary>Deactivate a GrabDetect on a GameObject.</summary>
    public void DisableGrabObject(GameObject obj)
    {
        if (obj == null) return;
        GrabDetect gd = obj.GetComponent<GrabDetect>();
        if (gd != null) gd.DeactivateGrab();
    }

    /// <summary>
    /// Enables Curves pipe interaction and positions player for air filling step.
    /// Wire from Sequence EventsToFollow.
    /// </summary>
    public void EnablePipeInteraction()
    {
        var pipe = GameObject.Find("Curves pipe");
        if (pipe == null)
        {
            var allGOs = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var g in allGOs) if (g.name == "Curves pipe") { pipe = g; break; }
        }

        if (pipe != null)
        {
            pipe.SetActive(true);

            var airfillPoint = GameObject.Find("AirfillPoint");
            if (airfillPoint != null)
            {
                var mgr = Manager.Instance != null ? Manager.Instance : UnityEngine.Object.FindFirstObjectByType<Manager>();
                if (mgr != null) mgr.MovePlayerTo(airfillPoint.transform);
            }

            var grab = pipe.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grab == null) grab = pipe.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

            if (grab != null)
            {
                grab.enabled = true;
                grab.interactionLayers = -1;
                grab.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Instantaneous;
                grab.trackPosition = true;
                grab.trackRotation = true;
                grab.throwOnDetach = false;

                if (grab.interactionManager == null)
                {
                    grab.interactionManager = UnityEngine.Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
                }

                var col = pipe.GetComponent<Collider>();
                if (col != null && !grab.colliders.Contains(col))
                {
                    col.enabled = true;
                    grab.colliders.Add(col);
                }
            }

            var colBox = pipe.GetComponent<BoxCollider>();
            if (colBox != null)
            {
                colBox.enabled = true;
                colBox.isTrigger = false;
                colBox.size = new Vector3(0.005f, 0.015f, 0.01f);
                colBox.center = Vector3.zero;
            }

            var rb = pipe.GetComponent<Rigidbody>();
            if (rb == null) rb = pipe.AddComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            GrabDetect gd = pipe.GetComponent<GrabDetect>();
            if (gd == null) gd = pipe.AddComponent<GrabDetect>();
            gd.ActivateGrab();

            ApplySafeGhostHighlight(pipe);
            Debug.Log("[SequenceHelperFunctions] EnablePipeInteraction: Curves pipe activated and highlighted.");
        }
    }

    /// <summary>Called by GrabDetect when Curves pipe is grabbed. Executes air filling routine and completes task.</summary>
    public void OnPipeGrabbed()
    {
        StartCoroutine(PipeGrabCompletionRoutine());
    }

    private IEnumerator PipeGrabCompletionRoutine()
    {
        var mgr = Manager.Instance != null ? Manager.Instance : UnityEngine.Object.FindFirstObjectByType<Manager>();
        if (mgr != null) mgr.StopSpeech();

        var pipe = GameObject.Find("Curves pipe");
        if (pipe != null)
        {
            RemoveSafeGhostHighlight(pipe);
            pipe.SetActive(false);
        }

        var allGOs = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var g in allGOs)
        {
            if (g.name == "pipe 2")
            {
                g.SetActive(true);
                break;
            }
        }

        Debug.Log("[AUDIO FLOW] Air Filling Sound START");
        AudioClip airFillClip = Resources.Load<AudioClip>("Audio/Air Filling Sound");
        if (airFillClip == null) airFillClip = Resources.Load<AudioClip>("Air Filling Sound");

        AudioSource sfxSource = mgr != null ? mgr.GetSFXAudioSource() : null;
        if (airFillClip != null && mgr != null)
        {
            mgr.PlaySFX(airFillClip);
            if (sfxSource != null)
            {
                float start = Time.realtimeSinceStartup;
                yield return new WaitUntil(() => sfxSource.isPlaying || (Time.realtimeSinceStartup - start) > 0.5f);
                if (sfxSource.isPlaying)
                {
                    yield return new WaitUntil(() => !sfxSource.isPlaying);
                }
            }
            else
            {
                yield return new WaitForSeconds(airFillClip.length);
            }
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }
        Debug.Log("[AUDIO FLOW] Air Filling Sound COMPLETE");
        yield return new WaitForSeconds(0.3f);

        CompleteCurrentTask();
    }

    /// <summary>Called by GrabDetect when an object is grabbed. Completes current task.</summary>
    public void OnObjectGrabbed()
    {
        if (handler == null) handler = SequenceHandler.instance != null ? SequenceHandler.instance : GetComponent<SequenceHandler>();
        if (handler != null)
        {
            Debug.Log("[SequenceHelperFunctions] OnObjectGrabbed -> CompleteCurrentTask");
            handler.TaskCompleted();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ANIMATION
    // ─────────────────────────────────────────────────────────────────────────

    public void PlayAnimation_TriggerOnComplete(int animIndex)
    {
        if (animIndex >= 0 && animIndex < sequenceAnimations.Count)
            StartCoroutine(AnimationRoutine(sequenceAnimations[animIndex], true));
    }

    public void PlayAnimation_WithoutNextTask(int animIndex)
    {
        if (animIndex >= 0 && animIndex < sequenceAnimations.Count)
            StartCoroutine(AnimationRoutine(sequenceAnimations[animIndex], false));
    }

    private IEnumerator AnimationRoutine(SequenceAnimation animData, bool completeTaskAndMoveNext)
    {
        if (animData.targetAnimator != null && !string.IsNullOrEmpty(animData.triggerName))
            animData.targetAnimator.SetTrigger(animData.triggerName);
        yield return new WaitForSeconds(animData.animationDuration);
        if (completeTaskAndMoveNext) handler.TaskCompleted();
        else handler.CurrentTaskCompleted();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FADE
    // ─────────────────────────────────────────────────────────────────────────

    public void FadeIn() => StartCoroutine(FadeTo(1f));
    public void FadeOut() => StartCoroutine(FadeTo(0f));
    public void TaskComplete_WithFade() => StartCoroutine(FadeTo(1f, () => handler.TaskCompleted()));
    public void NextSequence_WithFade() => StartCoroutine(FadeTo(1f, () => handler.NextSequence()));
    public void PreviousSequence_WithFade() => StartCoroutine(FadeTo(1f, () => handler.PreviousSequence()));
    public void ReloadSequence_WithFade() => StartCoroutine(FadeTo(1f, () => handler.ReloadSequence()));
    public void SelectSequence_WithFade(int n) => StartCoroutine(FadeTo(1f, () => handler.SequenceSelect(n)));

    private IEnumerator FadeTo(float targetAlpha, Action action = null)
    {
        if (fadeMaterial == null) { action?.Invoke(); yield break; }
        Color color = fadeMaterial.color;
        float startAlpha = color.a; float time = 0f;
        while (time < fadeDuration)
        {
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            fadeMaterial.color = new Color(color.r, color.g, color.b, alpha);
            time += Time.deltaTime; yield return null;
        }
        fadeMaterial.color = new Color(color.r, color.g, color.b, targetAlpha);
        action?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PPE Logic
    // ─────────────────────────────────────────────────────────────────────────

    public void RegisterPPE(GameObject ppeObj)
    {
        if (!ppeInitialPositions.ContainsKey(ppeObj))
        {
            ppeInitialPositions.Add(ppeObj, ppeObj.transform.position);
            ppeInitialRotations.Add(ppeObj, ppeObj.transform.rotation);
        }
    }

    public void SetPPEZoneStatus(bool inside) => isInsidePPEZone = inside;

    public void HandlePPERelease(SelectExitEventArgs args)
    {
        GameObject ppeObj = args.interactableObject.transform.gameObject;
        if (isInsidePPEZone)
        {
            if (ppeObjectToEnable != null) ppeObjectToEnable.SetActive(true);
            StopTransparentEffect();
            handler.TaskCompleted();
            ppeObj.SetActive(false);
        }
        else
        {
            if (ppeInitialPositions.ContainsKey(ppeObj))
            {
                ppeObj.transform.position = ppeInitialPositions[ppeObj];
                ppeObj.transform.rotation = ppeInitialRotations[ppeObj];
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Multi-Object Grab & Snap Logic
    // ─────────────────────────────────────────────────────────────────────────

    public void RegisterAllMultiGrabObjects()
    {
        foreach (var mapping in multiGrabMappings)
        {
            if (mapping.GrabbableObject != null)
            {
                mapping.InitialPosition = mapping.GrabbableObject.transform.position;
                mapping.InitialRotation = mapping.GrabbableObject.transform.rotation;
                mapping.IsInsideTargetCollider = false;
            }
        }
    }

    public void OnMultiObjectGrabbed(SelectEnterEventArgs args)
    {
        GameObject grabbedObj = args.interactableObject.transform.gameObject;
        GrabSnapMapping mapping = multiGrabMappings.Find(m => m.GrabbableObject == grabbedObj);
        if (mapping != null && mapping.GhostTargetObject != null)
        {
            mapping.GhostTargetObject.SetActive(true);
            ApplySafeGhostHighlight(mapping.GhostTargetObject);
        }
    }

    public void SetMultiDropZoneStatus(GameObject grabbedObj, bool isInside)
    {
        GrabSnapMapping mapping = multiGrabMappings.Find(m => m.GrabbableObject == grabbedObj);
        if (mapping != null) mapping.IsInsideTargetCollider = isInside;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Transparent Effect (Legacy)
    // ─────────────────────────────────────────────────────────────────────────

    public void OnObjectPlaced()
    {
        handler.TaskCompleted();
        StopTransparentEffect();
    }

    public void TransParentEffect(GameObject targetObject)
    {
        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            if (transparencyCoroutine != null) StopCoroutine(transparencyCoroutine);
            transparencyCoroutine = StartCoroutine(DelayedTransparentEffect(renderers, 0.1f));
        }
    }

    private IEnumerator DelayedTransparentEffect(Renderer[] renderers, float delay)
    {
        yield return new WaitForSeconds(delay);
        _isTransparent = true;
        yield return StartCoroutine(ApplyEffect(renderers));
    }

    public void StopTransparentEffect() => _isTransparent = false;

    private IEnumerator ApplyEffect(Renderer[] renderers)
    {
        Material[][] originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].materials;
            Material[] ghostMaterials = new Material[originalMaterials[i].Length];
            for (int j = 0; j < ghostMaterials.Length; j++) ghostMaterials[j] = transparentMaterial;
            renderers[i].materials = ghostMaterials;
        }
        while (_isTransparent) yield return null;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].materials = originalMaterials[i];
        transparencyCoroutine = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Button Utilities
    // ─────────────────────────────────────────────────────────────────────────

    public void ButtonOnIsSelected(Button button)
    {
        Color color;
        ColorUtility.TryParseHtmlString("#56BA1F", out color);
        button.image.color = color;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Switch Logic
    // ─────────────────────────────────────────────────────────────────────────

    public void EnableSwitch(GameObject switchObj) { }
    public void OnSwitchToggled(bool state) => handler.TaskCompleted();
    public void ExecuteSwitchToggle()
    {
        if (currentTarget != null)
        {
            SwitchController sc = currentTarget.GetComponent<SwitchController>();
            if (sc != null) sc.ToggleSwitch();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Universal Ray Interactions
    // ─────────────────────────────────────────────────────────────────────────

    public void SetCurrentTarget(HoverEnterEventArgs args) => currentTarget = args.interactableObject.transform.gameObject;
    public void ClearCurrentTarget(HoverExitEventArgs args) { if (currentTarget == args.interactableObject.transform.gameObject) currentTarget = null; }
    public void ProcessTriggerInteraction(SelectEnterEventArgs args)
    {
        GameObject hitObj = args.interactableObject.transform.gameObject;
        SwitchController switchObj = hitObj.GetComponent<SwitchController>();
        if (switchObj == null) switchObj = hitObj.GetComponentInParent<SwitchController>();
        if (switchObj != null) switchObj.ToggleSwitch();
    }

    public void ApplySequenceOnHover(GameObject obj) { if (UI_Canvas != null) UI_Canvas.SetActive(true); }

    // ─────────────────────────────────────────────────────────────────────────
    // Misc / Legacy
    // ─────────────────────────────────────────────────────────────────────────

    public void ExitApp() { PlayerPrefs.SetInt("Module", 0); Application.Quit(); }
    public void LoadScene(string a) => SceneManager.LoadScene(a);
    public void WaittoTaskComplete(float time) => StartCoroutine(TaskCompletetime(time));
    IEnumerator TaskCompletetime(float time) { yield return new WaitForSeconds(time); handler.TaskCompleted(); }
}


[RequireComponent(typeof(Collider))]
public class DropZoneTrigger : MonoBehaviour
{
    public GameObject ExpectedGrabbableObject;

    private void Start() { GetComponent<Collider>().isTrigger = true; }

    private void OnTriggerEnter(Collider other)
    {
        GameObject hitObj = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (hitObj == ExpectedGrabbableObject && SequenceHelperFunctions.instance != null)
        {
            SequenceHelperFunctions.instance.SetMultiDropZoneStatus(ExpectedGrabbableObject, true);
            XRGrabInteractable grab = ExpectedGrabbableObject.GetComponent<XRGrabInteractable>();
            if (grab != null && grab.isSelected)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        GameObject hitObj = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (hitObj == ExpectedGrabbableObject && SequenceHelperFunctions.instance != null)
            SequenceHelperFunctions.instance.SetMultiDropZoneStatus(ExpectedGrabbableObject, false);
    }
}
