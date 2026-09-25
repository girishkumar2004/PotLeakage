using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PotLeakage.UI
{
    public enum WalkieTalkieState
    {
        Inactive,
        WaitingForShift,
        ShiftAudioPlaying,
        WaitingForTechnical,
        TechnicalAudioPlaying,
        Completed
    }

    /// <summary>
    /// Controls the two-stage walkie-talkie interaction at Task 05 (Ideal CBT Temperature).
    /// Manages direct material swap highlighting for defaultMaterial.007,
    /// toggling Shift.png and Technical.png personnel images,
    /// and audio playback (or simulated completion when clips are unassigned).
    /// </summary>
    public class WalkieTalkieInteractionController : MonoBehaviour, IPointerClickHandler
    {
        [Header("Target & Materials")]
        [Tooltip("The clickable walkie-talkie button mesh (Part_5_L/defaultMaterial.007)")]
        public GameObject defaultMaterial007;

        [Tooltip("Designated highlight material (M_PotLeakage_Highlight.mat)")]
        public Material highlightMaterial;

        [Tooltip("Original preserved material (Image_0.mat)")]
        public Material originalMaterial;

        [Header("Personnel Images UI")]
        [Tooltip("Root container under UI/Canvas for the personnel images")]
        public GameObject personnelImagesRoot;

        [Tooltip("UI Image for Shift In-Charge (Shift.png)")]
        public GameObject shiftImage;

        [Tooltip("UI Image for Technical In-Charge (Technical.png)")]
        public GameObject technicalImage;

        [Header("Audio Configuration")]
        [Tooltip("Audio played on walkie-talkie click (click.mp3)")]
        public AudioClip clickAudioClip;

        [Tooltip("Audio played during call (call.mp3)")]
        public AudioClip callAudioClip;

        [Tooltip("Audio played during warning (warning.mp3)")]
        public AudioClip warningAudioClip;

        [Tooltip("Audio played during stage 1 (Shift Superintendent communication)")]
        public AudioClip ShiftSuperintendentCommunicationAudio;

        [Tooltip("Audio played during stage 2 (Technical In-charge communication)")]
        public AudioClip TechnicalInChargeCommunicationAudio;

        // Backward compatibility properties
        public AudioClip shiftCommunicationAudio
        {
            get => ShiftSuperintendentCommunicationAudio != null ? ShiftSuperintendentCommunicationAudio : callAudioClip;
            set => ShiftSuperintendentCommunicationAudio = value;
        }
        public AudioClip technicalInChargeCommunicationAudio
        {
            get => TechnicalInChargeCommunicationAudio != null ? TechnicalInChargeCommunicationAudio : callAudioClip;
            set => TechnicalInChargeCommunicationAudio = value;
        }
        public AudioClip clickAudio { get => clickAudioClip; set => clickAudioClip = value; }
        public AudioClip callAudio { get => callAudioClip; set => callAudioClip = value; }
        public AudioClip warningAudio { get => warningAudioClip; set => warningAudioClip = value; }

        [Tooltip("Audio source for playback")]
        public AudioSource audioSource;

        [Header("Simulation & Timing")]
        [Tooltip("Simulated audio duration in seconds if clips are not assigned (default 0.5s)")]
        public float fallbackAudioDuration = 0.5f;

        [Tooltip("In edit mode or automated tests, automatically advance state upon interaction")]
        public bool autoAdvanceInEditMode = true;

        [Header("Interaction State")]
        [SerializeField] private WalkieTalkieState currentState = WalkieTalkieState.Inactive;
        public WalkieTalkieState CurrentState => currentState;
        public bool IsCompleted => currentState == WalkieTalkieState.Completed;

        [Header("Blink Settings")]
        [SerializeField] private float blinkInterval = 0.5f;
        public float BlinkInterval { get => blinkInterval; set => blinkInterval = value; }
        private Coroutine blinkCoroutine;
        public bool IsBlinking { get; private set; }

        private MeshRenderer wtMeshRenderer;
        private Coroutine audioCoroutine;
        private Action pendingAudioCallback;

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
            if (defaultMaterial007 == null)
            {
                defaultMaterial007 = GameObject.Find("defaultMaterial.007");
                if (defaultMaterial007 == null && gameObject.name == "defaultMaterial.007")
                {
                    defaultMaterial007 = gameObject;
                }
            }

            if (defaultMaterial007 != null)
            {
                if (wtMeshRenderer == null)
                {
                    wtMeshRenderer = defaultMaterial007.GetComponent<MeshRenderer>();
                }

                if (originalMaterial == null && wtMeshRenderer != null)
                {
                    var curMat = wtMeshRenderer.sharedMaterial;
                    if (curMat != null && !curMat.name.Contains("Highlight"))
                    {
                        originalMaterial = curMat;
                    }
                }

                // Ensure BoxCollider exists for clicking
                var col = defaultMaterial007.GetComponent<Collider>();
                if (col == null)
                {
                    var bc = defaultMaterial007.AddComponent<BoxCollider>();
                    bc.center = new Vector3(0.02f, -0.11f, -0.82f);
                    bc.size = new Vector3(1.5f, 1.5f, 1.5f);
                }

                if (audioSource == null)
                {
                    audioSource = defaultMaterial007.GetComponent<AudioSource>();
                    if (audioSource == null)
                    {
                        audioSource = defaultMaterial007.AddComponent<AudioSource>();
                        audioSource.playOnAwake = false;
                        audioSource.spatialBlend = 0f; // 2D clean audio
                    }
                }
            }

            if (clickAudioClip == null)
            {
#if UNITY_EDITOR
                clickAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/click.mp3");
#endif
            }

            if (callAudioClip == null)
            {
#if UNITY_EDITOR
                callAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/call.mp3");
#endif
            }

            if (warningAudioClip == null)
            {
#if UNITY_EDITOR
                warningAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/warning.mp3");
#endif
            }

            if (ShiftSuperintendentCommunicationAudio == null)
            {
                ShiftSuperintendentCommunicationAudio = callAudioClip;
            }

            if (TechnicalInChargeCommunicationAudio == null)
            {
                TechnicalInChargeCommunicationAudio = callAudioClip;
            }

            if (originalMaterial == null)
            {
#if UNITY_EDITOR
                originalMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Model/Materials/Image_0.mat");
#endif
            }

            if (highlightMaterial == null)
            {
#if UNITY_EDITOR
                highlightMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/PotLeakage/Materials/M_PotLeakage_Highlight.mat");
#endif
            }

            if (personnelImagesRoot == null)
            {
                personnelImagesRoot = GameObject.Find("UI/Canvas/IdealCBTTemperatureUI/PersonnelImages")
                                   ?? GameObject.Find("UI/Canvas/PersonnelImages")
                                   ?? GameObject.Find("PersonnelImages");
            }

            if (personnelImagesRoot != null)
            {
                if (shiftImage == null)
                {
                    var sT = personnelImagesRoot.transform.Find("ShiftImage");
                    if (sT != null) shiftImage = sT.gameObject;
                }
                if (technicalImage == null)
                {
                    var tT = personnelImagesRoot.transform.Find("TechnicalImage");
                    if (tT != null) technicalImage = tT.gameObject;
                }
            }
        }

        private void Update()
        {
            // Only accept clicks when waiting for user interaction
            if (currentState != WalkieTalkieState.WaitingForShift && currentState != WalkieTalkieState.WaitingForTechnical)
            {
                return;
            }

            bool mouseClicked = false;
            Vector3 mousePos = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                mouseClicked = Mouse.current.leftButton.wasPressedThisFrame;
                mousePos = Mouse.current.position.ReadValue();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0))
            {
                mouseClicked = true;
                mousePos = Input.mousePosition;
            }
#endif

            if (mouseClicked)
            {
                var cam = UnityEngine.Camera.main;
                if (cam != null)
                {
                    Ray ray = cam.ScreenPointToRay(mousePos);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        if (hit.collider != null && (hit.collider.gameObject == defaultMaterial007 || hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)))
                        {
                            OnWalkieTalkieClicked();
                        }
                    }
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnWalkieTalkieClicked();
            }
        }

        /// <summary>
        /// Public interaction trigger callable programmatically, from tests or UI events.
        /// </summary>
        public void Interact()
        {
            OnWalkieTalkieClicked();
        }

        private void OnWalkieTalkieClicked()
        {
            InitializeReferences();
            StopWalkieTalkieBlink();

            if (currentState == WalkieTalkieState.WaitingForShift)
            {
                HandleShiftPress();
            }
            else if (currentState == WalkieTalkieState.WaitingForTechnical)
            {
                HandleTechnicalPress();
            }
        }

        private void HandleShiftPress()
        {
            currentState = WalkieTalkieState.ShiftAudioPlaying;

            // Shift.png DISAPPEARS
            if (shiftImage != null) shiftImage.SetActive(false);

            // Technical.png REMAINS VISIBLE
            if (technicalImage != null) technicalImage.SetActive(true);

            // Unhighlight / dim walkie talkie during audio
            RestoreOriginalMaterial();

            // Play Click -> Call -> Wait till call finishes -> Warning -> Wait till warning finishes -> Walkie available again
            PlayCommunicationSequence(() =>
            {
                currentState = WalkieTalkieState.WaitingForTechnical;
                // Re-highlight walkie talkie waiting for second press
                SwapHighlightMaterial();
            });
        }

        private void HandleTechnicalPress()
        {
            currentState = WalkieTalkieState.TechnicalAudioPlaying;

            // Technical.png DISAPPEARS
            if (technicalImage != null) technicalImage.SetActive(false);

            // Unhighlight walkie talkie during audio
            RestoreOriginalMaterial();

            // Play Click -> Call -> Wait till call finishes -> Warning -> Wait till warning finishes -> Walkie complete
            PlayCommunicationSequence(() =>
            {
                currentState = WalkieTalkieState.Completed;
                // Re-highlight walkie talkie upon completion
                SwapHighlightMaterial();
            });
        }

        private void PlayCommunicationSequence(Action onComplete)
        {
            pendingAudioCallback = onComplete;

            // 1. Play Click SFX on separate SFX channel (never cuts off communication audio)
            PlayClickSFX();

            if (Application.isPlaying)
            {
                if (audioCoroutine != null) StopCoroutine(audioCoroutine);
                audioCoroutine = StartCoroutine(CommunicationAudioSequenceRoutine(onComplete));
            }
            else
            {
                if (autoAdvanceInEditMode)
                {
                    onComplete?.Invoke();
                    pendingAudioCallback = null;
                }
            }
        }

        private void PlayClickSFX()
        {
            if (clickAudioClip != null)
            {
                if (SequenceHelperFunctions.instance != null)
                {
                    SequenceHelperFunctions.instance.PlaySFX(clickAudioClip);
                }
                else if (TruckTyreReplacement.Core.Manager.Instance != null)
                {
                    TruckTyreReplacement.Core.Manager.Instance.PlaySFX(clickAudioClip);
                }
            }
        }

        private IEnumerator CommunicationAudioSequenceRoutine(Action onComplete)
        {
            var callClip = callAudioClip != null ? callAudioClip : shiftCommunicationAudio;
            var warnClip = warningAudioClip;

            // 2. Play Call audio on dedicated walkie audioSource
            if (callClip != null && audioSource != null)
            {
                audioSource.clip = callClip;
                audioSource.Play();

                yield return null; // allow audio system to begin playback

                while (audioSource != null && audioSource.isPlaying)
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(fallbackAudioDuration);
            }

            // 3. Play Warning audio on dedicated walkie audioSource
            if (warnClip != null && audioSource != null)
            {
                audioSource.clip = warnClip;
                audioSource.Play();

                yield return null; // allow audio system to begin playback

                while (audioSource != null && audioSource.isPlaying)
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(fallbackAudioDuration);
            }

            audioCoroutine = null;
            pendingAudioCallback = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Immediately triggers the completion callback for the currently playing audio stage.
        /// </summary>
        public void CompleteCurrentAudioImmediately()
        {
            if (audioCoroutine != null)
            {
                StopCoroutine(audioCoroutine);
                audioCoroutine = null;
            }
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            var cb = pendingAudioCallback;
            pendingAudioCallback = null;
            cb?.Invoke();
        }

        /// <summary>
        /// Activates the interaction when Ideal CBT Temperature begins.
        /// Starts blinking defaultMaterial.007 and shows both personnel images.
        /// </summary>
        public void ActivateInteraction()
        {
            InitializeReferences();

            if (audioCoroutine != null)
            {
                StopCoroutine(audioCoroutine);
                audioCoroutine = null;
            }
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            currentState = WalkieTalkieState.WaitingForShift;

            if (personnelImagesRoot != null) personnelImagesRoot.SetActive(true);
            if (shiftImage != null) shiftImage.SetActive(true);
            if (technicalImage != null) technicalImage.SetActive(true);

            StartWalkieTalkieBlink();
        }

        /// <summary>
        /// Resets the interaction state and hides personnel images.
        /// Stops blinking and restores original material.
        /// </summary>
        public void ResetInteraction()
        {
            InitializeReferences();
            StopWalkieTalkieBlink();

            if (audioCoroutine != null)
            {
                StopCoroutine(audioCoroutine);
                audioCoroutine = null;
            }
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            currentState = WalkieTalkieState.Inactive;

            if (personnelImagesRoot != null) personnelImagesRoot.SetActive(false);
            if (shiftImage != null) shiftImage.SetActive(false);
            if (technicalImage != null) technicalImage.SetActive(false);

            RestoreOriginalMaterial();
        }

        public void StartWalkieTalkieBlink()
        {
            InitializeReferences();

            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }

            IsBlinking = true;
            SwapHighlightMaterial();

            if (Application.isPlaying)
            {
                blinkCoroutine = StartCoroutine(WalkieTalkieBlinkRoutine());
            }
        }

        public void StopWalkieTalkieBlink()
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
            IsBlinking = false;
            RestoreOriginalMaterial();
        }

        private IEnumerator WalkieTalkieBlinkRoutine()
        {
            IsBlinking = true;
            bool showHighlight = false; // already swapped to highlight initially
            while (IsBlinking)
            {
                yield return new WaitForSeconds(blinkInterval);
                if (!IsBlinking) break;

                if (showHighlight)
                {
                    SwapHighlightMaterial();
                }
                else
                {
                    RestoreOriginalMaterial();
                }
                showHighlight = !showHighlight;
            }
            blinkCoroutine = null;
        }

        public void SwapHighlightMaterial()
        {
            InitializeReferences();
            if (wtMeshRenderer != null && highlightMaterial != null)
            {
                wtMeshRenderer.sharedMaterial = highlightMaterial;
            }
        }

        public void RestoreOriginalMaterial()
        {
            InitializeReferences();
            if (wtMeshRenderer != null && originalMaterial != null)
            {
                wtMeshRenderer.sharedMaterial = originalMaterial;
            }
        }

        private void OnDisable()
        {
            StopWalkieTalkieBlink();
        }
    }
}
