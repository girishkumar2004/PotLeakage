using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using PotLeakage.Interaction;

namespace PotLeakage.Camera
{
    public class PotLeakageCameraController : MonoBehaviour
    {
        [Header("PC Mouse Look Around (Left Mouse Button)")]
        [Tooltip("Enable PC left-mouse-button look around")]
        public bool enableMouseLook = true;
        [Tooltip("Sensitivity of mouse look in degrees per pixel")]
        public float mouseLookSensitivity = 0.15f;
        [Tooltip("Minimum pitch angle (degrees) to prevent flipping")]
        public float minPitch = -85f;
        [Tooltip("Maximum pitch angle (degrees) to prevent flipping")]
        public float maxPitch = 85f;

        [Header("PC Keyboard Movement (WASD)")]
        [Tooltip("Enable PC WASD keyboard camera translation")]
        public bool enableWASDMovement = true;
        [Tooltip("Camera move speed in meters per second")]
        public float moveSpeed = 2.5f;

        private float currentYaw;
        private float currentPitch;
        private bool isLeftMouseDragging = false;
        private bool isPointerBlockedOnDown = false;

        [Header("Camera Targets")]
        [Tooltip("Authoritative starting camera position and rotation")]
        public Transform welcomeTarget;

        [Tooltip("Focus view on the main pot machine during normal operation")]
        public Transform normalPotOperationTarget;

        [Tooltip("Focus view on Normal Pot under TransformPoints (Task 04 Communication)")]
        public Transform normalPotTarget;

        [Tooltip("Focus view on Ideal Pot Voltage under TransformPoints")]
        public Transform idealPotVoltageTarget;

        [Tooltip("Focus view on Ideal CBT Temperature under TransformPoints")]
        public Transform idealCBTTemperatureTarget;

        // Backward compatibility aliases
        public Transform potMachineTarget { get => normalPotOperationTarget; set => normalPotOperationTarget = value; }
        public Transform welcomeCameraTarget { get => welcomeTarget; set => welcomeTarget = value; }
        public Transform potMachineCameraTarget { get => normalPotOperationTarget; set => normalPotOperationTarget = value; }
        public Transform normalPotCameraTarget { get => normalPotTarget; set => normalPotTarget = value; }
        public Transform voltageCameraTarget { get => idealPotVoltageTarget; set => idealPotVoltageTarget = value; }
        public Transform cbtCameraTarget { get => idealCBTTemperatureTarget; set => idealCBTTemperatureTarget = value; }

        [Header("Look Targets")]
        public Transform potMachineLookTarget;
        [Tooltip("Focus view on Pot Control Machine under TransformPoints")]
        public Transform potControlMachineTarget;
        [Tooltip("Focus view on Pot Inspection under TransformPoints (Task 09)")]
        public Transform potInspectionTarget;
        public Transform cbtLookTarget;
        public Transform toolsTarget;
        [Tooltip("Focus view on ToolBoxSelection under TransformPoints (Task 08)")]
        public Transform toolBoxSelectionTarget;
        [Tooltip("Focus view on PTM Crane under TransformPoints (Task 10)")]
        public Transform ptmCraneTarget;

        [Header("Audio")]
        [Tooltip("SFX played on camera transition (swap.mp3)")]
        public AudioClip swapAudioClip;
        [Tooltip("SFX played continuously during camera movement to PTM Crane (CraneHum.wav)")]
        public AudioClip craneHumAudioClip;

        private AudioSource craneHumAudioSource;
        private Coroutine ptmCraneMoveCoroutine;

        // Backward compatibility aliases
        public AudioClip cameraTransitionAudio { get => swapAudioClip; set => swapAudioClip = value; }
        public AudioClip swapAudio { get => swapAudioClip; set => swapAudioClip = value; }

        [Header("Duration Settings (Legacy / Backward Compatibility)")]
        [Tooltip("Legacy transition duration. For instant snapping, effective duration is 0 seconds.")]
        public float defaultDuration = 0f;
        [Tooltip("Legacy quick transition duration. For instant snapping, effective duration is 0 seconds.")]
        public float quickTransitionDuration = 0f;

        private Vector3 initialPosition;
        private Quaternion initialRotation;

        private void Start()
        {
            // Spawn Main Camera instantly at the authoritative Welcome position and rotation
            if (welcomeTarget == null)
            {
                var wt = GameObject.Find("CameraSystem/TransformPoints/Welcome") ?? GameObject.Find("TransformPoints/Welcome");
                if (wt != null) welcomeTarget = wt.transform;
            }

            if (toolsTarget == null)
            {
                var tt = GameObject.Find("CameraSystem/TransformPoints/Tools") ?? GameObject.Find("TransformPoints/Tools");
                if (tt != null) toolsTarget = tt.transform;
            }

            if (normalPotTarget == null)
            {
                var tp = GameObject.Find("CameraSystem/TransformPoints") ?? GameObject.Find("TransformPoints");
                if (tp != null)
                {
                    for (int i = 0; i < tp.transform.childCount; i++)
                    {
                        var child = tp.transform.GetChild(i);
                        if (child.name.Trim() == "Normal Pot")
                        {
                            normalPotTarget = child;
                            break;
                        }
                    }
                }
            }

            if (potInspectionTarget == null)
            {
                var pi = GameObject.Find("CameraSystem/TransformPoints/PotInspection") ?? GameObject.Find("TransformPoints/PotInspection");
                if (pi != null) potInspectionTarget = pi.transform;
            }

            if (potControlMachineTarget == null)
            {
                var pcm = GameObject.Find("CameraSystem/TransformPoints/PotControlMachine") ?? GameObject.Find("TransformPoints/PotControlMachine");
                if (pcm != null) potControlMachineTarget = pcm.transform;
            }

            if (ptmCraneTarget == null)
            {
                var pt = GameObject.Find("CameraSystem/TransformPoints/PTM Crane") ?? GameObject.Find("TransformPoints/PTM Crane") ?? GameObject.Find("PTM Crane");
                if (pt != null) ptmCraneTarget = pt.transform;
            }

            if (swapAudioClip == null)
            {
#if UNITY_EDITOR
                swapAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/swap.mp3");
#endif
            }

            if (craneHumAudioClip == null)
            {
#if UNITY_EDITOR
                craneHumAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/CraneHum.wav");
#endif
            }

            if (welcomeTarget != null)
            {
                transform.position = welcomeTarget.position;
                transform.rotation = welcomeTarget.rotation;
                initialPosition = welcomeTarget.position;
                initialRotation = welcomeTarget.rotation;
            }
            SyncMouseLookAngles();
        }

        private void Update()
        {
#if !UNITY_ANDROID
            HandleMouseLook();
            HandleWASDMovement();
#endif
        }

        public void SyncMouseLookAngles()
        {
            Vector3 euler = transform.eulerAngles;
            currentYaw = euler.y;
            currentPitch = euler.x;
            if (currentPitch > 180f) currentPitch -= 360f;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }

        private void HandleWASDMovement()
        {
            if (!enableWASDMovement) return;

            // UI input field safety - ignore WASD if an input field is currently focused
            if (IsInputFieldFocused()) return;

            Vector2 inputDir = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputDir.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputDir.y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputDir.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputDir.x += 1f;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
            if (inputDir.sqrMagnitude < 0.001f)
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) inputDir.y += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) inputDir.y -= 1f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) inputDir.x -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) inputDir.x += 1f;
            }
#endif

            if (inputDir.sqrMagnitude > 0.001f)
            {
                inputDir.Normalize();

                // Compute horizontal forward and right vectors (projected onto XZ plane, yaw only)
                Vector3 forward = transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.0001f)
                {
                    forward.Normalize();
                }
                else
                {
                    forward = Vector3.forward;
                }

                Vector3 right = transform.right;
                right.y = 0f;
                if (right.sqrMagnitude > 0.0001f)
                {
                    right.Normalize();
                }
                else
                {
                    right = Vector3.right;
                }

                Vector3 moveDelta = (forward * inputDir.y + right * inputDir.x) * (moveSpeed * Time.deltaTime);

                // Preserve exact Y coordinate (no vertical flying from camera pitch)
                float currentY = transform.position.y;
                Vector3 newPos = transform.position + moveDelta;
                newPos.y = currentY;
                transform.position = newPos;
            }
        }

        private bool IsInputFieldFocused()
        {
            if (EventSystem.current == null) return false;
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null) return false;

            if (selected.GetComponent<InputField>() != null) return true;
            if (selected.GetComponent<TMP_InputField>() != null) return true;

            return false;
        }

        private void HandleMouseLook()
        {
            if (!enableMouseLook) return;

            bool isLeftDown = false;
            bool isLeftDownThisFrame = false;
            Vector2 mouseDelta = Vector2.zero;
            Vector2 mousePosition = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                isLeftDown = Mouse.current.leftButton.isPressed;
                isLeftDownThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
                mouseDelta = Mouse.current.delta.ReadValue();
                mousePosition = Mouse.current.position.ReadValue();
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
            if (!isLeftDown && Input.GetMouseButton(0))
            {
                isLeftDown = true;
                mouseDelta = new Vector2(Input.GetAxis("Mouse X") * 10f, Input.GetAxis("Mouse Y") * 10f);
            }
            if (!isLeftDownThisFrame && Input.GetMouseButtonDown(0))
            {
                isLeftDownThisFrame = true;
            }
            if (mousePosition == Vector2.zero)
            {
                mousePosition = Input.mousePosition;
            }
#endif

            // On initial press, check if pointer is over UI or interactive elements
            if (isLeftDownThisFrame)
            {
                isPointerBlockedOnDown = IsPointerOverInteractive(mousePosition);
            }

            if (isLeftDown)
            {
                // UI & interactable safety: do not rotate camera if click started on UI/interactables
                if (isPointerBlockedOnDown)
                {
                    return;
                }

                if (!isLeftMouseDragging)
                {
                    isLeftMouseDragging = true;
                    SyncMouseLookAngles();
                }

                if (mouseDelta.sqrMagnitude > 0.0001f)
                {
                    currentYaw += mouseDelta.x * mouseLookSensitivity;
                    currentPitch -= mouseDelta.y * mouseLookSensitivity; // Moving mouse up pitches camera up
                    currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

                    // CAMERA ROTATION ONLY — ZERO CAMERA POSITION CHANGE
                    transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
                }
            }
            else
            {
                isLeftMouseDragging = false;
                isPointerBlockedOnDown = false;
            }
        }

        private bool IsPointerOverInteractive(Vector2 screenPosition)
        {
            // 1. UI Elements (EventSystem checks uGUI Canvas buttons, sliders, text fields, etc.)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }

            // 2. 3D Scene Interactables (gloves, tools, anode button, walkie-talkie)
            var cam = GetComponent<UnityEngine.Camera>();
            if (cam == null) cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(screenPosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    if (hit.collider != null)
                    {
                        var go = hit.collider.gameObject;
                        if (go.GetComponentInParent<SideBreakingToolInteraction>() != null ||
                            go.GetComponentInParent<AnodeDownButtonInteraction>() != null ||
                            go.GetComponentInParent<WalkieTalkie2Interaction>() != null ||
                            go.GetComponentInParent<IPointerClickHandler>() != null ||
                            go.GetComponentInParent<IPointerDownHandler>() != null ||
                            go.GetComponentInParent<Button>() != null)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void StartCraneHum()
        {
            if (craneHumAudioClip == null)
            {
#if UNITY_EDITOR
                craneHumAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/CraneHum.wav");
#endif
            }
            if (craneHumAudioClip == null) return;

            if (craneHumAudioSource == null)
            {
                craneHumAudioSource = gameObject.AddComponent<AudioSource>();
                craneHumAudioSource.playOnAwake = false;
                craneHumAudioSource.spatialBlend = 0f;
            }

            craneHumAudioSource.clip = craneHumAudioClip;
            craneHumAudioSource.loop = true;
            craneHumAudioSource.volume = 1f;
            if (!craneHumAudioSource.isPlaying)
            {
                craneHumAudioSource.Play();
            }
        }

        public void StopCraneHum(float fadeDuration = 0.2f)
        {
            if (craneHumAudioSource == null || !craneHumAudioSource.isPlaying) return;
            if (fadeDuration <= 0.01f || !Application.isPlaying)
            {
                craneHumAudioSource.Stop();
                return;
            }
            StartCoroutine(FadeOutCraneHumRoutine(fadeDuration));
        }

        private IEnumerator FadeOutCraneHumRoutine(float duration)
        {
            if (craneHumAudioSource == null) yield break;
            float startVol = craneHumAudioSource.volume;
            float elapsed = 0f;
            while (elapsed < duration && craneHumAudioSource != null)
            {
                elapsed += Time.deltaTime;
                craneHumAudioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }
            if (craneHumAudioSource != null)
            {
                craneHumAudioSource.Stop();
                craneHumAudioSource.volume = 1f;
            }
        }

        /// <summary>
        /// Instantly snaps the camera to the target transform's position and rotation with 0 duration.
        /// Zero panning, zero interpolation, zero coroutines.
        /// Plays swap audio SFX ONLY when there is a real camera target change.
        /// </summary>
        public void SnapCameraToTransform(Transform target)
        {
            if (ptmCraneMoveCoroutine != null)
            {
                StopCoroutine(ptmCraneMoveCoroutine);
                ptmCraneMoveCoroutine = null;
            }
            StopCraneHum(0f);

            if (target == null)
            {
                Debug.LogWarning("[PotLeakageCameraController] SnapCameraToTransform called with null target.");
                return;
            }

            bool positionChanged = Vector3.Distance(transform.position, target.position) > 0.01f;
            bool rotationChanged = Quaternion.Angle(transform.rotation, target.rotation) > 0.1f;
            bool isRealChange = positionChanged || rotationChanged;

            transform.position = target.position;
            transform.rotation = target.rotation;
            SyncMouseLookAngles();

            if (isRealChange)
            {
                PlayTransitionAudio();
            }
        }

        public void SnapToTarget(Transform target) => SnapCameraToTransform(target);

        // Preserved API signatures for existing UnityEvents and inspector references (all snap immediately)
        public void MoveToTarget(Transform target) => SnapCameraToTransform(target);
        public void MoveToTarget(Transform target, float duration) => SnapCameraToTransform(target);
        public void TeleportToTarget(Transform target) => SnapCameraToTransform(target);
        public void MoveAndLookAt(Transform positionTarget, Transform lookTarget) => SnapCameraToTransform(positionTarget);
        public void MoveAndLookAt(Transform positionTarget, Transform lookTarget, float duration) => SnapCameraToTransform(positionTarget);

        public void MoveToWelcome() => SnapCameraToTransform(welcomeTarget);
        public void MoveToWelcomeTarget() => SnapCameraToTransform(welcomeTarget);
        public void MoveToNormalPotOperation() => SnapCameraToTransform(normalPotOperationTarget);
        public void MoveToNormalPot()
        {
            if (normalPotTarget == null)
            {
                var tp = GameObject.Find("CameraSystem/TransformPoints") ?? GameObject.Find("TransformPoints");
                if (tp != null)
                {
                    for (int i = 0; i < tp.transform.childCount; i++)
                    {
                        var child = tp.transform.GetChild(i);
                        if (child.name.Trim() == "Normal Pot")
                        {
                            normalPotTarget = child;
                            break;
                        }
                    }
                }
            }
            SnapCameraToTransform(normalPotTarget);
        }
        public void MoveToNormalPotTarget() => MoveToNormalPot();
        public void MoveToPotMachine() => SnapCameraToTransform(normalPotOperationTarget);

        [Tooltip("Focus view on AirUnlock under TransformPoints")]
        public Transform airUnlockTarget;

        public void MoveToAirUnlock()
        {
            if (airUnlockTarget == null)
            {
                var au = GameObject.Find("CameraSystem/TransformPoints/AirUnlock") ?? GameObject.Find("TransformPoints/AirUnlock") ?? GameObject.Find("AirUnlock");
                if (au != null) airUnlockTarget = au.transform;
            }
            SnapCameraToTransform(airUnlockTarget != null ? airUnlockTarget : normalPotOperationTarget);
        }

        public void MoveToPotControlMachine()
        {
            if (potControlMachineTarget == null)
            {
                var pcm = GameObject.Find("CameraSystem/TransformPoints/PotControlMachine") ?? GameObject.Find("PotControlMachine");
                if (pcm != null) potControlMachineTarget = pcm.transform;
            }
            SnapCameraToTransform(potControlMachineTarget != null ? potControlMachineTarget : potMachineLookTarget);
        }

        public void MoveToPotInspection()
        {
            if (potInspectionTarget == null)
            {
                var pi = GameObject.Find("CameraSystem/TransformPoints/PotInspection") ?? GameObject.Find("PotInspection");
                if (pi != null) potInspectionTarget = pi.transform;
            }
            SnapCameraToTransform(potInspectionTarget);
        }
        public void MoveToIdealPotVoltage() => SnapCameraToTransform(idealPotVoltageTarget);
        public void MoveToIdealCBTTemperature() => SnapCameraToTransform(idealCBTTemperatureTarget);
        public void MoveToCBTTarget() => SnapCameraToTransform(idealCBTTemperatureTarget);
        public void MoveToTools() => SnapCameraToTransform(toolsTarget);
        public void MoveToToolsTarget() => SnapCameraToTransform(toolsTarget);
        public void MoveToToolBoxSelection()
        {
            if (toolBoxSelectionTarget == null)
            {
                var tb = GameObject.Find("CameraSystem/TransformPoints/ToolBoxSelection") ?? GameObject.Find("TransformPoints/ToolBoxSelection") ?? GameObject.Find("ToolBoxSelection");
                if (tb != null) toolBoxSelectionTarget = tb.transform;
            }
            SnapCameraToTransform(toolBoxSelectionTarget != null ? toolBoxSelectionTarget : toolsTarget);
        }

        public void MoveToPTMCrane()
        {
            MoveToPTMCrane(2.0f);
        }

        public void MoveToPTMCrane(float duration)
        {
            if (ptmCraneTarget == null)
            {
                var pt = GameObject.Find("CameraSystem/TransformPoints/PTM Crane") ?? GameObject.Find("TransformPoints/PTM Crane") ?? GameObject.Find("PTM Crane");
                if (pt != null) ptmCraneTarget = pt.transform;
            }

            Transform target = ptmCraneTarget != null ? ptmCraneTarget : normalPotOperationTarget;
            if (target == null) return;

            if (ptmCraneMoveCoroutine != null)
            {
                StopCoroutine(ptmCraneMoveCoroutine);
                ptmCraneMoveCoroutine = null;
            }

            if (Application.isPlaying && duration > 0.05f)
            {
                ptmCraneMoveCoroutine = StartCoroutine(MoveToPTMCraneRoutine(target, duration));
            }
            else
            {
                SnapCameraToTransform(target);
            }
        }

        private IEnumerator MoveToPTMCraneRoutine(Transform target, float duration)
        {
            if (target == null) yield break;

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;

            bool isRealChange = Vector3.Distance(startPos, target.position) > 0.05f || Quaternion.Angle(startRot, target.rotation) > 0.5f;

            if (isRealChange)
            {
                StartCraneHum();
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                transform.position = Vector3.Lerp(startPos, target.position, smoothT);
                transform.rotation = Quaternion.Slerp(startRot, target.rotation, smoothT);
                yield return null;
            }

            transform.position = target.position;
            transform.rotation = target.rotation;
            SyncMouseLookAngles();

            StopCraneHum(0.25f);
            ptmCraneMoveCoroutine = null;
        }

        /// <summary>
        /// Snaps camera directly to the authoritative waypoint for any task index (0 to 9).
        /// Instant snap (0 duration, no lerp/slerp).
        /// </summary>
        public void SnapToTaskIndex(int taskIndex)
        {
            switch (taskIndex)
            {
                case 0: SnapCameraToTransform(welcomeTarget); break;
                case 1: SnapCameraToTransform(normalPotOperationTarget); break;
                case 2: SnapCameraToTransform(idealPotVoltageTarget); break;
                case 3: SnapCameraToTransform(normalPotTarget != null ? normalPotTarget : idealCBTTemperatureTarget); break;
                case 4: SnapCameraToTransform(idealCBTTemperatureTarget); break;
                case 5: SnapCameraToTransform(potMachineLookTarget); break;
                case 6: SnapCameraToTransform(potMachineLookTarget); break;
                case 7: SnapCameraToTransform(cbtLookTarget); break;
                case 8: SnapCameraToTransform(toolsTarget); break;
                case 9:
                    if (ptmCraneTarget == null)
                    {
                        var pt = GameObject.Find("CameraSystem/TransformPoints/PTM Crane") ?? GameObject.Find("TransformPoints/PTM Crane") ?? GameObject.Find("PTM Crane");
                        if (pt != null) ptmCraneTarget = pt.transform;
                    }
                    SnapCameraToTransform(ptmCraneTarget != null ? ptmCraneTarget : idealPotVoltageTarget);
                    break;
                default:
                    Debug.LogWarning($"[PotLeakageCameraController] Invalid task index for camera snap: {taskIndex}");
                    break;
            }
        }

        public void ResetCamera()
        {
            if (welcomeTarget != null)
            {
                SnapCameraToTransform(welcomeTarget);
            }
            else
            {
                transform.position = initialPosition;
                transform.rotation = initialRotation;
            }
        }

        private void PlayTransitionAudio()
        {
            var clip = swapAudioClip != null ? swapAudioClip : cameraTransitionAudio;
            if (clip != null)
            {
                if (SequenceHelperFunctions.instance != null)
                {
                    SequenceHelperFunctions.instance.PlaySFX(clip);
                }
                else if (TruckTyreReplacement.Core.Manager.Instance != null)
                {
                    TruckTyreReplacement.Core.Manager.Instance.PlaySFX(clip);
                }
            }
        }
    }
}
