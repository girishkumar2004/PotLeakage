using UnityEngine;

namespace PotLeakage.Camera
{
    public class PotLeakageCameraController : MonoBehaviour
    {
        [Header("Camera Targets")]
        [Tooltip("Authoritative starting camera position and rotation")]
        public Transform welcomeTarget;

        [Tooltip("Focus view on the main pot machine during normal operation")]
        public Transform normalPotOperationTarget;

        [Tooltip("Focus view on Ideal Pot Voltage under TransformPoints")]
        public Transform idealPotVoltageTarget;

        [Tooltip("Focus view on Ideal CBT Temperature under TransformPoints")]
        public Transform idealCBTTemperatureTarget;

        // Backward compatibility aliases
        public Transform potMachineTarget { get => normalPotOperationTarget; set => normalPotOperationTarget = value; }
        public Transform welcomeCameraTarget { get => welcomeTarget; set => welcomeTarget = value; }
        public Transform potMachineCameraTarget { get => normalPotOperationTarget; set => normalPotOperationTarget = value; }
        public Transform voltageCameraTarget { get => idealPotVoltageTarget; set => idealPotVoltageTarget = value; }
        public Transform cbtCameraTarget { get => idealCBTTemperatureTarget; set => idealCBTTemperatureTarget = value; }

        [Header("Look Targets")]
        public Transform potMachineLookTarget;
        public Transform cbtLookTarget;
        public Transform toolsTarget;

        [Header("Audio")]
        [Tooltip("SFX played on camera transition (swap.mp3)")]
        public AudioClip swapAudioClip;

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

            if (swapAudioClip == null)
            {
#if UNITY_EDITOR
                swapAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/swap.mp3");
#endif
            }

            if (welcomeTarget != null)
            {
                transform.position = welcomeTarget.position;
                transform.rotation = welcomeTarget.rotation;
                initialPosition = welcomeTarget.position;
                initialRotation = welcomeTarget.rotation;
            }
        }

        /// <summary>
        /// Instantly snaps the camera to the target transform's position and rotation with 0 duration.
        /// Zero panning, zero interpolation, zero coroutines.
        /// Plays swap audio SFX ONLY when there is a real camera target change.
        /// </summary>
        public void SnapCameraToTransform(Transform target)
        {
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
        public void MoveToPotMachine() => SnapCameraToTransform(normalPotOperationTarget);
        public void MoveToPotMachineTarget() => SnapCameraToTransform(normalPotOperationTarget);
        public void MoveToIdealPotVoltage() => SnapCameraToTransform(idealPotVoltageTarget);
        public void MoveToIdealCBTTemperature() => SnapCameraToTransform(idealCBTTemperatureTarget);
        public void MoveToCBTTarget() => SnapCameraToTransform(idealCBTTemperatureTarget);
        public void MoveToTools() => SnapCameraToTransform(toolsTarget);
        public void MoveToToolsTarget() => SnapCameraToTransform(toolsTarget);

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
                case 3: SnapCameraToTransform(idealCBTTemperatureTarget); break;
                case 4: SnapCameraToTransform(idealCBTTemperatureTarget); break;
                case 5: SnapCameraToTransform(potMachineLookTarget); break;
                case 6: SnapCameraToTransform(potMachineLookTarget); break;
                case 7: SnapCameraToTransform(cbtLookTarget); break;
                case 8: SnapCameraToTransform(toolsTarget); break;
                case 9: SnapCameraToTransform(idealPotVoltageTarget); break;
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
