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
using PotLeakage.VFX;

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
        cube051InitialLocalPosition = new Vector3(-6.80000019f, 5.96000004f, -0.389999986f);
        cube051TargetLocalPosition = new Vector3(-6.80000019f, 1.23000002f, -0.389999986f);
        if (activeHighlightedObjects == null) activeHighlightedObjects = new List<GameObject>();
        if (originalMaterialsDict == null) originalMaterialsDict = new Dictionary<Renderer, Material[]>();
        activeHighlightedObjects.Clear();
        originalMaterialsDict.Clear();
    }

    private void OnEnable()
    {
        instance = this;
        cube051InitialLocalPosition = new Vector3(-6.80000019f, 5.96000004f, -0.389999986f);
        cube051TargetLocalPosition = new Vector3(-6.80000019f, 1.23000002f, -0.389999986f);
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
    public static bool disableDebounceForTesting = false;

    public void ResetDebounce() => lastCompletedFrame = -1;

    /// <summary>
    /// Generic decoupled interceptor delegate for active interactive presentations.
    /// When assigned and returning true, standard task completion is deferred until the presentation finishes.
    /// </summary>
    public static System.Func<bool> OnInterceptTaskCompletion;

    public static int voicePlaybackVersion = 0;

    public void StopAllVoicesAndInvalidate()
    {
        voicePlaybackVersion++;
        if (ptmCraneAnimationCoroutine != null)
        {
            if (Application.isPlaying) StopCoroutine(ptmCraneAnimationCoroutine);
            ptmCraneAnimationCoroutine = null;
        }
        TruckTyreReplacement.Core.Manager.Instance?.StopSpeech();
        if (Voice_Audio != null)
        {
            Voice_Audio.Stop();
            Voice_Audio.clip = null;
        }
        Debug.Log($"[SequenceHelperFunctions] StopAllVoicesAndInvalidate: voicePlaybackVersion={voicePlaybackVersion}");
    }

    /// <summary>Complete the current task. Wire from OnCompleted, OnTargetReached, OnGrabbed, etc.</summary>
    public void CompleteCurrentTask()
    {
        if (!disableDebounceForTesting && Application.isPlaying && Time.frameCount == lastCompletedFrame && Time.frameCount > 0)
        {
            Debug.LogWarning($"[SequenceHelperFunctions] Debounce: CompleteCurrentTask already called in frame {Time.frameCount}. Ignoring duplicate.");
            return;
        }
        lastCompletedFrame = Time.frameCount;

        // STOP CURRENT VOICE IMMEDIATELY & INVALIDATE PREVIOUS CALLBACKS
        StopAllVoicesAndInvalidate();

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

            ui.SpeakDescriptionText(desc, "TASK_05_POT_CONTROL_OBSERVATION", "Technical_Mark");
        }
        else
        {
            Manager.Instance?.SpeakText(desc, "TASK_05_POT_CONTROL_OBSERVATION", "Technical_Mark");
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

            ui.SpeakDescriptionText(desc, "TASK_07_BEAM_SCALE_VERIFICATION", "Technical_Mark");
        }
        else
        {
            Manager.Instance?.SpeakText(desc, "TASK_07_BEAM_SCALE_VERIFICATION", "Technical_Mark");
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

        if (!disableDebounceForTesting && Application.isPlaying && ((Time.frameCount == lastAnodeDownClickFrame && Time.frameCount > 0) || (Time.time - lastAnodeDownClickTime < 0.15f))) return;
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

            ui.SpeakDescriptionText(safeMsg, "TASK_08_SAFE_VOLTAGE", "Technical_Mark");
        }
        else
        {
            Manager.Instance?.SpeakText(safeMsg, "TASK_08_SAFE_VOLTAGE", "Technical_Mark");
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

    [Header("Side Breaking Tool (5) & PIPES Highlight State")]
    public bool isSideBreakingTool5Interactable = false;
    public bool hasClickedSideBreakingTool5 = false;
    public bool isPipesHighlightInteractable = false;
    public bool hasClickedPipesHighlight = false;
    public GameObject pipesHighlightTarget;
    [Tooltip("SFX played once when SIDEREAKING TOOL (5) is enabled (thud.mp3)")]
    public AudioClip thudAudioClip;
    private Coroutine sideBreakingTool5BlinkCoroutine;
    private Coroutine pipesHighlightBlinkCoroutine;
    private readonly List<RendererSlotHighlight> sideBreakingTool5HighlightSlots = new List<RendererSlotHighlight>();
    private readonly List<RendererSlotHighlight> pipesHighlightSlots = new List<RendererSlotHighlight>();

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
            glovesTarget.SetActive(false);
            var col = glovesTarget.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            var interaction = glovesTarget.GetComponent<SideBreakingToolInteraction>();
            if (interaction != null) interaction.enabled = false;
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
            interaction.role = SideBreakingToolInteraction.ToolRole.SideBreakingTool5;
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

    public GameObject ResolvePipesHighlightTarget()
    {
        if (pipesHighlightTarget == null)
        {
            var p = GameObject.Find("PIPES Highlight");
            if (p == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "PIPES Highlight" && !IsPersistentObject(g))
                    {
                        p = g;
                        break;
                    }
                }
            }
            pipesHighlightTarget = p;
        }

        if (pipesHighlightTarget != null)
        {
            var col = pipesHighlightTarget.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = pipesHighlightTarget.AddComponent<BoxCollider>();
                col.center = new Vector3(-0.01f, 0.05f, 0.09f);
                col.size = new Vector3(0.55f, 0.35f, 1.25f);
            }
            col.enabled = true;

            var interaction = pipesHighlightTarget.GetComponent<SideBreakingToolInteraction>();
            if (interaction == null) interaction = pipesHighlightTarget.AddComponent<SideBreakingToolInteraction>();
            interaction.role = SideBreakingToolInteraction.ToolRole.PipesHighlight;
            interaction.uiController = ResolvePotLeakageUI();
        }

        return pipesHighlightTarget;
    }

    /// <summary>
    /// Safety reset for tools (4), (5), and (6), and PIPES Highlight.
    /// Guarantees that neither tool remains active during opening stages (Welcome, Normal Pot Operation,
    /// Normal Pot / Leakage, Pot Control Machine, Verify Beam Level) or from previous test runs.
    /// Does NOT touch SIDEREAKING TOOL (1), (2), or (3).
    /// </summary>
    public void ResetOpeningToolStates()
    {
        var s4 = ResolveSideBreakingTool4();
        if (s4 != null) s4.SetActive(false);

        StopSideBreakingTool5Blink();
        var s5 = ResolveSideBreakingTool5();
        if (s5 != null)
        {
            RestoreSideBreakingTool5OriginalMaterials();
            s5.SetActive(false);
        }

        var s6 = ResolveSideBreakingTool6();
        if (s6 != null) s6.SetActive(false);

        var pipesH = ResolvePipesHighlightTarget();
        if (pipesH != null) pipesH.SetActive(false);
        StopPipesHighlightBlink();

        isStopperToolInteractable = false;
        isStopperToolConfirmed = false;
        isSideBreakingTool5Interactable = false;
        hasClickedSideBreakingTool5 = false;
        isPipesHighlightInteractable = false;
        hasClickedPipesHighlight = false;

        var g = ResolveGlovesTarget();
        if (g != null) g.SetActive(false);
        isGlovesInteractable = false;
        isGlovesConfirmed = true;

        RestoreMoltenLeakageVFX();
        DeactivateCoolingPipes();
        DeactivatePipes1();
        ResetPTMCraneState();
    }

    private GameObject pipes1Target;
    public GameObject ResolvePipes1Target()
    {
        if (pipes1Target == null)
        {
            var p = GameObject.Find("PIPES (1)");
            if (p == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "PIPES (1)" && !IsPersistentObject(g))
                    {
                        p = g;
                        break;
                    }
                }
            }
            pipes1Target = p;
        }
        return pipes1Target;
    }

    public void ActivatePipes1()
    {
        var p = ResolvePipes1Target();
        if (p != null)
        {
            p.SetActive(true);
            var ctrl = p.GetComponent<PotLeakage.VFX.CoolingPipesController>();
            if (ctrl != null)
            {
                ctrl.ActivateCoolingPipes();
            }
            var psList = p.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in psList)
            {
                if (ps != null)
                {
                    if (!ps.gameObject.activeSelf) ps.gameObject.SetActive(true);
                    if (!ps.isPlaying) ps.Play(true);
                }
            }
            Debug.Log("[SequenceHelperFunctions] PIPES (1) activated and ParticleSystem(s) playing.");
        }
    }

    public void DeactivatePipes1()
    {
        var p = ResolvePipes1Target();
        if (p != null)
        {
            var ctrl = p.GetComponent<PotLeakage.VFX.CoolingPipesController>();
            if (ctrl != null)
            {
                ctrl.DeactivateCoolingPipes();
            }
            else
            {
                var psList = p.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in psList)
                {
                    if (ps != null && ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                p.SetActive(false);
            }
        }
    }

    private GameObject pipes2Target;
    public GameObject ResolvePipes2Target()
    {
        if (pipes2Target == null)
        {
            var p = GameObject.Find("PIPES (2)");
            if (p == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "PIPES (2)" && !IsPersistentObject(g))
                    {
                        p = g;
                        break;
                    }
                }
            }
            pipes2Target = p;
        }
        if (pipes2Target != null)
        {
            var p2Hi = pipes2Target.GetComponent<PotLeakage.Interaction.Pipes2Interaction>();
            if (p2Hi == null) p2Hi = pipes2Target.AddComponent<PotLeakage.Interaction.Pipes2Interaction>();
        }
        return pipes2Target;
    }


    public GameObject cube051Target;
    public Vector3 cube051InitialLocalPosition = new Vector3(-6.80000019f, 5.96000004f, -0.389999986f);
    public Vector3 cube051TargetLocalPosition = new Vector3(-6.80000019f, 1.23000002f, -0.389999986f);

    public GameObject ResolveCube051Target()
    {
        if (cube051Target == null)
        {
            var c = GameObject.Find("Cube.051");
            if (c == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "Cube.051" && !IsPersistentObject(g))
                    {
                        c = g;
                        break;
                    }
                }
            }
            cube051Target = c;
        }
        return cube051Target;
    }

    public Vector3 plane058ClosedLocalPosition = new Vector3(-6.11000013f, 1.21775293f, -76.3903198f);
    public Vector3 plane058OpenLocalPosition   = new Vector3(-5.19199991f, 1.21775293f, -76.3903198f);
    public Vector3 plane057ClosedLocalPosition = new Vector3(-6.92999983f, 1.21775293f, -76.3903198f);
    public Vector3 plane057OpenLocalPosition   = new Vector3(-7.81400013f, 1.21775293f, -76.3903198f);

    public Vector3 plane058FinalCloseStartLocalPosition  = new Vector3(-6.08199978f, 1.21775293f, -76.3903198f);
    public Vector3 plane058FinalCloseTargetLocalPosition = new Vector3(-6.07700014f, 1.21775293f, -76.3903198f);
    public Vector3 plane057FinalCloseStartLocalPosition  = new Vector3(-6.92999983f, 1.21775293f, -76.3903198f);
    public Vector3 plane057FinalCloseTargetLocalPosition = new Vector3(-6.90399981f, 1.21775293f, -76.3903198f);

    public int task03SubStage = 0;
    public bool isHoodRemovalCompleted = false;
    public bool isFinalHoodClosingCompleted = false;

    private GameObject plane057Target;
    public GameObject ResolvePlane057Target()
    {
        if (plane057Target == null)
        {
            var p = GameObject.Find("LINE/line (6)/Plane.057") ?? GameObject.Find("line (6)/Plane.057");
            if (p == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "Plane.057" && g.transform.parent != null && g.transform.parent.name.Contains("(6)"))
                    {
                        p = g;
                        break;
                    }
                }
            }
            plane057Target = p;
        }
        return plane057Target;
    }

    private GameObject plane058Target;
    public GameObject ResolvePlane058Target()
    {
        if (plane058Target == null)
        {
            var p = GameObject.Find("LINE/line (6)/Plane.058") ?? GameObject.Find("line (6)/Plane.058");
            if (p == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "Plane.058" && g.transform.parent != null && g.transform.parent.name.Contains("(6)"))
                    {
                        p = g;
                        break;
                    }
                }
            }
            plane058Target = p;
        }
        return plane058Target;
    }

    public PotLeakage.Interaction.HoodPanelInteraction ResolvePlane057Interaction()
    {
        var p57 = ResolvePlane057Target();
        if (p57 == null) return null;
        var hi = p57.GetComponent<PotLeakage.Interaction.HoodPanelInteraction>();
        if (hi == null) hi = p57.AddComponent<PotLeakage.Interaction.HoodPanelInteraction>();
        hi.InitializeComponents();
        return hi;
    }

    public PotLeakage.Interaction.HoodPanelInteraction ResolvePlane058Interaction()
    {
        var p58 = ResolvePlane058Target();
        if (p58 == null) return null;
        var hi = p58.GetComponent<PotLeakage.Interaction.HoodPanelInteraction>();
        if (hi == null) hi = p58.AddComponent<PotLeakage.Interaction.HoodPanelInteraction>();
        hi.InitializeComponents();
        return hi;
    }

    public void EnsureHoodPanelsOpen()
    {
        var p58 = ResolvePlane058Target();
        if (p58 != null)
        {
            var hi58 = p58.GetComponent<PotLeakage.Interaction.HoodPanelInteraction>();
            if (hi58 != null)
            {
                hi58.RestoreOriginalMaterial();
                hi58.SetHighlight(false);
            }
            p58.transform.localPosition = plane058OpenLocalPosition;
        }

        var p57 = ResolvePlane057Target();
        if (p57 != null)
        {
            var hi57 = p57.GetComponent<PotLeakage.Interaction.HoodPanelInteraction>();
            if (hi57 != null)
            {
                hi57.RestoreOriginalMaterial();
                hi57.SetHighlight(false);
            }
            p57.transform.localPosition = plane057OpenLocalPosition;
        }
    }

    public void ResetHoodPanels()
    {
        task03SubStage = 0;
        isHoodRemovalCompleted = false;
        isFinalHoodClosingCompleted = false;

        var p58 = ResolvePlane058Target();
        if (p58 != null)
        {
            var hi58 = p58.GetComponent<PotLeakage.Interaction.HoodPanelInteraction>();
            if (hi58 != null)
            {
                hi58.RestoreOriginalMaterial();
                hi58.SetHighlight(false);
            }
            p58.transform.localPosition = plane058ClosedLocalPosition;
        }

        var p57 = ResolvePlane057Target();
        if (p57 != null)
        {
            var hi57 = p57.GetComponent<PotLeakage.Interaction.HoodPanelInteraction>();
            if (hi57 != null)
            {
                hi57.RestoreOriginalMaterial();
                hi57.SetHighlight(false);
            }
            p57.transform.localPosition = plane057ClosedLocalPosition;
        }
    }

    public void OnTask03Started()
    {
        task03SubStage = 0;
        isHoodRemovalCompleted = false;
        OnInterceptTaskCompletion = HandleTask03ProgressionIntercept;
        ResetHoodPanels();
        Debug.Log("[SequenceHelperFunctions] OnTask03Started: Task 03 interceptor attached, subStage=0");
    }

    public bool HandleTask03ProgressionIntercept()
    {
        ResolveHandler();
        if (handler != null && handler.currentTask != 2)
        {
            task03SubStage = 0;
            OnInterceptTaskCompletion = null;
            return false;
        }

        if (task03SubStage == 0)
        {
            // Trainee clicked NEXT on "Pot leakage has occurred in Pot 69." -> Start manual hood sliding stage
            StartHoodSlidingInteraction();
            return true;
        }
        else if (task03SubStage == 1)
        {
            Debug.Log("[SequenceHelperFunctions] Task 03: Trainee must click Plane.058 (Right hood panel).");
            return true;
        }
        else if (task03SubStage == 2)
        {
            Debug.Log("[SequenceHelperFunctions] Task 03: Trainee must click Plane.057 (Left hood panel).");
            return true;
        }
        else if (task03SubStage >= 3)
        {
            if (isHoodRemovalCompleted)
            {
                task03SubStage = 0;
                OnInterceptTaskCompletion = null;
                return false;
            }
            return true;
        }

        return false;
    }

    public void StartHoodRemovalInteraction() => StartHoodSlidingInteraction();

    public void StartHoodSlidingInteraction()
    {
        task03SubStage = 1;
        isHoodRemovalCompleted = false;

        // Ensure panels start in exact closed positions
        var p58 = ResolvePlane058Target();
        if (p58 != null) p58.transform.localPosition = plane058ClosedLocalPosition;
        var p57 = ResolvePlane057Target();
        if (p57 != null) p57.transform.localPosition = plane057ClosedLocalPosition;

        UpdateUI(SlideHoodDescriptionText, SlideHoodStatusText, 3, 10, "TASK_03_SLIDE_HOOD", "Technical_Mark");

        var hi58 = ResolvePlane058Interaction();
        if (hi58 != null)
        {
            hi58.OnClicked = OnPlane058SlideClicked;
            hi58.SetHighlight(true);
        }

        var hi57 = ResolvePlane057Interaction();
        if (hi57 != null)
        {
            hi57.SetHighlight(false);
            hi57.RestoreOriginalMaterial();
        }

        Debug.Log("[SequenceHelperFunctions] StartHoodSlidingInteraction: Plane.058 highlighted yellow. Waiting for click to slide open.");
    }

    public void OnPlane058SlideClicked()
    {
        var hi58 = ResolvePlane058Interaction();
        if (hi58 != null)
        {
            hi58.OnClicked = null;
            hi58.SetHighlight(false);
            hi58.RestoreOriginalMaterial();
            hi58.MoveSmoothly(plane058OpenLocalPosition, 1.2f, () =>
            {
                Debug.Log("[SequenceHelperFunctions] Plane.058 slid open to target position. Starting Plane.057 slide interaction.");
                StartPlane057Slide();
            });
        }
        else
        {
            var p58 = ResolvePlane058Target();
            if (p58 != null) p58.transform.localPosition = plane058OpenLocalPosition;
            StartPlane057Slide();
        }
    }

    public void OnPlane058RemovalClicked() => OnPlane058SlideClicked();
    public void OnPlane058SlideOutClicked() => OnPlane058SlideClicked();
    public void OnPlane058SlideBackClicked() => OnPlane058SlideClicked();

    public void StartPlane057Removal() => StartPlane057Slide();

    public void StartPlane057Slide()
    {
        task03SubStage = 2;

        var hi57 = ResolvePlane057Interaction();
        if (hi57 != null)
        {
            hi57.OnClicked = OnPlane057SlideClicked;
            hi57.SetHighlight(true);
        }
        Debug.Log("[SequenceHelperFunctions] StartPlane057Slide: Plane.057 highlighted yellow. Waiting for click to slide open.");
    }

    public void OnPlane057SlideClicked()
    {
        var hi57 = ResolvePlane057Interaction();
        if (hi57 != null)
        {
            hi57.OnClicked = null;
            hi57.SetHighlight(false);
            hi57.RestoreOriginalMaterial();
            hi57.MoveSmoothly(plane057OpenLocalPosition, 1.2f, () =>
            {
                Debug.Log("[SequenceHelperFunctions] Plane.057 slid open to target position. Hood sliding completed.");
                task03SubStage = 3;
                isHoodRemovalCompleted = true;
            });
        }
        else
        {
            var p57 = ResolvePlane057Target();
            if (p57 != null) p57.transform.localPosition = plane057OpenLocalPosition;
            task03SubStage = 3;
            isHoodRemovalCompleted = true;
        }
    }

    public void OnPlane057RemovalClicked() => OnPlane057SlideClicked();
    public void OnPlane057SlideOutClicked() => OnPlane057SlideClicked();
    public void OnPlane057SlideBackClicked() => OnPlane057SlideClicked();

    public void StartHoodClosingInteraction()
    {
        isFinalHoodClosingCompleted = false;

        var p58 = ResolvePlane058Target();
        if (p58 != null)
        {
            p58.transform.localPosition = plane058FinalCloseStartLocalPosition;
        }

        var p57 = ResolvePlane057Target();
        if (p57 != null)
        {
            p57.transform.localPosition = plane057FinalCloseStartLocalPosition;
        }

        var hi58 = ResolvePlane058Interaction();
        if (hi58 != null)
        {
            hi58.OnClicked = OnPlane058ClosingClicked;
            hi58.SetHighlight(true);
        }

        var hi57 = ResolvePlane057Interaction();
        if (hi57 != null)
        {
            hi57.SetHighlight(false);
            hi57.RestoreOriginalMaterial();
        }

        Debug.Log("[SequenceHelperFunctions] StartHoodClosingInteraction: Plane.058 highlighted yellow. Waiting for click to close.");
    }

    public void OnPlane058ClosingClicked()
    {
        var hi58 = ResolvePlane058Interaction();
        if (hi58 != null)
        {
            hi58.OnClicked = null;
            hi58.SetHighlight(false);
            hi58.RestoreOriginalMaterial();
            hi58.MoveSmoothly(plane058FinalCloseTargetLocalPosition, 1.2f, () =>
            {
                Debug.Log("[SequenceHelperFunctions] Plane.058 closed to target position.");
                StartPlane057Closing();
            });
        }
        else
        {
            var p58 = ResolvePlane058Target();
            if (p58 != null) p58.transform.localPosition = plane058FinalCloseTargetLocalPosition;
            StartPlane057Closing();
        }
    }

    public void StartPlane057Closing()
    {
        var hi57 = ResolvePlane057Interaction();
        if (hi57 != null)
        {
            hi57.OnClicked = OnPlane057ClosingClicked;
            hi57.SetHighlight(true);
        }
        Debug.Log("[SequenceHelperFunctions] StartPlane057Closing: Plane.057 highlighted yellow. Waiting for click to close.");
    }

    public void OnPlane057ClosingClicked()
    {
        var hi57 = ResolvePlane057Interaction();
        if (hi57 != null)
        {
            hi57.OnClicked = null;
            hi57.SetHighlight(false);
            hi57.RestoreOriginalMaterial();
            hi57.MoveSmoothly(plane057FinalCloseTargetLocalPosition, 1.2f, () =>
            {
                Debug.Log("[SequenceHelperFunctions] Plane.057 closed to target position. Final hood closing completed!");
                isFinalHoodClosingCompleted = true;
            });
        }
        else
        {
            var p57 = ResolvePlane057Target();
            if (p57 != null) p57.transform.localPosition = plane057FinalCloseTargetLocalPosition;
            isFinalHoodClosingCompleted = true;
        }
    }

    public PotLeakage.VFX.BathPowderController ResolveBathPowderController()
    {
        var c51 = ResolveCube051Target();
        if (c51 != null)
        {
            var piper = c51.transform.Find("piper");
            var release = piper != null ? piper.Find("release") : null;
            if (release != null)
            {
                var ctrl = release.GetComponent<PotLeakage.VFX.BathPowderController>();
                if (ctrl == null) ctrl = release.gameObject.AddComponent<PotLeakage.VFX.BathPowderController>();
                ctrl.InitializeComponents();
                return ctrl;
            }
        }
        return UnityEngine.Object.FindFirstObjectByType<PotLeakage.VFX.BathPowderController>();
    }

    private GameObject pipesTarget;
    public GameObject ResolvePipesTarget()
    {
        if (pipesTarget == null)
        {
            var p = GameObject.Find("PIPES");
            if (p == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "PIPES" && !IsPersistentObject(g))
                    {
                        p = g;
                        break;
                    }
                }
            }
            pipesTarget = p;
        }
        return pipesTarget;
    }

    public void ActivateCoolingPipes()
    {
        var p = ResolvePipesTarget();
        if (p != null)
        {
            var ctrl = p.GetComponent<PotLeakage.VFX.CoolingPipesController>();
            if (ctrl == null)
            {
                ctrl = p.AddComponent<PotLeakage.VFX.CoolingPipesController>();
            }
            ctrl.ActivateCoolingPipes();
        }
    }

    public void DeactivateCoolingPipes()
    {
        var p = ResolvePipesTarget();
        if (p != null)
        {
            var ctrl = p.GetComponent<PotLeakage.VFX.CoolingPipesController>();
            if (ctrl != null)
            {
                ctrl.DeactivateCoolingPipes();
            }
            else
            {
                p.SetActive(false);
            }
        }
    }

    public void SnapCameraToNormalPotOperation()
    {
        var camCtrl = ResolveCameraController();
        if (camCtrl != null)
        {
            camCtrl.MoveToNormalPotOperation();
            return;
        }

        var tp = GameObject.Find("CameraSystem/TransformPoints/Normal Pot Operation")
              ?? GameObject.Find("TransformPoints/Normal Pot Operation")
              ?? GameObject.Find("Normal Pot Operation");
        if (tp != null)
        {
            SnapCameraToTransform(tp.transform);
        }
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
    // 1. TOOLBOX CAMERA → SIDE BREAKING TOOL (GLOVE STEP REMOVED)
    // ─────────────────────────────────────────────────────────────────────────

    public const string GlovesInstructionText = "Select the Side Breaking Tool from the toolbox to proceed with side breaking and crust removal.";
    public const string SideBreakingToolInstructionText = "Select the Side Breaking Tool from the toolbox to proceed with side breaking and crust removal.";
    public const string StopperInstructionText = "Now carefully apply the stopper to the leakage point on the pot. Look for the area where smoke is visible and molten metal is flowing. If the leakage is wider, you may use an additional stopper to help control it.";

    public void BeginToolboxGloveInteraction()
    {
        Debug.Log("[PotLeakage] BeginToolboxSelection: Snapping to ToolBoxSelection (Direct Side Breaking Tool)");
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

        // Gloves remain permanently disabled and do not participate in training interaction
        var gloves = ResolveGlovesTarget();
        if (gloves != null)
        {
            gloves.SetActive(false);
            StopGlovesBlink();
        }

        isGlovesConfirmed = true;
        isGlovesInteractable = false;

        // During Toolbox Selection, SIDEREAKING TOOL is ON, SIDEREAKING TOOL (4) is OFF.
        var sideTool = ResolveSideBreakingTool();
        if (sideTool != null) sideTool.SetActive(true);

        isSideBreakingToolInteractable = true;
        isSideBreakingToolConfirmed = false;
        isStopperToolInteractable = false;

        OnInterceptTaskCompletion = () => !isSideBreakingToolConfirmed;

        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            if (ui.welcomePanel != null) ui.welcomePanel.SetActive(true);
            if (ui.titleText != null) ui.titleText.text = "Toolbox Selection";
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
            if (ui.nextButton != null) ui.nextButton.gameObject.SetActive(true);

            ui.isGlovesInteractable = false;
            ui.isGlovesConfirmed = true;
            ui.isSideBreakingToolInteractable = true;
            ui.isSideBreakingToolConfirmed = false;

            if (ui.valueDisplay != null)
            {
                ui.valueDisplay.SetTitle("Toolbox Selection");
                ui.valueDisplay.DisplayNormalStatus("SELECT SIDE BREAKING TOOL");
                ui.valueDisplay.HideValueDisplay();
            }

            ui.StartSideBreakingToolHighlight();
            ui.SpeakDescriptionText(SideBreakingToolInstructionText, "TASK_09_TOOLBOX", "Technical_Mark");
        }
        else
        {
            HighlightSideBreakingTool();
            TruckTyreReplacement.Core.Manager.Instance?.SpeakText(SideBreakingToolInstructionText, "TASK_09_TOOLBOX", "Technical_Mark");
        }
    }

    public void StartGlovesBlink()
    {
        // Glove interaction removed - keep gloves disabled and do not blink
        var gloves = ResolveGlovesTarget();
        if (gloves != null) gloves.SetActive(false);
    }

    public void StopGlovesBlink()
    {
        isGlovesBlinking = false;
        if (glovesBlinkCoroutine != null)
        {
            StopCoroutine(glovesBlinkCoroutine);
            glovesBlinkCoroutine = null;
        }
    }

    private void ApplyGlovesHighlightState(bool highlight)
    {
        // Glove interaction removed - keep gloves disabled
        var gloves = ResolveGlovesTarget();
        if (gloves != null) gloves.SetActive(false);
    }

    private IEnumerator GlovesBlinkRoutine()
    {
        isGlovesBlinking = false;
        glovesBlinkCoroutine = null;
        yield break;
    }

    public void HandleGlovesClicked()
    {
        // Glove interaction removed - ensure gloves disabled
        var gloves = ResolveGlovesTarget();
        if (gloves != null) gloves.SetActive(false);
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

    public void RestoreSideBreakingTool5OriginalMaterials()
    {
        var tool5 = ResolveSideBreakingTool5();
        if (tool5 != null)
        {
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
        }
    }

    public void PlayThudSFX()
    {
        if (thudAudioClip == null)
        {
#if UNITY_EDITOR
            thudAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/thud.mp3");
#endif
        }
        if (thudAudioClip != null)
        {
            PlaySFX(thudAudioClip);
        }
    }

    [SerializeField] private AudioClip gasAudioClip;
    public void PlayGasSFX()
    {
        if (gasAudioClip == null)
        {
#if UNITY_EDITOR
            gasAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/gass.mp3");
#endif
        }
        if (gasAudioClip != null)
        {
            PlaySFX(gasAudioClip);
        }
    }

    public void ActivateSideBreakingTool5()
    {
        var tool5 = ResolveSideBreakingTool5();
        if (tool5 != null)
        {
            RestoreSideBreakingTool5OriginalMaterials();
            tool5.SetActive(true);
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
            ui.SpeakDescriptionText(StopperInstructionText, "TASK_10_APPLY_STOPPER", "Technical_Mark");
        }
        else
        {
            TruckTyreReplacement.Core.Manager.Instance?.SpeakText(StopperInstructionText, "TASK_10_APPLY_STOPPER", "Technical_Mark");
        }

        Debug.Log("[PotLeakage] Normal Pot Stopper Interaction active. Description displayed, SIDEREAKING TOOL (6) interactable.");
    }

    public void HighlightLeakagePointMaterials()
    {
        StopLeakagePointHighlight();

        var yellowMat = ResolveYellowHighlightMaterial();
        leakagePointHighlightSlots.Clear();

        // Scan scene for renderers on tool6 that use Stylized_MetalGrid_01_basecolor or plane_divided_DefaultMaterial_BaseColor
        var tool6 = ResolveSideBreakingTool6();
        var tool5 = ResolveSideBreakingTool5();
        List<Renderer> candidateRenderers = new List<Renderer>();
        if (tool6 != null)
        {
            candidateRenderers.AddRange(tool6.GetComponentsInChildren<Renderer>(true));
        }

        var allRenderers = Resources.FindObjectsOfTypeAll<Renderer>();
        foreach (var r in allRenderers)
        {
            if (r == null || IsPersistentObject(r.gameObject)) continue;
            // SIDEREAKING TOOL (5) must NEVER have any highlight applied (Part 1)
            if (tool5 != null && (r.gameObject == tool5 || r.transform.IsChildOf(tool5.transform))) continue;
            if (r.gameObject.name.Contains("(5)")) continue;

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

    public const string StopperAppliedText = "The stopper has been applied to the leakage point. Use the tool to complete the leakage-control step.";
    public const string FurtherToolsDescriptionText = "Let us use further tools in order to reduce or stop the intensity of the leakage.";
    public const string ArrangeCoolingPipesText = "Let us arrange the cooling pipes.";
    public const string ClickCoolingPipesText = "Click the highlighted cooling pipes to begin cooling the leakage area.";
    public const string CoolingGasDirectedText = "Cooling gas is now being directed toward the leakage point. The leakage intensity is reducing.";
    public const string CoolingGasReducingText = "The cooling gas is reducing the intensity of the molten metal leakage.";

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

        // Play thud sound once per successful SIDEREAKING TOOL (6) click
        PlayThudSFX();

        // 1. Stop any highlight/blink associated with stopper interaction
        StopLeakagePointHighlight();

        // 2. Disable SIDEREAKING TOOL (6)
        var tool6 = ResolveSideBreakingTool6();
        if (tool6 != null) tool6.SetActive(false);

        // 3. Enable SIDEREAKING TOOL (5) with its ORIGINAL materials (NO highlight material, NO blinking)
        ActivateSideBreakingTool5();
        isSideBreakingTool5Interactable = true;
        hasClickedSideBreakingTool5 = false;
        if (potLeakageUI != null)
        {
            potLeakageUI.isSideBreakingTool5Interactable = true;
            potLeakageUI.isSideBreakingTool5Confirmed = false;
        }

        // 4. Snap camera directly to TransformPoints/Normal Pot Operation
        SnapCameraToNormalPotOperation();

        // 5. Reduce intensity of MoltenAluminium_VFX smoothly over 1.2s (Stage 1: Stopper Applied)
        ApplyStopperReductionVFX(1.2f);

        // Keep task completion intercepted so Next cannot advance prematurely
        OnInterceptTaskCompletion = () => !hasClickedPipesHighlight;

        // 6. Update UI description and voiceover: Old Male Voice ("Technical_Mark")
        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            if (ui.statusText != null)
            {
                ui.statusText.gameObject.SetActive(true);
                ui.statusText.text = "STOPPER APPLIED";
            }
            if (ui.descriptionText != null)
            {
                ui.descriptionText.gameObject.SetActive(true);
                ui.descriptionText.text = StopperAppliedText;
            }
            if (ui.valueDisplay != null)
            {
                ui.valueDisplay.SetTitle("Stopper Applied");
                ui.valueDisplay.DisplayNormalStatus("STOPPER APPLIED");
                ui.valueDisplay.HideValueDisplay();
            }
            ui.SpeakDescriptionText(StopperAppliedText, "TASK_10_STOPPER_APPLIED", "Technical_Mark");
        }
        else
        {
            TruckTyreReplacement.Core.Manager.Instance?.SpeakText(StopperAppliedText, "TASK_10_STOPPER_APPLIED", "Technical_Mark");
        }

        // 7. Wait for Voice to finish -> then enable PIPES Highlight, start blinking yellow, speak FurtherToolsDescriptionText
        if (Application.isPlaying)
        {
            StartCoroutine(WaitForStopperVoiceAndTransitionRoutine());
        }

        Debug.Log("[PotLeakage] Stopper tool (6) clicked -> camera snapped to Normal Pot Operation, tool (5) active, stopper applied. Waiting for voice completion to enable PIPES Highlight.");
    }

    private IEnumerator WaitForStopperVoiceAndTransitionRoutine()
    {
        var mgr = TruckTyreReplacement.Core.Manager.Instance;
        AudioSource voiceSource = mgr != null ? mgr.GetVoiceAudioSource() : null;

        float startTime = Time.realtimeSinceStartup;
        // Wait for speech to start (or timeout)
        yield return new WaitUntil(() => (mgr != null && mgr.IsSpeaking) || (voiceSource != null && voiceSource.isPlaying) || (Time.realtimeSinceStartup - startTime) > 1.0f);

        // Wait for speech to finish (or fallback timeout so training never hangs)
        float speechStart = Time.realtimeSinceStartup;
        if ((mgr != null && mgr.IsSpeaking) || (voiceSource != null && voiceSource.isPlaying))
        {
            yield return new WaitUntil(() => (!mgr.IsSpeaking && (voiceSource == null || !voiceSource.isPlaying)) || (Time.realtimeSinceStartup - speechStart) > 10.0f);
        }
        else
        {
            yield return new WaitForSeconds(2.0f);
        }

        yield return new WaitForSeconds(0.2f);

        // VOICE FINISHES -> Transition to PIPES Highlight
        TransitionStopperVoiceFinishedToPipesHighlight();
    }

    public void TransitionStopperVoiceFinishedToPipesHighlight()
    {
        // 1. Instantly snap camera to TransformPoints/Normal Pot Operation (zero pan)
        SnapCameraToNormalPotOperation();

        // SIDEREAKING TOOL (5) MUST NOT be disabled during cooling
        StopSideBreakingTool5Blink();
        var tool5 = ResolveSideBreakingTool5();
        if (tool5 != null) tool5.SetActive(true);

        // 2. Enable PIPES Highlight and start blinking yellow
        isPipesHighlightInteractable = true;
        hasClickedPipesHighlight = false;
        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            ui.isPipesHighlightInteractable = true;
            ui.hasClickedPipesHighlight = false;
            if (ui.statusText != null)
            {
                ui.statusText.gameObject.SetActive(true);
                ui.statusText.text = "ARRANGE COOLING PIPES";
            }
            if (ui.descriptionText != null)
            {
                ui.descriptionText.gameObject.SetActive(true);
                ui.descriptionText.text = FurtherToolsDescriptionText;
            }
            ui.SpeakDescriptionText(FurtherToolsDescriptionText, "TASK_10_FURTHER_TOOLS", "Technical_Mark");
        }
        else
        {
            TruckTyreReplacement.Core.Manager.Instance?.SpeakText(FurtherToolsDescriptionText, "TASK_10_FURTHER_TOOLS", "Technical_Mark");
        }

        StartPipesHighlightBlink();

        Debug.Log("[PotLeakage] Voice finished -> Snapped to Normal Pot Operation -> PIPES Highlight enabled and blinking.");
    }

    public void HandleSideBreakingTool5Clicked()
    {
        if (hasClickedSideBreakingTool5) return;
        hasClickedSideBreakingTool5 = true;
        isSideBreakingTool5Interactable = false;

        if (potLeakageUI != null)
        {
            potLeakageUI.isSideBreakingTool5Interactable = false;
            potLeakageUI.isSideBreakingTool5Confirmed = true;
            potLeakageUI.PlaySFX(potLeakageUI.clickAudioClip);
        }

        StopSideBreakingTool5Blink();
        var tool5 = ResolveSideBreakingTool5();
        if (tool5 != null) tool5.SetActive(true); // Tool 5 remains active during cooling

        if (!isPipesHighlightInteractable && !hasClickedPipesHighlight)
        {
            TransitionStopperVoiceFinishedToPipesHighlight();
        }
    }

    public void HandlePipesHighlightClicked()
    {
        if (hasClickedPipesHighlight) return;
        if (!isPipesHighlightInteractable && (potLeakageUI == null || !potLeakageUI.isPipesHighlightInteractable)) return;

        hasClickedPipesHighlight = true;
        isPipesHighlightInteractable = false;

        if (potLeakageUI != null)
        {
            potLeakageUI.isPipesHighlightInteractable = false;
            potLeakageUI.hasClickedPipesHighlight = true;
            potLeakageUI.PlaySFX(potLeakageUI.clickAudioClip);
        }

        // 1. Stop blinking and restore original materials on PIPES Highlight, then disable it
        StopPipesHighlightBlink();
        var pipesH = ResolvePipesHighlightTarget();
        if (pipesH != null) pipesH.SetActive(false);

        // 2. Enable PIPES and start cooling gas
        ActivateCoolingPipes();

        // 3. Second molten metal reduction: intensity reduces further & becomes EVEN SLOWER (Stage 2: Cooling Pipes)
        ApplyPipesReductionVFX(2.0f);

        // 4. Start coroutine for voiceovers and completion
        if (Application.isPlaying)
        {
            StartCoroutine(CoolingGasReductionSequenceRoutine());
        }
        else
        {
            OnInterceptTaskCompletion = null;
        }
    }

    private IEnumerator CoolingGasReductionSequenceRoutine()
    {
        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            if (ui.statusText != null)
            {
                ui.statusText.gameObject.SetActive(true);
                ui.statusText.text = "COOLING LEAKAGE AREA";
            }
            if (ui.descriptionText != null)
            {
                ui.descriptionText.gameObject.SetActive(true);
                ui.descriptionText.text = CoolingGasDirectedText;
            }
            ui.SpeakDescriptionText(CoolingGasDirectedText, "TASK_10_GAS_DIRECTED", "Technical_Mark");
        }
        else
        {
            TruckTyreReplacement.Core.Manager.Instance?.SpeakText(CoolingGasDirectedText, "TASK_10_GAS_DIRECTED", "Technical_Mark");
        }

        // Wait 2.0s for molten reduction visual progress and first VO to be heard
        yield return new WaitForSeconds(2.0f);

        if (ui != null)
        {
            if (ui.descriptionText != null)
            {
                ui.descriptionText.text = CoolingGasReducingText;
            }
            ui.SpeakDescriptionText(CoolingGasReducingText, "TASK_10_GAS_REDUCING", "Technical_Mark");
            if (ui.nextButton != null) ui.nextButton.gameObject.SetActive(true);
        }
        else
        {
            TruckTyreReplacement.Core.Manager.Instance?.SpeakText(CoolingGasReducingText, "TASK_10_GAS_REDUCING", "Technical_Mark");
        }

        // Wait for voice over "The cooling gas is reducing the intensity of the molten metal leakage." to finish
        var mgr = TruckTyreReplacement.Core.Manager.Instance;
        AudioSource voiceSource = mgr != null ? mgr.GetVoiceAudioSource() : null;
        float startTime = Time.realtimeSinceStartup;
        yield return new WaitUntil(() => (mgr != null && mgr.IsSpeaking) || (voiceSource != null && voiceSource.isPlaying) || (Time.realtimeSinceStartup - startTime) > 1.0f);
        float speechStart = Time.realtimeSinceStartup;
        if ((mgr != null && mgr.IsSpeaking) || (voiceSource != null && voiceSource.isPlaying))
        {
            yield return new WaitUntil(() => (!mgr.IsSpeaking && (voiceSource == null || !voiceSource.isPlaying)) || (Time.realtimeSinceStartup - speechStart) > 8.0f);
        }
        else
        {
            yield return new WaitForSeconds(2.5f);
        }

        // When voice finishes, reduce molten leakage width slightly (narrower stream, slow movement, does NOT stop)
        ApplyCoolingVoiceFinishedReductionVFX(1.5f);

        // Release Next button interceptor! Trainee can now click Next/Continue to complete Task
        OnInterceptTaskCompletion = null;
        Debug.Log("[PotLeakage] Cooling gas voice complete -> Molten stream width reduced slightly. Trainee may now advance to next task.");
    }

    public void StartSideBreakingTool5Blink()
    {
        // SIDEREAKING TOOL (5) must NOT use any highlight material and must NEVER blink (Part 1).
        // It must always retain its original materials.
        ActivateSideBreakingTool5();
    }

    private void ApplySideBreakingTool5HighlightState(bool highlight)
    {
        // Safe no-op: SIDEREAKING TOOL (5) retains original materials
        ActivateSideBreakingTool5();
    }

    private IEnumerator SideBreakingTool5BlinkRoutine()
    {
        // Safe no-op
        yield break;
    }

    public void StopSideBreakingTool5Blink()
    {
        if (sideBreakingTool5BlinkCoroutine != null)
        {
            if (Application.isPlaying) StopCoroutine(sideBreakingTool5BlinkCoroutine);
            sideBreakingTool5BlinkCoroutine = null;
        }

        RestoreSideBreakingTool5OriginalMaterials();
        sideBreakingTool5HighlightSlots.Clear();
    }

    public void StartPipesHighlightBlink()
    {
        StopPipesHighlightBlink();
        PlayGasSFX();
        var pipesH = ResolvePipesHighlightTarget();
        if (pipesH == null) return;

        pipesH.SetActive(true);
        var yellowMat = ResolveYellowHighlightMaterial();
        pipesHighlightSlots.Clear();

        var renderers = pipesH.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in renderers)
        {
            if (r == null) continue;
            Material[] mats = r.sharedMaterials;
            for (int s = 0; s < mats.Length; s++)
            {
                var m = mats[s];
                Material orig = m;
                if (orig != null && orig.name.Contains("Highlight"))
                {
#if UNITY_EDITOR
                    if (r.gameObject.name.Contains("Plane"))
                        orig = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Model/Materials/grained white plastic_BaseColor.mat");
                    else
                        orig = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Model/Materials/6_Color.mat");
#endif
                }
                pipesHighlightSlots.Add(new RendererSlotHighlight
                {
                    renderer = r,
                    slotIndex = s,
                    originalMaterial = orig,
                    targetMatName = m != null ? m.name : ""
                });
            }
        }

        if (Application.isPlaying)
        {
            pipesHighlightBlinkCoroutine = StartCoroutine(PipesHighlightBlinkRoutine());
        }
        else
        {
            ApplyPipesHighlightState(true);
        }
    }

    private void ApplyPipesHighlightState(bool highlight)
    {
        var yellowMat = ResolveYellowHighlightMaterial();
        for (int i = 0; i < pipesHighlightSlots.Count; i++)
        {
            var slot = pipesHighlightSlots[i];
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

    private IEnumerator PipesHighlightBlinkRoutine()
    {
        bool showHighlight = true;
        while (isPipesHighlightInteractable && !hasClickedPipesHighlight)
        {
            ApplyPipesHighlightState(showHighlight);
            yield return new WaitForSeconds(0.45f);
            showHighlight = !showHighlight;
        }
        ApplyPipesHighlightState(false);
        pipesHighlightBlinkCoroutine = null;
    }

    public void StopPipesHighlightBlink()
    {
        if (pipesHighlightBlinkCoroutine != null)
        {
            if (Application.isPlaying) StopCoroutine(pipesHighlightBlinkCoroutine);
            pipesHighlightBlinkCoroutine = null;
        }

        for (int i = 0; i < pipesHighlightSlots.Count; i++)
        {
            var slot = pipesHighlightSlots[i];
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
        pipesHighlightSlots.Clear();
    }

    public void ApplyStopperReductionVFX(float duration = 1.2f)
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
                controller.ApplyStopperReduction(duration);
                Debug.Log($"[PotLeakage] MoltenAluminiumVFXController.ApplyStopperReduction({duration}) called successfully.");
                return;
            }

            var streamParticles = vfxGo.GetComponentInChildren<PotLeakage.VFX.MoltenMetalStreamParticles>(true);
            if (streamParticles != null)
            {
                streamParticles.ApplyStopperReduction(duration);
                Debug.Log($"[PotLeakage] MoltenMetalStreamParticles.ApplyStopperReduction({duration}) called directly.");
                return;
            }
        }

        var standaloneStreamParticles = UnityEngine.Object.FindFirstObjectByType<PotLeakage.VFX.MoltenMetalStreamParticles>();
        if (standaloneStreamParticles != null)
        {
            standaloneStreamParticles.ApplyStopperReduction(duration);
            Debug.Log($"[PotLeakage] Standalone MoltenMetalStreamParticles.ApplyStopperReduction({duration}) called directly.");
            return;
        }

        ReduceMoltenLeakageVFX(duration);
    }

    public void ApplyPipesReductionVFX(float duration = 2.0f)
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
                controller.ApplyPipesReduction(duration);
                Debug.Log($"[PotLeakage] MoltenAluminiumVFXController.ApplyPipesReduction({duration}) called successfully.");
                return;
            }

            var streamParticles = vfxGo.GetComponentInChildren<PotLeakage.VFX.MoltenMetalStreamParticles>(true);
            if (streamParticles != null)
            {
                streamParticles.ApplyPipesReduction(duration);
                Debug.Log($"[PotLeakage] MoltenMetalStreamParticles.ApplyPipesReduction({duration}) called directly.");
                return;
            }
        }

        var standaloneStreamParticles = UnityEngine.Object.FindFirstObjectByType<PotLeakage.VFX.MoltenMetalStreamParticles>();
        if (standaloneStreamParticles != null)
        {
            standaloneStreamParticles.ApplyPipesReduction(duration);
            Debug.Log($"[PotLeakage] Standalone MoltenMetalStreamParticles.ApplyPipesReduction({duration}) called directly.");
            return;
        }

        ReduceMoltenLeakageVFX(duration);
    }

    public void ApplyCoolingVoiceFinishedReductionVFX(float duration = 1.5f)
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
                controller.ApplyCoolingVoiceFinishedReduction(duration);
                Debug.Log($"[PotLeakage] MoltenAluminiumVFXController.ApplyCoolingVoiceFinishedReduction({duration}) called successfully.");
                return;
            }

            var streamParticles = vfxGo.GetComponentInChildren<PotLeakage.VFX.MoltenMetalStreamParticles>(true);
            if (streamParticles != null)
            {
                streamParticles.ApplyCoolingVoiceFinishedReduction(duration);
                Debug.Log($"[PotLeakage] MoltenMetalStreamParticles.ApplyCoolingVoiceFinishedReduction({duration}) called directly.");
                return;
            }
        }

        var standaloneStreamParticles = UnityEngine.Object.FindFirstObjectByType<PotLeakage.VFX.MoltenMetalStreamParticles>();
        if (standaloneStreamParticles != null)
        {
            standaloneStreamParticles.ApplyCoolingVoiceFinishedReduction(duration);
            Debug.Log($"[PotLeakage] Standalone MoltenMetalStreamParticles.ApplyCoolingVoiceFinishedReduction({duration}) called directly.");
        }
    }

    public void ReduceMoltenLeakageVFX(float duration = 1.5f)
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
                controller.SetControlledLeakage(true, duration);
                if (controller.flowMesh != null)
                {
                    controller.flowMesh.SetControlled(true, duration);
                }
                Debug.Log($"[PotLeakage] MoltenAluminiumVFXController.SetControlledLeakage(true, {duration}) called successfully.");
                return;
            }

            var flowMesh = vfxGo.GetComponentInChildren<PotLeakage.VFX.MoltenMetalFlowMesh>(true);
            if (flowMesh != null)
            {
                flowMesh.SetControlled(true, duration);
                Debug.Log($"[PotLeakage] MoltenMetalFlowMesh.SetControlled(true, {duration}) called directly.");
                return;
            }
        }

        var standaloneFlowMesh = UnityEngine.Object.FindFirstObjectByType<PotLeakage.VFX.MoltenMetalFlowMesh>();
        if (standaloneFlowMesh != null)
        {
            standaloneFlowMesh.SetControlled(true, duration);
            Debug.Log($"[PotLeakage] Standalone MoltenMetalFlowMesh.SetControlled(true, {duration}) called directly.");
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
                if (controller.streamParticles != null)
                {
                    controller.streamParticles.ResetToFull(0f);
                }
                Debug.Log("[PotLeakage] MoltenAluminiumVFXController.SetControlledLeakage(false) and ResetToFull restored.");
                return;
            }

            var streamParticles = vfxGo.GetComponentInChildren<PotLeakage.VFX.MoltenMetalStreamParticles>(true);
            if (streamParticles != null)
            {
                streamParticles.ResetToFull(0f);
            }

            var flowMesh = vfxGo.GetComponentInChildren<PotLeakage.VFX.MoltenMetalFlowMesh>(true);
            if (flowMesh != null)
            {
                flowMesh.SetControlled(false, 0f);
                Debug.Log("[PotLeakage] MoltenMetalFlowMesh.SetControlled(false) restored directly.");
                return;
            }
        }

        var standaloneStreamParticles = UnityEngine.Object.FindFirstObjectByType<PotLeakage.VFX.MoltenMetalStreamParticles>();
        if (standaloneStreamParticles != null)
        {
            standaloneStreamParticles.ResetToFull(0f);
        }

        var standaloneFlowMesh = UnityEngine.Object.FindFirstObjectByType<PotLeakage.VFX.MoltenMetalFlowMesh>();
        if (standaloneFlowMesh != null)
        {
            standaloneFlowMesh.SetControlled(false, 0f);
            Debug.Log("[PotLeakage] Standalone MoltenMetalFlowMesh.SetControlled(false) restored directly.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PTM CRANE PREPARATION (Task 10)
    // ─────────────────────────────────────────────────────────────────────────

    public const string SlideHoodDescriptionText = "Slide the hood.";
    public const string SlideHoodStatusText = "Slide the Hood";
    public const string RemoveHoodDescriptionText = "Slide the hood.";
    public const string RemoveHoodStatusText = "Slide the Hood";
    public const string PTMCraneDescriptionText = "The intensity is reduced but not stopped. Let us arrange PTM crane for pot breaking process, and the bath bin is also arranged by forklift.";
    public const string SideBreakingDescriptionText = "Side breaking above the leakage has started.";
    public const string ObserveRedShellDescriptionText = "Observe the red shell formation in the shell.";
    public const string RedShellDescriptionText = ObserveRedShellDescriptionText;
    public const string UseHoesDescriptionText = "Let us use hoes to reduce the red shell formation.";
    public const string VoltageCheckDescriptionText = "Check whether the voltage is within the safer limit before working on the red shell formation.";
    public const string PickUpHoesDescriptionText = "Pick up the hose to reduce the red shell formation.";
    public const string PlaceHoesPipeDescriptionText = "Place the hoes pipe near the red shell area.";
    public const string OpenFLRValveDescriptionText = "Open the FLR block valve.";
    public const string ValveOpenCoolAirDescriptionText = "Now that the valve is open, cool air is released over there.";
    public const string ObserveRedShellCoolingDescriptionText = "Let us observe the red shell. It still requires more cooling; use hoes to reduce red shell formation.";
    public const string StartBathPackingDescriptionText = "Let us start bath packing.";
    public const string SafeDistanceDescriptionText = "Maintain safe distance to avoid splashing of molten bath. Packing is started.";
    public const string PackingDoneDescriptionText = "Packing is done successfully. The red shell intensity has been reduced.";
    public const string VoltageSaferLimitRangeDescriptionText = "Check if the voltage is within the safer limit range between 4.1 to 4.3 V.";
    public const string FinalVerificationDescriptionText = "Now that the leakage and the shell formation is completely stopped you have completed the pot leakage module now close the hood.";
    public const string FinalCloseHoodDescriptionText = FinalVerificationDescriptionText;

    public const string PTMCraneStatusText = "PTM Crane Arrangement";
    public const string SideBreakingStatusText = "Side Breaking";
    public const string ObserveRedShellStatusText = "Observe Red Shell";
    public const string UseHoesStatusText = "Use Hoes";
    public const string VoltageCheckStatusText = "Check Safe Voltage";
    public const string PickUpHoesStatusText = "Pick Up Hose";
    public const string OpenFLRValveStatusText = "Open FLR Block Valve";
    public const string ValveOpenCoolAirStatusText = "Cool Air Released";
    public const string StartBathPackingStatusText = "Bath Packing";
    public const string SafeDistanceStatusText = "Bath Packing in Progress";
    public const string PackingDoneStatusText = "Packing Completed";
    public const string VoltageSaferLimitRangeStatusText = "Verify Safe Voltage";
    public const string FinalVerificationStatusText = "Pot Leakage Module Completed";
    public const string FinalCloseHoodStatusText = "Close the Hood";

    public void UpdateUI(string description, string status, int taskNum = 10, int totalTasks = 10, string audioKey = null, string voice = "Technical_Mark")
    {
        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            ui.UpdateStageUI(description, status, taskNum, totalTasks, audioKey, voice);
        }
        else
        {
            if (DescriptionText != null)
            {
                DescriptionText.gameObject.SetActive(true);
                DescriptionText.text = description;
            }
            if (TitleText != null)
            {
                TitleText.gameObject.SetActive(true);
                TitleText.text = status;
            }
            TruckTyreReplacement.Core.Manager.Instance?.SpeakText(description, audioKey, voice);
        }
    }

    [Header("PTM Crane References & Targets")]
    public GameObject craneTarget;
    public GameObject cube013Target;
    public GameObject cylinder003Target;
    public GameObject cylinderTarget;

    [Header("Plane.045 & Red Shell References")]
    public GameObject plane045Target;
    public GameObject plane045_1Target;
    public Vector3 plane045InitialLocalPosition = new Vector3(-7.33232689f, 0.776885569f, -78.0439987f);
    public Vector3 plane045TargetLocalPosition = new Vector3(-7.33232689f, 0.776885569f, -78.4199982f);
    public Vector3 plane045BathPackingStartLocalPosition = new Vector3(-7.33232689f, 0.776885569f, -78.1200027f);
    public Vector3 plane045BathPackingTargetLocalPosition = new Vector3(-7.33232689f, 0.776885569f, -78.0f);
    private RedShellController redShellController;

    [Header("Bath Packing Coroutine")]
    private Coroutine bathPackingAnimationCoroutine = null;

    [Header("Hoe References & Targets")]
    public GameObject hoeTransformPoint;
    public GameObject holdSpiral001Target;

    [Header("Hoe Pipe & Electrical Box Targets")]
    public GameObject holdSpiral001MatTarget;
    public GameObject holdSpiral002Target;
    public GameObject electricalBoxHandleTarget;
    public GameObject electricalBoxEnableTarget;
    public GameObject coolGasTarget;
    private Coroutine coolAirToRedShellCoroutine = null;

    public GameObject cube014Target;

    public Vector3 craneInitialPosition = new Vector3(0f, 1.08000004f, -265.506042f);
    public Vector3 craneTargetPosition = new Vector3(0f, 1.08000004f, -229.360001f);
    private Quaternion craneInitialRotation = Quaternion.identity;
    private Vector3 cube013InitialLocalPosition = new Vector3(-6.59000015f, 8.10000038f, -0.879999995f);
    private Vector3 cube013TargetLocalPosition = new Vector3(-6.59000015f, 0.720000029f, -0.879999995f);
    private Quaternion cube013InitialLocalRotation = Quaternion.identity;
    private Vector3 cube014InitialLocalPosition = new Vector3(-6.59000015f, 8.01000023f, -0.879999995f);
    private Vector3 cube014TargetLocalPosition = new Vector3(-6.59000015f, 5.48000002f, -0.879999995f);
    private Quaternion cube014InitialLocalRotation = Quaternion.identity;
    private bool hasCachedCraneInitial = false;
    private Coroutine ptmCraneAnimationCoroutine = null;
    private Coroutine waitForVoltageAudioCoroutine = null;

    [Header("PTM Crane Audio")]
    [SerializeField] private AudioClip craneHumAudioClip;
    private AudioSource craneHumAudioSource;

    public void StartCraneHumSFX()
    {
        if (craneHumAudioClip == null)
        {
#if UNITY_EDITOR
            craneHumAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/CraneHum.wav");
#endif
        }
        if (craneHumAudioClip != null)
        {
            var mgr = TruckTyreReplacement.Core.Manager.Instance;
            if (mgr != null)
            {
                craneHumAudioSource = mgr.GetSFXAudioSource();
            }
            if (craneHumAudioSource == null)
            {
                craneHumAudioSource = GetComponent<AudioSource>();
            }
            if (craneHumAudioSource != null)
            {
                craneHumAudioSource.clip = craneHumAudioClip;
                craneHumAudioSource.loop = true;
                craneHumAudioSource.Play();
            }
        }
    }

    public void StopCraneHumSFX()
    {
        if (craneHumAudioSource != null && craneHumAudioSource.clip == craneHumAudioClip)
        {
            craneHumAudioSource.Stop();
            craneHumAudioSource.loop = false;
            craneHumAudioSource.clip = null;
        }
    }

    public GameObject ResolvePlane045Target()
    {
        if (plane045Target == null)
        {
            var p = GameObject.Find("LINE/line (6)/Plane.045")
                 ?? GameObject.Find("line (6)/Plane.045")
                 ?? GameObject.Find("Plane.045");
            if (p == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "Plane.045" && !IsPersistentObject(g))
                    {
                        p = g;
                        break;
                    }
                }
            }
            plane045Target = p;
        }
        return plane045Target;
    }

    public RedShellController ResolveRedShellController()
    {
        if (redShellController != null) return redShellController;

        var p = ResolvePlane045Target();
        if (p != null)
        {
            var child = p.transform.Find("red shell component");
            if (child != null)
            {
                redShellController = child.GetComponent<RedShellController>();
                if (redShellController == null)
                {
                    redShellController = child.gameObject.AddComponent<RedShellController>();
                }
            }
        }

        if (redShellController == null)
        {
            redShellController = RedShellController.Instance ?? UnityEngine.Object.FindFirstObjectByType<RedShellController>();
        }

        return redShellController;
    }

    public GameObject ResolveRedGameObjectTarget()
    {
        var rs = ResolveRedShellController();
        if (rs != null && rs.redGameObject != null) return rs.redGameObject;
        var p045 = ResolvePlane045Target();
        if (p045 != null)
        {
            foreach (Transform child in p045.transform)
            {
                if (child.name.Equals("red", StringComparison.OrdinalIgnoreCase) || child.name.Equals("Red GameObject", StringComparison.OrdinalIgnoreCase))
                {
                    return child.gameObject;
                }
            }
        }
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < all.Length; i++)
        {
            if ((all[i].name == "red" || all[i].name == "Red" || all[i].name == "Red GameObject") && !IsPersistentObject(all[i]))
            {
                return all[i];
            }
        }
        return null;
    }

    public GameObject ResolveCraneTarget()
    {
        if (craneTarget == null)
        {
            var c = GameObject.Find("crane ");
            if (c == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "crane " && !IsPersistentObject(g) && g.transform.parent == null)
                    {
                        c = g;
                        break;
                    }
                }
            }
            craneTarget = c;
        }

        if (craneTarget != null && !hasCachedCraneInitial)
        {
            craneInitialPosition = new Vector3(0f, 1.08000004f, -265.506042f);
            craneInitialRotation = craneTarget.transform.rotation;
            var c13 = craneTarget.transform.Find("Cube.013");
            if (c13 != null)
            {
                cube013Target = c13.gameObject;
                cube013InitialLocalPosition = new Vector3(-6.59000015f, 8.10000038f, -0.879999995f);
                cube013InitialLocalRotation = Quaternion.Euler(270f, 0f, 0f);
                var cyl = c13.Find("Cylinder.003");
                if (cyl != null) cylinder003Target = cyl.gameObject;
            }
            var c14 = craneTarget.transform.Find("Cube.014");
            if (c14 != null)
            {
                cube014Target = c14.gameObject;
                cube014InitialLocalPosition = new Vector3(-6.59000015f, 8.01000023f, -0.879999995f);
                cube014InitialLocalRotation = c14.localRotation;
            }
            hasCachedCraneInitial = true;
        }

        return craneTarget;
    }

    public GameObject ResolveCube013Target()
    {
        if (cube013Target == null)
        {
            var crane = ResolveCraneTarget();
            if (crane != null)
            {
                var c13 = crane.transform.Find("Cube.013");
                if (c13 != null) cube013Target = c13.gameObject;
            }
        }
        return cube013Target;
    }

    public GameObject ResolveCube014Target()
    {
        if (cube014Target == null)
        {
            var crane = ResolveCraneTarget();
            if (crane != null)
            {
                var c14 = crane.transform.Find("Cube.014");
                if (c14 != null) cube014Target = c14.gameObject;
            }
            if (cube014Target == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "Cube.014" && !IsPersistentObject(g))
                    {
                        cube014Target = g;
                        break;
                    }
                }
            }
        }
        return cube014Target;
    }

    public GameObject ResolveCylinder003Target()
    {
        if (cylinder003Target == null)
        {
            var c13 = ResolveCube013Target();
            if (c13 != null)
            {
                var cyl = c13.transform.Find("Cylinder.003");
                if (cyl != null) cylinder003Target = cyl.gameObject;
            }
        }
        return cylinder003Target;
    }

    public void SnapCameraToPTMCrane()
    {
        var camCtrl = ResolveCameraController();
        if (camCtrl != null)
        {
            camCtrl.MoveToPTMCrane();
            return;
        }

        var tp = GameObject.Find("CameraSystem/TransformPoints/PTM Crane")
              ?? GameObject.Find("TransformPoints/PTM Crane")
              ?? GameObject.Find("PTM Crane");
        if (tp != null)
        {
            SnapCameraToTransform(tp.transform);
        }
    }

    public GameObject ResolvePlane045_1Target()
    {
        if (plane045_1Target == null)
        {
            var p = GameObject.Find("LINE/line (6)/Plane.045 (1)")
                 ?? GameObject.Find("line (6)/Plane.045 (1)")
                 ?? GameObject.Find("Plane.045 (1)");
            if (p == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if (g.name == "Plane.045 (1)" && !IsPersistentObject(g))
                    {
                        p = g;
                        break;
                    }
                }
            }
            plane045_1Target = p;
        }
        return plane045_1Target;
    }

    public void EnsurePlane045_1Material()
    {
        var p045_1 = ResolvePlane045_1Target();
        if (p045_1 != null)
        {
            var r = p045_1.GetComponent<MeshRenderer>();
            if (r != null)
            {
                var mat = Resources.Load<Material>("M_Magma_Overflow_Liquid");
#if UNITY_EDITOR
                if (mat == null)
                {
                    mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/PotLeakage/Materials/M_Magma_Overflow_Liquid.mat");
                }
#endif
                if (mat != null && r.sharedMaterial != mat)
                {
                    r.sharedMaterial = mat;
                }
            }
        }
    }

    public GameObject ResolveHoeTransformPoint()
    {
        if (hoeTransformPoint == null)
        {
            hoeTransformPoint = GameObject.Find("CameraSystem/TransformPoints/Hoe")
                             ?? GameObject.Find("TransformPoints/Hoe")
                             ?? GameObject.Find("Hoe");
        }
        return hoeTransformPoint;
    }

    public void SnapCameraToHoe()
    {
        var hoeTP = ResolveHoeTransformPoint();
        if (hoeTP != null)
        {
            SnapCameraToTransform(hoeTP.transform);
            Debug.Log("[SequenceHelperFunctions] Camera snapped directly to TransformPoints/Hoe");
        }
        else
        {
            Debug.LogWarning("[SequenceHelperFunctions] Hoe transform point not found!");
        }
    }

    public PotLeakage.Interaction.HoeInteraction ResolveHoeInteraction()
    {
        if (holdSpiral001Target == null)
        {
            var rp = GameObject.Find("round_PIPES");
            if (rp != null)
            {
                var t = rp.transform.Find("HoldSpiral.001") ?? rp.transform.Find("HoldSpiral.001 ");
                if (t != null) holdSpiral001Target = t.gameObject;
            }
            if (holdSpiral001Target == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var g in all)
                {
                    if ((g.name == "HoldSpiral.001" || g.name == "HoldSpiral.001 ") && !IsPersistentObject(g))
                    {
                        holdSpiral001Target = g;
                        break;
                    }
                }
            }
        }
        if (holdSpiral001Target != null)
        {
            var hi = holdSpiral001Target.GetComponent<PotLeakage.Interaction.HoeInteraction>();
            if (hi == null) hi = holdSpiral001Target.AddComponent<PotLeakage.Interaction.HoeInteraction>();
            return hi;
        }
        return null;
    }

    public GameObject ResolveHoldSpiral001Target()
    {
        if (holdSpiral001Target == null)
        {
            ResolveHoeInteraction();
        }
        return holdSpiral001Target;
    }

    public void SnapToNormalPotOperation()
    {
        var camCtrl = ResolveCameraController();
        if (camCtrl != null)
        {
            camCtrl.MoveToNormalPotOperation();
            return;
        }

        var tp = GameObject.Find("CameraSystem/TransformPoints/Normal Pot Operation")
              ?? GameObject.Find("TransformPoints/Normal Pot Operation")
              ?? GameObject.Find("Normal Pot Operation");
        if (tp != null)
        {
            SnapCameraToTransform(tp.transform);
        }
    }

    public void SnapToAirUnlock()
    {
        var camCtrl = ResolveCameraController();
        if (camCtrl != null)
        {
            camCtrl.MoveToAirUnlock();
            return;
        }

        var tp = GameObject.Find("CameraSystem/TransformPoints/AirUnlock")
              ?? GameObject.Find("TransformPoints/AirUnlock")
              ?? GameObject.Find("AirUnlock");
        if (tp != null)
        {
            SnapCameraToTransform(tp.transform);
        }
    }

    public GameObject ResolveHoldSpiral001MatTarget()
    {
        if (holdSpiral001MatTarget != null) return holdSpiral001MatTarget;

        var matGo = GameObject.Find("HoldSpiral.001 mat");
        if (matGo == null)
        {
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var g in all)
            {
                if (g.name == "HoldSpiral.001 mat" && !IsPersistentObject(g))
                {
                    matGo = g;
                    break;
                }
            }
        }
        if (matGo != null)
        {
            holdSpiral001MatTarget = matGo;
            var hi = matGo.GetComponent<PotLeakage.Interaction.HoeInteraction>();
            if (hi == null) hi = matGo.AddComponent<PotLeakage.Interaction.HoeInteraction>();
            hi.targetType = PotLeakage.Interaction.HoeInteraction.HoeTargetType.PotPlacement;
        }
        return holdSpiral001MatTarget;
    }

    public GameObject ResolveHoldSpiral002Target()
    {
        if (holdSpiral002Target != null) return holdSpiral002Target;

        var go = GameObject.Find("HoldSpiral.002");
        if (go == null)
        {
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var g in all)
            {
                if (g.name == "HoldSpiral.002" && !IsPersistentObject(g))
                {
                    go = g;
                    break;
                }
            }
        }
        holdSpiral002Target = go;
        return holdSpiral002Target;
    }

    public GameObject ResolveElectricalBoxHandle()
    {
        if (electricalBoxHandleTarget != null) return electricalBoxHandleTarget;

        var eb = GameObject.Find("elect box");
        if (eb != null)
        {
            var h = eb.transform.Find("electrical box handle");
            if (h != null) electricalBoxHandleTarget = h.gameObject;
        }
        if (electricalBoxHandleTarget == null)
        {
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var g in all)
            {
                if (g.name == "electrical box handle" && !IsPersistentObject(g))
                {
                    electricalBoxHandleTarget = g;
                    break;
                }
            }
        }
        if (electricalBoxHandleTarget != null)
        {
            var handleComp = electricalBoxHandleTarget.GetComponent<PotLeakage.Interaction.ElectricalBoxHandleInteraction>();
            if (handleComp == null) handleComp = electricalBoxHandleTarget.AddComponent<PotLeakage.Interaction.ElectricalBoxHandleInteraction>();
        }
        return electricalBoxHandleTarget;
    }

    public GameObject ResolveElectricalBoxEnable()
    {
        if (electricalBoxEnableTarget != null) return electricalBoxEnableTarget;

        var eb = GameObject.Find("elect box");
        if (eb != null)
        {
            var e = eb.transform.Find("electrical box enable");
            if (e != null) electricalBoxEnableTarget = e.gameObject;
        }
        if (electricalBoxEnableTarget == null)
        {
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var g in all)
            {
                if (g.name == "electrical box enable" && !IsPersistentObject(g))
                {
                    electricalBoxEnableTarget = g;
                    break;
                }
            }
        }
        return electricalBoxEnableTarget;
    }

    public ParticleSystem ResolveHoldSpiralCoolGas()
    {
        if (coolGasTarget != null) return coolGasTarget.GetComponent<ParticleSystem>();

        var hs2 = ResolveHoldSpiral002Target();
        if (hs2 != null)
        {
            var child = hs2.transform.Find("GasDensity_Fill Cool") 
                     ?? hs2.transform.Find("Cool/GasDensity_Fill");
            if (child != null)
            {
                coolGasTarget = child.gameObject;
            }
        }
        if (coolGasTarget == null)
        {
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var g in all)
            {
                if ((g.name == "GasDensity_Fill Cool" || g.name == "GasDensity_Fill") && !IsPersistentObject(g))
                {
                    coolGasTarget = g;
                    break;
                }
            }
        }
        return coolGasTarget != null ? coolGasTarget.GetComponent<ParticleSystem>() : null;
    }

    public void EnsureSideBreakingTool5Active()
    {
        var srt5 = ResolveSideBreakingTool5();
        if (srt5 != null)
        {
            if (!srt5.activeSelf) srt5.SetActive(true);
        }
    }

    [Header("DustParticle References")]
    public GameObject dustParticleTarget;
    private PotLeakage.VFX.DustParticleController dustParticleController;

    public GameObject ResolveDustParticleTarget()
    {
        if (dustParticleTarget != null) return dustParticleTarget;

        var c13 = ResolveCube013Target();
        if (c13 != null)
        {
            var dp = c13.transform.Find("DustParticle");
            if (dp != null)
            {
                dustParticleTarget = dp.gameObject;
                return dustParticleTarget;
            }
        }

        var g = GameObject.Find("DustParticle");
        if (g != null) dustParticleTarget = g;
        return dustParticleTarget;
    }

    public PotLeakage.VFX.DustParticleController ResolveDustParticleController()
    {
        if (dustParticleController != null) return dustParticleController;

        var dpTarget = ResolveDustParticleTarget();
        if (dpTarget != null)
        {
            dustParticleController = dpTarget.GetComponent<PotLeakage.VFX.DustParticleController>();
            if (dustParticleController == null)
            {
                dustParticleController = dpTarget.AddComponent<PotLeakage.VFX.DustParticleController>();
            }
        }
        else
        {
            dustParticleController = UnityEngine.Object.FindFirstObjectByType<PotLeakage.VFX.DustParticleController>();
        }

        return dustParticleController;
    }

    public GameObject ResolveCylinderTarget()
    {
        if (cylinderTarget != null) return cylinderTarget;

        var c13 = ResolveCube013Target();
        if (c13 != null)
        {
            var cyl = c13.transform.Find("Cylinder");
            if (cyl != null)
            {
                cylinderTarget = cyl.gameObject;
                return cylinderTarget;
            }
        }

        var crane = ResolveCraneTarget();
        if (crane != null)
        {
            var cyl = crane.transform.Find("Cylinder");
            if (cyl != null)
            {
                cylinderTarget = cyl.gameObject;
                return cylinderTarget;
            }
        }

        var g = GameObject.Find("Cylinder");
        if (g != null && !IsPersistentObject(g))
        {
            cylinderTarget = g;
            return cylinderTarget;
        }

        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in all)
        {
            if (go.name == "Cylinder" && !IsPersistentObject(go))
            {
                cylinderTarget = go;
                break;
            }
        }

        return cylinderTarget;
    }

    public void StartDrillVibration()
    {
        // Disable drill vibration on Cylinder.003
        var cyl003 = ResolveCylinder003Target();
        if (cyl003 != null)
        {
            var drill003 = cyl003.GetComponent<PotLeakage.VFX.DrillVibrationController>();
            if (drill003 != null)
            {
                drill003.StopDrilling();
                drill003.enabled = false;
            }
        }

        // Disable drill vibration and rotation shaking on Cylinder
        var cyl = ResolveCylinderTarget();
        if (cyl != null)
        {
            var drill = cyl.GetComponent<PotLeakage.VFX.DrillVibrationController>();
            if (drill != null)
            {
                drill.StopDrilling();
                drill.enabled = false;
            }
        }
        else if (PotLeakage.VFX.DrillVibrationController.Instance != null)
        {
            PotLeakage.VFX.DrillVibrationController.Instance.StopDrilling();
            PotLeakage.VFX.DrillVibrationController.Instance.enabled = false;
        }
    }

    public void StopDrillVibration()
    {
        var cyl003 = ResolveCylinder003Target();
        if (cyl003 != null)
        {
            var drill003 = cyl003.GetComponent<PotLeakage.VFX.DrillVibrationController>();
            if (drill003 != null)
            {
                drill003.StopDrilling();
                drill003.enabled = false;
            }
        }

        var cyl = ResolveCylinderTarget();
        if (cyl != null)
        {
            var drill = cyl.GetComponent<PotLeakage.VFX.DrillVibrationController>();
            if (drill != null)
            {
                drill.StopDrilling();
                drill.enabled = false;
            }
        }
        else if (PotLeakage.VFX.DrillVibrationController.Instance != null)
        {
            PotLeakage.VFX.DrillVibrationController.Instance.StopDrilling();
            PotLeakage.VFX.DrillVibrationController.Instance.enabled = false;
        }
    }

    public void TriggerDustParticleBurst(int count = 10)
    {
        var dp = ResolveDustParticleController();
        if (dp != null)
        {
            dp.TriggerBurst(count);
        }
    }

    public void ResetPTMCraneState()
    {
        task10SubStage = 0;
        OnInterceptTaskCompletion = null;

        if (ptmCraneAnimationCoroutine != null)
        {
            if (Application.isPlaying) StopCoroutine(ptmCraneAnimationCoroutine);
            ptmCraneAnimationCoroutine = null;
        }

        StopDrillVibration();

        var crane = ResolveCraneTarget();
        if (crane != null)
        {
            crane.transform.position = craneInitialPosition;
            crane.transform.rotation = craneInitialRotation;
        }

        var c13 = ResolveCube013Target();
        if (c13 != null)
        {
            c13.transform.localPosition = cube013InitialLocalPosition;
            c13.transform.localRotation = cube013InitialLocalRotation;
        }

        var cyl = ResolveCylinder003Target();
        if (cyl != null)
        {
            cyl.SetActive(false);
            var drill = cyl.GetComponent<PotLeakage.VFX.DrillVibrationController>();
            if (drill != null) drill.RestoreOriginalTransforms();
        }

        var dp = ResolveDustParticleController();
        if (dp != null)
        {
            dp.ResetDust();
        }

        var p045 = ResolvePlane045Target();
        if (p045 != null)
        {
            p045.transform.localPosition = plane045InitialLocalPosition;
        }

        var redShell = ResolveRedShellController();
        if (redShell != null)
        {
            redShell.ResetRedShell();
            redShell.gameObject.SetActive(false);
        }

        var pipesCtrl = CoolingPipesController.Instance ?? UnityEngine.Object.FindFirstObjectByType<CoolingPipesController>();
        if (pipesCtrl != null)
        {
            pipesCtrl.StopGasDissipation();
        }

        DeactivateCoolingPipes();

        if (coolAirToRedShellCoroutine != null)
        {
            if (Application.isPlaying) StopCoroutine(coolAirToRedShellCoroutine);
            coolAirToRedShellCoroutine = null;
        }

        var hi = ResolveHoeInteraction();
        if (hi != null)
        {
            hi.RestoreOriginalMaterial();
            hi.SetHighlight(false);
        }
        if (holdSpiral001Target != null) holdSpiral001Target.SetActive(true);

        var matGo = ResolveHoldSpiral001MatTarget();
        if (matGo != null)
        {
            var matHi = matGo.GetComponent<PotLeakage.Interaction.HoeInteraction>();
            if (matHi != null)
            {
                matHi.RestoreOriginalMaterial();
                matHi.SetHighlight(false);
            }
            matGo.SetActive(false);
        }

        var redShellCtrl = ResolveRedShellController();
        if (redShellCtrl != null)
        {
            redShellCtrl.ResolveRedGameObject();
        }

        var hs2 = ResolveHoldSpiral002Target();
        if (hs2 != null) hs2.SetActive(false);

        var handleGo = ResolveElectricalBoxHandle();
        if (handleGo != null)
        {
            var handleHi = handleGo.GetComponent<PotLeakage.Interaction.ElectricalBoxHandleInteraction>();
            if (handleHi != null) handleHi.SetHighlight(false);
            handleGo.SetActive(true);
        }

        var enableGo = ResolveElectricalBoxEnable();
        if (enableGo != null) enableGo.SetActive(false);

        var coolGas = ResolveHoldSpiralCoolGas();
        if (coolGas != null)
        {
            coolGas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            coolGas.gameObject.SetActive(false);
        }

        DeactivatePipes1();
        var p2Reset = ResolvePipes2Target();
        if (p2Reset != null) p2Reset.SetActive(false);

        var c51 = ResolveCube051Target();
        if (c51 != null)
        {
            var pw = ResolveBathPowderController();
            if (pw != null) pw.ResetPowder();
            c51.transform.localPosition = cube051InitialLocalPosition;
            c51.SetActive(false);
        }

        if (bathPackingAnimationCoroutine != null)
        {
            if (Application.isPlaying) StopCoroutine(bathPackingAnimationCoroutine);
            bathPackingAnimationCoroutine = null;
        }

        EnsureHoodPanelsOpen();
    }

    public void BeginPTMCranePreparation()
    {
        ResolveCraneTarget();
        ResolveCube013Target();
        ResolveCylinder003Target();
        ResolveDustParticleController();
        ResolvePlane045Target();
        ResolveRedShellController();
        EnsureHoodPanelsOpen();

        // 1. Camera snap to TransformPoints/PTM Crane (instant snap, zero pan)
        SnapCameraToPTMCrane();

        // Ensure SIDEREAKING TOOL (5) remains active and visible
        var tool5 = ResolveSideBreakingTool5();
        if (tool5 != null) tool5.SetActive(true);

        // Ensure cooling gas is active and running at intensified rate
        var pipesCtrl = CoolingPipesController.Instance ?? UnityEngine.Object.FindFirstObjectByType<CoolingPipesController>();
        if (pipesCtrl != null)
        {
            pipesCtrl.IntensifyCoolingGas();
        }

        // 2. Normal UI update
        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            if (ui.welcomePanel != null) ui.welcomePanel.SetActive(true);
            if (ui.titleText != null) ui.titleText.text = "PTM Crane Preparation";
            if (ui.statusText != null)
            {
                ui.statusText.gameObject.SetActive(true);
                ui.statusText.text = "ARRANGE PTM CRANE";
            }
            if (ui.descriptionText != null)
            {
                ui.descriptionText.gameObject.SetActive(true);
                ui.descriptionText.text = PTMCraneDescriptionText;
            }
            if (ui.nextButton != null) ui.nextButton.gameObject.SetActive(true);

            if (ui.valueDisplay != null)
            {
                ui.valueDisplay.SetTitle("PTM Crane Preparation");
                ui.valueDisplay.DisplayNormalStatus("ARRANGE PTM CRANE");
                ui.valueDisplay.HideValueDisplay();
            }
        }

        // Setup Task 10 progression interception
        task10SubStage = 0;
        OnInterceptTaskCompletion = HandleTask10ProgressionIntercept;

        // 3. Physical visible movement
        if (Application.isPlaying)
        {
            if (ptmCraneAnimationCoroutine != null) StopCoroutine(ptmCraneAnimationCoroutine);
            ptmCraneAnimationCoroutine = StartCoroutine(AnimatePTMCraneAndPusherRoutine());
        }
        else
        {
            // Edit Mode / test runner: set final targets directly
            if (craneTarget != null) craneTarget.transform.position = craneTargetPosition;
            if (cube013Target != null)
            {
                cube013Target.transform.localPosition = new Vector3(-6.91900015f, 1.02999997f, -0.879999995f);
                cube013Target.transform.localRotation = Quaternion.Euler(270f, 0f, 0f);
            }
            if (cube014Target != null) cube014Target.transform.localPosition = cube014TargetLocalPosition;
            if (cylinder003Target != null) cylinder003Target.SetActive(false);
            StopDrillVibration();
            var dp = ResolveDustParticleController();
            if (dp != null) dp.InitializeComponents();
            var p045 = ResolvePlane045Target();
            if (p045 != null) p045.transform.localPosition = plane045TargetLocalPosition;
            var redShell = ResolveRedShellController();
            if (redShell != null)
            {
                redShell.gameObject.SetActive(true);
                redShell.StartFormation(2.0f);
            }
            TruckTyreReplacement.Core.Manager.Instance?.SpeakText(PTMCraneDescriptionText, "TASK_10_PTM_CRANE", "Technical_Mark");
        }

        Debug.Log("[PotLeakage] PTM Crane Preparation stage begun.");
    }

    private IEnumerator AnimatePTMCraneAndPusherRoutine()
    {
        int myVersion = ++voicePlaybackVersion;
        var crane = ResolveCraneTarget();
        var c13 = ResolveCube013Target();
        var c14 = ResolveCube014Target();
        var cyl = ResolveCylinder003Target();
        var p045 = ResolvePlane045Target();
        var redShell = ResolveRedShellController();

        // Ensure start state
        if (crane != null) crane.transform.position = craneInitialPosition;
        if (c13 != null) c13.transform.localPosition = new Vector3(-6.17000008f, 1.02999997f, -0.879999995f);
        if (c14 != null) c14.transform.localPosition = cube014InitialLocalPosition;
        if (cyl != null) cyl.SetActive(false);
        StopDrillVibration();
        if (p045 != null) p045.transform.localPosition = plane045InitialLocalPosition;
        if (redShell != null)
        {
            redShell.ResetRedShell();
            redShell.gameObject.SetActive(false);
        }

        SnapCameraToPTMCrane();

        yield return new WaitForSeconds(0.2f);
        if (myVersion != voicePlaybackVersion) yield break;

        // Step 1: ONE combined description + ONE combined voice-over:
        // "The intensity is reduced but not stopped. Let us arrange PTM crane for pot breaking process, and the bath bin is also arranged by forklift."
        var ui = ResolvePotLeakageUI();
        if (ui != null)
        {
            if (ui.descriptionText != null)
            {
                ui.descriptionText.gameObject.SetActive(true);
                ui.descriptionText.text = PTMCraneDescriptionText;
            }
            ui.SpeakDescriptionText(PTMCraneDescriptionText, "TASK_10_PTM_CRANE", "Technical_Mark");
        }
        else
        {
            TruckTyreReplacement.Core.Manager.Instance?.SpeakText(PTMCraneDescriptionText, "TASK_10_PTM_CRANE", "Technical_Mark");
        }

        // Step 2: CRANE TRAVEL FIRST (8–12 seconds, 9.0s)
        // Crane: (0, 1.08000004, -265.506042) -> (0, 1.08000004, -229.360001)
        // Cube.013 and Cube.014 DO NOT move yet!
        StartCraneHumSFX();
        float craneDuration = 9.0f;
        float elapsedCrane = 0f;
        while (elapsedCrane < craneDuration)
        {
            if (myVersion != voicePlaybackVersion)
            {
                StopCraneHumSFX();
                yield break;
            }
            elapsedCrane += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedCrane / craneDuration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (crane != null)
            {
                float z = Mathf.Lerp(craneInitialPosition.z, craneTargetPosition.z, smooth);
                crane.transform.position = new Vector3(craneInitialPosition.x, craneInitialPosition.y, z);
            }
            yield return null;
        }

        StopCraneHumSFX();

        if (myVersion != voicePlaybackVersion) yield break;
        if (crane != null) crane.transform.position = craneTargetPosition;

        Debug.Log("[PotLeakage] Crane reached target position (0, 1.08000004, -229.360001).");

        // Step 3: SNAP CAMERA TO Normal Pot AFTER crane has completely reached target
        SnapToNormalPot();

        yield return new WaitForSeconds(0.3f);
        if (myVersion != voicePlaybackVersion) yield break;

        // Step 4: ONLY AFTER CAMERA IS AT NORMAL POT -> Move Cube.014 downward
        float pusherDuration = 2.5f;
        float elapsedPusher = 0f;
        while (elapsedPusher < pusherDuration)
        {
            if (myVersion != voicePlaybackVersion) yield break;
            elapsedPusher += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedPusher / pusherDuration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (c14 != null)
            {
                float y = Mathf.Lerp(cube014InitialLocalPosition.y, cube014TargetLocalPosition.y, smooth);
                c14.transform.localPosition = new Vector3(cube014InitialLocalPosition.x, y, cube014InitialLocalPosition.z);
            }
            yield return null;
        }

        if (myVersion != voicePlaybackVersion) yield break;
        if (c14 != null) c14.transform.localPosition = cube014TargetLocalPosition;

        // Side breaking voice
        UpdateUI(SideBreakingDescriptionText, SideBreakingStatusText, 10, 10, "TASK_10_SIDE_BREAKING", "Technical_Mark");

        // Step 5: Cube.013 THREE WORKING DRILL-LIKE PASSES (Whole tool horizontal + subtle vertical + whole-tool vibration)
        if (c13 != null)
        {
            yield return StartCoroutine(ExecuteCube013DrillingMotionRoutine(c13, myVersion));
        }

        if (myVersion != voicePlaybackVersion) yield break;

        // Controlled GasPoint white gas remains active
        var pipes = CoolingPipesController.Instance ?? UnityEngine.Object.FindFirstObjectByType<CoolingPipesController>();
        if (pipes != null)
        {
            pipes.IntensifyCoolingGas();
        }

        yield return new WaitForSeconds(0.4f);
        if (myVersion != voicePlaybackVersion) yield break;

        // Step 6: Plane.045 instantaneous SNAP directly to target position (-78.4199982)
        if (p045 == null) p045 = ResolvePlane045Target();
        if (p045 != null)
        {
            p045.transform.localPosition = plane045TargetLocalPosition;
        }

        EnsurePlane045_1Material();
        EnsureSideBreakingTool5Active();

        // Step 7: Enable existing red shell component under Plane.045 and start gradual formation over 2.0s
        if (redShell == null) redShell = ResolveRedShellController();
        if (redShell != null)
        {
            redShell.gameObject.SetActive(true);
            redShell.StartFormation(2.0f);
        }

        // Wait until formation visually completes before playing voice!
        yield return new WaitForSeconds(2.0f);
        if (myVersion != voicePlaybackVersion) yield break;

        // Step 8: Red shell established.
        // VO: "Observe the red shell formation in the shell."
        task10SubStage = 1;
        UpdateUI(ObserveRedShellDescriptionText, ObserveRedShellStatusText, 10, 10, "TASK_10_OBSERVE_RED_SHELL", "Technical_Mark");

        // Gas gradually weakens over 6.0s now that red shell formation has established
        if (pipes != null)
        {
            pipes.StartGasDissipation(6.0f);
        }

        // Setup Task 10 Next progression interception for subsequent stages
        OnInterceptTaskCompletion = HandleTask10ProgressionIntercept;

        ptmCraneAnimationCoroutine = null;
    }

    private IEnumerator ExecuteCube013DrillingMotionRoutine(GameObject c13, int capturedVersion)
    {
        if (c13 == null) yield break;

        c13.SetActive(true);

        // 1. Particle effect starts IMMEDIATELY at movement start
        var dp = ResolveDustParticleController();
        if (dp != null)
        {
            dp.PlayDust();
            dp.TriggerBurst(10);
        }

        Vector3 p1Start = new Vector3(-6.17000008f, 1.02999997f, -0.879999995f);
        Vector3 p1End   = new Vector3(-6.54199982f, 1.02999997f, -0.879999995f);
        Vector3 p2Start = new Vector3(-7.23699999f, 1.02999997f, -0.879999995f);
        Vector3 p2End   = new Vector3(-6.65399981f, 1.02999997f, -0.879999995f);
        Vector3 p3Start = new Vector3(-6.53599977f, 1.02999997f, -0.879999995f);
        Vector3 p3End   = new Vector3(-6.91900015f, 1.02999997f, -0.879999995f);

        Quaternion baseRot = Quaternion.Euler(270f, 0f, 0f);
        c13.transform.localPosition = p1Start;
        c13.transform.localRotation = baseRot;

        float totalTime = 0f;
        const float vibFreq = 8f;       // 8 Hz mechanical contact vibration
        const float vibPosAmp = 0.005f; // ~5mm whole-tool vibration
        const float vibRotAmp = 0.45f;  // ~0.45 deg tilt vibration
        const float vertAmp = 0.022f;   // ~22mm subtle vertical wave

        // PASS 1: p1Start -> p1End (Smooth, slow drilling feed)
        float pass1Duration = 2.0f;
        float elapsed = 0f;
        while (elapsed < pass1Duration)
        {
            if (capturedVersion != voicePlaybackVersion) yield break;
            elapsed += Time.deltaTime;
            totalTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pass1Duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float x = Mathf.Lerp(p1Start.x, p1End.x, smoothT);
            float yOffset = Mathf.Sin(smoothT * Mathf.PI * 2f) * vertAmp;
            float vibX = Mathf.Sin(totalTime * vibFreq * Mathf.PI * 2f) * vibPosAmp;
            float vibY = Mathf.Cos(totalTime * vibFreq * Mathf.PI * 2f) * (vibPosAmp * 0.6f);
            float vibRot = Mathf.Sin(totalTime * vibFreq * Mathf.PI * 2f) * vibRotAmp;

            c13.transform.localPosition = new Vector3(x + vibX, p1Start.y + yOffset + vibY, p1Start.z);
            c13.transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, vibRot);
            yield return null;
        }

        // TRANSITION 1->2: Smooth continuous repositioning blend (no teleport / no snap)
        float trans1Duration = 0.8f;
        elapsed = 0f;
        while (elapsed < trans1Duration)
        {
            if (capturedVersion != voicePlaybackVersion) yield break;
            elapsed += Time.deltaTime;
            totalTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / trans1Duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float x = Mathf.Lerp(p1End.x, p2Start.x, smoothT);
            float y = Mathf.Lerp(p1End.y, p2Start.y, smoothT);
            float vibX = Mathf.Sin(totalTime * vibFreq * Mathf.PI * 2f) * (vibPosAmp * 0.5f);
            float vibRot = Mathf.Sin(totalTime * vibFreq * Mathf.PI * 2f) * (vibRotAmp * 0.5f);

            c13.transform.localPosition = new Vector3(x + vibX, y, p1Start.z);
            c13.transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, vibRot);
            yield return null;
        }

        // PASS 2: p2Start -> p2End
        float pass2Duration = 2.0f;
        elapsed = 0f;
        while (elapsed < pass2Duration)
        {
            if (capturedVersion != voicePlaybackVersion) yield break;
            elapsed += Time.deltaTime;
            totalTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pass2Duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float x = Mathf.Lerp(p2Start.x, p2End.x, smoothT);
            float yOffset = Mathf.Sin(smoothT * Mathf.PI * 2f) * vertAmp;
            float vibX = Mathf.Sin(totalTime * vibFreq * Mathf.PI * 2f) * vibPosAmp;
            float vibY = Mathf.Cos(totalTime * vibFreq * Mathf.PI * 2f) * (vibPosAmp * 0.6f);
            float vibRot = Mathf.Sin(totalTime * vibFreq * Mathf.PI * 2f) * vibRotAmp;

            c13.transform.localPosition = new Vector3(x + vibX, p2Start.y + yOffset + vibY, p2Start.z);
            c13.transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, vibRot);
            yield return null;
        }

        // TRANSITION 2->3: Smooth continuous repositioning blend (no teleport / no snap)
        float trans2Duration = 0.6f;
        elapsed = 0f;
        while (elapsed < trans2Duration)
        {
            if (capturedVersion != voicePlaybackVersion) yield break;
            elapsed += Time.deltaTime;
            totalTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / trans2Duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float x = Mathf.Lerp(p2End.x, p3Start.x, smoothT);
            float y = Mathf.Lerp(p2End.y, p3Start.y, smoothT);
            float vibX = Mathf.Sin(totalTime * vibFreq * Mathf.PI * 2f) * (vibPosAmp * 0.5f);
            float vibRot = Mathf.Sin(totalTime * vibFreq * Mathf.PI * 2f) * (vibRotAmp * 0.5f);

            c13.transform.localPosition = new Vector3(x + vibX, y, p2Start.z);
            c13.transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, vibRot);
            yield return null;
        }

        // PASS 3: p3Start -> p3End
        float pass3Duration = 2.0f;
        elapsed = 0f;
        while (elapsed < pass3Duration)
        {
            if (capturedVersion != voicePlaybackVersion) yield break;
            elapsed += Time.deltaTime;
            totalTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pass3Duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float x = Mathf.Lerp(p3Start.x, p3End.x, smoothT);
            float yOffset = Mathf.Sin(smoothT * Mathf.PI * 2f) * vertAmp;
            float vibX = Mathf.Sin(totalTime * vibFreq * Mathf.PI * 2f) * vibPosAmp;
            float vibY = Mathf.Cos(totalTime * vibFreq * Mathf.PI * 2f) * (vibPosAmp * 0.6f);
            float vibRot = Mathf.Sin(totalTime * vibFreq * Mathf.PI * 2f) * vibRotAmp;

            c13.transform.localPosition = new Vector3(x + vibX, p3Start.y + yOffset + vibY, p3Start.z);
            c13.transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, vibRot);
            yield return null;
        }

        // SETTLE & SMOOTH DECELERATION (Gradual ease-out to zero vibration, settle naturally)
        float settleDuration = 0.5f;
        elapsed = 0f;
        Vector3 settleStart = c13.transform.localPosition;
        Quaternion rotStart = c13.transform.localRotation;
        while (elapsed < settleDuration)
        {
            if (capturedVersion != voicePlaybackVersion) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / settleDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            c13.transform.localPosition = Vector3.Lerp(settleStart, p3End, smoothT);
            c13.transform.localRotation = Quaternion.Slerp(rotStart, baseRot, smoothT);
            yield return null;
        }

        c13.transform.localPosition = p3End;
        c13.transform.localRotation = baseRot;
        c13.transform.eulerAngles = new Vector3(270f, 0f, 0f);

        // Particle effect smoothly stops only after movement is complete
        if (dp != null) dp.StopDust();
    }

    private int task10SubStage = 0;

    public bool HandleTask10ProgressionIntercept()
    {
        ResolveHandler();
        if (handler != null && handler.currentTask != 9)
        {
            task10SubStage = 0;
            OnInterceptTaskCompletion = null;
            return false;
        }

        var ui = ResolvePotLeakageUI();
        if (task10SubStage == 0)
        {
            // Early click during PTM crane: fast-forward physical state to established red shell
            task10SubStage = 1;
            if (ptmCraneAnimationCoroutine != null)
            {
                StopCoroutine(ptmCraneAnimationCoroutine);
                ptmCraneAnimationCoroutine = null;
            }
            var crane = ResolveCraneTarget();
            if (crane != null) crane.transform.position = craneTargetPosition;
            var c14 = ResolveCube014Target();
            if (c14 != null) c14.transform.localPosition = cube014TargetLocalPosition;
            var c13 = ResolveCube013Target();
            if (c13 != null)
            {
                c13.transform.localPosition = new Vector3(-6.91900015f, 1.02999997f, -0.879999995f);
                c13.transform.localRotation = Quaternion.Euler(270f, 0f, 0f);
            }
            var cyl = ResolveCylinder003Target();
            if (cyl != null) cyl.SetActive(false);
            StopDrillVibration();
            var dp = ResolveDustParticleController();
            if (dp != null) dp.InitializeComponents();
            var p045 = ResolvePlane045Target();
            if (p045 != null) p045.transform.localPosition = plane045TargetLocalPosition;
            EnsurePlane045_1Material();
            EnsureSideBreakingTool5Active();
            var rs = ResolveRedShellController();
            if (rs != null)
            {
                rs.gameObject.SetActive(true);
                rs.formationProgress = 1.0f;
                rs.UpdateVisuals();
            }
            SnapToNormalPot();

            UpdateUI(ObserveRedShellDescriptionText, ObserveRedShellStatusText, 10, 10, "TASK_10_OBSERVE_RED_SHELL", "Technical_Mark");
            return true;
        }
        else if (task10SubStage == 1)
        {
            // Trainee clicks Next on Observe Red Shell:
            // Progression: "Let us use hoes to reduce the red shell formation."
            task10SubStage = 2;
            EnsureSideBreakingTool5Active();
            EnsurePlane045_1Material();

            UpdateUI(UseHoesDescriptionText, UseHoesStatusText, 10, 10, "TASK_10_USE_HOES", "Technical_Mark");
            Debug.Log("[SequenceHelperFunctions] Task 10: Step 2 'Let us use hoes to reduce the red shell formation.' played.");
            return true;
        }
        else if (task10SubStage == 2)
        {
            // Trainee clicks Next on Use Hoes:
            // Camera snaps directly to TransformPoints/PotControlMachine
            task10SubStage = 3;
            var camCtrl = ResolveCameraController();
            if (camCtrl != null)
            {
                camCtrl.MoveToPotControlMachine();
            }
            else
            {
                var pcm = GameObject.Find("CameraSystem/TransformPoints/PotControlMachine") ?? GameObject.Find("TransformPoints/PotControlMachine");
                if (pcm != null) SnapCameraToTransform(pcm.transform);
            }

            UpdateUI(VoltageCheckDescriptionText, VoltageCheckStatusText, 10, 10, "TASK_10_VOLTAGE_CHECK", "Technical_Mark");

            Debug.Log("[SequenceHelperFunctions] Task 10: Snapped to PotControlMachine and voltage check voice played. Waiting for audio to finish or Next click.");

            if (waitForVoltageAudioCoroutine != null) StopCoroutine(waitForVoltageAudioCoroutine);
            if (Application.isPlaying)
            {
                waitForVoltageAudioCoroutine = StartCoroutine(WaitForVoltageAudioOrAdvanceRoutine(voicePlaybackVersion));
            }
            return true;
        }
        else if (task10SubStage == 3)
        {
            // Trainee clicked Next early during voltage check speech:
            if (waitForVoltageAudioCoroutine != null)
            {
                StopCoroutine(waitForVoltageAudioCoroutine);
                waitForVoltageAudioCoroutine = null;
            }
            TransitionToHoeStage();
            return true;
        }
        else if (task10SubStage == 4)
        {
            // Currently at Hoe rack stage, waiting for trainee to click round_PIPES/HoldSpiral.001
            Debug.Log("[SequenceHelperFunctions] Trainee must click round_PIPES/HoldSpiral.001 to pick up the hose.");
            return true;
        }
        else if (task10SubStage == 5)
        {
            // Currently at AirUnlock stage, waiting for trainee to click electrical box handle
            Debug.Log("[SequenceHelperFunctions] Trainee must click electrical box handle to open the FLR block valve.");
            return true;
        }
        else if (task10SubStage == 6)
        {
            // Currently at PIPES (2) highlight stage, waiting for trainee to click PIPES (2)
            Debug.Log("[SequenceHelperFunctions] Trainee must click PIPES (2) to proceed with cooling.");
            return true;
        }
        else if (task10SubStage == 7)
        {
            // Trainee clicked Next on 'Let us start bath packing.' -> Start bath packing visual!
            StartBathPackingSequence();
            return true;
        }
        else if (task10SubStage == 8)
        {
            // Trainee clicked Next on 'Packing is done successfully.' -> Move to PotControlMachine safety check
            if (bathPackingAnimationCoroutine != null)
            {
                StopCoroutine(bathPackingAnimationCoroutine);
                bathPackingAnimationCoroutine = null;
            }
            var c51 = ResolveCube051Target();
            if (c51 != null) c51.transform.localPosition = cube051TargetLocalPosition;
            var powder = ResolveBathPowderController();
            if (powder != null) powder.PlayPowder();
            var p045 = ResolvePlane045Target();
            if (p045 != null) p045.transform.localPosition = plane045BathPackingTargetLocalPosition;

            TransitionToBathPackingVoltageCheck();
            return true;
        }
        else if (task10SubStage == 9)
        {
            // Trainee clicked Next on 'Check if the voltage is within the safer limit range between 4.1 to 4.3 V.' -> Move to Final Normal Pot verification
            TransitionToFinalNormalPotVerification();
            return true;
        }
        else if (task10SubStage >= 10)
        {
            if (!isFinalHoodClosingCompleted)
            {
                Debug.Log("[SequenceHelperFunctions] Task 10: Trainee must close the hood before completing the module.");
                return true;
            }

            // Trainee clicked Next after hood is closed -> Complete Task 10!
            task10SubStage = 0;
            OnInterceptTaskCompletion = null;
            return false;
        }

        return false;
    }

    private IEnumerator WaitForVoltageAudioOrAdvanceRoutine(int capturedVersion)
    {
        yield return new WaitForSeconds(0.5f);
        var mgr = TruckTyreReplacement.Core.Manager.Instance;
        while (mgr != null && mgr.IsSpeaking && capturedVersion == voicePlaybackVersion && task10SubStage == 3)
        {
            yield return null;
        }

        if (capturedVersion != voicePlaybackVersion || task10SubStage != 3) yield break;

        yield return new WaitForSeconds(0.2f);
        if (capturedVersion != voicePlaybackVersion || task10SubStage != 3) yield break;

        Debug.Log("[SequenceHelperFunctions] Voltage check audio finished naturally. Auto-transitioning to Hoe stage.");
        TransitionToHoeStage();
    }

    public enum HoldSpiralVisualState
    {
        StateA_BeforeHoe,
        StateB_CameraAtHoe,
        StateC_HoeInteraction,
        StateD_CameraAtNormalPotOperation,
        StateE_ClickedAndDisabled
    }

    public void ApplyHoldSpiralState(HoldSpiralVisualState state)
    {
        var hs001 = ResolveHoldSpiral001Target();
        var hsRackHi = hs001 != null ? hs001.GetComponent<PotLeakage.Interaction.HoeInteraction>() : null;
        var hsPotGo = ResolveHoldSpiral001MatTarget();

        switch (state)
        {
            case HoldSpiralVisualState.StateB_CameraAtHoe:
                // Camera at Hoe:
                // holdSpiral001Target at round_PIPES:
                // ACTIVE, MESH VISIBLE, ORIGINAL MATERIAL VISIBLE
                if (hs001 != null)
                {
                    hs001.SetActive(true);
                    if (hsRackHi != null)
                    {
                        hsRackHi.InitializeComponents();
                        hsRackHi.RestoreOriginalMaterial();
                        hsRackHi.LogDiagnosticState("StateB_CameraAtHoe");
                    }
                }
                if (hsPotGo != null) hsPotGo.SetActive(false);
                break;

            case HoldSpiralVisualState.StateC_HoeInteraction:
                // Hoe Interaction:
                // holdSpiral001Target: ACTIVE, MESH VISIBLE, highlight yellow
                if (hs001 != null)
                {
                    hs001.SetActive(true);
                    if (hsRackHi != null)
                    {
                        hsRackHi.SetHighlight(true);
                        hsRackHi.LogDiagnosticState("StateC_HoeInteraction");
                    }
                }
                if (hsPotGo != null) hsPotGo.SetActive(false);
                break;

            case HoldSpiralVisualState.StateD_CameraAtNormalPotOperation:
            case HoldSpiralVisualState.StateE_ClickedAndDisabled:
                // HoldSpiral.001 clicked: disabled, no highlight, original material restored
                if (hs001 != null)
                {
                    if (hsRackHi != null)
                    {
                        hsRackHi.SetHighlight(false);
                        hsRackHi.RestoreOriginalMaterial();
                    }
                    hs001.SetActive(false);
                }

                if (hsPotGo != null)
                {
                    var potHi = hsPotGo.GetComponent<PotLeakage.Interaction.HoeInteraction>();
                    if (potHi != null)
                    {
                        potHi.SetHighlight(false);
                        potHi.RestoreOriginalMaterial();
                    }
                    hsPotGo.SetActive(false);
                }
                break;
        }
    }

    public void EnsureProcessParticlesActive()
    {
        var moltenCtrl = UnityEngine.Object.FindFirstObjectByType<PotLeakage.VFX.MoltenAluminiumVFXController>();
        if (moltenCtrl != null && moltenCtrl.streamParticles != null && moltenCtrl.streamParticles.particleSys != null && !moltenCtrl.streamParticles.particleSys.isPlaying)
        {
            moltenCtrl.streamParticles.particleSys.Play(true);
        }
        var smoke = GameObject.Find("Smoke") ?? GameObject.Find("smoke");
        if (smoke != null)
        {
            var smokePS = smoke.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in smokePS)
            {
                if (ps != null && !ps.isPlaying) ps.Play(true);
            }
        }
        var dp = ResolveDustParticleController();
        if (dp != null && dp.dustParticleSystem != null && !dp.dustParticleSystem.isPlaying)
        {
            dp.dustParticleSystem.Play(true);
        }
    }

    public void EnsureRedShellObservationParticlesActive()
    {
        // 1. PIPES/GasDensity_Fill & GasPoint systems
        var pipes = ResolvePipesTarget();
        if (pipes != null)
        {
            if (!pipes.activeSelf) pipes.SetActive(true);
            var pipesCtrl = pipes.GetComponent<CoolingPipesController>();
            if (pipesCtrl != null)
            {
                pipesCtrl.isCoolingActive = true;
                pipesCtrl.IntensifyCoolingGas();
            }
            var pipesPS = pipes.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in pipesPS)
            {
                if (ps != null)
                {
                    if (!ps.gameObject.activeSelf) ps.gameObject.SetActive(true);
                    if (!ps.isPlaying) ps.Play(true);
                }
            }
        }

        // 2. Cool/GasDensity_Fill (HoldSpiral.002) - keep HoldSpiral.002 permanently disabled
        var hs2 = ResolveHoldSpiral002Target();
        if (hs2 != null) hs2.SetActive(false);
        var coolGas = ResolveHoldSpiralCoolGas();
        if (coolGas != null)
        {
            coolGas.transform.localEulerAngles = new Vector3(72.3570251f, 250.899704f, 252.822556f);
            if (!coolGas.gameObject.activeSelf) coolGas.gameObject.SetActive(true);
            if (!coolGas.isPlaying) coolGas.Play(true);
        }


        // 4. Existing molten leakage particle effect
        var moltenCtrl = UnityEngine.Object.FindFirstObjectByType<PotLeakage.VFX.MoltenAluminiumVFXController>();
        if (moltenCtrl != null)
        {
            if (!moltenCtrl.gameObject.activeSelf) moltenCtrl.gameObject.SetActive(true);
            if (moltenCtrl.streamParticles != null && moltenCtrl.streamParticles.particleSys != null && !moltenCtrl.streamParticles.particleSys.isPlaying)
            {
                moltenCtrl.streamParticles.particleSys.Play(true);
            }
        }

        // 5. Existing smoke effect
        var smoke = GameObject.Find("Smoke") ?? GameObject.Find("smoke");
        if (smoke != null)
        {
            if (!smoke.activeSelf) smoke.SetActive(true);
            var smokePS = smoke.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in smokePS)
            {
                if (ps != null && !ps.isPlaying) ps.Play(true);
            }
        }

        // 6. DustParticle
        var dp = ResolveDustParticleController();
        if (dp != null && dp.dustParticleSystem != null && !dp.dustParticleSystem.isPlaying)
        {
            dp.dustParticleSystem.Play(true);
        }
    }

    public void LogElectricalBoxState(string stage)
    {
        var handleGo = ResolveElectricalBoxHandle();
        var enableGo = ResolveElectricalBoxEnable();

        bool handleActive = handleGo != null && handleGo.activeSelf;
        var handleMr = handleGo != null ? handleGo.GetComponent<MeshRenderer>() : null;
        bool handleRenderer = handleMr != null && handleMr.enabled;
        var handleMf = handleGo != null ? handleGo.GetComponent<MeshFilter>() : null;
        string handleMesh = handleMf != null && handleMf.sharedMesh != null ? handleMf.sharedMesh.name : "null";
        string handleMaterial = handleMr != null && handleMr.sharedMaterial != null ? handleMr.sharedMaterial.name : "null";

        bool enableActive = enableGo != null && enableGo.activeSelf;
        var enableMr = enableGo != null ? enableGo.GetComponent<MeshRenderer>() : null;
        bool enableRenderer = enableMr != null && enableMr.enabled;
        var enableMf = enableGo != null ? enableGo.GetComponent<MeshFilter>() : null;
        string enableMesh = enableMf != null && enableMf.sharedMesh != null ? enableMf.sharedMesh.name : "null";
        string enableMaterial = enableMr != null && enableMr.sharedMaterial != null ? enableMr.sharedMaterial.name : "null";

        Debug.Log($"[ELECTRICAL BOX] ({stage})\n" +
                  $"[ELECTRICAL BOX] Handle active={handleActive}\n" +
                  $"[ELECTRICAL BOX] Handle renderer={handleRenderer}\n" +
                  $"[ELECTRICAL BOX] Handle mesh={handleMesh}\n" +
                  $"[ELECTRICAL BOX] Handle material={handleMaterial}\n" +
                  $"[ELECTRICAL BOX] Enable active={enableActive}\n" +
                  $"[ELECTRICAL BOX] Enable renderer={enableRenderer}\n" +
                  $"[ELECTRICAL BOX] Enable mesh={enableMesh}\n" +
                  $"[ELECTRICAL BOX] Enable material={enableMaterial}");
    }

    public void TransitionToHoeStage()
    {
        task10SubStage = 4;

        // Invalidate previous speech generation and stop previous voice
        voicePlaybackVersion++;
        var mgr = TruckTyreReplacement.Core.Manager.Instance;
        if (mgr != null) mgr.StopSpeech();

        // 1. Camera snap to Hoe
        SnapCameraToHoe();

        // 2. Enable HoldSpiral.001, restore original material
        ApplyHoldSpiralState(HoldSpiralVisualState.StateB_CameraAtHoe);

        // 3. Start required process particles
        EnsureProcessParticlesActive();

        // 4. Apply interaction highlight on HoldSpiral.001
        ApplyHoldSpiralState(HoldSpiralVisualState.StateC_HoeInteraction);

        // Ensure HoldSpiral.002 is disabled
        var hs2 = ResolveHoldSpiral002Target();
        if (hs2 != null) hs2.SetActive(false);

        // 5. Display description, status & start voice: "Pick up the hose to reduce the red shell formation."
        UpdateUI(PickUpHoesDescriptionText, PickUpHoesStatusText, 10, 10, "TASK_10_PICK_UP_HOES", "Technical_Mark");

        EnsureSideBreakingTool5Active();
        Debug.Log("[SequenceHelperFunctions] Transitioned to Hoe stage: Camera snapped to Hoe, hoes visible & highlighted, pick up hose VO started.");
    }

    public void OnHoeClicked()
    {
        task10SubStage = 5;

        // 1. Stop highlight/blink and restore original material on HoldSpiral.001
        var hs001 = ResolveHoldSpiral001Target();
        if (hs001 != null)
        {
            var hsRackHi = hs001.GetComponent<PotLeakage.Interaction.HoeInteraction>();
            if (hsRackHi != null)
            {
                hsRackHi.SetHighlight(false);
                hsRackHi.RestoreOriginalMaterial();
            }
            // 2. Disable HoldSpiral.001 only after the click
            hs001.SetActive(false);
        }

        var matGo = ResolveHoldSpiral001MatTarget();
        if (matGo != null)
        {
            var matHi = matGo.GetComponent<PotLeakage.Interaction.HoeInteraction>();
            if (matHi != null)
            {
                matHi.SetHighlight(false);
                matHi.RestoreOriginalMaterial();
            }
            matGo.SetActive(false);
        }

        // 3. SNAP camera to TransformPoints/AirUnlock
        SnapToAirUnlock();

        // 4. Highlight existing electrical box handle yellow and ensure it is active & visible
        var handleGo = ResolveElectricalBoxHandle();
        if (handleGo != null)
        {
            handleGo.SetActive(true);
            var mr = handleGo.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = true;

            var handleHi = handleGo.GetComponent<PotLeakage.Interaction.ElectricalBoxHandleInteraction>();
            if (handleHi == null) handleHi = handleGo.AddComponent<PotLeakage.Interaction.ElectricalBoxHandleInteraction>();
            handleHi.SetHighlight(true);
        }

        var enableGo = ResolveElectricalBoxEnable();
        if (enableGo != null)
        {
            enableGo.SetActive(false);
        }

        LogElectricalBoxState("Entering Open FLR Block Valve");

        // 5. Description, status + voice: "Open the FLR block valve."
        voicePlaybackVersion++;
        var mgr = TruckTyreReplacement.Core.Manager.Instance;
        if (mgr != null) mgr.StopSpeech();

        UpdateUI(OpenFLRValveDescriptionText, OpenFLRValveStatusText, 10, 10, "TASK_10_OPEN_FLR_VALVE", "Technical_Mark");

        EnsureSideBreakingTool5Active();
        EnsureProcessParticlesActive();
        Debug.Log("[SequenceHelperFunctions] SubStage 5: HoldSpiral.001 clicked -> disabled, camera snapped to AirUnlock, electrical box handle highlighted yellow, Open FLR valve VO started.");
    }

    public void OnHoldSpiral001MatClicked()
    {
        OnHoeClicked();
    }

    private Coroutine electricalBoxHandleCoroutine = null;

    public void OnElectricalBoxHandleClicked()
    {
        task10SubStage = 6;

        // 1. Stop highlight/blink and disable handle
        var handleGo = ResolveElectricalBoxHandle();
        if (handleGo != null)
        {
            var handleHi = handleGo.GetComponent<PotLeakage.Interaction.ElectricalBoxHandleInteraction>();
            if (handleHi != null) handleHi.SetHighlight(false);

            handleGo.SetActive(false);
        }

        // 2. Enable existing electrical box enable GameObject and ensure it is visible
        var enableGo = ResolveElectricalBoxEnable();
        if (enableGo != null)
        {
            enableGo.SetActive(true);
            var mr = enableGo.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = true;
            var smr = enableGo.GetComponent<SkinnedMeshRenderer>();
            if (smr != null) smr.enabled = true;
        }

        LogElectricalBoxState("Handle Clicked -> Enable Active (1.5s observation delay begun)");

        if (electricalBoxHandleCoroutine != null)
        {
            StopCoroutine(electricalBoxHandleCoroutine);
            electricalBoxHandleCoroutine = null;
        }

        if (Application.isPlaying)
        {
            electricalBoxHandleCoroutine = StartCoroutine(ElectricalBoxHandleSequenceRoutine());
        }
        else
        {
            ExecuteElectricalBoxHandleTransition();
        }
    }

    private IEnumerator ElectricalBoxHandleSequenceRoutine()
    {
        int myVersion = ++voicePlaybackVersion;

        // Keep the enabled electrical box state visible to trainee for exactly 1.5 seconds
        yield return new WaitForSeconds(1.5f);
        if (myVersion != voicePlaybackVersion || task10SubStage != 6) yield break;

        ExecuteElectricalBoxHandleTransition();
        electricalBoxHandleCoroutine = null;
    }

    public void ExecuteElectricalBoxHandleTransition()
    {
        // 3. Direct SNAP camera to TransformPoints/Normal Pot Operation
        SnapToNormalPotOperation();

        // 4. Enable existing PIPES (2) GameObject and highlight/blink PIPES (2)
        var p2 = ResolvePipes2Target();
        if (p2 != null)
        {
            p2.SetActive(true);
            var p2Hi = p2.GetComponent<PotLeakage.Interaction.Pipes2Interaction>();
            if (p2Hi == null) p2Hi = p2.AddComponent<PotLeakage.Interaction.Pipes2Interaction>();
            p2Hi.SetHighlight(true);

            // IMPORTANT: PIPES (2) particle system is NOT playing yet!
            var p2Ctrl = p2.GetComponent<PotLeakage.VFX.CoolingPipesController>();
            if (p2Ctrl != null)
            {
                p2Ctrl.isCoolingActive = false;
                p2Ctrl.StopGasDissipation();
            }

            var p2PS = p2.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in p2PS)
            {
                if (ps != null)
                {
                    var main = ps.main;
                    main.playOnAwake = false;
                    var em = ps.emission;
                    em.enabled = false;
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        // 5. Ensure HoldSpiral.002 remains disabled & enable/play cool gas particle effect if present
        var hs2 = ResolveHoldSpiral002Target();
        if (hs2 != null) hs2.SetActive(false);
        var coolGas = ResolveHoldSpiralCoolGas();
        if (coolGas != null)
        {
            coolGas.transform.localEulerAngles = new Vector3(72.3570251f, 250.899704f, 252.822556f);
            if (!coolGas.gameObject.activeSelf) coolGas.gameObject.SetActive(true);
            if (!coolGas.isPlaying) coolGas.Play(true);
        }

        // 6. Red shell progressive hot lava formation (2.0s)
        var rs = ResolveRedShellController();
        if (rs != null)
        {
            rs.gameObject.SetActive(true);
            rs.TriggerProgressiveFormation(2.0f);
        }

        EnsureRedShellObservationParticlesActive();
        EnsureSideBreakingTool5Active();

        // 7. Description, status + voice: "Now that the valve is open, cool air is released over there."
        voicePlaybackVersion++;
        var mgr = TruckTyreReplacement.Core.Manager.Instance;
        if (mgr != null) mgr.StopSpeech();

        UpdateUI(ValveOpenCoolAirDescriptionText, ValveOpenCoolAirStatusText, 10, 10, "TASK_10_VALVE_OPEN_COOL_AIR", "Technical_Mark");

        Debug.Log("[SequenceHelperFunctions] SubStage 6: Electrical box handle clicked -> 1.5s delay complete, camera snapped to Normal Pot, PIPES (2) enabled & highlighted (no particles yet), cool air VO playing.");
    }

    public void OnPipes2Clicked()
    {
        task10SubStage = 7;

        // Play gas sound once when PIPES (2) is clicked
        PlayGasSFX();

        // 1. Stop PIPES (2) highlight/blink, restore original materials
        var p2 = ResolvePipes2Target();
        if (p2 != null)
        {
            var p2Hi = p2.GetComponent<PotLeakage.Interaction.Pipes2Interaction>();
            if (p2Hi != null)
            {
                p2Hi.SetHighlight(false);
                p2Hi.RestoreOriginalMaterials();
            }
            // 2. Disable PIPES (2)
            p2.SetActive(false);
        }

        // 3. Enable PIPES (1) and start its particle/cooling effect
        ActivatePipes1();

        // 4. Invalidate previous voice & stop speech
        voicePlaybackVersion++;
        var mgr = TruckTyreReplacement.Core.Manager.Instance;
        if (mgr != null) mgr.StopSpeech();

        // 5. Description, status + voice: "Let us start bath packing."
        UpdateUI(StartBathPackingDescriptionText, StartBathPackingStatusText, 10, 10, "TASK_10_START_BATH_PACKING", "Technical_Mark");

        Debug.Log("[SequenceHelperFunctions] SubStage 7: PIPES (2) clicked -> disabled, PIPES (1) active & particles playing, 'Let us start bath packing.' VO started.");
    }

    public void StartBathPackingSequence()
    {
        task10SubStage = 8;

        if (bathPackingAnimationCoroutine != null)
        {
            if (Application.isPlaying) StopCoroutine(bathPackingAnimationCoroutine);
            bathPackingAnimationCoroutine = null;
        }

        if (Application.isPlaying)
        {
            bathPackingAnimationCoroutine = StartCoroutine(AnimateBathPackingRoutine());
        }
        else
        {
            // Direct apply for edit/test mode
            var c14 = ResolveCube014Target();
            if (c14 != null) c14.SetActive(false);
            var c13 = ResolveCube013Target();
            if (c13 != null) c13.SetActive(false);
            var c51 = ResolveCube051Target();
            if (c51 != null)
            {
                c51.SetActive(true);
                c51.transform.localPosition = cube051TargetLocalPosition;
            }
            var camCtrl = ResolveCameraController();
            if (camCtrl != null) camCtrl.MoveToNormalPot();
            else
            {
                var np = GameObject.Find("CameraSystem/TransformPoints/Normal Pot") ?? GameObject.Find("TransformPoints/Normal Pot");
                if (np != null) SnapCameraToTransform(np.transform);
            }
            var powder = ResolveBathPowderController();
            if (powder != null)
            {
                if (powder.impactParticleSystem != null)
                {
                    powder.impactParticleSystem.transform.localPosition = new Vector3(0.00899999961f, -0.0353999995f, 0.0027999999f);
                    powder.impactParticleSystem.transform.localEulerAngles = new Vector3(4.26886828e-07f, 353.050018f, 60.9000015f);
                }
                powder.PlayPowder();
            }
            var p045 = ResolvePlane045Target();
            if (p045 != null) p045.transform.localPosition = plane045BathPackingTargetLocalPosition;
            var rs = ResolveRedShellController();
            if (rs != null) rs.DisableRedShell();
            var redGo = ResolveRedGameObjectTarget();
            if (redGo != null) redGo.SetActive(false);
        }
    }

    private IEnumerator AnimateBathPackingRoutine()
    {
        // 1. Disable Cube.014 and Cube.013 initially
        var c14 = ResolveCube014Target();
        if (c14 != null) c14.SetActive(false);
        var c13 = ResolveCube013Target();
        if (c13 != null) c13.SetActive(false);

        // 2. Enable Cube.051 at initial position
        var c51 = ResolveCube051Target();
        if (c51 != null)
        {
            c51.SetActive(true);
            c51.transform.localPosition = cube051InitialLocalPosition;
        }

        // 3. Move Cube.051 slowly (Y only) to target position Vector3(-6.80000019, 1.23000002, -0.389999986)
        float duration = 3.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            if (c51 != null)
            {
                c51.transform.localPosition = Vector3.Lerp(cube051InitialLocalPosition, cube051TargetLocalPosition, smoothT);
            }
            yield return null;
        }
        if (c51 != null) c51.transform.localPosition = cube051TargetLocalPosition;

        // 4. Snap camera from Normal Pot Operation to TransformPoints/Normal Pot BEFORE BathPowder_Impact plays
        var camCtrlAnim = ResolveCameraController();
        if (camCtrlAnim != null)
        {
            camCtrlAnim.MoveToNormalPot();
        }
        else
        {
            var np = GameObject.Find("CameraSystem/TransformPoints/Normal Pot") ?? GameObject.Find("TransformPoints/Normal Pot");
            if (np != null) SnapCameraToTransform(np.transform);
        }
        yield return null;

        // 5. Ensure BathPowder_Impact exact fixed local transform and start powder effect
        var powderAnim = ResolveBathPowderController();
        if (powderAnim != null)
        {
            if (powderAnim.impactParticleSystem != null)
            {
                powderAnim.impactParticleSystem.transform.localPosition = new Vector3(0.00899999961f, -0.0353999995f, 0.0027999999f);
                powderAnim.impactParticleSystem.transform.localEulerAngles = new Vector3(4.26886828e-07f, 353.050018f, 60.9000015f);
            }
            powderAnim.PlayPowder();
        }

        // 5. Description, status + voice: "Maintain safe distance to avoid splashing of molten bath. Packing is started."
        voicePlaybackVersion++;
        int capturedVersion = voicePlaybackVersion;
        var mgr = TruckTyreReplacement.Core.Manager.Instance;
        if (mgr != null) mgr.StopSpeech();

        UpdateUI(SafeDistanceDescriptionText, SafeDistanceStatusText, 10, 10, "TASK_10_SAFE_DISTANCE", "Technical_Mark");

        // 6. Wait for voice to finish speaking (or max timeout)
        yield return new WaitForSeconds(0.5f);
        while (mgr != null && mgr.IsSpeaking && capturedVersion == voicePlaybackVersion)
        {
            yield return null;
        }

        // 7. Wait 3 seconds after safe distance voice finishes
        yield return new WaitForSeconds(3.0f);
        if (capturedVersion != voicePlaybackVersion || task10SubStage != 8) yield break;

        // 8. Move Line (6)/Plane.045 smoothly from -78.12 to -78.0
        var p045 = ResolvePlane045Target();
        float pDuration = 1.5f;
        float pElapsed = 0f;
        while (pElapsed < pDuration)
        {
            pElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(pElapsed / pDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            if (p045 != null)
            {
                p045.transform.localPosition = Vector3.Lerp(plane045BathPackingStartLocalPosition, plane045BathPackingTargetLocalPosition, smoothT);
            }
            yield return null;
        }
        if (p045 != null) p045.transform.localPosition = plane045BathPackingTargetLocalPosition;

        // 9. Immediately after Plane.045 completely reaches target position:
        // Disable red shell component and disable Red GameObject
        var rs = ResolveRedShellController();
        if (rs != null) rs.DisableRedShell();
        var redGo = ResolveRedGameObjectTarget();
        if (redGo != null) redGo.SetActive(false);

        // Stop powder emission
        if (powderAnim != null) powderAnim.StopPowder();

        // 10. Description, status + voice: "Packing is done successfully. The red shell intensity has been reduced."
        voicePlaybackVersion++;
        if (mgr != null) mgr.StopSpeech();

        UpdateUI(PackingDoneDescriptionText, PackingDoneStatusText, 10, 10, "TASK_10_PACKING_DONE", "Technical_Mark");

        bathPackingAnimationCoroutine = null;
        Debug.Log("[SequenceHelperFunctions] SubStage 8: Bath packing completed. Plane.045 positioned at -78.0, Red Shell & Red GameObject disabled, VO 'Packing is done successfully.' playing.");
    }

    public void TransitionToBathPackingVoltageCheck()
    {
        task10SubStage = 9;

        // 1. Direct SNAP camera to TransformPoints/PotControlMachine
        var camCtrl = ResolveCameraController();
        if (camCtrl != null)
        {
            camCtrl.MoveToPotControlMachine();
        }
        else
        {
            var pcm = GameObject.Find("CameraSystem/TransformPoints/PotControlMachine") ?? GameObject.Find("TransformPoints/PotControlMachine");
            if (pcm != null) SnapCameraToTransform(pcm.transform);
        }

        // 2. Invalidate previous voice & stop speech
        voicePlaybackVersion++;
        var mgr = TruckTyreReplacement.Core.Manager.Instance;
        if (mgr != null) mgr.StopSpeech();

        // 3. Description, status + voice: "Check if the voltage is within the safer limit range between 4.1 to 4.3 V."
        UpdateUI(VoltageSaferLimitRangeDescriptionText, VoltageSaferLimitRangeStatusText, 10, 10, "TASK_10_VOLTAGE_SAFER_LIMIT", "Technical_Mark");

        Debug.Log("[SequenceHelperFunctions] SubStage 9: Camera snapped to PotControlMachine, 'Check if the voltage is within the safer limit range between 4.1 to 4.3 V.' VO started.");
    }

    public void TransitionToFinalNormalPotVerification()
    {
        task10SubStage = 10;
        isFinalHoodClosingCompleted = false;

        // 1. Direct SNAP camera to TransformPoints/Normal Pot
        SnapToNormalPot();

        // 2. Ensure visual state: Cube.051 = DISABLED, Cube.013 = DISABLED
        var c51 = ResolveCube051Target();
        if (c51 != null) c51.SetActive(false);
        var c13 = ResolveCube013Target();
        if (c13 != null) c13.SetActive(false);
        var c14 = ResolveCube014Target();
        if (c14 != null) c14.SetActive(false);

        // Ensure HoldSpiral.002 is disabled
        var hs2 = ResolveHoldSpiral002Target();
        if (hs2 != null) hs2.SetActive(false);

        // Ensure Red Shell & Red GameObject remain disabled
        var rs = ResolveRedShellController();
        if (rs != null) rs.DisableRedShell();
        var redGo = ResolveRedGameObjectTarget();
        if (redGo != null) redGo.SetActive(false);

        // 3. STOP ALL LEAKAGE EFFECTS FIRST BEFORE HOOD CLOSING:
        DeactivatePipes1();
        var p1 = GameObject.Find("PIPES (1)");
        if (p1 != null) p1.SetActive(false);

        DeactivateCoolingPipes();
        var p = ResolvePipesTarget();
        if (p != null) p.SetActive(false);

        var vfxGo = GameObject.Find("VFX/MoltenAluminium_VFX") ?? GameObject.Find("MoltenAluminium_VFX");
        if (vfxGo != null)
        {
            var vfxCtrl = vfxGo.GetComponent<PotLeakage.VFX.MoltenAluminiumVFXController>();
            if (vfxCtrl != null)
            {
                vfxCtrl.StopMoltenMetalOverflow();
            }
            var allPS = vfxGo.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in allPS)
            {
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.gameObject.SetActive(false);
                }
            }
            var meshRenderers = vfxGo.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in meshRenderers)
            {
                if (mr != null) mr.enabled = false;
            }
        }

        // 4. Invalidate previous voice & stop speech
        voicePlaybackVersion++;
        var mgr = TruckTyreReplacement.Core.Manager.Instance;
        if (mgr != null) mgr.StopSpeech();

        // 5. Description, status + voice: "Now that the leakage and the shell formation is completely stopped you have completed the pot leakage module now close the hood."
        UpdateUI(FinalVerificationDescriptionText, FinalVerificationStatusText, 10, 10, "TASK_10_FINAL_VERIFICATION", "Technical_Mark");

        // 6. Start Hood Closing Interaction
        StartHoodClosingInteraction();

        Debug.Log("[SequenceHelperFunctions] SubStage 10: Camera snapped to Normal Pot, PIPES/PIPES (1)/Molten leakage stopped, VO playing, hood closing interaction started.");
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
        int myVersion = ++voicePlaybackVersion;
        EnsureVoiceAudio();
        Voice_Audio.Stop();
        Voice_Audio.clip = clip;
        yield return null;
        if (myVersion != voicePlaybackVersion) yield break;
        Voice_Audio.Play();

        float startWait = Time.realtimeSinceStartup;
        yield return new WaitUntil(() => (Voice_Audio != null && Voice_Audio.isPlaying) || (Time.realtimeSinceStartup - startWait) > 1.0f || myVersion != voicePlaybackVersion);

        if (myVersion != voicePlaybackVersion) yield break;

        if (Voice_Audio != null && Voice_Audio.isPlaying)
        {
            while (Voice_Audio != null && Voice_Audio.isPlaying)
            {
                if (myVersion != voicePlaybackVersion)
                {
                    Voice_Audio.Stop();
                    Voice_Audio.clip = null;
                    yield break;
                }
                yield return null;
            }
        }
        if (myVersion != voicePlaybackVersion) yield break;
        yield return new WaitForSeconds(0.2f);
        if (myVersion != voicePlaybackVersion) yield break;
        onComplete?.Invoke();
    }

    public void PlayAudio_TriggerOnComplete(AudioClip clip) { if (clip != null) StartCoroutine(PlayAudioWhenReady(clip)); }
    private IEnumerator PlayAudioWhenReady(AudioClip clip)
    {
        int myVersion = ++voicePlaybackVersion;
        EnsureVoiceAudio();
        Voice_Audio.Stop();
        Voice_Audio.clip = clip;
        yield return null;
        if (myVersion != voicePlaybackVersion) yield break;
        Voice_Audio.Play();
        float startWait = Time.realtimeSinceStartup;
        yield return new WaitUntil(() => (Voice_Audio != null && Voice_Audio.isPlaying) || (Time.realtimeSinceStartup - startWait) > 1.0f || myVersion != voicePlaybackVersion);
        if (myVersion != voicePlaybackVersion) yield break;
        if (Voice_Audio != null && Voice_Audio.isPlaying)
        {
            while (Voice_Audio != null && Voice_Audio.isPlaying)
            {
                if (myVersion != voicePlaybackVersion)
                {
                    Voice_Audio.Stop();
                    Voice_Audio.clip = null;
                    yield break;
                }
                yield return null;
            }
        }
        if (myVersion != voicePlaybackVersion) yield break;
        yield return new WaitForSeconds(0.2f);
        if (myVersion != voicePlaybackVersion) yield break;
        Debug.Log("[SequenceHelperFunctions] Audio finished. Progression requires trainee Next click.");
    }

    public void PlayAudio_WithoutNextTask(AudioClip clip) { if (clip != null) StartCoroutine(PlayAudioWhenReady_WithoutNextTask(clip)); }
    private IEnumerator PlayAudioWhenReady_WithoutNextTask(AudioClip clip)
    {
        int myVersion = ++voicePlaybackVersion;
        EnsureVoiceAudio();
        Voice_Audio.Stop();
        Voice_Audio.clip = clip;
        yield return null;
        if (myVersion != voicePlaybackVersion) yield break;
        Voice_Audio.Play();
        float startWait = Time.realtimeSinceStartup;
        yield return new WaitUntil(() => (Voice_Audio != null && Voice_Audio.isPlaying) || (Time.realtimeSinceStartup - startWait) > 1.0f || myVersion != voicePlaybackVersion);
        if (myVersion != voicePlaybackVersion) yield break;
        if (Voice_Audio != null && Voice_Audio.isPlaying)
        {
            while (Voice_Audio != null && Voice_Audio.isPlaying)
            {
                if (myVersion != voicePlaybackVersion)
                {
                    Voice_Audio.Stop();
                    Voice_Audio.clip = null;
                    yield break;
                }
                yield return null;
            }
        }
        if (myVersion != voicePlaybackVersion) yield break;
        yield return new WaitForSeconds(0.2f);
        if (myVersion != voicePlaybackVersion) yield break;
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
        int myVersion = ++voicePlaybackVersion;
        var mgr = Manager.Instance != null ? Manager.Instance : UnityEngine.Object.FindFirstObjectByType<Manager>();

        if (mgr == null)
        {
            Debug.LogWarning("[SequenceHelperFunctions][TTS] Manager instance unavailable — cannot play audio.");
            yield return new WaitForSeconds(1.0f);
            if (myVersion != voicePlaybackVersion) yield break;
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
            float waitStart = Time.realtimeSinceStartup;
            yield return new WaitUntil(() => mgr.IsSpeaking || (Time.realtimeSinceStartup - waitStart) > 1.0f || myVersion != voicePlaybackVersion);
            if (myVersion != voicePlaybackVersion) yield break;

            waitStart = Time.realtimeSinceStartup;
            yield return new WaitUntil(() => source.isPlaying || (Time.realtimeSinceStartup - waitStart) > 2.5f || myVersion != voicePlaybackVersion);
            if (myVersion != voicePlaybackVersion) yield break;

            Debug.Log($"[SequenceHelperFunctions][TTS] isPlaying={source.isPlaying} clip={source.clip?.name} length={source.clip?.length}s");

            if (source.isPlaying)
            {
                while (source.isPlaying || mgr.IsSpeaking)
                {
                    if (myVersion != voicePlaybackVersion) yield break;
                    yield return null;
                }
                Debug.Log($"[TTS PLAY COMPLETE]\nKey = {key}\nLanguage = {mgr.CurrentLanguage}");
                audioActuallyCompleted = true;
            }
            else
            {
                var status = mgr.GetSpeechCacheStatus(key);
                Debug.LogWarning($"[TTS BLOCKED]\nKey = {key}\nLanguage = {mgr.CurrentLanguage}\nReason = {status.ToString().ToUpperInvariant()}");
                while (mgr.IsSpeaking)
                {
                    if (myVersion != voicePlaybackVersion) yield break;
                    yield return null;
                }
            }
        }

        if (myVersion != voicePlaybackVersion) yield break;

        if (!audioActuallyCompleted)
        {
            yield break;
        }

        yield return new WaitForSeconds(0.3f);
        if (myVersion != voicePlaybackVersion) yield break;

        Debug.Log("[SequenceHelperFunctions] Locale audio finished. Progression requires trainee Next click.");
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
