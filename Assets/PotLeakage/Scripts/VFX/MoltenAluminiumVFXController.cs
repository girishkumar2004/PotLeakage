using System.Collections;
using UnityEngine;

namespace PotLeakage.VFX
{
    /// <summary>
    /// Controls molten metal leakage visualization.
    /// Machine/Cube.010 serves strictly as the stationary source/fire reference.
    /// The molten stream is represented by a single continuous organic tube mesh (MoltenMetalFlowMesh)
    /// rendered with the imported Free Lava Shader (Assets/Shaders/Lava/Lavafall.mat).
    /// Cube.010 itself is NEVER translated, deformed, or scaled.
    /// TransformPoints/drop is strictly READ-ONLY and untouched.
    /// </summary>
    public class MoltenAluminiumVFXController : MonoBehaviour
    {
        [Header("Continuous Molten Metal Flow (Primary VFX)")]
        [Tooltip("The procedural tube mesh generator for the single continuous molten stream")]
        public MoltenMetalFlowMesh flowMesh;

        [Tooltip("The MeshRenderer displaying the continuous lava flow")]
        public MeshRenderer flowMeshRenderer;

        [Tooltip("The imported Lavafall material (Shader Graphs/Lava)")]
        public Material flowMaterial;

        [Tooltip("Source reference transform: Machine/Cube.010 (READ-ONLY)")]
        public Transform sourceTransform;

        [Tooltip("Drop destination reference: TransformPoints/drop (READ-ONLY)")]
        public Transform dropTransform;

        [Tooltip("Optional secondary droplets around the drop area (Max 40 particles)")]
        public ParticleSystem moltenMetalDroplets;

        [Header("Gradual Molten Metal Floor Spill")]
        [Tooltip("Procedural molten metal puddle expanding on Machine/Floor below drop")]
        public MoltenMetalFloorSpill floorSpill;

        [Tooltip("Current flow visual state")]
        public bool flowEnabled = false;

        [Header("Cube.010 Material Synchronization")]
        [Tooltip("MeshRenderer of Cube.010 source reference")]
        public MeshRenderer cube010Renderer;

        [Tooltip("Cached original material of Cube.010 for restoration when leakage stops")]
        public Material cube010OriginalMaterial;

        public MeshRenderer Cube010Renderer => cube010Renderer;
        public Material Cube010OriginalMaterial => cube010OriginalMaterial;

        [Header("Legacy/Accompanying References")]
        public GameObject cube010;
        public Transform dropTarget;
        public Material magmaMeshMaterial;
        public Material liquidMaterial;
        public Material lavaParticleMaterial;
        public ParticleSystem moltenMetalOverflowVFX;
        public ParticleSystem droolParticleSystem;
        public Transform moltenAluminiumVFXTransform;
        public int maxParticles = 50;
        public float emissionRate = 15f;
        public float particleSize = 0.05f;
        public float glowIntensity = 1.8f;

        public static readonly Vector3 PathStart = new Vector3(-4.32000017f, 0.262073308f, -68.1200027f);
        public static readonly Vector3 PathDrop  = new Vector3(-3.76804996f, -0.156000003f, -67.387001f);

        public Vector3 StartPosition => PathStart;

        private void Awake()
        {
            InitializeTargets();
        }

        public void InitializeTargets()
        {
            // Resolve sourceTransform / cube010 strictly as reference
            if (sourceTransform == null)
            {
                var machine = GameObject.Find("Machine");
                if (machine != null)
                {
                    var c = machine.transform.Find("Cube.010");
                    if (c != null) sourceTransform = c;
                }
                if (sourceTransform == null)
                {
                    var cGo = GameObject.Find("Cube.010");
                    if (cGo != null) sourceTransform = cGo.transform;
                }
            }

            if (cube010 == null && sourceTransform != null)
            {
                cube010 = sourceTransform.gameObject;
            }

            // Resolve cube010Renderer and cache original material
            if (cube010Renderer == null)
            {
                if (cube010 != null) cube010Renderer = cube010.GetComponent<MeshRenderer>();
                if (cube010Renderer == null && sourceTransform != null) cube010Renderer = sourceTransform.GetComponent<MeshRenderer>();
                if (cube010Renderer == null)
                {
                    var cGo = GameObject.Find("Machine/Cube.010") ?? GameObject.Find("Cube.010");
                    if (cGo != null) cube010Renderer = cGo.GetComponent<MeshRenderer>();
                }
            }

            if (cube010Renderer != null && cube010OriginalMaterial == null)
            {
                Material currentMat = cube010Renderer.sharedMaterial;
                Material currentFlowMat = flowMeshRenderer != null ? flowMeshRenderer.sharedMaterial : flowMaterial;
                if (currentFlowMat == null || currentMat != currentFlowMat)
                {
                    cube010OriginalMaterial = currentMat;
                }
#if UNITY_EDITOR
                if (cube010OriginalMaterial == null)
                {
                    cube010OriginalMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/PotLeakage/Materials/M_Magma_HotMetal.mat");
                }
#endif
            }

            // Resolve dropTransform / dropTarget strictly as reference
            if (dropTransform == null)
            {
                var dropGo = GameObject.Find("CameraSystem/TransformPoints/drop")
                          ?? GameObject.Find("TransformPoints/drop")
                          ?? GameObject.Find("drop");
                if (dropGo != null) dropTransform = dropGo.transform;
            }

            if (dropTarget == null && dropTransform != null)
            {
                dropTarget = dropTransform;
            }

            // Resolve MoltenMetalFlow hierarchy
            if (flowMesh == null)
            {
                var flowMeshGo = GameObject.Find("Machine/MoltenMetalFlow/MoltenMetalFlowMesh")
                              ?? GameObject.Find("MoltenMetalFlowMesh");
                if (flowMeshGo != null)
                {
                    flowMesh = flowMeshGo.GetComponent<MoltenMetalFlowMesh>();
                }
            }

            if (flowMesh != null)
            {
                if (flowMeshRenderer == null)
                {
                    flowMeshRenderer = flowMesh.GetComponent<MeshRenderer>();
                }
                if (flowMesh.sourceTransform == null && sourceTransform != null)
                {
                    flowMesh.sourceTransform = sourceTransform;
                }
                if (flowMesh.dropTransform == null && dropTransform != null)
                {
                    flowMesh.dropTransform = dropTransform;
                }
            }

            // Resolve Flow Material
            if (flowMaterial == null)
            {
#if UNITY_EDITOR
                flowMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/Lava/Lavafall.mat");
#endif
            }

            if (flowMesh != null && flowMaterial != null)
            {
                flowMesh.flowMaterial = flowMaterial;
                if (flowMeshRenderer != null && flowMeshRenderer.sharedMaterial != flowMaterial)
                {
                    flowMeshRenderer.sharedMaterial = flowMaterial;
                }
            }

            // Resolve Droplets
            if (moltenMetalDroplets == null)
            {
                var dropletsGo = GameObject.Find("Machine/MoltenMetalFlow/MoltenMetalDroplets")
                              ?? GameObject.Find("MoltenMetalDroplets");
                if (dropletsGo != null)
                {
                    moltenMetalDroplets = dropletsGo.GetComponent<ParticleSystem>();
                }
            }

            if (moltenMetalDroplets != null)
            {
                var main = moltenMetalDroplets.main;
                main.maxParticles = Mathf.Min(main.maxParticles, 40);
                main.playOnAwake = false;
                main.loop = true;
            }

            // Ensure mesh is constructed
            if (flowMesh != null)
            {
                var mf = flowMesh.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null || mf.sharedMesh.vertexCount == 0)
                {
                    flowMesh.BuildStreamMesh();
                }
            }

            // Resolve Floor Spill
            if (floorSpill == null)
            {
                var floorSpillGo = GameObject.Find("Machine/MoltenMetalFlow/MoltenMetalFloorSpill")
                                ?? GameObject.Find("MoltenMetalFloorSpill");
                if (floorSpillGo != null)
                {
                    floorSpill = floorSpillGo.GetComponent<MoltenMetalFloorSpill>();
                }
            }

            if (floorSpill != null && flowMaterial != null)
            {
                floorSpill.spillMaterial = flowMaterial;
                floorSpill.InitializeComponents();
            }
        }

        /// <summary>
        /// Starts/restarts the continuous molten metal stream, optional droplets,
        /// and applies the molten-metal flow material to Cube.010.
        /// Reusable across multiple re-entries (e.g. Task 03, Task 09).
        /// </summary>
        public void PlayMoltenMetalOverflow()
        {
            InitializeTargets();

            // Ensure flow mesh exists and is built
            if (flowMesh != null)
            {
                var mf = flowMesh.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null || mf.sharedMesh.vertexCount == 0)
                {
                    flowMesh.BuildStreamMesh();
                }

                flowMesh.gameObject.SetActive(true);
            }

            if (flowMeshRenderer != null)
            {
                flowMeshRenderer.enabled = true;
            }

            // Play secondary droplets if present
            if (moltenMetalDroplets != null)
            {
                moltenMetalDroplets.gameObject.SetActive(true);
                moltenMetalDroplets.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                moltenMetalDroplets.Clear(true);
                moltenMetalDroplets.Play(true);
            }

            // Apply exact molten-metal flow material to Cube.010
            ApplyMoltenMaterialToCube010();

            flowEnabled = true;
        }

        /// <summary>
        /// Applies the exact same material instance/appearance from MoltenMetalFlowMesh to Cube.010.
        /// Reuses the material actually assigned to MoltenMetalFlowMesh.
        /// Does NOT modify Cube.010's transform, geometry, or create duplicate GameObjects.
        /// </summary>
        public void ApplyMoltenMaterialToCube010()
        {
            InitializeTargets();

            if (cube010Renderer == null)
            {
                Debug.LogError("[MoltenAluminiumVFXController] Cube.010 MeshRenderer is missing! Cannot synchronize material.");
                return;
            }

            Material matToApply = null;
            if (flowMesh != null)
            {
                var flowR = flowMesh.GetComponent<MeshRenderer>();
                if (flowR != null && flowR.sharedMaterial != null)
                {
                    matToApply = flowR.sharedMaterial;
                }
            }

            if (matToApply == null && flowMeshRenderer != null && flowMeshRenderer.sharedMaterial != null)
            {
                matToApply = flowMeshRenderer.sharedMaterial;
            }

            if (matToApply == null && flowMaterial != null)
            {
                matToApply = flowMaterial;
            }

            if (matToApply == null)
            {
                Debug.LogError("[MoltenAluminiumVFXController] MoltenMetalFlowMesh material is missing! Cannot synchronize Cube.010 material.");
                return;
            }

            // Cache original material before replacing
            if (cube010OriginalMaterial == null && cube010Renderer.sharedMaterial != matToApply)
            {
                cube010OriginalMaterial = cube010Renderer.sharedMaterial;
            }

            if (Application.isPlaying)
            {
                cube010Renderer.material = matToApply;
            }
            else
            {
                cube010Renderer.sharedMaterial = matToApply;
            }
        }

        /// <summary>
        /// Stops the molten metal stream, clears any active secondary droplets,
        /// and restores Cube.010's original material.
        /// Does NOT destroy GameObjects or meshes, keeping the system fully reusable.
        /// </summary>
        public void StopMoltenMetalOverflow()
        {
            if (flowMeshRenderer != null)
            {
                flowMeshRenderer.enabled = false;
            }

            if (moltenMetalDroplets != null)
            {
                moltenMetalDroplets.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                moltenMetalDroplets.Clear(true);
            }

            RestoreCube010Material();
            SetControlledLeakage(false, 0f);

            flowEnabled = false;
        }

        /// <summary>
        /// Restores Cube.010's original material cached before the leakage VFX was triggered.
        /// </summary>
        public void RestoreCube010Material()
        {
            if (cube010Renderer == null)
            {
                InitializeTargets();
            }

            if (cube010Renderer != null && cube010OriginalMaterial != null)
            {
                cube010Renderer.sharedMaterial = cube010OriginalMaterial;
            }
        }

        // Gradual Molten Metal Floor Spill Control
        public void StartFloorSpill()
        {
            InitializeTargets();
            if (floorSpill != null)
            {
                floorSpill.StartSpill();
            }
        }

        public void StopFloorSpill()
        {
            if (floorSpill != null)
            {
                floorSpill.StopSpill();
            }
        }

        public void ResetFloorSpill()
        {
            if (floorSpill != null)
            {
                floorSpill.ResetSpill();
            }
        }

        [Header("Controlled Leakage State")]
        public bool isControlledLeakage = false;

        /// <summary>
        /// Transitions the molten stream into a thinner, controlled stream (e.g. after side breaking & fused alumina pushing).
        /// Preserves the continuous stream, source, and drop targets without abruptly deleting or disabling the flow.
        /// When controlled is true, also settles the floor spill so the existing pool stops expanding while remaining visible.
        /// </summary>
        public void SetControlledLeakage(bool controlled, float duration = 1.0f)
        {
            isControlledLeakage = controlled;

            if (flowMesh != null)
            {
                flowMesh.SetControlled(controlled, duration);
            }

            if (moltenMetalDroplets != null)
            {
                var emission = moltenMetalDroplets.emission;
                emission.rateOverTime = controlled ? 3f : emissionRate;
                var main = moltenMetalDroplets.main;
                main.startSize = controlled ? 0.025f : particleSize;
            }

            if (controlled && floorSpill != null)
            {
                // Settle floor spill: stops rapid pool expansion, keeping the existing pool visible on the floor
                floorSpill.SettleSpill();
            }
        }

        // Backward compatibility hooks
        public void StartMagmaDrool() => PlayMoltenMetalOverflow();
        public void StopMagmaDrool(bool resetPosition = true) => StopMoltenMetalOverflow();
        public void EnableVFX() => PlayMoltenMetalOverflow();
        public void DisableVFX() => StopMoltenMetalOverflow();

        public void ResetPosition()
        {
            // Cube.010 is ALWAYS preserved at PathStart
            if (cube010 != null)
            {
                cube010.transform.position = PathStart;
            }
            RestoreCube010Material();
            ResetFloorSpill();
            SetControlledLeakage(false, 0f);
        }

        private void OnDisable()
        {
            StopMoltenMetalOverflow();
            StopFloorSpill();
        }
    }
}
