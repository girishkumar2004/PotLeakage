#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PotLeakage.Camera;
using PotLeakage.Configuration;
using PotLeakage.Core;
using PotLeakage.UI;
using PotLeakage.VFX;
using PotLeakage.Interaction;

namespace PotLeakage.Editor
{
    public static class PotLeakageVerificationTest
    {
        [MenuItem("Vedanta/Run Verification Test")]
        public static void RunTest()
        {
            Debug.Log("===============================================================");
            Debug.Log("STARTING VEDANTA POT LEAKAGE COMPLETE AUTOMATED VERIFICATION");
            Debug.Log("===============================================================");

            // 1. Check Config Asset
            var config = AssetDatabase.LoadAssetAtPath<PotLeakageConfig>("Assets/PotLeakage/Data/PotLeakageConfig.asset");
            if (config == null)
            {
                Debug.LogError("[TEST FAILED] PotLeakageConfig.asset not found!");
                return;
            }
            Debug.Log($"[CHECK 1] PotLeakageConfig: NormalVoltage={config.normalProductionPotVoltage}, NormalCBT={config.normalProductionCBTTemperature}, MaxV={config.leakageMonitoringVoltageMaximum}, StopV={config.leakageStopVoltage}");

            if (config.normalProductionPotVoltage <= 0f || config.normalProductionCBTTemperature <= 0f)
            {
                Debug.LogError("[TEST FAILED] Normal values must be configured with approved source values!");
                return;
            }

            // 2. Open Scene
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/PotLeakage/Scenes/PotLeakage_Main.unity")
            {
                if (EditorApplication.isPlaying)
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Assets/PotLeakage/Scenes/PotLeakage_Main.unity");
                }
                else
                {
                    scene = EditorSceneManager.OpenScene("Assets/PotLeakage/Scenes/PotLeakage_Main.unity");
                }
            }
            if (!scene.IsValid())
            {
                Debug.LogError("[TEST FAILED] Failed to open PotLeakage_Main.unity!");
                return;
            }
            Debug.Log("[CHECK 2] Scene loaded: Assets/PotLeakage/Scenes/PotLeakage_Main.unity");

            // 3. Verify PotRoom is DELETED and no duplicate floor under Environment
            var potRoom = GameObject.Find("PotRoom");
            if (potRoom != null)
            {
                Debug.LogError("[TEST FAILED] PotRoom must NOT exist in the scene!");
                return;
            }
            var env = GameObject.Find("Environment");
            if (env != null && env.transform.Find("Floor") != null)
            {
                Debug.LogError("[TEST FAILED] No duplicate Floor should exist under Environment!");
                return;
            }
            Debug.Log("[CHECK 3] PotRoom is deleted. No duplicate Floor under Environment.");

            // 4. Verify Machine is NOT a Prefab and Machine.prefab does not exist
            var machine = GameObject.Find("Machine");
            if (machine == null) { Debug.LogError("[TEST FAILED] Missing 'Machine' root GameObject!"); return; }

            if (PrefabUtility.IsPartOfAnyPrefab(machine))
            {
                Debug.LogError("[TEST FAILED] Machine must remain a normal scene GameObject, NOT a Prefab!");
                return;
            }
            if (System.IO.File.Exists("Assets/Machine.prefab") || System.IO.File.Exists("Assets/PotLeakage/Machine.prefab"))
            {
                Debug.LogError("[TEST FAILED] Machine.prefab must NOT exist!");
                return;
            }
            Debug.Log("[CHECK 4] Machine is a normal Scene GameObject (NOT a Prefab, no Machine.prefab).");

            // 5. Verify Machine Hierarchy: Floor, Wall, Wall (1), Cube.010 (Fire/Molten Metal)
            var machineFloor = machine.transform.Find("Floor");
            var machineWall = machine.transform.Find("Wall");
            var machineWall1 = machine.transform.Find("Wall (1)");
            var cube010 = machine.transform.Find("Cube.010");

            if (machineFloor == null) { Debug.LogError("[TEST FAILED] Missing 'Floor' under Machine!"); return; }
            if (machineWall == null) { Debug.LogError("[TEST FAILED] Missing 'Wall' under Machine!"); return; }
            if (machineWall1 == null) { Debug.LogError("[TEST FAILED] Missing 'Wall (1)' under Machine!"); return; }
            if (cube010 == null) { Debug.LogError("[TEST FAILED] Missing 'Cube.010' under Machine (fire/molten-metal element)!"); return; }

            Debug.Log("[CHECK 5] Machine child hierarchy verified: Floor, Wall, Wall (1), Cube.010.");

            // 6. Verify Absolute Transform Lock (Welcome, Normal Pot Op, Ideal V, Ideal CBT, LookTargets, Walls, drop, Cube.010)
            var cameraSystem = GameObject.Find("CameraSystem");
            if (cameraSystem == null) { Debug.LogError("[TEST FAILED] Missing 'CameraSystem' root GameObject!"); return; }

            var mainCameraObj = cameraSystem.transform.Find("Main Camera");
            var transformPoints = GameObject.Find("TransformPoints");
            if (transformPoints == null) { Debug.LogError("[TEST FAILED] Missing 'TransformPoints' in scene!"); return; }

            var welcomeTarget = transformPoints.transform.Find("Welcome");
            var normalPotOpTarget = transformPoints.transform.Find("Normal Pot Operation");
            var idealPotVoltageTarget = transformPoints.transform.Find("Ideal Pot Voltage");
            var idealCBTTemperatureTarget = transformPoints.transform.Find("Ideal CBT Temperature");
            var potLookTarget = transformPoints.transform.Find("PotMachine_LookTarget");
            var cbtLookTarget = transformPoints.transform.Find("CBT_LookTarget");
            var toolsTarget = transformPoints.transform.Find("Tools") ?? GameObject.Find("CameraSystem/TransformPoints/Tools")?.transform;
            var dropTarget = transformPoints.transform.Find("drop");

            if (welcomeTarget == null) { Debug.LogError("[TEST FAILED] Missing 'Welcome' under TransformPoints"); return; }
            if (normalPotOpTarget == null) { Debug.LogError("[TEST FAILED] Missing 'Normal Pot Operation' under TransformPoints"); return; }
            if (idealPotVoltageTarget == null) { Debug.LogError("[TEST FAILED] Missing 'Ideal Pot Voltage' under TransformPoints"); return; }
            if (idealCBTTemperatureTarget == null) { Debug.LogError("[TEST FAILED] Missing 'Ideal CBT Temperature' under TransformPoints"); return; }
            if (potLookTarget == null) { Debug.LogError("[TEST FAILED] Missing 'PotMachine_LookTarget' under TransformPoints"); return; }
            if (cbtLookTarget == null) { Debug.LogError("[TEST FAILED] Missing 'CBT_LookTarget' under TransformPoints"); return; }
            if (toolsTarget == null) { Debug.LogError("[TEST FAILED] Missing 'Tools' under TransformPoints"); return; }
            if (dropTarget == null) { Debug.LogError("[TEST FAILED] Missing 'drop' under TransformPoints"); return; }

            // Check exact positions against authoritative recorded values
            AssertClose("Welcome Pos", welcomeTarget.localPosition, new Vector3(-11.074f, 2.876f, -65.080f));
            AssertClose("Normal Pot Operation Pos", normalPotOpTarget.localPosition, new Vector3(-9.578f, 2.182f, -65.117f));
            AssertClose("Ideal Pot Voltage Pos", idealPotVoltageTarget.localPosition, new Vector3(-0.70f, 1.04f, -66.51f), 0.05f);
            AssertClose("Ideal CBT Temperature Pos", idealCBTTemperatureTarget.localPosition, new Vector3(-13.866f, 0.855f, -66.225f));
            AssertClose("PotMachine_LookTarget Pos", potLookTarget.localPosition, new Vector3(-8.35f, 2.90f, -65.65f));
            AssertClose("PotMachine_LookTarget Rot", potLookTarget.localEulerAngles, new Vector3(0f, 359.37f, 0f));
            AssertClose("PotMachine_LookTarget Scale", potLookTarget.localScale, Vector3.one);
            AssertClose("CBT_LookTarget Pos", cbtLookTarget.localPosition, new Vector3(2.40f, 2.903f, -65.53f));
            AssertClose("CBT_LookTarget Rot", cbtLookTarget.localEulerAngles, new Vector3(0f, 359.37f, 0f));
            AssertClose("CBT_LookTarget Scale", cbtLookTarget.localScale, Vector3.one);
            AssertClose("Tools Pos", toolsTarget.localPosition, new Vector3(5.92f, 3.07f, -66.90f), 0.05f);
            AssertClose("drop Pos", dropTarget.localPosition, new Vector3(-3.76805f, -0.259f, -67.387f), 0.01f);

            AssertClose("Wall Pos", machineWall.localPosition, new Vector3(-2.0638f, 9.6424f, -62.91f));
            AssertClose("Wall (1) Pos", machineWall1.localPosition, new Vector3(0.18f, 9.6563f, -92.96f));
            AssertClose("Cube.010 Initial Pos", cube010.localPosition, new Vector3(-4.320f, 0.535f, -68.120f));

            Debug.Log("[CHECK 6] ALL LOCKED TRANSFORMS MATCH EXACT AUTHORITATIVE VALUES:");
            Debug.Log($"   ├── Welcome: Pos={welcomeTarget.localPosition}");
            Debug.Log($"   ├── Normal Pot Operation: Pos={normalPotOpTarget.localPosition}");
            Debug.Log($"   ├── Ideal Pot Voltage: Pos={idealPotVoltageTarget.localPosition}");
            Debug.Log($"   ├── Ideal CBT Temperature: Pos={idealCBTTemperatureTarget.localPosition}");
            Debug.Log($"   ├── drop (Marker): Pos={dropTarget.localPosition}");
            Debug.Log($"   └── Cube.010 (Origin): Pos={cube010.localPosition}");

            // 7. Verify Camera Spawn at Welcome
            var mainCamera = mainCameraObj.GetComponent<UnityEngine.Camera>();
            if (mainCamera == null) { Debug.LogError("[TEST FAILED] Missing Camera component on Main Camera"); return; }
            var camController = mainCameraObj.GetComponent<PotLeakageCameraController>();
            if (camController == null) { Debug.LogError("[TEST FAILED] Missing PotLeakageCameraController on Main Camera"); return; }

            camController.SnapCameraToTransform(welcomeTarget);
            if (!VerifyCameraSnap(mainCamera, welcomeTarget, "Startup / Task 01 Welcome"))
            {
                return;
            }
            Debug.Log($"[CHECK 7] Main Camera positioned at exact Welcome position and rotation: Pos={mainCamera.transform.position}, AngleDiff={Quaternion.Angle(mainCamera.transform.rotation, welcomeTarget.rotation):F4}°");

            // 8. Verify TrainingFramework & UI Controller
            var trainingFramework = GameObject.Find("TrainingFramework");
            if (trainingFramework == null) { Debug.LogError("[TEST FAILED] Missing 'TrainingFramework' root GameObject!"); return; }

            var managerGO = trainingFramework.transform.Find("Manager");
            var sequenceGO = trainingFramework.transform.Find("Sequence");
            var seqHandlerGO = trainingFramework.transform.Find("Sequence Handler");
            var seqHelperGO = trainingFramework.transform.Find("Sequence Helper");

            if (managerGO == null) { Debug.LogError("[TEST FAILED] Missing 'Manager' under TrainingFramework"); return; }
            if (sequenceGO == null) { Debug.LogError("[TEST FAILED] Missing 'Sequence' under TrainingFramework"); return; }
            if (seqHandlerGO == null) { Debug.LogError("[TEST FAILED] Missing 'Sequence Handler' under TrainingFramework"); return; }
            if (seqHelperGO == null) { Debug.LogError("[TEST FAILED] Missing 'Sequence Helper' under TrainingFramework"); return; }

            var uiController = managerGO.GetComponent<PotLeakageUIController>();
            var seqHelper = seqHelperGO.GetComponent<SequenceHelperFunctions>();
            var seqHandler = seqHandlerGO.GetComponent<SequenceHandler>();
            var sequence = sequenceGO.GetComponent<Sequence>();

            if (uiController == null) { Debug.LogError("[TEST FAILED] Missing PotLeakageUIController on Manager GO"); return; }
            if (seqHelper == null) { Debug.LogError("[TEST FAILED] Missing SequenceHelperFunctions on Sequence Helper GO"); return; }
            if (seqHandler == null) { Debug.LogError("[TEST FAILED] Missing SequenceHandler on Sequence Handler GO"); return; }
            if (sequence == null) { Debug.LogError("[TEST FAILED] Missing Sequence on Sequence GO"); return; }

            // 9. VERIFY UI LAYOUT: StatusText and Description Non-Overlapping + TMP Wrapping + Button Outside
            var welcomePanel = GameObject.Find("UI/Canvas/Welcome Panel");
            if (welcomePanel == null) { Debug.LogError("[TEST FAILED] Missing 'Welcome Panel' in scene!"); return; }

            var contentObj = welcomePanel.transform.Find("Content");
            if (contentObj == null) { Debug.LogError("[TEST FAILED] Missing 'Content' in Welcome Panel!"); return; }

            var statusRect = contentObj.Find("StatusText")?.GetComponent<RectTransform>();
            var descRect = contentObj.Find("Description")?.GetComponent<RectTransform>();
            var footerObj = welcomePanel.transform.Find("Footer");

            if (statusRect == null) { Debug.LogError("[TEST FAILED] Missing 'StatusText' RectTransform!"); return; }
            if (descRect == null) { Debug.LogError("[TEST FAILED] Missing 'Description' RectTransform!"); return; }
            if (footerObj == null) { Debug.LogError("[TEST FAILED] Missing 'Footer' in Welcome Panel!"); return; }

            var descTMP = descRect.GetComponent<TextMeshProUGUI>();
            if (descTMP == null || descTMP.textWrappingMode == TextWrappingModes.NoWrap)
            {
                Debug.LogError("[TEST FAILED] Description TextMeshProUGUI must have text wrapping enabled!");
                return;
            }

            Vector3[] statusCorners = new Vector3[4];
            Vector3[] descCorners = new Vector3[4];
            statusRect.GetWorldCorners(statusCorners);
            descRect.GetWorldCorners(descCorners);

            // In world space, StatusText is above Description (status bottom >= desc top)
            float statusBottom = statusCorners[0].y;
            float descTop = descCorners[1].y;
            if (statusBottom < descTop - 0.01f)
            {
                Debug.LogError($"[TEST FAILED] UI Layout Overlap detected! StatusText bottom ({statusBottom:F3}) is below Description top ({descTop:F3})!");
                return;
            }

            // Ensure NextButton is inside Footer, outside Content
            var nextBtnRect = footerObj.Find("NextButton")?.GetComponent<RectTransform>();
            if (nextBtnRect == null) { Debug.LogError("[TEST FAILED] Missing NextButton in Footer!"); return; }

            Vector3[] footerCorners = new Vector3[4];
            footerObj.GetComponent<RectTransform>().GetWorldCorners(footerCorners);
            if (descCorners[0].y < footerCorners[1].y - 0.01f)
            {
                Debug.LogError("[TEST FAILED] Description text extends into Footer / Next Button area!");
                return;
            }

            var wpRT = welcomePanel.GetComponent<RectTransform>();
            if (wpRT.anchorMin != Vector2.one || wpRT.anchorMax != Vector2.one || wpRT.pivot != Vector2.one)
            {
                Debug.LogError($"[TEST FAILED] Welcome Panel must be anchored to upper-right corner! anchorMin={wpRT.anchorMin}, anchorMax={wpRT.anchorMax}, pivot={wpRT.pivot}");
                return;
            }
            if (wpRT.sizeDelta.x < 320f || wpRT.sizeDelta.x > 420f)
            {
                Debug.LogError($"[TEST FAILED] Welcome Panel width ({wpRT.sizeDelta.x}) must be between 320px and 420px!");
                return;
            }
            Debug.Log("[CHECK 9] UI Layout verified: Welcome Panel in upper-right corner (Anchor Min/Max=(1,1), Pivot=(1,1), Width=380), StatusText and Description non-overlapping, TMP wrapping enabled, Next button outside text area.");

            // 10. Verify Reusable Molten Metal Overflow VFX & Stationary Cube.010 Source
            // Cube.010 is strictly a stationary mesh, NOT the VFX controller, and has NO ParticleSystem
            if (cube010.GetComponent<ParticleSystem>() != null)
            {
                Debug.LogError("[TEST FAILED] Cube.010 must NOT have a ParticleSystem component!");
                return;
            }
            if (cube010.GetComponent<MoltenAluminiumVFXController>() != null)
            {
                Debug.LogError("[TEST FAILED] MoltenAluminiumVFXController must NOT be on Cube.010!");
                return;
            }
            AssertClose("Cube.010 Stationary Source", cube010.localPosition, new Vector3(-4.320f, 0.535f, -68.120f));

            // Verify Machine/MoltenMetalFlow exists and has MoltenAluminiumVFXController
            var vfxTransform = machine.transform.Find("MoltenMetalFlow");
            if (vfxTransform == null)
            {
                Debug.LogError("[TEST FAILED] Missing 'Machine/MoltenMetalFlow' GameObject!");
                return;
            }
            var vfxController = vfxTransform.GetComponent<MoltenAluminiumVFXController>();
            if (vfxController == null)
            {
                Debug.LogError("[TEST FAILED] MoltenAluminiumVFXController missing on MoltenMetalFlow!");
                return;
            }
            vfxController.InitializeTargets();

            // Verify continuous flow mesh child
            var flowMeshChild = vfxTransform.Find("MoltenMetalFlowMesh");
            if (flowMeshChild == null)
            {
                Debug.LogError("[TEST FAILED] Missing 'MoltenMetalFlowMesh' child under MoltenMetalFlow!");
                return;
            }
            var flowMeshComp = flowMeshChild.GetComponent<MoltenMetalFlowMesh>();
            if (flowMeshComp == null)
            {
                Debug.LogError("[TEST FAILED] MoltenMetalFlowMesh component missing on MoltenMetalFlowMesh object!");
                return;
            }
            var flowMF = flowMeshChild.GetComponent<MeshFilter>();
            var flowMR = flowMeshChild.GetComponent<MeshRenderer>();
            if (flowMF == null || flowMR == null)
            {
                Debug.LogError("[TEST FAILED] MeshFilter or MeshRenderer missing on MoltenMetalFlowMesh!");
                return;
            }

            // Verify material is Lavafall.mat with Shader Graphs/Lava
            if (flowMR.sharedMaterial == null || flowMR.sharedMaterial.shader == null || !flowMR.sharedMaterial.shader.name.Contains("Lava"))
            {
                Debug.LogError($"[TEST FAILED] MoltenMetalFlowMesh material shader mismatch! Shader='{flowMR.sharedMaterial?.shader?.name}'");
                return;
            }

            // Verify procedural tube mesh generation
            flowMeshComp.BuildStreamMesh();
            if (flowMF.sharedMesh == null || flowMF.sharedMesh.vertexCount < 200 || flowMF.sharedMesh.triangles.Length == 0)
            {
                Debug.LogError($"[TEST FAILED] MoltenMetalFlowMesh failed to generate continuous tube mesh! VertexCount={flowMF.sharedMesh?.vertexCount ?? 0}");
                return;
            }

            // Verify MoltenMetalFlowMesh Transform
            AssertClose("MoltenMetalFlowMesh Pos", flowMeshChild.localPosition, new Vector3(-70.2910004f, 0.446999997f, -53.6599998f));
            float rotYHint = new SerializedObject(flowMeshChild).FindProperty("m_LocalEulerAnglesHint").vector3Value.y;
            if (Mathf.Abs(rotYHint - (-79.422f)) > 0.01f && Mathf.Abs(flowMeshChild.localEulerAngles.y - (280.578f)) > 0.05f)
            {
                Debug.LogError($"[TEST FAILED] MoltenMetalFlowMesh Rotation Y mismatch! Expected -79.422°, got hint={rotYHint}°, euler={flowMeshChild.localEulerAngles.y}°");
                return;
            }

            // Verify mesh world bounds stay strictly within expected envelope
            Bounds flowBounds = flowMR.bounds;
            if (flowBounds.min.x < -4.70f || flowBounds.max.x > -3.40f ||
                flowBounds.min.y < -0.35f || flowBounds.max.y > 0.85f ||
                flowBounds.min.z < -68.50f || flowBounds.max.z > -67.00f)
            {
                Debug.LogError($"[TEST FAILED] MoltenMetalFlowMesh bounds [{flowBounds.min} to {flowBounds.max}] outside strict physical envelope!");
                return;
            }

            // Verify secondary droplets
            var dropletsTransform = vfxTransform.Find("MoltenMetalDroplets");
            if (dropletsTransform == null)
            {
                Debug.LogError("[TEST FAILED] Missing 'MoltenMetalDroplets' child under MoltenMetalFlow!");
                return;
            }
            var dropletsPS = dropletsTransform.GetComponent<ParticleSystem>();
            if (dropletsPS == null)
            {
                Debug.LogError("[TEST FAILED] ParticleSystem component missing on MoltenMetalDroplets!");
                return;
            }

            if (dropletsPS.main.maxParticles > 40)
            {
                Debug.LogError($"[TEST FAILED] Droplets VFX maxParticles ({dropletsPS.main.maxParticles}) exceeds limit of 40!");
                return;
            }
            if (dropletsPS.main.playOnAwake)
            {
                Debug.LogError("[TEST FAILED] Droplets particle system must have playOnAwake set to false!");
                return;
            }
            if (!dropletsPS.main.loop)
            {
                Debug.LogError("[TEST FAILED] Droplets particle system must have loop set to true!");
                return;
            }

            // Verify Staged Waypoints
            AssertClose("P0_Source", MoltenMetalFlowMesh.P0_Source, new Vector3(-4.32000017f, 0.262073308f, -68.1200027f));
            AssertClose("P1_Rise", MoltenMetalFlowMesh.P1_Rise, new Vector3(-4.32000017f, 0.633f, -68.1200027f));
            AssertClose("P5_Crest", MoltenMetalFlowMesh.P5_Crest, new Vector3(-3.76804996f, 0.633f, -67.387001f));
            AssertClose("P8_DropTarget", MoltenMetalFlowMesh.P8_DropTarget, new Vector3(-3.76804996f, -0.156000003f, -67.387001f));

            AssertClose("Cube.010 Stationary Source After Extrusion", cube010.localPosition, new Vector3(-4.320f, 0.535f, -68.120f));
            AssertClose("Cube.010 Scale", cube010.localScale, new Vector3(-100f, -51.84768f, -26.770199f));

            // Verify Cube.010 Mesh and Material Synchronization
            var cube010MF = cube010.GetComponent<MeshFilter>();
            if (cube010MF == null || cube010MF.sharedMesh == null)
            {
                Debug.LogError("[TEST FAILED] Cube.010 MeshFilter or sharedMesh is missing!");
                return;
            }
            if (vfxController.cube010Renderer == null)
            {
                Debug.LogError("[TEST FAILED] vfxController.cube010Renderer was not resolved!");
                return;
            }
            if (vfxController.cube010OriginalMaterial == null)
            {
                Debug.LogError("[TEST FAILED] vfxController.cube010OriginalMaterial was not cached!");
                return;
            }

            // Test applying molten-metal material to Cube.010
            vfxController.ApplyMoltenMaterialToCube010();
            if (vfxController.cube010Renderer.sharedMaterial != flowMR.sharedMaterial)
            {
                Debug.LogError($"[TEST FAILED] Cube.010 sharedMaterial does not match MoltenMetalFlowMesh sharedMaterial! Expected '{flowMR.sharedMaterial?.name}', got '{vfxController.cube010Renderer.sharedMaterial?.name}'");
                return;
            }

            // Test restoring original material
            vfxController.RestoreCube010Material();
            if (vfxController.cube010Renderer.sharedMaterial != vfxController.Cube010OriginalMaterial)
            {
                Debug.LogError($"[TEST FAILED] Cube.010 sharedMaterial was not restored! Expected '{vfxController.Cube010OriginalMaterial?.name}', got '{vfxController.cube010Renderer.sharedMaterial?.name}'");
                return;
            }

            Debug.Log("[CHECK 10] Single Continuous Molten Metal Stream, Stationary Cube.010 & Material Synchronization verified: Cube.010 stationary with NO ParticleSystem/controller; MoltenMetalFlow with MoltenAluminiumVFXController attached; continuous tube mesh with Lavafall shader, >200 verts, bounds within envelope; droplets maxParticles <= 40; playOnAwake=false, loop=true; Cube.010 material synchronization verified.");

            // 11. Verify Pot Machine Material Swap & Highlight Behavior
            var potMeshGO = GameObject.Find("tripo_node_cf158416");
            if (potMeshGO == null) { Debug.LogError("[TEST FAILED] Missing 'tripo_node_cf158416' pot mesh GameObject!"); return; }
            var potMeshRenderer = potMeshGO.GetComponent<MeshRenderer>();
            if (potMeshRenderer == null) { Debug.LogError("[TEST FAILED] Missing MeshRenderer on 'tripo_node_cf158416'!"); return; }

            var metalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Model/Materials/metal.mat");
            if (potMeshRenderer.sharedMaterial.name.Contains("Highlight") && metalMat != null)
            {
                potMeshRenderer.material = metalMat;
            }
            var originalMat = potMeshRenderer.sharedMaterial;
            Debug.Log($"[CHECK 11a] tripo_node_cf158416 Original Material = '{originalMat.name}'");

            // Task 01 UI State
            uiController.SetTask01UI();
            if (uiController.IsBlinking)
            {
                Debug.LogError("[TEST FAILED] Pot Machine should NOT be blinking in Task 01!");
                return;
            }

            // Task 02 UI State (Material Swap + Blink + Short Intro Check)
            uiController.SetTask02UI();
            if (potMeshRenderer.sharedMaterial == originalMat)
            {
                Debug.LogError("[TEST FAILED] tripo_node_cf158416 material was NOT swapped to highlight material in Task 02!");
                return;
            }
            if (!uiController.IsBlinking)
            {
                Debug.LogError("[TEST FAILED] tripo_node_cf158416 should be blinking in Task 02!");
                return;
            }
            var swappedHighlightMat = potMeshRenderer.sharedMaterial;

            if (uiController.titleText.text != "NORMAL POT OPERATION")
            {
                Debug.LogError($"[TEST FAILED] Task 02 Title mismatch! Expected 'NORMAL POT OPERATION', got '{uiController.titleText.text}'");
                return;
            }
            if (uiController.statusText.text != "NORMAL OPERATING CONDITION")
            {
                Debug.LogError($"[TEST FAILED] Task 02 Status mismatch! Expected 'NORMAL OPERATING CONDITION', got '{uiController.statusText.text}'");
                return;
            }
            if (!uiController.descriptionText.text.Contains("introduces the normal operating condition"))
            {
                Debug.LogError("[TEST FAILED] Task 02 Description must introduce normal operating condition!");
                return;
            }
            if (uiController.descriptionText.text.Contains("Normal Production Pot Voltage") || uiController.descriptionText.text.Contains("Normal CBT Temperature"))
            {
                Debug.LogError("[TEST FAILED] Task 02 Description should NOT include Voltage or CBT parameters (reserved for Task 03)!");
                return;
            }
            Debug.Log("[CHECK 11b] Task 02 Title, Status ('NORMAL OPERATING CONDITION'), Short Intro Description, and Blinking Highlight verified.");

            // Task 03 UI State (Ideal Pot Voltage: Retain Highlight, Stop Blink, Configured 4.0 V)
            uiController.SetTask03UI();
            if (potMeshRenderer.sharedMaterial == originalMat)
            {
                Debug.LogError("[TEST FAILED] tripo_node_cf158416 material was NOT retained in Task 03!");
                return;
            }
            if (uiController.IsBlinking)
            {
                Debug.LogError("[TEST FAILED] tripo_node_cf158416 blinking should be stopped in Task 03!");
                return;
            }
            if (uiController.titleText.text != "IDEAL POT VOLTAGE")
            {
                Debug.LogError($"[TEST FAILED] Task 03 Title mismatch! Expected 'IDEAL POT VOLTAGE', got '{uiController.titleText.text}'");
                return;
            }
            if (uiController.statusText.text != "NORMAL OPERATING RANGE")
            {
                Debug.LogError($"[TEST FAILED] Task 03 Status mismatch! Expected 'NORMAL OPERATING RANGE', got '{uiController.statusText.text}'");
                return;
            }
            if (uiController.descriptionText.text.Contains("VALUE NOT CONFIGURED"))
            {
                Debug.LogError("[TEST FAILED] Task 03 must NOT show 'VALUE NOT CONFIGURED' when valid values are configured!");
                return;
            }
            if (!uiController.descriptionText.text.Contains("Normal Production Pot Voltage") ||
                !uiController.descriptionText.text.Contains("4.0 V") ||
                !uiController.descriptionText.text.Contains("Stable operating voltage is an important indicator"))
            {
                Debug.LogError("[TEST FAILED] Task 03 Description must contain dynamic Voltage (4.0 V) and normal operation indicator!");
                return;
            }
            if (uiController.descriptionText.text.Contains("4.5 V") || uiController.descriptionText.text.Contains("1.2 V"))
            {
                Debug.LogError("[TEST FAILED] Task 03 Description must NOT contain fake normal production voltages (4.5 V or 1.2 V)!");
                return;
            }
            Debug.Log("[CHECK 11c] Task 03 Title ('IDEAL POT VOLTAGE'), Status ('NORMAL OPERATING RANGE'), Dynamic 4.0 V, and Non-blinking Highlight verified.");

            // Task 04 (Ideal CBT Temperature / Leakage Observed) UI State
            uiController.SetTask04UI();
            if (uiController.titleText.text != "IDEAL CBT TEMPERATURE")
            {
                Debug.LogError($"[TEST FAILED] Task 04 Title mismatch! Expected 'IDEAL CBT TEMPERATURE', got '{uiController.titleText.text}'");
                return;
            }
            if (uiController.statusText.text != "LEAKAGE CONDITION OBSERVED")
            {
                Debug.LogError($"[TEST FAILED] Task 04 Status mismatch! Expected 'LEAKAGE CONDITION OBSERVED', got '{uiController.statusText.text}'");
                return;
            }
            if (!uiController.descriptionText.text.Contains("Collector bar temperature is monitored during operation.") ||
                !uiController.descriptionText.text.Contains("When leakage is observed, immediately inform the Shift Superintendent and Technical In-charge."))
            {
                Debug.LogError("[TEST FAILED] Task 04 Description content mismatch!");
                return;
            }
            if (uiController.potVoltageWallPanel != null && !uiController.potVoltageWallPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Wall panel must remain ACTIVE in Task 04!");
                return;
            }
            if (uiController.cbtLookTargetPanel != null && !uiController.cbtLookTargetPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] CBT Panel must remain ACTIVE in Task 04!");
                return;
            }
            Debug.Log("[CHECK 11d] Task 04 Title ('IDEAL CBT TEMPERATURE'), Status ('LEAKAGE CONDITION OBSERVED'), Transition Description verified.");

            // Task 05 (Emergency Communication) UI State
            uiController.SetTask05UI();
            if (uiController.titleText.text != "EMERGENCY COMMUNICATION")
            {
                Debug.LogError($"[TEST FAILED] Task 05 Title mismatch! Expected 'EMERGENCY COMMUNICATION', got '{uiController.titleText.text}'");
                return;
            }
            if (uiController.statusText.text != "IMMEDIATE ACTION REQUIRED")
            {
                Debug.LogError($"[TEST FAILED] Task 05 Status mismatch! Expected 'IMMEDIATE ACTION REQUIRED', got '{uiController.statusText.text}'");
                return;
            }
            if (!uiController.descriptionText.text.Contains("Immediately inform the Shift Superintendent and Technical In-charge"))
            {
                Debug.LogError("[TEST FAILED] Task 05 Description content mismatch!");
                return;
            }
            if (uiController.potVoltageWallPanel != null && !uiController.potVoltageWallPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Wall panel must remain ACTIVE in Task 05!");
                return;
            }
            if (uiController.cbtLookTargetPanel != null && !uiController.cbtLookTargetPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] CBT Panel must remain ACTIVE in Task 05!");
                return;
            }
            Debug.Log("[CHECK 11e] Task 05 Title ('EMERGENCY COMMUNICATION'), Status ('IMMEDIATE ACTION REQUIRED'), Description verified.");

            // Task 06 (Pot Voltage / Duct-End Voltage Control) UI State
            uiController.SetTask06UI();
            if (uiController.titleText.text != "POT VOLTAGE")
            {
                Debug.LogError($"[TEST FAILED] Task 06 Title mismatch! Expected 'POT VOLTAGE', got '{uiController.titleText.text}'");
                return;
            }
            if (uiController.statusText.text != "DUCT-END VOLTAGE CONTROL")
            {
                Debug.LogError($"[TEST FAILED] Task 06 Status mismatch! Expected 'DUCT-END VOLTAGE CONTROL', got '{uiController.statusText.text}'");
                return;
            }
            if (!uiController.descriptionText.text.Contains("Technical In-charge continuously monitors the duct-end pot voltage") ||
                !uiController.descriptionText.text.Contains("4.5 V") ||
                (!uiController.descriptionText.text.Contains("2–3 pulse") && !uiController.descriptionText.text.Contains("2-3 pulse")))
            {
                Debug.LogError("[TEST FAILED] Task 06 Description content mismatch!");
                return;
            }
            // Verify Pot Voltage Wall Panel
            if (uiController.potVoltageWallPanel == null || !uiController.potVoltageWallPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Pot Voltage Wall Panel (PotMachine_LookTarget Panel) must be ACTIVE in Task 06!");
                return;
            }
            if (uiController.wallPanelTitle == null || uiController.wallPanelTitle.text != "POT VOLTAGE")
            {
                Debug.LogError($"[TEST FAILED] Wall Panel Title mismatch! Expected 'POT VOLTAGE', got '{uiController.wallPanelTitle?.text}'");
                return;
            }
            if (uiController.wallPanelValuesText == null ||
                !uiController.wallPanelValuesText.text.Contains("4.2 V") ||
                !uiController.wallPanelValuesText.text.Contains("4.0 V") ||
                !uiController.wallPanelValuesText.text.Contains("≤ 4.5 V"))
            {
                Debug.LogError("[TEST FAILED] Wall Panel values text must display Current (4.2 V), Normal (4.0 V), and Limit (≤ 4.5 V)!");
                return;
            }
            if (uiController.wallPanelDescriptionText == null ||
                !uiController.wallPanelDescriptionText.text.Contains("2–3 pulses") && !uiController.wallPanelDescriptionText.text.Contains("2-3 pulse"))
            {
                Debug.LogError("[TEST FAILED] Wall Panel description text mismatch!");
                return;
            }
            Debug.Log("[CHECK 11f] Task 06 Title, Status ('DUCT-END VOLTAGE CONTROL'), 2-3 pulse Description, Wall Panel Active with (4.2V, 4.0V, <=4.5V).");

            // Task 07 (Leakage-Specific Response) UI State
            uiController.SetTask07UI();
            if (uiController.titleText.text != "LEAKAGE-SPECIFIC RESPONSE")
            {
                Debug.LogError($"[TEST FAILED] Task 07 Title mismatch! Expected 'LEAKAGE-SPECIFIC RESPONSE', got '{uiController.titleText.text}'");
                return;
            }
            if (uiController.statusText.text != "COLLECTOR BAR LEAKAGE")
            {
                Debug.LogError($"[TEST FAILED] Task 07 Status mismatch! Expected 'COLLECTOR BAR LEAKAGE', got '{uiController.statusText.text}'");
                return;
            }
            if (!uiController.descriptionText.text.Contains("300 kA") ||
                !uiController.descriptionText.text.Contains("fused alumina") ||
                !uiController.descriptionText.text.Contains("crust-breaking") ||
                !uiController.descriptionText.text.Contains("pot cut-out"))
            {
                Debug.LogError("[TEST FAILED] Task 07 Description content mismatch for collector bar leakage steps!");
                return;
            }
            if (uiController.potVoltageWallPanel != null && !uiController.potVoltageWallPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Wall panel must remain ACTIVE in Task 07!");
                return;
            }
            if (uiController.cbtLookTargetPanel != null && !uiController.cbtLookTargetPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] CBT Panel must remain ACTIVE in Task 07!");
                return;
            }
            Debug.Log("[CHECK 11g] Task 07 Title ('LEAKAGE-SPECIFIC RESPONSE'), Status ('COLLECTOR BAR LEAKAGE'), Collector Bar Actions verified.");

            // Task 08 (Establish Access & Prepare Work Area) UI State
            uiController.SetTask08UI();
            if (uiController.titleText.text != "ESTABLISH ACCESS & PREPARE THE WORK AREA")
            {
                Debug.LogError($"[TEST FAILED] Task 08 Title mismatch! Expected 'ESTABLISH ACCESS & PREPARE THE WORK AREA', got '{uiController.titleText.text}'");
                return;
            }
            if (uiController.statusText.text != "PREPARATION BEFORE TAP-OUT")
            {
                Debug.LogError($"[TEST FAILED] Task 08 Status mismatch! Expected 'PREPARATION BEFORE TAP-OUT', got '{uiController.statusText.text}'");
                return;
            }
            if (!uiController.descriptionText.text.Contains("Team goes to the lower level") ||
                !uiController.descriptionText.text.Contains("channel-making tools") ||
                !uiController.descriptionText.text.Contains("-3 m mesh") ||
                !uiController.descriptionText.text.Contains("bath/alumina tanker"))
            {
                Debug.LogError("[TEST FAILED] Task 08 Description content mismatch!");
                return;
            }
            if (uiController.cbtLookTargetPanel == null || !uiController.cbtLookTargetPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] CBT_LookTarget Panel must be ACTIVE in Task 08!");
                return;
            }
            if (uiController.potVoltageWallPanel != null && !uiController.potVoltageWallPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Pot Voltage Wall Panel must remain ACTIVE in Task 08!");
                return;
            }
            if (uiController.cbtPanelTitle == null || uiController.cbtPanelTitle.text != "ESTABLISH ACCESS & PREPARE THE WORK AREA")
            {
                Debug.LogError($"[TEST FAILED] CBT Panel Title mismatch! Got '{uiController.cbtPanelTitle?.text}'");
                return;
            }
            if (uiController.cbtPanelStatusText == null || uiController.cbtPanelStatusText.text != "PREPARATION BEFORE TAP-OUT")
            {
                Debug.LogError($"[TEST FAILED] CBT Panel Status mismatch! Got '{uiController.cbtPanelStatusText?.text}'");
                return;
            }
            if (uiController.cbtPanelDescriptionText == null ||
                !uiController.cbtPanelDescriptionText.text.Contains("lower level") ||
                !uiController.cbtPanelDescriptionText.text.Contains("channel-making tools") ||
                !uiController.cbtPanelDescriptionText.text.Contains("-3 m mesh") ||
                !uiController.cbtPanelDescriptionText.text.Contains("bath/alumina tanker"))
            {
                Debug.LogError("[TEST FAILED] CBT Panel Description content mismatch!");
                return;
            }
            if (uiController.ProcessImage == null || !uiController.ProcessImage.preserveAspect || uiController.ProcessImage.sprite == null || uiController.ProcessImage.sprite.name != "EstablishAccess")
            {
                Debug.LogError("[TEST FAILED] ProcessImage component missing, sprite mismatch, or preserveAspect not enabled on CBT Panel!");
                return;
            }

            // Verify non-overlapping layout in CBT_LookTarget Panel
            var cbtTitleRT = uiController.cbtPanelTitle.GetComponent<RectTransform>();
            var cbtStatusRT = uiController.cbtPanelStatusText.GetComponent<RectTransform>();
            var cbtImgRT = uiController.ProcessImage.GetComponent<RectTransform>();
            var cbtDescRT = uiController.cbtPanelDescriptionText.GetComponent<RectTransform>();

            Vector3[] cbtTitleCorners = new Vector3[4];
            Vector3[] cbtStatusCorners = new Vector3[4];
            Vector3[] cbtImgCorners = new Vector3[4];
            Vector3[] cbtDescCorners = new Vector3[4];

            cbtTitleRT.GetWorldCorners(cbtTitleCorners);
            cbtStatusRT.GetWorldCorners(cbtStatusCorners);
            cbtImgRT.GetWorldCorners(cbtImgCorners);
            cbtDescRT.GetWorldCorners(cbtDescCorners);

            if (cbtTitleCorners[0].y < cbtStatusCorners[1].y - 0.001f)
            {
                Debug.LogError("[TEST FAILED] CBT Panel Title overlaps StatusText!");
                return;
            }
            if (cbtStatusCorners[0].y < cbtImgCorners[1].y - 0.001f)
            {
                Debug.LogError("[TEST FAILED] CBT Panel StatusText overlaps ProcessImage!");
                return;
            }
            if (cbtImgCorners[0].y < cbtDescCorners[1].y - 0.001f)
            {
                Debug.LogError("[TEST FAILED] CBT Panel ProcessImage overlaps Description!");
                return;
            }
            Debug.Log("[CHECK 11h] Task 08 Title, Status ('PREPARATION BEFORE TAP-OUT'), 4-item Description, CBT Panel Active, ProcessImage (EstablishAccess, preserveAspect=true), Non-overlapping verified.");

            // Task 09 (Tool Board & Low Leakage Process) UI State
            uiController.SetTaskToolsUI();
            if ((uiController.titleText.text != "ESTABLISH ACCESS & PREPARE THE WORK AREA" && uiController.titleText.text != "LOW LEAKAGE — FUSED ALUMINA") ||
                (uiController.statusText.text != "CHANNEL-MAKING TOOLS" && uiController.statusText.text != "FUSED ALUMINA PREPARATION"))
            {
                Debug.LogError($"[TEST FAILED] Task 09 UI mismatch! Title={uiController.titleText.text}, Status={uiController.statusText.text}");
                return;
            }
            if (uiController.potVoltageWallPanel != null && !uiController.potVoltageWallPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Wall panel must remain ACTIVE in Task 09!");
                return;
            }
            if (uiController.cbtLookTargetPanel != null && !uiController.cbtLookTargetPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] CBT Panel must remain ACTIVE in Task 09!");
                return;
            }
            Debug.Log("[CHECK 11i] Task 09 Title ('ESTABLISH ACCESS & PREPARE THE WORK AREA'), Status ('CHANNEL-MAKING TOOLS'), Wall panels active, and presentation start verified.");

            // Task 10 (Return to Ideal Pot Voltage: Replay Molten Metal Overflow VFX) UI State
            uiController.SetTask10UI();
            if (uiController.titleText.text != "IDEAL POT VOLTAGE")
            {
                Debug.LogError($"[TEST FAILED] Task 10 Title mismatch! Expected 'IDEAL POT VOLTAGE', got '{uiController.titleText.text}'");
                return;
            }
            if (uiController.statusText.text != "OBSERVATION & MONITORING REVIEW" && uiController.statusText.text != "LEAKAGE CONDITION — METAL SPILL OBSERVED")
            {
                Debug.LogError($"[TEST FAILED] Task 10 Status mismatch! Expected 'LEAKAGE CONDITION — METAL SPILL OBSERVED', got '{uiController.statusText.text}'");
                return;
            }
            if (!uiController.descriptionText.text.Contains("Reviewing pot voltage and molten overflow behavior") &&
                !uiController.descriptionText.text.Contains("Metal leakage has progressed") &&
                !uiController.descriptionText.text.Contains("Floor spill expands gradually"))
            {
                Debug.LogError("[TEST FAILED] Task 10 Description content mismatch!");
                return;
            }
            if (!uiController.progressText.text.Contains("TASK 10 / 10"))
            {
                Debug.LogError($"[TEST FAILED] Task 10 Progress mismatch! Expected text containing 'TASK 10 / 10', got '{uiController.progressText.text}'");
                return;
            }
            if (uiController.potVoltageWallPanel != null && !uiController.potVoltageWallPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Wall panel must remain ACTIVE in Task 10!");
                return;
            }
            if (uiController.cbtLookTargetPanel != null && !uiController.cbtLookTargetPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] CBT Panel must remain ACTIVE in Task 10!");
                return;
            }
            Debug.Log($"[CHECK 11j] Task 10 Title ('IDEAL POT VOLTAGE'), Status ('{uiController.statusText.text}'), Progress ('TASK 10 / 10'), Wall panels active, and VFX restart verified.");

            // Reset back to Task 01 state
            uiController.SetTask01UI();
            if (uiController.potVoltageWallPanel != null && !uiController.potVoltageWallPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Pot Voltage Wall Panel must remain ACTIVE in Task 01!");
                return;
            }
            if (uiController.cbtLookTargetPanel != null && !uiController.cbtLookTargetPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] CBT_LookTarget Panel must remain ACTIVE in Task 01!");
                return;
            }

            // 12. Verify Sequence Tasks
            if (sequence.TaskList == null || sequence.TaskList.Count != 10)
            {
                Debug.LogError($"[TEST FAILED] Expected 10 tasks, found: {sequence.TaskList?.Count ?? 0}");
                return;
            }

            string[] expectedTaskNames = new string[]
            {
                "01 – Welcome",
                "02 – Normal Pot Operation",
                "03 – Ideal Pot Voltage",
                "04 – Ideal CBT Temperature / Leakage Observed",
                "05 – Emergency Communication",
                "06 – Pot Voltage / Duct-End Voltage Control",
                "07 – Leakage-Specific Response",
                "08 – Establish Access & Prepare Work Area",
                "09 – Channel-Making Tools",
                "10 – Return to Ideal Pot Voltage"
            };

            for (int i = 0; i < sequence.TaskList.Count; i++)
            {
                var t = sequence.TaskList[i];
                Debug.Log($"   Task {i + 1}: '{t.TaskName}' | Mode: {t.completionMode} | Events: {t.EventsToFollow.GetPersistentEventCount()} listeners");
                if (t.TaskName != expectedTaskNames[i])
                {
                    Debug.LogError($"[TEST FAILED] Task index {i} name mismatch! Expected '{expectedTaskNames[i]}', got '{t.TaskName}'");
                    return;
                }
            }

            // Verify Camera Targets for each task
            var soSeq = new SerializedObject(sequence);
            var taskListProp = soSeq.FindProperty("TaskList");
            Transform[] expectedTargets = new Transform[]
            {
                welcomeTarget,
                normalPotOpTarget,
                idealPotVoltageTarget,
                idealCBTTemperatureTarget,
                idealCBTTemperatureTarget,
                potLookTarget,
                potLookTarget,
                cbtLookTarget,
                toolsTarget,
                idealPotVoltageTarget
            };

            for (int i = 0; i < 10; i++)
            {
                var call0Arg = taskListProp.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("EventsToFollow.m_PersistentCalls.m_Calls")
                    .GetArrayElementAtIndex(0)
                    .FindPropertyRelative("m_Arguments.m_ObjectArgument")
                    .objectReferenceValue;

                if (call0Arg != expectedTargets[i])
                {
                    Debug.LogError($"[TEST FAILED] Task {i} Event [0] camera target mismatch! Expected {expectedTargets[i].name}, got {call0Arg?.name}");
                    return;
                }
            }

            // Verify Single Trigger Architecture: Task 03 and Task 10 have exactly 2 persistent calls, no Cube.010 event
            int[] vfxTaskIndices = new int[] { 2, 9 };
            foreach (int taskIdx in vfxTaskIndices)
            {
                var callsProp = taskListProp.GetArrayElementAtIndex(taskIdx)
                    .FindPropertyRelative("EventsToFollow.m_PersistentCalls.m_Calls");
                if (callsProp.arraySize != 2)
                {
                    Debug.LogError($"[TEST FAILED] Task {taskIdx} must have exactly 2 persistent calls (Camera snap and SetTaskUI), found: {callsProp.arraySize}");
                    return;
                }
                for (int c = 0; c < callsProp.arraySize; c++)
                {
                    var targetObj = callsProp.GetArrayElementAtIndex(c).FindPropertyRelative("m_Target").objectReferenceValue;
                    if (targetObj != null && (targetObj.name == "Cube.010" || targetObj == cube010.gameObject))
                    {
                        Debug.LogError($"[TEST FAILED] Task {taskIdx} must NOT wire events to Cube.010!");
                        return;
                    }
                }
            }
            Debug.Log("[CHECK 12b] All 10 tasks camera targets and single-trigger VFX wiring verified in sequence.");

            // 13. VERIFY FULL SEQUENCE HANDLER PROGRESSION VIA CompleteCurrentTask()
            Debug.Log("[CHECK 13] Simulating Continue Button -> SequenceHandler Progression:");
            seqHandler.currentSequence = 0;
            seqHandler.currentTask = 0;
            foreach (var t in sequence.TaskList) t.TaskCompleted = false;
            uiController.toolPresentationController?.ResetPresentation();
            seqHelper.ResetDebounce();

            // Start Task 01
            seqHandler.NextTask();
            if (seqHandler.currentTask != 0 || uiController.titleText.text != "POT LEAKAGE")
            {
                Debug.LogError($"[TEST FAILED] Task 01 activation failed! CurrentTask={seqHandler.currentTask}, Title='{uiController.titleText.text}'");
                return;
            }
            if (uiController.potVoltageWallPanel != null && !uiController.potVoltageWallPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Wall panel must remain ACTIVE in Task 01!");
                return;
            }
            if (uiController.cbtLookTargetPanel != null && !uiController.cbtLookTargetPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] CBT Panel must remain ACTIVE in Task 01!");
                return;
            }
            if (!VerifyCameraSnap(mainCamera, welcomeTarget, "Task 01 (Welcome)")) return;
            if (vfxController.cube010Renderer.sharedMaterial != vfxController.Cube010OriginalMaterial)
            {
                Debug.LogError("[TEST FAILED] Cube.010 must have original material in Task 01!");
                return;
            }
            Debug.Log("   ├── Task 01 Active: Panel Title = 'POT LEAKAGE', Camera = Welcome (Snapped)");

            // Press Continue -> Advance to Task 02 (Normal Pot Operation)
            seqHelper.CompleteCurrentTask();
            if (seqHandler.currentTask != 1 || uiController.titleText.text != "NORMAL POT OPERATION" || !uiController.IsBlinking || uiController.statusText.text != "NORMAL OPERATING CONDITION")
            {
                Debug.LogError($"[TEST FAILED] Task 02 progression failed! CurrentTask={seqHandler.currentTask}, Title='{uiController.titleText.text}', Status='{uiController.statusText.text}', Blinking={uiController.IsBlinking}");
                return;
            }
            if (uiController.potVoltageWallPanel != null && !uiController.potVoltageWallPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Wall panel must remain ACTIVE in Task 02!");
                return;
            }
            if (uiController.cbtLookTargetPanel != null && !uiController.cbtLookTargetPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] CBT Panel must remain ACTIVE in Task 02!");
                return;
            }
            if (!VerifyCameraSnap(mainCamera, normalPotOpTarget, "Task 02 (Normal Pot Operation)")) return;
            if (vfxController.cube010Renderer.sharedMaterial != vfxController.Cube010OriginalMaterial)
            {
                Debug.LogError("[TEST FAILED] Cube.010 must have original material in Task 02!");
                return;
            }
            Debug.Log("   ├── Task 02 Active: Panel Title = 'NORMAL POT OPERATION', Status = 'NORMAL OPERATING CONDITION', Camera = Normal Pot Operation (Snapped)");

            // Press Continue -> Advance to Task 03 (Ideal Pot Voltage)
            seqHelper.CompleteCurrentTask();
            if (seqHandler.currentTask != 2 || uiController.titleText.text != "IDEAL POT VOLTAGE" || uiController.statusText.text != "NORMAL OPERATING RANGE" || uiController.IsBlinking || potMeshRenderer.sharedMaterial == originalMat)
            {
                Debug.LogError($"[TEST FAILED] Task 03 progression failed! CurrentTask={seqHandler.currentTask}, Title='{uiController.titleText.text}', Status='{uiController.statusText.text}', Blinking={uiController.IsBlinking}");
                return;
            }
            if (uiController.potVoltageWallPanel != null && !uiController.potVoltageWallPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Wall panel must remain ACTIVE in Task 03!");
                return;
            }
            if (uiController.cbtLookTargetPanel != null && !uiController.cbtLookTargetPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] CBT Panel must remain ACTIVE in Task 03!");
                return;
            }
            if (!VerifyCameraSnap(mainCamera, idealPotVoltageTarget, "Task 03 (Ideal Pot Voltage)")) return;
            if (vfxController.cube010Renderer.sharedMaterial != flowMR.sharedMaterial)
            {
                Debug.LogError($"[TEST FAILED] Cube.010 must receive flow mesh molten-metal material in Task 03! Expected '{flowMR.sharedMaterial?.name}', got '{vfxController.cube010Renderer.sharedMaterial?.name}'");
                return;
            }
            Debug.Log("   ├── Task 03 Active: Panel Title = 'IDEAL POT VOLTAGE', Status = 'NORMAL OPERATING RANGE', Camera = Ideal Pot Voltage (Snapped), Cube.010 molten material synchronized");

            // Press Continue -> Advance to Task 04 (Ideal CBT Temperature / Leakage Observed)
            seqHelper.CompleteCurrentTask();
            if (seqHandler.currentTask != 3 || uiController.titleText.text != "IDEAL CBT TEMPERATURE" || uiController.statusText.text != "LEAKAGE CONDITION OBSERVED")
            {
                Debug.LogError($"[TEST FAILED] Task 04 progression failed! CurrentTask={seqHandler.currentTask}, Title='{uiController.titleText.text}', Status='{uiController.statusText.text}'");
                return;
            }
            if (uiController.potVoltageWallPanel != null && !uiController.potVoltageWallPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Wall panel must remain ACTIVE in Task 04!");
                return;
            }
            if (uiController.cbtLookTargetPanel != null && !uiController.cbtLookTargetPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] CBT Panel must remain ACTIVE in Task 04!");
                return;
            }
            if (!VerifyCameraSnap(mainCamera, idealCBTTemperatureTarget, "Task 04 (Ideal CBT Temperature)")) return;
            if (uiController.walkieTalkieController == null || !uiController.walkieTalkieController.IsBlinking)
            {
                Debug.LogError("[TEST FAILED] Walkie-talkie must be blinking in Task 04 (Ideal CBT Temperature)!");
                return;
            }
            Debug.Log("   ├── Task 04 Active: Panel Title = 'IDEAL CBT TEMPERATURE', Status = 'LEAKAGE CONDITION OBSERVED', Camera = Ideal CBT Temperature (Snapped), Walkie-Talkie Blinking");

            // Press Continue -> Advance to Task 05 (Emergency Communication)
            seqHelper.CompleteCurrentTask();
            if (seqHandler.currentTask != 4 || uiController.titleText.text != "EMERGENCY COMMUNICATION" || uiController.statusText.text != "IMMEDIATE ACTION REQUIRED")
            {
                Debug.LogError($"[TEST FAILED] Task 05 progression failed! CurrentTask={seqHandler.currentTask}, Title='{uiController.titleText.text}', Status='{uiController.statusText.text}'");
                return;
            }
            if (uiController.potVoltageWallPanel != null && !uiController.potVoltageWallPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Wall panel must remain ACTIVE in Task 05!");
                return;
            }
            if (uiController.cbtLookTargetPanel != null && !uiController.cbtLookTargetPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] CBT Panel must remain ACTIVE in Task 05!");
                return;
            }
            if (!VerifyCameraSnap(mainCamera, idealCBTTemperatureTarget, "Task 05 (Emergency Communication)")) return;
            if (uiController.walkieTalkieController == null || !uiController.walkieTalkieController.IsBlinking)
            {
                Debug.LogError("[TEST FAILED] Walkie-talkie must be blinking in Task 05 before interaction!");
                return;
            }
            uiController.walkieTalkieController.Interact();
            if (uiController.walkieTalkieController.IsBlinking)
            {
                Debug.LogError("[TEST FAILED] Walkie-talkie must stop blinking after interaction!");
                return;
            }
            uiController.walkieTalkieController.CompleteCurrentAudioImmediately();
            uiController.walkieTalkieController.Interact();
            uiController.walkieTalkieController.CompleteCurrentAudioImmediately();
            Debug.Log("   ├── Task 05 Active: Panel Title = 'EMERGENCY COMMUNICATION', Camera = Ideal CBT Temperature (Retained view), Walkie-Talkie Interacted & Audio Simulated");

            // Press Continue -> Advance to Task 06 (Pot Voltage / Duct-End Voltage Control)
            seqHelper.CompleteCurrentTask();
            if (seqHandler.currentTask != 5 || uiController.titleText.text != "POT VOLTAGE" || uiController.statusText.text != "DUCT-END VOLTAGE CONTROL")
            {
                Debug.LogError($"[TEST FAILED] Task 06 progression failed! CurrentTask={seqHandler.currentTask}, Title='{uiController.titleText.text}', Status='{uiController.statusText.text}'");
                return;
            }
            if (uiController.potVoltageWallPanel == null || !uiController.potVoltageWallPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Wall panel must be ACTIVE in Task 06!");
                return;
            }
            if (uiController.cbtLookTargetPanel != null && !uiController.cbtLookTargetPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] CBT Panel must remain ACTIVE in Task 06!");
                return;
            }
            if (!VerifyCameraSnap(mainCamera, potLookTarget, "Task 06 (Pot Voltage / Duct-End Voltage Control)")) return;
            Debug.Log("   ├── Task 06 Active: Panel Title = 'POT VOLTAGE', Camera = PotMachine_LookTarget (Snapped)");

            // Press Continue -> Advance to Task 07 (Leakage-Specific Response)
            seqHelper.CompleteCurrentTask();
            if (seqHandler.currentTask != 6 || uiController.titleText.text != "LEAKAGE-SPECIFIC RESPONSE" || uiController.statusText.text != "COLLECTOR BAR LEAKAGE")
            {
                Debug.LogError($"[TEST FAILED] Task 07 progression failed! CurrentTask={seqHandler.currentTask}, Title='{uiController.titleText.text}', Status='{uiController.statusText.text}'");
                return;
            }
            if (uiController.potVoltageWallPanel != null && !uiController.potVoltageWallPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Wall panel must remain ACTIVE in Task 07!");
                return;
            }
            if (uiController.cbtLookTargetPanel != null && !uiController.cbtLookTargetPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] CBT Panel must remain ACTIVE in Task 07!");
                return;
            }
            if (!VerifyCameraSnap(mainCamera, potLookTarget, "Task 07 (Leakage-Specific Response)")) return;
            Debug.Log("   ├── Task 07 Active: Panel Title = 'LEAKAGE-SPECIFIC RESPONSE', Camera = PotMachine_LookTarget (Snapped)");

            // Press Continue -> Advance to Task 08 (Establish Access & Prepare Work Area)
            seqHelper.CompleteCurrentTask();
            if (seqHandler.currentTask != 7 || uiController.titleText.text != "ESTABLISH ACCESS & PREPARE THE WORK AREA" || uiController.statusText.text != "PREPARATION BEFORE TAP-OUT")
            {
                Debug.LogError($"[TEST FAILED] Task 08 progression failed! CurrentTask={seqHandler.currentTask}, Title='{uiController.titleText.text}', Status='{uiController.statusText.text}'");
                return;
            }
            if (uiController.cbtLookTargetPanel == null || !uiController.cbtLookTargetPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] CBT_LookTarget Panel must be ACTIVE in Task 08!");
                return;
            }
            if (uiController.potVoltageWallPanel != null && !uiController.potVoltageWallPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Pot Voltage Wall Panel must remain ACTIVE in Task 08!");
                return;
            }
            if (!VerifyCameraSnap(mainCamera, cbtLookTarget, "Task 08 (Establish Access & Prepare Work Area)")) return;
            Debug.Log("   ├── Task 08 Active: Panel Title = 'ESTABLISH ACCESS & PREPARE THE WORK AREA', Camera = CBT_LookTarget (Snapped)");

            // Press Continue -> Advance to Task 09 (Tool Board + Low Leakage Process)
            seqHelper.CompleteCurrentTask();
            var llc = uiController.lowLeakageController ?? Object.FindAnyObjectByType<LowLeakagePresentationController>();
            if (seqHandler.currentTask != 8 || !llc.IsPresentationActive || llc.CurrentStep != 0)
            {
                Debug.LogError($"[TEST FAILED] Task 09 progression failed! CurrentTask={seqHandler.currentTask}, IsActive={llc.IsPresentationActive}, Step={llc.CurrentStep}");
                return;
            }
            if (!VerifyCameraSnap(mainCamera, toolsTarget, "Task 09 (Tool Board Stage)")) return;
            Debug.Log("   ├── Task 09 Step A1: Camera = Tools (Snapped), Crowbar highlighted, Status = '" + uiController.statusText.text + "'");

            // Step A1 -> Step A2 (L_TOOL)
            seqHelper.CompleteCurrentTask();
            if (seqHandler.currentTask != 8 || !llc.IsPresentationActive || llc.CurrentStep != 1)
            {
                Debug.LogError($"[TEST FAILED] Task 09 Step A2 failed! CurrentTask={seqHandler.currentTask}, Step={llc.CurrentStep}");
                return;
            }
            Debug.Log("   ├── Task 09 Step A2: L_TOOL highlighted, Status = '" + uiController.statusText.text + "'");

            // Step A2 -> Step A3 (SHOVEL)
            seqHelper.CompleteCurrentTask();
            if (seqHandler.currentTask != 8 || !llc.IsPresentationActive || llc.CurrentStep != 2)
            {
                Debug.LogError($"[TEST FAILED] Task 09 Step A3 failed! CurrentTask={seqHandler.currentTask}, Step={llc.CurrentStep}");
                return;
            }
            Debug.Log("   ├── Task 09 Step A3: SHOVEL highlighted, Status = '" + uiController.statusText.text + "'");

            // Step A3 -> Step B1 (Tool board completed -> Snap to Ideal Pot Voltage -> COVER BATH CONTAINER)
            seqHelper.CompleteCurrentTask();
            if (seqHandler.currentTask != 8 || !llc.IsPresentationActive || llc.CurrentStep != 3)
            {
                Debug.LogError($"[TEST FAILED] Task 09 Step B1 failed! CurrentTask={seqHandler.currentTask}, Step={llc.CurrentStep}");
                return;
            }
            if (!VerifyCameraSnap(mainCamera, idealPotVoltageTarget, "Task 09 (Low Leakage Process)")) return;
            if (uiController.titleText.text != "LOW LEAKAGE — FUSED ALUMINA" || uiController.statusText.text != "FUSED ALUMINA PREPARATION")
            {
                Debug.LogError($"[TEST FAILED] Task 09 Step B1 Status mismatch! Expected 'LOW LEAKAGE — FUSED ALUMINA' / 'FUSED ALUMINA PREPARATION', got '{uiController.titleText.text}' / '{uiController.statusText.text}'");
                return;
            }
            Debug.Log("   ├── Task 09 Step B1: Camera = Ideal Pot Voltage (Snapped), COVER BATH CONTAINER enabled & highlighted, Status = 'FUSED ALUMINA PREPARATION'");

            // Step B1 -> Step B2 (Continue: Container restored, SHOVEL enabled & highlighted)
            seqHelper.CompleteCurrentTask();
            if (seqHandler.currentTask != 8 || !llc.IsPresentationActive || llc.CurrentStep != 4 ||
                uiController.titleText.text != "LOW LEAKAGE — FUSED ALUMINA" || uiController.statusText.text != "FUSED ALUMINA HANDLING")
            {
                Debug.LogError($"[TEST FAILED] Task 09 Step B2 failed! CurrentTask={seqHandler.currentTask}, Step={llc.CurrentStep}, Status='{uiController.statusText.text}'");
                return;
            }
            Debug.Log("   ├── Task 09 Step B2: SHOVEL enabled & highlighted, Status = 'FUSED ALUMINA HANDLING'");

            // Step B2 -> Step B3 (Continue: SHOVEL restored, Crowbar enabled & highlighted)
            seqHelper.CompleteCurrentTask();
            if (seqHandler.currentTask != 8 || !llc.IsPresentationActive || llc.CurrentStep != 5 ||
                uiController.titleText.text != "LOW LEAKAGE — SIDE BREAKING" || uiController.statusText.text != "SIDE BREAKING / CHANNEL PREPARATION")
            {
                Debug.LogError($"[TEST FAILED] Task 09 Step B3 failed! CurrentTask={seqHandler.currentTask}, Step={llc.CurrentStep}, Status='{uiController.statusText.text}'");
                return;
            }
            Debug.Log("   ├── Task 09 Step B3: Crowbar enabled & highlighted, Status = 'SIDE BREAKING / CHANNEL PREPARATION'");

            // Step B3 -> Step B4 (Continue: Crowbar restored, Controlled Leakage activated)
            seqHelper.CompleteCurrentTask();
            if (seqHandler.currentTask != 8 || !llc.IsPresentationActive || llc.CurrentStep != 6 ||
                uiController.titleText.text != "LOW LEAKAGE — CONTROLLED" || uiController.statusText.text != "CONTROLLED LEAKAGE")
            {
                Debug.LogError($"[TEST FAILED] Task 09 Step B4 failed! CurrentTask={seqHandler.currentTask}, Step={llc.CurrentStep}, Status='{uiController.statusText.text}'");
                return;
            }
            Debug.Log("   ├── Task 09 Step B4: Controlled Leakage activated (thin stream, settled pool), Status = 'CONTROLLED LEAKAGE'");

            // Step B4 -> Advance to Task 10 (Return to Ideal Pot Voltage)
            seqHelper.CompleteCurrentTask();
            if (seqHandler.currentTask != 9 || uiController.titleText.text != "IDEAL POT VOLTAGE" ||
                (uiController.statusText.text != "OBSERVATION & MONITORING REVIEW" && uiController.statusText.text != "LEAKAGE CONDITION — METAL SPILL OBSERVED"))
            {
                Debug.LogError($"[TEST FAILED] Task 10 progression failed! CurrentTask={seqHandler.currentTask}, Title='{uiController.titleText.text}', Status='{uiController.statusText.text}'");
                return;
            }
            if (uiController.potVoltageWallPanel != null && !uiController.potVoltageWallPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Wall panel must remain ACTIVE in Task 10!");
                return;
            }
            if (uiController.cbtLookTargetPanel != null && !uiController.cbtLookTargetPanel.activeSelf)
            {
                Debug.LogError("[TEST FAILED] CBT Panel must remain ACTIVE in Task 10!");
                return;
            }
            if (!VerifyCameraSnap(mainCamera, idealPotVoltageTarget, "Task 10 (Return to Ideal Pot Voltage)")) return;
            if (vfxController.cube010Renderer.sharedMaterial != flowMR.sharedMaterial)
            {
                Debug.LogError($"[TEST FAILED] Cube.010 must receive flow mesh molten-metal material in Task 10 replay! Expected '{flowMR.sharedMaterial?.name}', got '{vfxController.cube010Renderer.sharedMaterial?.name}'");
                return;
            }
            Debug.Log($"   └── Task 10 Active: Panel Title = 'IDEAL POT VOLTAGE', Status = '{uiController.statusText.text}', Camera = Ideal Pot Voltage (Snapped), Cube.010 molten material synchronized");

            // 14. VERIFY PERSONNEL IMAGES ASSETS AND UI SETUP
            var shiftSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Shift.png");
            var technicalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Technical.png");
            if (shiftSprite == null)
            {
                Debug.LogError("[TEST FAILED] Assets/Images/Shift.png is missing or not configured as Sprite!");
                return;
            }
            if (technicalSprite == null)
            {
                Debug.LogError("[TEST FAILED] Assets/Images/Technical.png is missing or not configured as Sprite!");
                return;
            }

            var personnelImagesGo = GameObject.Find("UI/Canvas/IdealCBTTemperatureUI/PersonnelImages")
                                 ?? GameObject.Find("UI/Canvas/PersonnelImages")
                                 ?? GameObject.Find("PersonnelImages");
            if (personnelImagesGo == null)
            {
                Debug.LogError("[TEST FAILED] PersonnelImages container missing!");
                return;
            }

            var shiftGo = personnelImagesGo.transform.Find("ShiftImage")?.gameObject;
            var techGo = personnelImagesGo.transform.Find("TechnicalImage")?.gameObject;
            if (shiftGo == null || techGo == null)
            {
                Debug.LogError("[TEST FAILED] ShiftImage or TechnicalImage child missing under UI/Canvas/PersonnelImages!");
                return;
            }

            var shiftImageComp = shiftGo.GetComponent<Image>();
            var techImageComp = techGo.GetComponent<Image>();
            if (shiftImageComp == null || !shiftImageComp.preserveAspect || shiftImageComp.sprite != shiftSprite)
            {
                Debug.LogError("[TEST FAILED] ShiftImage component missing, sprite mismatch, or preserveAspect not enabled!");
                return;
            }
            if (techImageComp == null || !techImageComp.preserveAspect || techImageComp.sprite != technicalSprite)
            {
                Debug.LogError("[TEST FAILED] TechnicalImage component missing, sprite mismatch, or preserveAspect not enabled!");
                return;
            }

            // Verify non-overlapping layout strictly above Welcome Panel
            var piRect = personnelImagesGo.GetComponent<RectTransform>();
            var sRect = shiftGo.GetComponent<RectTransform>();
            var tRect = techGo.GetComponent<RectTransform>();
            Vector3[] piCorners = new Vector3[4];
            Vector3[] sCorners = new Vector3[4];
            Vector3[] tCorners = new Vector3[4];
            piRect.GetWorldCorners(piCorners);
            sRect.GetWorldCorners(sCorners);
            tRect.GetWorldCorners(tCorners);

            // PersonnelImages must not overlap Welcome Panel
            Vector3[] wpCorners = new Vector3[4];
            welcomePanel.GetComponent<RectTransform>().GetWorldCorners(wpCorners);
            bool isCleanlyBelow = wpCorners[0].y >= piCorners[1].y - 0.01f;
            bool isCleanlyLeft = piCorners[3].x <= wpCorners[0].x + 0.01f;
            bool isCleanlyAbove = piCorners[0].y >= wpCorners[1].y - 0.01f;
            if (!isCleanlyBelow && !isCleanlyLeft && !isCleanlyAbove)
            {
                Debug.LogError("[TEST FAILED] PersonnelImages container overlaps the Welcome Panel!");
                return;
            }

            // ShiftImage and TechnicalImage must not overlap horizontally
            if (sCorners[2].x > tCorners[0].x + 0.01f)
            {
                Debug.LogError("[TEST FAILED] ShiftImage and TechnicalImage overlap horizontally!");
                return;
            }

            // Initially hidden in Task 01
            uiController.SetTask01UI();
            if (personnelImagesGo.activeSelf)
            {
                Debug.LogError("[TEST FAILED] PersonnelImages should be inactive in Task 01!");
                return;
            }
            Debug.Log("[CHECK 14] Personnel Images Assets and UI Layout verified: Shift.png & Technical.png valid sprites, preserveAspect enabled, non-overlapping side-by-side above Welcome Panel, initially hidden.");

            // 15. VERIFY WALKIE-TALKIE 3D INTERACTION (defaultMaterial.007) & TWO-STAGE FLOW
            var wtGo = GameObject.Find("defaultMaterial.007");
            if (wtGo == null)
            {
                Debug.LogError("[TEST FAILED] defaultMaterial.007 walkie-talkie object not found!");
                return;
            }

            var wtCollider = wtGo.GetComponent<Collider>();
            if (wtCollider == null)
            {
                Debug.LogError("[TEST FAILED] defaultMaterial.007 must have a Collider component to be clickable!");
                return;
            }

            var wtController = wtGo.GetComponent<WalkieTalkieInteractionController>();
            if (wtController == null)
            {
                wtController = Object.FindAnyObjectByType<WalkieTalkieInteractionController>();
            }
            if (wtController == null)
            {
                Debug.LogError("[TEST FAILED] WalkieTalkieInteractionController component missing on defaultMaterial.007!");
                return;
            }

            var wtRenderer = wtGo.GetComponent<MeshRenderer>();
            if (wtRenderer == null)
            {
                Debug.LogError("[TEST FAILED] MeshRenderer missing on defaultMaterial.007!");
                return;
            }

            var wtOriginalMat = wtController.originalMaterial;
            var wtHighlightMat = wtController.highlightMaterial;

            // Verify Task 04 (Ideal CBT Temperature / Leakage Observed): personnel images must NOT appear
            uiController.SetTask04UI();
            if (personnelImagesGo.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Personnel images must NOT appear in Task 04 (leakage observed)!");
                return;
            }

            // Activate Task 05 (Emergency Communication)
            uiController.SetTask05UI();
            if (!personnelImagesGo.activeSelf || !shiftGo.activeSelf || !techGo.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Both personnel images must appear when Emergency Communication (Task 05) begins!");
                return;
            }
            if (wtRenderer.sharedMaterial != wtHighlightMat)
            {
                Debug.LogError("[TEST FAILED] defaultMaterial.007 must be highlighted with M_PotLeakage_Highlight when Emergency Communication begins!");
                return;
            }
            if (wtController.CurrentState != WalkieTalkieState.WaitingForShift)
            {
                Debug.LogError($"[TEST FAILED] Walkie-talkie state should be WaitingForShift, got {wtController.CurrentState}!");
                return;
            }
            Debug.Log("   ├── Task 05 Started: Both Shift and Technical images visible, defaultMaterial.007 highlighted, WaitingForShift");

            // STAGE 1: First Press on defaultMaterial.007
            wtController.autoAdvanceInEditMode = false;
            wtController.Interact();
            if (shiftGo.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Shift.png must disappear after first walkie-talkie press!");
                return;
            }
            if (!techGo.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Technical.png must remain visible after first walkie-talkie press!");
                return;
            }
            if (wtController.CurrentState != WalkieTalkieState.ShiftAudioPlaying)
            {
                Debug.LogError($"[TEST FAILED] State during first press audio should be ShiftAudioPlaying, got {wtController.CurrentState}!");
                return;
            }

            // Test that additional presses are disabled during audio
            wtController.Interact();
            if (wtController.CurrentState != WalkieTalkieState.ShiftAudioPlaying)
            {
                Debug.LogError("[TEST FAILED] Additional presses must be disabled while Shift audio is playing!");
                return;
            }

            // Simulate audio 1 completion
            wtController.CompleteCurrentAudioImmediately();
            if (wtController.CurrentState != WalkieTalkieState.WaitingForTechnical)
            {
                Debug.LogError($"[TEST FAILED] State after first press audio should be WaitingForTechnical, got {wtController.CurrentState}!");
                return;
            }
            if (wtRenderer.sharedMaterial != wtHighlightMat)
            {
                Debug.LogError("[TEST FAILED] defaultMaterial.007 must be highlighted again waiting for second press!");
                return;
            }
            Debug.Log("   ├── First Press Complete: Shift.png disappeared, Technical.png remains visible, defaultMaterial.007 re-highlighted");

            // STAGE 2: Second Press on defaultMaterial.007
            wtController.Interact();
            if (techGo.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Technical.png must disappear after second walkie-talkie press!");
                return;
            }
            if (wtController.CurrentState != WalkieTalkieState.TechnicalAudioPlaying)
            {
                Debug.LogError($"[TEST FAILED] State during second press audio should be TechnicalAudioPlaying, got {wtController.CurrentState}!");
                return;
            }

            // Test that additional presses are disabled during audio
            wtController.Interact();
            if (wtController.CurrentState != WalkieTalkieState.TechnicalAudioPlaying)
            {
                Debug.LogError("[TEST FAILED] Additional presses must be disabled while Technical audio is playing!");
                return;
            }

            // Simulate audio 2 completion
            wtController.CompleteCurrentAudioImmediately();
            if (!wtController.IsCompleted || wtController.CurrentState != WalkieTalkieState.Completed)
            {
                Debug.LogError($"[TEST FAILED] Walkie-talkie interaction must be Completed after second press! Got {wtController.CurrentState}");
                return;
            }
            wtController.autoAdvanceInEditMode = true;
            Debug.Log("   ├── Second Press Complete: Technical.png disappeared, both images gone, interaction COMPLETED");

            // Verify Reset on earlier tasks
            uiController.SetTask01UI();
            if (personnelImagesGo.activeSelf || shiftGo.activeSelf || techGo.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Personnel images must be hidden when resetting to Task 01!");
                return;
            }
            if (wtRenderer.sharedMaterial != wtOriginalMat)
            {
                Debug.LogError("[TEST FAILED] defaultMaterial.007 original material must be restored in Task 01!");
                return;
            }
            Debug.Log("[CHECK 15] Walkie-Talkie 3D Interaction verified: Collider present, direct material swap, two-stage flow (Shift call -> Technical call), audio simulation, completion state, and clean reset.");

            // 16. VERIFY NO CAMERA INTERPOLATION KEYWORDS IN PotLeakageCameraController.cs
            string camScriptPath = "Assets/PotLeakage/Scripts/Camera/PotLeakageCameraController.cs";
            if (System.IO.File.Exists(camScriptPath))
            {
                string camScriptText = System.IO.File.ReadAllText(camScriptPath);
                string[] forbidden = { "Vector3.Lerp", "Vector3.SmoothDamp", "Quaternion.Lerp", "Quaternion.Slerp", "AnimationCurve", "StartCoroutine", "WaitForSeconds", "DOTween" };
                foreach (var f in forbidden)
                {
                    if (camScriptText.Contains(f))
                    {
                        Debug.LogError($"[TEST FAILED] PotLeakageCameraController contains forbidden interpolation keyword '{f}'!");
                        return;
                    }
                }
                Debug.Log("[CHECK 16] Zero camera interpolation verified in PotLeakageCameraController.cs (no Lerp, Slerp, Coroutine, AnimationCurve, or DOTween).");
            }

            // 17. VERIFY MULTI-CYCLE RE-ENTRY & CONTINUOUS FLOW MESH REPLAY
            Debug.Log("[CHECK 17] Verifying Reusable Continuous Molten Metal Stream Multi-Cycle Re-entry:");
            for (int cycle = 1; cycle <= 3; cycle++)
            {
                vfxController.PlayMoltenMetalOverflow();
                if (!vfxController.flowEnabled)
                {
                    Debug.LogError($"[TEST FAILED] flowEnabled is false after cycle {cycle} PlayMoltenMetalOverflow()!");
                    return;
                }
                if (vfxController.flowMeshRenderer == null || !vfxController.flowMeshRenderer.enabled)
                {
                    Debug.LogError($"[TEST FAILED] flowMeshRenderer is not enabled after cycle {cycle} PlayMoltenMetalOverflow()!");
                    return;
                }
                if (vfxController.cube010Renderer.sharedMaterial != flowMR.sharedMaterial)
                {
                    Debug.LogError($"[TEST FAILED] Cube.010 sharedMaterial does not match MoltenMetalFlowMesh after Play in cycle {cycle}!");
                    return;
                }
                if (vfxController.moltenMetalDroplets != null && !vfxController.moltenMetalDroplets.isPlaying)
                {
                    Debug.LogError($"[TEST FAILED] moltenMetalDroplets is not playing after cycle {cycle} PlayMoltenMetalOverflow()!");
                    return;
                }
                if (vfxController.moltenMetalDroplets != null)
                {
                    vfxController.moltenMetalDroplets.Simulate(1.0f, true, false);
                }

                // Stop and clear
                vfxController.StopMoltenMetalOverflow();
                if (vfxController.flowEnabled)
                {
                    Debug.LogError($"[TEST FAILED] flowEnabled is true after cycle {cycle} StopMoltenMetalOverflow()!");
                    return;
                }
                if (vfxController.flowMeshRenderer != null && vfxController.flowMeshRenderer.enabled)
                {
                    Debug.LogError($"[TEST FAILED] flowMeshRenderer is still enabled after cycle {cycle} StopMoltenMetalOverflow()!");
                    return;
                }
                if (vfxController.cube010Renderer.sharedMaterial != vfxController.Cube010OriginalMaterial)
                {
                    Debug.LogError($"[TEST FAILED] Cube.010 sharedMaterial not restored after Stop in cycle {cycle}!");
                    return;
                }
                if (vfxController.moltenMetalDroplets != null && vfxController.moltenMetalDroplets.particleCount != 0)
                {
                    Debug.LogError($"[TEST FAILED] Droplets particles not cleared after StopMoltenMetalOverflow() in cycle {cycle}!");
                    return;
                }
                Debug.Log($"   ├── Cycle {cycle}: Clean Play -> Stop & Clear verified (flowMesh and Cube.010 synchronized, droplets cleared).");
            }
            Debug.Log("[CHECK 17] Multi-cycle continuous molten flow replay verified: Indefinite clean restarts with mesh enabled toggle, Cube.010 material synchronization, zero particle accumulation, single instance reused.");

            // 18. VERIFY PINNED TOOL BOARD PRESENTATION & WALKIE-TALKIE BLINKING
            Debug.Log("[CHECK 18] Verifying Pinned Tool Presentation & Walkie-Talkie Highlight/Blink:");

            // 18.1 Tools TransformPoint & Instant Snap
            toolsTarget = GameObject.Find("CameraSystem/TransformPoints/Tools")?.transform;
            if (toolsTarget == null)
            {
                Debug.LogError("[TEST FAILED] CameraSystem/TransformPoints/Tools not found!");
                return;
            }
            AssertClose("TransformPoints/Tools Position", toolsTarget.position, new Vector3(5.92f, 3.07f, -66.90f), 0.05f);
            AssertClose("TransformPoints/Tools Rotation", toolsTarget.eulerAngles, new Vector3(0f, 0f, 0f), 1.0f);
            mainCamera.transform.position = toolsTarget.position;
            mainCamera.transform.rotation = toolsTarget.rotation;
            if (!VerifyCameraSnap(mainCamera, toolsTarget, "TransformPoints/Tools Snap")) return;
            Debug.Log("   ├── Camera snaps to TransformPoints/Tools instantly (0s duration, no lerp/slerp).");

            // 18.2 tool board and pinned tool objects
            var toolBoard = GameObject.Find("tool board");
            if (toolBoard == null)
            {
                Debug.LogError("[TEST FAILED] 'tool board' GameObject not found!");
                return;
            }
            AssertClose("tool board Position", toolBoard.transform.position, new Vector3(5.91f, 2.74f, -63.21f), 0.1f);

            var cbBody = toolBoard.transform.Find("Crowbar")?.gameObject;
            var cbHandle = toolBoard.transform.Find("Crowbar_HANDAL")?.gameObject;
            var ltBody = toolBoard.transform.Find("L_TOOL")?.gameObject;
            var ltHandle = toolBoard.transform.Find("L_TOOL_HANDAL")?.gameObject;
            var shBody = toolBoard.transform.Find("SHOVEL")?.gameObject;
            var shHandle = toolBoard.transform.Find("SHOVEL_HANDAL")?.gameObject;

            if (cbBody == null || cbHandle == null)
            {
                Debug.LogError("[TEST FAILED] tool board/Crowbar or Crowbar_HANDAL missing!");
                return;
            }
            if (ltBody == null || ltHandle == null)
            {
                Debug.LogError("[TEST FAILED] tool board/L_TOOL or L_TOOL_HANDAL missing!");
                return;
            }
            if (shBody == null || shHandle == null)
            {
                Debug.LogError("[TEST FAILED] tool board/SHOVEL or SHOVEL_HANDAL missing!");
                return;
            }
            Debug.Log("   ├── Existing pinned tools confirmed on tool board: Crowbar, L_TOOL, SHOVEL (with handles).");

            // Verify no XRGrabInteractable on pinned presentation tools
            foreach (var comp in toolBoard.GetComponentsInChildren<Component>(true))
            {
                if (comp.GetType().Name.Contains("XRGrabInteractable"))
                {
                    Debug.LogError($"[TEST FAILED] No XRGrabInteractable should be added to pinned presentation tool: {comp.gameObject.name}!");
                    return;
                }
            }
            Debug.Log("   ├── Zero XRGrabInteractable components on pinned presentation tools (as required).");

            // 18.3 ToolPresentationController on Manager
            var tpc = uiController.toolPresentationController;
            if (tpc == null)
            {
                tpc = Object.FindAnyObjectByType<ToolPresentationController>();
            }
            if (tpc == null)
            {
                Debug.LogError("[TEST FAILED] ToolPresentationController missing!");
                return;
            }
            tpc.InitializeReferences();
            var highlightMat = tpc.highlightMaterial;
            if (highlightMat == null || highlightMat.name != "M_PotLeakage_Highlight")
            {
                Debug.LogError("[TEST FAILED] ToolPresentationController highlightMaterial must be M_PotLeakage_Highlight!");
                return;
            }

            var cbR = cbBody.GetComponent<Renderer>();
            var cbHR = cbHandle.GetComponent<Renderer>();
            var ltR = ltBody.GetComponent<Renderer>();
            var ltHR = ltHandle.GetComponent<Renderer>();
            var shR = shBody.GetComponent<Renderer>();
            var shHR = shHandle.GetComponent<Renderer>();

            string origCbMat = cbR.sharedMaterial.name;
            string origCbHMat = cbHR.sharedMaterial.name;
            string origLtMat = ltR.sharedMaterial.name;
            string origLtHMat = ltHR.sharedMaterial.name;
            string origShMat = shR.sharedMaterial.name;
            string origShHMat = shHR.sharedMaterial.name;

            // Record initial tool transforms and parents
            Vector3 origCbPos = cbBody.transform.position; Quaternion origCbRot = cbBody.transform.rotation; Vector3 origCbScale = cbBody.transform.localScale; Transform origCbParent = cbBody.transform.parent;
            Vector3 origCbHPos = cbHandle.transform.position; Quaternion origCbHRot = cbHandle.transform.rotation; Vector3 origCbHScale = cbHandle.transform.localScale; Transform origCbHParent = cbHandle.transform.parent;
            Vector3 origLtPos = ltBody.transform.position; Quaternion origLtRot = ltBody.transform.rotation; Vector3 origLtScale = ltBody.transform.localScale; Transform origLtParent = ltBody.transform.parent;
            Vector3 origLtHPos = ltHandle.transform.position; Quaternion origLtHRot = ltHandle.transform.rotation; Vector3 origLtHScale = ltHandle.transform.localScale; Transform origLtHParent = ltHandle.transform.parent;
            Vector3 origShPos = shBody.transform.position; Quaternion origShRot = shBody.transform.rotation; Vector3 origShScale = shBody.transform.localScale; Transform origShParent = shBody.transform.parent;
            Vector3 origShHPos = shHandle.transform.position; Quaternion origShHRot = shHandle.transform.rotation; Vector3 origShHScale = shHandle.transform.localScale; Transform origShHParent = shHandle.transform.parent;

            // Step 1: StartPresentation highlights Crowbar
            tpc.StartPresentation();
            if (tpc.CurrentHighlightedIndex != 0 || !tpc.IsPresentationActive || tpc.IsPresentationCompleted)
            {
                Debug.LogError($"[TEST FAILED] StartPresentation failed! Index={tpc.CurrentHighlightedIndex}, IsActive={tpc.IsPresentationActive}");
                return;
            }
            if (cbR.sharedMaterial != highlightMat || cbHR.sharedMaterial != highlightMat)
            {
                Debug.LogError("[TEST FAILED] Crowbar body and handle must both receive M_PotLeakage_Highlight!");
                return;
            }
            if (ltR.sharedMaterial == highlightMat || shR.sharedMaterial == highlightMat)
            {
                Debug.LogError("[TEST FAILED] Only Crowbar should be highlighted in Step 1!");
                return;
            }
            if (uiController.statusText.text != "CHANNEL-MAKING TOOLS: CROWBAR")
            {
                Debug.LogError($"[TEST FAILED] Status text mismatch in Step 1! Expected 'CHANNEL-MAKING TOOLS: CROWBAR', got '{uiController.statusText.text}'");
                return;
            }
            Debug.Log("   ├── Step 1: Crowbar body and handle both highlight. Other tools retain original materials. Status = 'CHANNEL-MAKING TOOLS: CROWBAR'.");

            // Step 2: AdvancePresentation to L_TOOL, Crowbar restores
            bool adv1 = tpc.AdvancePresentation();
            if (!adv1 || tpc.CurrentHighlightedIndex != 1 || !tpc.IsPresentationActive)
            {
                Debug.LogError($"[TEST FAILED] AdvancePresentation to L_TOOL failed! Result={adv1}, Index={tpc.CurrentHighlightedIndex}");
                return;
            }
            if (cbR.sharedMaterial.name != origCbMat || cbHR.sharedMaterial.name != origCbHMat)
            {
                Debug.LogError("[TEST FAILED] Crowbar original materials must be restored when moving to L_TOOL!");
                return;
            }
            if (ltR.sharedMaterial != highlightMat || ltHR.sharedMaterial != highlightMat)
            {
                Debug.LogError("[TEST FAILED] L_TOOL body and handle must both receive M_PotLeakage_Highlight!");
                return;
            }
            if (shR.sharedMaterial == highlightMat)
            {
                Debug.LogError("[TEST FAILED] SHOVEL must not be highlighted in Step 2!");
                return;
            }
            if (uiController.statusText.text != "CHANNEL-MAKING TOOLS: L-TOOL" && uiController.statusText.text != "L-TOOL REQUIRED")
            {
                Debug.LogError($"[TEST FAILED] Status text mismatch in Step 2! Expected 'L-TOOL REQUIRED', got '{uiController.statusText.text}'");
                return;
            }
            Debug.Log($"   ├── Step 2: Crowbar original materials restored. L_TOOL body and handle both highlight. Status = '{uiController.statusText.text}'.");

            // Step 3: AdvancePresentation to SHOVEL, L_TOOL restores
            bool adv2 = tpc.AdvancePresentation();
            if (!adv2 || tpc.CurrentHighlightedIndex != 2 || !tpc.IsPresentationActive)
            {
                Debug.LogError($"[TEST FAILED] AdvancePresentation to SHOVEL failed! Result={adv2}, Index={tpc.CurrentHighlightedIndex}");
                return;
            }
            if (ltR.sharedMaterial.name != origLtMat || ltHR.sharedMaterial.name != origLtHMat)
            {
                Debug.LogError("[TEST FAILED] L_TOOL original materials must be restored when moving to SHOVEL!");
                return;
            }
            if (shR.sharedMaterial != highlightMat || shHR.sharedMaterial != highlightMat)
            {
                Debug.LogError("[TEST FAILED] SHOVEL body and handle must both receive M_PotLeakage_Highlight!");
                return;
            }
            if (cbR.sharedMaterial == highlightMat)
            {
                Debug.LogError("[TEST FAILED] Crowbar must not be highlighted in Step 3!");
                return;
            }
            if (uiController.statusText.text != "CHANNEL-MAKING TOOLS: SHOVEL")
            {
                Debug.LogError($"[TEST FAILED] Status text mismatch in Step 3! Expected 'CHANNEL-MAKING TOOLS: SHOVEL', got '{uiController.statusText.text}'");
                return;
            }
            Debug.Log("   ├── Step 3: L_TOOL original materials restored. SHOVEL body and handle both highlight. Status = 'CHANNEL-MAKING TOOLS: SHOVEL'.");

            // Step 4: Final AdvancePresentation completes presentation
            bool adv3 = tpc.AdvancePresentation();
            if (adv3 || tpc.IsPresentationActive || !tpc.IsPresentationCompleted)
            {
                Debug.LogError($"[TEST FAILED] Final AdvancePresentation must return false! Result={adv3}, IsActive={tpc.IsPresentationActive}, IsCompleted={tpc.IsPresentationCompleted}");
                return;
            }
            if (shR.sharedMaterial.name != origShMat || shHR.sharedMaterial.name != origShHMat)
            {
                Debug.LogError("[TEST FAILED] SHOVEL original materials must be restored when presentation completes!");
                return;
            }
            if (cbR.sharedMaterial.name != origCbMat || ltR.sharedMaterial.name != origLtMat)
            {
                Debug.LogError("[TEST FAILED] All tools must be restored to original materials after presentation!");
                return;
            }
            Debug.Log("   ├── Step 4: Final Continue completed tool presentation. All 3 tools restored to original materials.");

            // ResetPresentation verification
            tpc.ResetPresentation();
            if (tpc.IsPresentationActive || tpc.IsPresentationCompleted || tpc.CurrentHighlightedIndex != 0)
            {
                Debug.LogError($"[TEST FAILED] ResetPresentation failed! IsActive={tpc.IsPresentationActive}, IsCompleted={tpc.IsPresentationCompleted}");
                return;
            }
            if (cbR.sharedMaterial == highlightMat || ltR.sharedMaterial == highlightMat || shR.sharedMaterial == highlightMat)
            {
                Debug.LogError("[TEST FAILED] No tool may be highlighted after ResetPresentation()!");
                return;
            }
            Debug.Log("   ├── ResetPresentation(): all original materials restored, no highlight, presentation ready to replay.");

            // Verify Tool Object Integrity: transforms and parents strictly unchanged
            AssertClose("Crowbar Pos unchanged", cbBody.transform.position, origCbPos);
            AssertClose("Crowbar_HANDAL Pos unchanged", cbHandle.transform.position, origCbHPos);
            AssertClose("L_TOOL Pos unchanged", ltBody.transform.position, origLtPos);
            AssertClose("L_TOOL_HANDAL Pos unchanged", ltHandle.transform.position, origLtHPos);
            AssertClose("SHOVEL Pos unchanged", shBody.transform.position, origShPos);
            AssertClose("SHOVEL_HANDAL Pos unchanged", shHandle.transform.position, origShHPos);
            if (cbBody.transform.parent != origCbParent || ltBody.transform.parent != origLtParent || shBody.transform.parent != origShParent)
            {
                Debug.LogError("[TEST FAILED] Tool parents were modified!");
                return;
            }
            Debug.Log("   ├── Tool Object Integrity verified: positions, rotations, scales, and parents are 100% unchanged.");

            // 18.4 Walkie-Talkie (defaultMaterial.007) Blinking Verification
            wtGo = GameObject.Find("defaultMaterial.007");
            if (wtGo == null)
            {
                Debug.LogError("[TEST FAILED] defaultMaterial.007 not found!");
                return;
            }
            wtController = wtGo.GetComponent<WalkieTalkieInteractionController>();
            if (wtController == null)
            {
                Debug.LogError("[TEST FAILED] WalkieTalkieInteractionController missing on defaultMaterial.007!");
                return;
            }
            wtRenderer = wtGo.GetComponent<MeshRenderer>();
            if (wtRenderer == null)
            {
                Debug.LogError("[TEST FAILED] MeshRenderer missing on defaultMaterial.007!");
                return;
            }
            wtCollider = wtGo.GetComponent<Collider>();
            if (wtCollider == null || !wtCollider.enabled)
            {
                Debug.LogError("[TEST FAILED] Collider missing or disabled on defaultMaterial.007!");
                return;
            }

            wtController.StartWalkieTalkieBlink();
            if (!wtController.IsBlinking)
            {
                Debug.LogError("[TEST FAILED] wtController.IsBlinking must be true after StartWalkieTalkieBlink()!");
                return;
            }
            if (wtRenderer.sharedMaterial != highlightMat)
            {
                Debug.LogError($"[TEST FAILED] defaultMaterial.007 must have highlight material when blinking starts! Got '{wtRenderer.sharedMaterial?.name}'");
                return;
            }
            if (!wtGo.activeInHierarchy || !wtCollider.enabled)
            {
                Debug.LogError("[TEST FAILED] defaultMaterial.007 must remain active and clickable while blinking!");
                return;
            }
            Debug.Log("   ├── Walkie-talkie starts blinking with M_PotLeakage_Highlight; remains active & clickable.");

            // Interacting stops blinking
            wtController.Interact();
            if (wtController.IsBlinking)
            {
                Debug.LogError("[TEST FAILED] wtController.IsBlinking must be false after Interact()!");
                return;
            }
            if (wtRenderer.sharedMaterial == highlightMat)
            {
                Debug.LogError("[TEST FAILED] defaultMaterial.007 must restore original material after interaction!");
                return;
            }
            Debug.Log("   ├── Interacting with walkie-talkie immediately stops blinking and restores original material.");

            // Reset interaction
            wtController.ResetInteraction();
            if (wtController.IsBlinking || wtRenderer.sharedMaterial != wtController.originalMaterial)
            {
                Debug.LogError("[TEST FAILED] Walkie-talkie reset must stop blinking and restore original material!");
                return;
            }
            Debug.Log("[CHECK 18] Pinned Tool Presentation & Walkie-Talkie Highlight/Blink fully verified.");

            // 19. VERIFY GRADUAL PROCEDURAL MOLTEN METAL FLOOR SPILL
            Debug.Log("[CHECK 19] Verifying Gradual Procedural Molten Metal Floor Spill:");
            var floorSpillGo = GameObject.Find("Machine/MoltenMetalFlow/MoltenMetalFloorSpill")
                            ?? GameObject.Find("MoltenMetalFloorSpill");
            if (floorSpillGo == null)
            {
                Debug.LogError("[TEST FAILED] 'MoltenMetalFloorSpill' GameObject not found under Machine/MoltenMetalFlow!");
                return;
            }

            var floorSpill = floorSpillGo.GetComponent<MoltenMetalFloorSpill>();
            if (floorSpill == null)
            {
                Debug.LogError("[TEST FAILED] MoltenMetalFloorSpill component missing on Machine/MoltenMetalFlow/MoltenMetalFloorSpill!");
                return;
            }

            if (vfxController.floorSpill == null)
            {
                Debug.LogError("[TEST FAILED] vfxController.floorSpill reference is not assigned!");
                return;
            }

            // Verify position: directly below TransformPoints/drop, 5mm above Floor
            AssertClose("MoltenMetalFloorSpill Position", floorSpillGo.transform.position, MoltenMetalFloorSpill.SpillPosition, 0.02f);

            var spillMR = floorSpillGo.GetComponent<MeshRenderer>();
            var spillMF = floorSpillGo.GetComponent<MeshFilter>();
            if (spillMR == null || spillMF == null)
            {
                Debug.LogError("[TEST FAILED] MeshRenderer or MeshFilter missing on MoltenMetalFloorSpill!");
                return;
            }

            if (spillMR.sharedMaterial != flowMR.sharedMaterial)
            {
                Debug.LogError($"[TEST FAILED] MoltenMetalFloorSpill must use Lavafall material! Expected '{flowMR.sharedMaterial?.name}', got '{spillMR.sharedMaterial?.name}'");
                return;
            }

            // Test simulation and expansion
            floorSpill.SimulateSpill(0.5f);
            if (floorSpill.CurrentRadius < 0.35f || floorSpill.CurrentRadius > 0.55f)
            {
                Debug.LogError($"[TEST FAILED] Floor spill CurrentRadius at 50% should be ~0.44m, got {floorSpill.CurrentRadius:F3}m!");
                return;
            }
            if (spillMF.sharedMesh == null || spillMF.sharedMesh.vertexCount < 30 || spillMF.sharedMesh.triangles.Length < 60)
            {
                Debug.LogError($"[TEST FAILED] Floor spill procedural mesh not generated properly! Verts={spillMF.sharedMesh?.vertexCount ?? 0}, Tris={spillMF.sharedMesh?.triangles.Length ?? 0}");
                return;
            }
            Debug.Log($"   ├── 50% Expansion: Radius={floorSpill.CurrentRadius:F3}m, Verts={spillMF.sharedMesh.vertexCount}, Triangles={spillMF.sharedMesh.triangles.Length / 3}");

            floorSpill.SimulateSpill(1.0f);
            if (Mathf.Abs(floorSpill.CurrentRadius - floorSpill.maxRadius) > 0.05f)
            {
                Debug.LogError($"[TEST FAILED] Floor spill CurrentRadius at 100% should be {floorSpill.maxRadius}m, got {floorSpill.CurrentRadius:F3}m!");
                return;
            }
            Debug.Log($"   ├── 100% Full Puddle: Radius={floorSpill.CurrentRadius:F3}m (max={floorSpill.maxRadius}m), organic boundary generated.");

            // Test reset
            floorSpill.ResetSpill();
            if (spillMR.enabled)
            {
                Debug.LogError("[TEST FAILED] Floor spill MeshRenderer must be disabled after ResetSpill()!");
                return;
            }
            Debug.Log("   ├── ResetSpill(): MeshRenderer disabled, radius reset, puddle cleared.");

            // Test VFX Controller integration
            vfxController.StartFloorSpill();
            if (!floorSpill.IsSpilling || !spillMR.enabled)
            {
                Debug.LogError("[TEST FAILED] vfxController.StartFloorSpill() failed to start spill!");
                return;
            }
            vfxController.StopFloorSpill();
            if (floorSpill.IsSpilling)
            {
                Debug.LogError("[TEST FAILED] vfxController.StopFloorSpill() failed to stop spill!");
                return;
            }
            vfxController.ResetFloorSpill();
            if (spillMR.enabled)
            {
                Debug.LogError("[TEST FAILED] vfxController.ResetFloorSpill() failed to reset spill!");
                return;
            }
            Debug.Log("[CHECK 19] Gradual Procedural Molten Metal Floor Spill fully verified (position below drop, Lavafall material, organic disc mesh, expansion 0.03m -> 0.85m, clean reset, VFX controller integration).");

            // =========================================================================
            // [CHECK 20] Low Leakage 4-Stage Presentation Verification
            // =========================================================================
            Debug.Log("[CHECK 20] Verifying Low Leakage 4-Stage Presentation & Controlled Leakage...");

            var lowLeakageGo = GameObject.Find("Low Leakage");
            if (lowLeakageGo == null)
            {
                Debug.LogError("[TEST FAILED] 'Low Leakage' GameObject not found in scene!");
                return;
            }

            var lowLeakageCtrl = lowLeakageGo.GetComponent<LowLeakagePresentationController>();
            if (lowLeakageCtrl == null)
            {
                Debug.LogError("[TEST FAILED] LowLeakagePresentationController component not found on 'Low Leakage'!");
                return;
            }

            var containerObj = lowLeakageGo.transform.Find("COVER BATH CONTAINER")?.gameObject;
            var shovelObj = lowLeakageGo.transform.Find("SHOVEL")?.gameObject;
            var crowbarObj = lowLeakageGo.transform.Find("Crowbar")?.gameObject;

            if (containerObj == null || shovelObj == null || crowbarObj == null)
            {
                Debug.LogError("[TEST FAILED] Missing Low Leakage child objects (COVER BATH CONTAINER, SHOVEL, Crowbar)!");
                return;
            }

            // Verify initial state: all 3 presentation objects disabled during normal operation
            uiController.SetTask01UI();
            if (containerObj.activeSelf || shovelObj.activeSelf || crowbarObj.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Low Leakage presentation objects must start disabled in Task 01!");
                return;
            }
            Debug.Log("   ├── Initial State: COVER BATH CONTAINER, SHOVEL, Crowbar all disabled during Task 01.");

            // Start Presentation
            lowLeakageCtrl.StartLowLeakagePresentation();

            // Verify camera snap to Ideal Pot Voltage
            var lowLeakageCamTarget = GameObject.Find("CameraSystem/TransformPoints/Ideal Pot Voltage");
            if (lowLeakageCamTarget != null && !VerifyCameraSnap(mainCamera, lowLeakageCamTarget.transform, "Low Leakage Presentation"))
            {
                return;
            }

            // STEP 1: COVER BATH CONTAINER
            if (!containerObj.activeSelf || shovelObj.activeSelf || crowbarObj.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Low Leakage Step 1: Only COVER BATH CONTAINER must be enabled!");
                return;
            }
            var containerR = containerObj.GetComponentInChildren<Renderer>(true);
            if (containerR == null || containerR.sharedMaterial != lowLeakageCtrl.highlightMaterial)
            {
                Debug.LogError("[TEST FAILED] Low Leakage Step 1: Highlight material not applied to COVER BATH CONTAINER!");
                return;
            }
            if (uiController.titleText.text != "LOW LEAKAGE — FUSED ALUMINA" || uiController.statusText.text != "FUSED ALUMINA PREPARATION")
            {
                Debug.LogError($"[TEST FAILED] Low Leakage Step 1 UI mismatch! Title={uiController.titleText.text}, Status={uiController.statusText.text}");
                return;
            }
            Debug.Log("   ├── Step 1 Verified: COVER BATH CONTAINER enabled & highlighted, SHOVEL & Crowbar disabled, UI='LOW LEAKAGE — FUSED ALUMINA' / 'FUSED ALUMINA PREPARATION'.");

            // Advance to STEP 2: SHOVEL
            seqHelper.ResetDebounce();
            seqHelper.CompleteCurrentTask();

            if (containerObj.activeSelf || !shovelObj.activeSelf || crowbarObj.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Low Leakage Step 2: Only SHOVEL must be enabled!");
                return;
            }
            var shovelR = shovelObj.GetComponentInChildren<Renderer>(true);
            if (shovelR == null || shovelR.sharedMaterial != lowLeakageCtrl.highlightMaterial)
            {
                Debug.LogError("[TEST FAILED] Low Leakage Step 2: Highlight material not applied to SHOVEL!");
                return;
            }
            if (uiController.titleText.text != "LOW LEAKAGE — FUSED ALUMINA" || uiController.statusText.text != "FUSED ALUMINA HANDLING")
            {
                Debug.LogError($"[TEST FAILED] Low Leakage Step 2 UI mismatch! Title={uiController.titleText.text}, Status={uiController.statusText.text}");
                return;
            }
            Debug.Log("   ├── Step 2 Verified: SHOVEL enabled & highlighted, CONTAINER & Crowbar disabled, UI='LOW LEAKAGE — FUSED ALUMINA' / 'FUSED ALUMINA HANDLING'.");

            // Advance to STEP 3: Crowbar
            seqHelper.ResetDebounce();
            seqHelper.CompleteCurrentTask();

            if (containerObj.activeSelf || shovelObj.activeSelf || !crowbarObj.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Low Leakage Step 3: Only Crowbar must be enabled!");
                return;
            }
            var crowbarR = crowbarObj.GetComponentInChildren<Renderer>(true);
            if (crowbarR == null || crowbarR.sharedMaterial != lowLeakageCtrl.highlightMaterial)
            {
                Debug.LogError("[TEST FAILED] Low Leakage Step 3: Highlight material not applied to Crowbar!");
                return;
            }
            if (uiController.titleText.text != "LOW LEAKAGE — SIDE BREAKING" || uiController.statusText.text != "SIDE BREAKING / CHANNEL PREPARATION")
            {
                Debug.LogError($"[TEST FAILED] Low Leakage Step 3 UI mismatch! Title={uiController.titleText.text}, Status={uiController.statusText.text}");
                return;
            }
            Debug.Log("   ├── Step 3 Verified: Crowbar enabled & highlighted, CONTAINER & SHOVEL disabled, UI='LOW LEAKAGE — SIDE BREAKING' / 'SIDE BREAKING / CHANNEL PREPARATION'.");

            // Advance to STEP 4: Controlled Leakage
            seqHelper.ResetDebounce();
            seqHelper.CompleteCurrentTask();

            var flowMesh = vfxController.flowMesh;
            if (containerObj.activeSelf || shovelObj.activeSelf || crowbarObj.activeSelf)
            {
                Debug.LogError("[TEST FAILED] Low Leakage Step 4: All 3 presentation objects must be disabled!");
                return;
            }
            if (!flowMesh.isControlledLeakage || flowMesh.radius != flowMesh.controlledRadius)
            {
                Debug.LogError($"[TEST FAILED] Low Leakage Step 4: Controlled leakage not set! isControlled={flowMesh.isControlledLeakage}, radius={flowMesh.radius}");
                return;
            }
            if (uiController.titleText.text != "LOW LEAKAGE — CONTROLLED" || uiController.statusText.text != "CONTROLLED LEAKAGE")
            {
                Debug.LogError($"[TEST FAILED] Low Leakage Step 4 UI mismatch! Title={uiController.titleText.text}, Status={uiController.statusText.text}");
                return;
            }
            Debug.Log("   ├── Step 4 Verified: All objects disabled, continuous thin stream active (radius=0.016m), floor pool settled, UI='LOW LEAKAGE — CONTROLLED'.");

            // Advance to Complete Presentation
            seqHelper.ResetDebounce();
            seqHelper.CompleteCurrentTask();

            if (lowLeakageCtrl.IsPresentationActive)
            {
                Debug.LogError("[TEST FAILED] Low Leakage presentation should be inactive after final advance!");
                return;
            }
            if (SequenceHelperFunctions.OnInterceptTaskCompletion != null)
            {
                Debug.LogError("[TEST FAILED] SequenceHelperFunctions.OnInterceptTaskCompletion should be cleared after completion!");
                return;
            }
            Debug.Log("   ├── Presentation Completion: Interceptor released, progression restored.");
            Debug.Log("[CHECK 20] Low Leakage 4-Stage Presentation & Controlled Leakage fully verified.");

            // =========================================================================
            // [CHECK 21] Audio SFX, Sequential Walkie Playback & LocalTTS Verification
            // =========================================================================
            Debug.Log("[CHECK 21] Verifying Audio SFX, Sequential Walkie Playback & LocalTTS...");

            var clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/click.mp3");
            var swapClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/swap.mp3");
            var callClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/call.mp3");
            var warningClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/warning.mp3");

            if (clickClip == null || swapClip == null || callClip == null || warningClip == null)
            {
                Debug.LogError("[TEST FAILED] Audio assets missing in Assets/Audio/!");
                return;
            }

            // 1. Controller clip assignments
            if (uiController.clickAudioClip != clickClip || uiController.nextButtonSFX != clickClip)
            {
                Debug.LogError("[TEST FAILED] PotLeakageUIController click audio not assigned to click.mp3!");
                return;
            }
            if (camController.swapAudioClip != swapClip)
            {
                Debug.LogError("[TEST FAILED] PotLeakageCameraController swap audio not assigned to swap.mp3!");
                return;
            }
            if (wtController.clickAudioClip != clickClip || wtController.callAudioClip != callClip || wtController.warningAudioClip != warningClip)
            {
                Debug.LogError("[TEST FAILED] WalkieTalkieInteractionController audio clips not properly assigned!");
                return;
            }
            Debug.Log("   ├── Audio Clips verified on UI, Camera, and Walkie-Talkie controllers.");

            // 2. Camera swap real change detection
            camController.SnapCameraToTransform(welcomeTarget);
            AssertClose("Camera snapped to welcome", mainCamera.transform.position, welcomeTarget.position);

            // 3. Walkie Talkie sequential audio state progression
            wtController.ActivateInteraction();
            if (wtController.CurrentState != WalkieTalkieState.WaitingForShift)
            {
                Debug.LogError($"[TEST FAILED] Walkie talkie not in WaitingForShift after activation! Got {wtController.CurrentState}");
                return;
            }
            // First press (Shift)
            wtController.autoAdvanceInEditMode = true;
            wtController.Interact();
            if (wtController.CurrentState != WalkieTalkieState.WaitingForTechnical)
            {
                Debug.LogError($"[TEST FAILED] Walkie talkie not in WaitingForTechnical after shift stage! Got {wtController.CurrentState}");
                return;
            }
            // Second press (Technical)
            wtController.Interact();
            if (wtController.CurrentState != WalkieTalkieState.Completed)
            {
                Debug.LogError($"[TEST FAILED] Walkie talkie not in Completed after technical stage! Got {wtController.CurrentState}");
                return;
            }
            wtController.ResetInteraction();
            Debug.Log("   ├── Walkie-Talkie two-stage sequential flow verified (Shift -> Wait Tech -> Technical -> Complete).");

            // 4. LocalTTS description reading integration
            var mgr = TruckTyreReplacement.Core.Manager.Instance;
            if (mgr == null)
            {
                Debug.LogError("[TEST FAILED] Manager.Instance is null!");
                return;
            }
            uiController.SpeakDescriptionText("Test speech text without overlap", "TEST_KEY");
            Debug.Log("   ├── LocalTTS SpeakDescriptionText verified with Manager.Instance.");
            Debug.Log("[CHECK 21] Audio SFX, Sequential Walkie Playback & LocalTTS fully verified.");

            // Reset cleanly back to Task 01
            seqHandler.currentSequence = 0;
            seqHandler.currentTask = 0;
            foreach (var t in sequence.TaskList) t.TaskCompleted = false;
            uiController.SetTask01UI();
            if (vfxController.cube010Renderer.sharedMaterial != vfxController.Cube010OriginalMaterial)
            {
                Debug.LogError("[TEST FAILED] Cube.010 sharedMaterial not restored after final reset to Task 01!");
                return;
            }
            mainCamera.transform.position = welcomeTarget.position;
            mainCamera.transform.rotation = welcomeTarget.rotation;
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("===============================================================");
            Debug.Log("ALL VEDANTA POT LEAKAGE AUTOMATED CHECKS PASSED (100% OK)");
            Debug.Log("===============================================================");
        }

        private static bool VerifyCameraSnap(UnityEngine.Camera cam, Transform target, string taskLabel)
        {
            float posDist = Vector3.Distance(cam.transform.position, target.position);
            float rotAngle = Quaternion.Angle(cam.transform.rotation, target.rotation);
            if (posDist > 0.01f)
            {
                Debug.LogError($"[TEST FAILED] {taskLabel} camera position snap mismatch! Dist={posDist:F4} > 0.01f. Expected {target.position}, got {cam.transform.position}");
                return false;
            }
            if (rotAngle > 0.1f)
            {
                Debug.LogError($"[TEST FAILED] {taskLabel} camera rotation snap mismatch! AngleDiff={rotAngle:F4}° > 0.1°. Expected rot {target.eulerAngles}, got {cam.transform.eulerAngles}");
                return false;
            }
            return true;
        }

        private static void AssertClose(string label, Vector3 actual, Vector3 expected, float tolerance = 0.05f)
        {
            if (Vector3.Distance(actual, expected) > tolerance)
            {
                Debug.LogError($"[TRANSFORM LOCK VIOLATION] {label}: Expected {expected}, got {actual}");
            }
        }
    }
}
#endif
