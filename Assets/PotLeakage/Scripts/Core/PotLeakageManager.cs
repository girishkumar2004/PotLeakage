using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PotLeakage.Camera;
using PotLeakage.UI;
using PotLeakage.VFX;
using PotLeakage.Configuration;

namespace PotLeakage.Core
{
    /// <summary>
    /// Main scene-level presentation manager for Vedanta Aluminium Pot Leakage module.
    /// Exposes cleanly organized Inspector fields for Camera, Highlights, UI, VFX, FireEffect, and Sequence.
    /// Reuses the existing Vedanta sequence framework (SequenceHandler, SequenceHelperFunctions, Sequence, Task).
    /// </summary>
    public class PotLeakageManager : MonoBehaviour
    {
        public static PotLeakageManager Instance { get; private set; }

        [Header("CAMERA SYSTEM")]
        public UnityEngine.Camera mainCamera;
        public Transform welcomeTarget;
        public Transform normalPotOperationTarget;
        public Transform idealPotVoltageTarget;
        public Transform idealCBTTemperatureTarget;
        public PotLeakageCameraController cameraController;

        [Header("HIGHLIGHT SYSTEM")]
        [Tooltip("Full Pot Machine mesh (tripo_node_cf158416)")]
        public GameObject potMachineHighlightTarget;

        [Tooltip("CBT area visual target (Machine/Fire)")]
        public GameObject cbtHighlightTarget;

        [Header("FIRE / MAGMA EFFECT")]
        [Tooltip("Machine/FireEffect representing hot metal / CBT region")]
        public GameObject fireEffect;

        [Header("UI REFERENCES")]
        public GameObject welcomePanel;
        public TextMeshProUGUI panelTitle;
        public TextMeshProUGUI panelDescription;
        public TextMeshProUGUI valueText;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI progressText;
        public Image companyLogo;
        public Button nextButton;
        public PotLeakageUIController uiController;
        public PotLeakageValueDisplay valueDisplay;
        public WalkieTalkieInteractionController walkieTalkieController;

        [Header("POT VOLTAGE WALL PANEL (TASK 04)")]
        public GameObject potVoltageWallPanel;

        [Header("OPTIONAL SFX")]
        public AudioClip nextButtonSFX;
        public AudioClip cameraTransitionSFX;

        [Header("VFX")]
        public MoltenAluminiumVFXController moltenAluminiumVFX;

        [Header("SEQUENCE & CONFIGURATION")]
        public SequenceHandler sequenceHandler;
        public SequenceHelperFunctions sequenceHelper;
        public Sequence sequenceAsset;
        public PotLeakageConfig potLeakageConfig;

        [Header("SEQUENCE CONVENIENCE ACCESSORS")]
        public int CurrentTaskIndex => sequenceHandler != null ? sequenceHandler.currentTask : -1;
        public int TotalTasks => (sequenceAsset != null && sequenceAsset.TaskList != null) ? sequenceAsset.TaskList.Count : 0;
        public Task CurrentTask => sequenceHandler != null ? sequenceHandler.GetCurrentTask() : null;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            ResolveReferences();
        }

        private void OnEnable()
        {
            SequenceHandler.OnTaskStarted += HandleTaskStarted;
            SequenceHandler.OnTaskCompletedEvent += HandleTaskCompleted;
        }

        private void OnDisable()
        {
            SequenceHandler.OnTaskStarted -= HandleTaskStarted;
            SequenceHandler.OnTaskCompletedEvent -= HandleTaskCompleted;
        }

        public void ResolveReferences()
        {
            if (sequenceHandler == null)
            {
                sequenceHandler = Object.FindAnyObjectByType<SequenceHandler>();
            }
            if (sequenceHelper == null)
            {
                sequenceHelper = Object.FindAnyObjectByType<SequenceHelperFunctions>();
            }
            if (sequenceAsset == null)
            {
                sequenceAsset = Object.FindAnyObjectByType<Sequence>();
            }
            if (uiController == null)
            {
                uiController = Object.FindAnyObjectByType<PotLeakageUIController>();
            }
            if (cameraController == null)
            {
                cameraController = Object.FindAnyObjectByType<PotLeakageCameraController>();
            }
            if (moltenAluminiumVFX == null)
            {
                moltenAluminiumVFX = Object.FindAnyObjectByType<MoltenAluminiumVFXController>();
            }
        }

        private void HandleTaskStarted(int taskIndex, Task task)
        {
            Debug.Log($"[PotLeakageManager] Task Started: Index={taskIndex}, Name='{task?.TaskName}'");
        }

        private void HandleTaskCompleted(int taskIndex, Task task)
        {
            Debug.Log($"[PotLeakageManager] Task Completed: Index={taskIndex}, Name='{task?.TaskName}'");
        }

        /// <summary>
        /// Authoritative forwarder to advance the current sequence task.
        /// Respects active sub-presentations (e.g. Task 09 multi-step low leakage presentation).
        /// </summary>
        public void CompleteCurrentTask()
        {
            if (sequenceHelper != null)
            {
                sequenceHelper.CompleteCurrentTask();
            }
            else if (sequenceHandler != null)
            {
                sequenceHandler.TaskCompleted();
            }
        }

        /// <summary>
        /// Resets all presentation controllers, resets task completion flags, and restarts from Task 01.
        /// </summary>
        public void RestartSequence()
        {
            ResolveReferences();

            if (uiController != null)
            {
                if (uiController.toolPresentationController != null)
                {
                    uiController.toolPresentationController.ResetPresentation();
                }
                if (uiController.lowLeakageController != null)
                {
                    uiController.lowLeakageController.ResetPresentation();
                }
            }

            if (sequenceAsset != null && sequenceAsset.TaskList != null)
            {
                foreach (var t in sequenceAsset.TaskList)
                {
                    if (t != null) t.TaskCompleted = false;
                }
            }

            if (sequenceHandler != null)
            {
                sequenceHandler.currentSequence = 0;
                sequenceHandler.currentTask = 0;
                sequenceHandler.NextTask();
            }
        }
    }
}
