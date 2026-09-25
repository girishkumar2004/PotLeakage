#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using PotLeakage.Camera;
using PotLeakage.Configuration;
using PotLeakage.Core;
using PotLeakage.UI;
using PotLeakage.VFX;

namespace PotLeakage.Editor
{
    [InitializeOnLoad]
    public static class PotLeakageSceneBuilder
    {
        private const string ScenePath = "Assets/PotLeakage/Scenes/PotLeakage_Main.unity";
        private const string ConfigPath = "Assets/PotLeakage/Data/PotLeakageConfig.asset";
        private const string HighlightMatPath = "Assets/PotLeakage/Materials/M_PotLeakage_Highlight.mat";
        private const string MagmaMatPath = "Assets/PotLeakage/Materials/M_Magma_HotMetal.mat";

        [MenuItem("Vedanta Training Data/Deploy Training JSON")]
        public static void DeployTrainingJson()
        {
            string sourcePath = Path.Combine(Application.dataPath, "PotLeakage", "Data", "training.json");
            string targetPath = Path.Combine(Application.persistentDataPath, "TrainingData", "training.json");

            if (!File.Exists(sourcePath))
            {
                Debug.LogError($"[JSON DEPLOY] Source training.json not found at {sourcePath}");
                return;
            }

            string targetDir = Path.GetDirectoryName(targetPath);
            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

            File.Copy(sourcePath, targetPath, true);
            Debug.Log($"[JSON DEPLOY] Successfully deployed training.json to {targetPath}");

            var mgr = TruckTyreReplacement.Core.Manager.Instance;
            if (mgr != null)
            {
                mgr.ReloadTrainingData();
            }
        }

        [MenuItem("Vedanta/Build Pot Leakage Scene")]
        public static void BuildScene()
        {
            // 1. Ensure Data directories exist
            if (!AssetDatabase.IsValidFolder("Assets/PotLeakage"))
            {
                AssetDatabase.CreateFolder("Assets", "PotLeakage");
            }
            if (!AssetDatabase.IsValidFolder("Assets/PotLeakage/Data"))
            {
                AssetDatabase.CreateFolder("Assets/PotLeakage", "Data");
            }
            if (!AssetDatabase.IsValidFolder("Assets/PotLeakage/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/PotLeakage", "Scenes");
            }

            // 2. Create or load Config ScriptableObject
            var config = AssetDatabase.LoadAssetAtPath<PotLeakageConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PotLeakageConfig>();
                config.normalProductionPotVoltage = 4.0f;
                config.normalProductionCBTTemperature = 100f;
                config.leakageMonitoringVoltageMaximum = 4.5f;
                config.leakageStopVoltage = 1.2f;
                AssetDatabase.CreateAsset(config, ConfigPath);
                AssetDatabase.SaveAssets();
            }

            // 3. Create new Scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── 1. ENVIRONMENT (No PotRoom, No Floor under Environment) ───
            var environment = new GameObject("Environment");

            var dirLightGo = new GameObject("Directional Light");
            dirLightGo.transform.SetParent(environment.transform, false);
            var dirLight = dirLightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1.0f, 0.96f, 0.90f);
            dirLight.intensity = 1.8f;
            dirLight.shadows = LightShadows.Soft;
            dirLightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            dirLightGo.transform.position = new Vector3(0, 8, 0);

            var globalVolumeGo = new GameObject("Global Volume");
            globalVolumeGo.transform.SetParent(environment.transform, false);
            var volume = globalVolumeGo.AddComponent<Volume>();
            volume.isGlobal = true;

            // ── 2. MACHINE (Contains Floor, Wall, Wall (1), Fire, FireEffect) ──
            GameObject machine = null;
            var machineModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Model/Machine.fbx");
            if (machineModelPrefab != null)
            {
                machine = (GameObject)PrefabUtility.InstantiatePrefab(machineModelPrefab);
                machine.name = "Machine";
                machine.transform.localPosition = Vector3.zero;
                machine.transform.localRotation = Quaternion.identity;
                machine.transform.localScale = Vector3.one;
            }
            else
            {
                machine = new GameObject("Machine");
            }

            var concreteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Model/Materials/concrete_10_color.mat");
            var wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Model/Materials/warehouse side pannel texture.mat") ?? concreteMat;

            // Machine/Floor (authoritative floor under Machine)
            var floorTransform = machine.transform.Find("Floor");
            GameObject floorGo = null;
            if (floorTransform != null)
            {
                floorGo = floorTransform.gameObject;
            }
            else
            {
                floorGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floorGo.name = "Floor";
                floorGo.transform.SetParent(machine.transform, false);
                floorGo.transform.localPosition = new Vector3(0, 0, 0);
                floorGo.transform.localScale = new Vector3(5, 1, 5);
                if (concreteMat != null)
                {
                    floorGo.GetComponent<MeshRenderer>().sharedMaterial = concreteMat;
                }
            }

            // Machine/Wall
            var wallTransform = machine.transform.Find("Wall");
            GameObject wallGo = null;
            if (wallTransform != null)
            {
                wallGo = wallTransform.gameObject;
            }
            else
            {
                wallGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallGo.name = "Wall";
                wallGo.transform.SetParent(machine.transform, false);
                wallGo.transform.localPosition = new Vector3(0, 3f, 15f);
                wallGo.transform.localScale = new Vector3(30f, 6f, 0.5f);
                if (wallMat != null)
                {
                    wallGo.GetComponent<MeshRenderer>().sharedMaterial = wallMat;
                }
            }

            // Machine/Wall (1)
            var wall1Transform = machine.transform.Find("Wall (1)");
            GameObject wall1Go = null;
            if (wall1Transform != null)
            {
                wall1Go = wall1Transform.gameObject;
            }
            else
            {
                wall1Go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall1Go.name = "Wall (1)";
                wall1Go.transform.SetParent(machine.transform, false);
                wall1Go.transform.localPosition = new Vector3(-15f, 3f, 0f);
                wall1Go.transform.localScale = new Vector3(0.5f, 6f, 30f);
                if (wallMat != null)
                {
                    wall1Go.GetComponent<MeshRenderer>().sharedMaterial = wallMat;
                }
            }

            // Locate full pot machine mesh tripo_node_cf158416
            GameObject potMachineHighlightGo = null;
            if (machine != null)
            {
                var fullMeshT = machine.transform.Find("tripo_node_cf158416");
                if (fullMeshT != null)
                {
                    potMachineHighlightGo = fullMeshT.gameObject;
                }
                else
                {
                    var renderers = machine.GetComponentsInChildren<MeshRenderer>();
                    if (renderers.Length > 0)
                    {
                        potMachineHighlightGo = renderers[0].gameObject;
                    }
                }
            }

            // Locate or create Fire GameObject representing CBT / Collector Bar focus zone
            GameObject fireGo = null;
            if (machine != null)
            {
                var fireTransform = machine.transform.Find("Fire");
                if (fireTransform != null)
                {
                    fireGo = fireTransform.gameObject;
                }
            }
            if (fireGo == null)
            {
                fireGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fireGo.name = "Fire";
                fireGo.transform.SetParent(machine.transform, false);
                fireGo.transform.localPosition = new Vector3(-1.2f, 0.9f, 0f);
                fireGo.transform.localScale = new Vector3(0.6f, 0.4f, 0.6f);

                var col = fireGo.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }

            // Machine/FireEffect (Subtle Magma / Hot Metal Visual Effect)
            var fireEffectTransform = machine.transform.Find("FireEffect");
            GameObject fireEffectGo = null;
            if (fireEffectTransform != null)
            {
                fireEffectGo = fireEffectTransform.gameObject;
            }
            else
            {
                fireEffectGo = new GameObject("FireEffect");
                fireEffectGo.transform.SetParent(machine.transform, false);
                fireEffectGo.transform.localPosition = new Vector3(-1.2f, 0.9f, 0f);
            }

            // Configure subtle glowing magma surface & low-intensity heat embers
            var magmaMat = AssetDatabase.LoadAssetAtPath<Material>(MagmaMatPath);
            
            // Glowing hot metal core mesh
            var hotCoreTransform = fireEffectGo.transform.Find("HotMetalCore");
            GameObject hotCoreGo = null;
            if (hotCoreTransform != null)
            {
                hotCoreGo = hotCoreTransform.gameObject;
            }
            else
            {
                hotCoreGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hotCoreGo.name = "HotMetalCore";
                hotCoreGo.transform.SetParent(fireEffectGo.transform, false);
                hotCoreGo.transform.localPosition = Vector3.zero;
                hotCoreGo.transform.localScale = new Vector3(0.55f, 0.35f, 0.55f);
                var col = hotCoreGo.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }
            if (magmaMat != null)
            {
                hotCoreGo.GetComponent<MeshRenderer>().sharedMaterial = magmaMat;
            }

            // Subtle localized heat glow point light
            var heatLight = fireEffectGo.GetComponent<Light>();
            if (heatLight == null) heatLight = fireEffectGo.AddComponent<Light>();
            heatLight.type = LightType.Point;
            heatLight.color = new Color(1.0f, 0.35f, 0.05f);
            heatLight.intensity = 1.5f;
            heatLight.range = 2.5f;

            // Subtle heat embers particle system
            var heatPS = fireEffectGo.GetComponent<ParticleSystem>();
            if (heatPS == null) heatPS = fireEffectGo.AddComponent<ParticleSystem>();
            var psRenderer = fireEffectGo.GetComponent<ParticleSystemRenderer>();
            psRenderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");

            var mainModule = heatPS.main;
            mainModule.startColor = new Color(1.0f, 0.45f, 0.1f, 0.85f);
            mainModule.startSize = 0.04f;
            mainModule.startSpeed = 0.15f;
            mainModule.startLifetime = 1.8f;
            mainModule.maxParticles = 25;
            mainModule.playOnAwake = true;
            mainModule.loop = true;

            var emission = heatPS.emission;
            emission.rateOverTime = 8f;

            var shape = heatPS.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.5f, 0.2f, 0.5f);

            var colorOverLifetime = heatPS.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.6f, 0.1f), 0.0f), new GradientColorKey(new Color(0.9f, 0.2f, 0.05f), 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorOverLifetime.color = gradient;

            // Start FireEffect inactive (Task 01-03 OFF, Task 04 ON)
            fireEffectGo.SetActive(false);

            // ── 3. CAMERA SYSTEM & TRANSFORM POINTS (PRESERVE COORDINATES) ──
            var cameraSystem = new GameObject("CameraSystem");

            var transformPoints = new GameObject("TransformPoints");
            transformPoints.transform.SetParent(cameraSystem.transform, false);

            var welcomeTarget = new GameObject("Welcome");
            welcomeTarget.transform.SetParent(transformPoints.transform, false);
            welcomeTarget.transform.position = new Vector3(0f, 5.5f, -12.5f);
            welcomeTarget.transform.rotation = Quaternion.Euler(18f, 0f, 0f);

            var normalPotOperationTarget = new GameObject("Normal Pot Operation");
            normalPotOperationTarget.transform.SetParent(transformPoints.transform, false);
            normalPotOperationTarget.transform.position = new Vector3(0f, 3.2f, -6.5f);
            normalPotOperationTarget.transform.rotation = Quaternion.Euler(14f, 0f, 0f);

            var idealPotVoltageTarget = new GameObject("Ideal Pot Voltage");
            idealPotVoltageTarget.transform.SetParent(transformPoints.transform, false);
            idealPotVoltageTarget.transform.position = new Vector3(0f, 3.2f, -6.5f);
            idealPotVoltageTarget.transform.rotation = Quaternion.Euler(14f, 0f, 0f);

            var idealCBTTemperatureTarget = new GameObject("Ideal CBT Temperature");
            idealCBTTemperatureTarget.transform.SetParent(transformPoints.transform, false);
            idealCBTTemperatureTarget.transform.position = new Vector3(-2.2f, 1.8f, -3.2f);
            idealCBTTemperatureTarget.transform.rotation = Quaternion.Euler(12f, 25f, 0f);

            var potMachineLookTarget = new GameObject("PotMachine_LookTarget");
            potMachineLookTarget.transform.SetParent(transformPoints.transform, false);
            potMachineLookTarget.transform.position = new Vector3(0f, 1.5f, 0f);

            var cbtLookTarget = new GameObject("CBT_LookTarget");
            cbtLookTarget.transform.SetParent(transformPoints.transform, false);
            cbtLookTarget.transform.position = new Vector3(-0.8f, 1.0f, 0f);

            // Main Camera: spawns at the exact Welcome position and rotation
            var mainCameraGo = new GameObject("Main Camera");
            mainCameraGo.tag = "MainCamera";
            mainCameraGo.transform.SetParent(cameraSystem.transform, false);
            mainCameraGo.transform.position = welcomeTarget.transform.position;
            mainCameraGo.transform.rotation = welcomeTarget.transform.rotation;

            var cam = mainCameraGo.AddComponent<UnityEngine.Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            mainCameraGo.AddComponent<AudioListener>();
            mainCameraGo.AddComponent<UniversalAdditionalCameraData>();

            var cameraController = mainCameraGo.AddComponent<PotLeakageCameraController>();
            cameraController.welcomeTarget = welcomeTarget.transform;
            cameraController.normalPotOperationTarget = normalPotOperationTarget.transform;
            cameraController.idealPotVoltageTarget = idealPotVoltageTarget.transform;
            cameraController.idealCBTTemperatureTarget = idealCBTTemperatureTarget.transform;
            cameraController.potMachineLookTarget = potMachineLookTarget.transform;
            cameraController.cbtLookTarget = cbtLookTarget.transform;
            cameraController.defaultDuration = 2.0f;
            cameraController.quickTransitionDuration = 0.8f;

            // ── 4. HIGHLIGHT SYSTEM ───────────────────────────────────
            var highlightSystem = new GameObject("HighlightSystem");

            var potMachineHighlight = new GameObject("PotMachine_Highlight");
            potMachineHighlight.transform.SetParent(highlightSystem.transform, false);

            var cbtHighlight = new GameObject("CBT_Highlight");
            cbtHighlight.transform.SetParent(highlightSystem.transform, false);

            var highlightMat = AssetDatabase.LoadAssetAtPath<Material>(HighlightMatPath);

            // ── 5. VFX ────────────────────────────────────────────────
            var vfxParent = new GameObject("VFX");

            var moltenVfxGo = new GameObject("MoltenAluminium_VFX");
            moltenVfxGo.transform.SetParent(vfxParent.transform, false);
            moltenVfxGo.transform.localPosition = new Vector3(-1.2f, 0.9f, 0f);

            var ps = moltenVfxGo.AddComponent<ParticleSystem>();
            var moltenPsRenderer = moltenVfxGo.GetComponent<ParticleSystemRenderer>();
            moltenPsRenderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");

            var vfxMainModule = ps.main;
            vfxMainModule.startColor = new Color(1.0f, 0.62f, 0.15f, 0.9f);
            vfxMainModule.startSize = 0.05f;
            vfxMainModule.startSpeed = 0.25f;
            vfxMainModule.startLifetime = 2.0f;
            vfxMainModule.maxParticles = 100;
            vfxMainModule.playOnAwake = false;

            var vfxEmission = ps.emission;
            vfxEmission.rateOverTime = 15f;

            var vfxShape = ps.shape;
            vfxShape.shapeType = ParticleSystemShapeType.Sphere;
            vfxShape.radius = 0.5f;
            vfxShape.scale = new Vector3(0.8f, 0.3f, 0.8f);

            var vfxController = moltenVfxGo.AddComponent<MoltenAluminiumVFXController>();
            vfxController.moltenAluminiumVFXTransform = moltenVfxGo.transform;
            vfxController.maxParticles = 100;
            vfxController.emissionRate = 15f;
            vfxController.particleSize = 0.05f;
            vfxController.glowIntensity = 1.8f;
            moltenVfxGo.SetActive(false);

            // ── 6. UI SYSTEM ─────────────────────────────────────────
            var uiParent = new GameObject("UI");

            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(uiParent.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.transform.SetParent(uiParent.transform, false);
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            var fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (fontAsset == null)
            {
                fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }

            // Explanation / Welcome Panel (authoritative presentation panel under UI Canvas)
            var welcomePanelGo = new GameObject("Welcome Panel", typeof(RectTransform));
            welcomePanelGo.transform.SetParent(canvasGo.transform, false);
            var infoRect = welcomePanelGo.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.5f, 0f);
            infoRect.anchorMax = new Vector2(0.5f, 0f);
            infoRect.pivot = new Vector2(0.5f, 0f);
            infoRect.anchoredPosition = new Vector2(0, 40);
            infoRect.sizeDelta = new Vector2(1100, 260);

            var panelBg = welcomePanelGo.AddComponent<Image>();
            panelBg.color = new Color(0.04f, 0.07f, 0.12f, 0.92f);

            // Header Container inside Welcome Panel
            var headerGo = new GameObject("Header", typeof(RectTransform));
            headerGo.transform.SetParent(welcomePanelGo.transform, false);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = new Vector2(0, 0);
            headerRect.sizeDelta = new Vector2(0, 60);

            // Title Text inside Header
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(headerGo.transform, false);
            var titleText = titleGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) titleText.font = fontAsset;
            titleText.text = "POT LEAKAGE";
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0, 0.5f);
            titleRect.offsetMin = new Vector2(35, 10);
            titleRect.offsetMax = new Vector2(-120, -10);

            // Company Logo Image slot inside Header at TOP RIGHT of Panel (empty for user assignment)
            var logoGo = new GameObject("CompanyLogo", typeof(RectTransform));
            logoGo.transform.SetParent(headerGo.transform, false);
            var logoRect = logoGo.GetComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(1, 1);
            logoRect.anchorMax = new Vector2(1, 1);
            logoRect.pivot = new Vector2(1, 1);
            logoRect.anchoredPosition = new Vector2(-25, -10);
            logoRect.sizeDelta = new Vector2(80, 80);

            var logoImg = logoGo.AddComponent<Image>();
            logoImg.preserveAspect = true;
            logoImg.color = Color.white;
            var logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PotLeakage/Textures/Vedanta_Logo.jpg");
            if (logoSprite != null)
            {
                logoImg.sprite = logoSprite;
            }

            // Content Container inside Welcome Panel
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(welcomePanelGo.transform, false);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(35, 80);
            contentRect.offsetMax = new Vector2(-300, -60);

            // Description Text
            var descGo = new GameObject("Description", typeof(RectTransform));
            descGo.transform.SetParent(contentGo.transform, false);
            var descText = descGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) descText.font = fontAsset;
            descText.text = "Welcome to the Pot Leakage training module.\n\nThis module introduces normal pot operation and the important parameters used to identify abnormal pot conditions.";
            descText.fontSize = 18;
            descText.color = new Color(0.85f, 0.90f, 0.95f);
            var descRect = descGo.GetComponent<RectTransform>();
            descRect.anchorMin = Vector2.zero;
            descRect.anchorMax = Vector2.one;
            descRect.offsetMin = Vector2.zero;
            descRect.offsetMax = Vector2.zero;

            // Status Text
            var statusGo = new GameObject("StatusText", typeof(RectTransform));
            statusGo.transform.SetParent(contentGo.transform, false);
            var statusText = statusGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) statusText.font = fontAsset;
            statusText.text = "POT STATUS: <color=#00FF66>NORMAL OPERATION</color>";
            statusText.fontSize = 18;
            statusText.fontStyle = FontStyles.Bold;
            statusText.color = new Color(0.1f, 1f, 0.5f);
            var statusRect = statusGo.GetComponent<RectTransform>();
            statusRect.anchorMin = Vector2.zero;
            statusRect.anchorMax = Vector2.one;
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;
            statusGo.SetActive(false);

            // Value Text
            var valueGo = new GameObject("ValueText", typeof(RectTransform));
            valueGo.transform.SetParent(contentGo.transform, false);
            var valueText = valueGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) valueText.font = fontAsset;
            valueText.text = "NORMAL PRODUCTION VALUE:\n<size=110%><b><color=#FFAA00>VALUE NOT CONFIGURED</color></b></size>";
            valueText.fontSize = 18;
            valueText.color = Color.white;
            var valueRect = valueGo.GetComponent<RectTransform>();
            valueRect.anchorMin = Vector2.zero;
            valueRect.anchorMax = Vector2.one;
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;
            valueGo.SetActive(false);

            // Footer Container inside Welcome Panel
            var footerGo = new GameObject("Footer", typeof(RectTransform));
            footerGo.transform.SetParent(welcomePanelGo.transform, false);
            var footerRect = footerGo.GetComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0, 0);
            footerRect.anchorMax = new Vector2(1, 0);
            footerRect.pivot = new Vector2(0.5f, 0);
            footerRect.anchoredPosition = Vector2.zero;
            footerRect.sizeDelta = new Vector2(0, 80);

            // Progress Text inside Footer
            var progressGo = new GameObject("ProgressText", typeof(RectTransform));
            progressGo.transform.SetParent(footerGo.transform, false);
            var progressText = progressGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) progressText.font = fontAsset;
            progressText.text = "<color=#00E5FF>TASK 01 / 04</color>";
            progressText.fontSize = 17;
            progressText.fontStyle = FontStyles.Bold;
            var progRect = progressGo.GetComponent<RectTransform>();
            progRect.anchorMin = new Vector2(0, 0.5f);
            progRect.anchorMax = new Vector2(0, 0.5f);
            progRect.pivot = new Vector2(0, 0.5f);
            progRect.anchoredPosition = new Vector2(35, 0);
            progRect.sizeDelta = new Vector2(250, 30);

            // Next Button inside Footer
            var nextBtnGo = new GameObject("NextButton", typeof(RectTransform));
            nextBtnGo.transform.SetParent(footerGo.transform, false);
            var nextBtnRect = nextBtnGo.GetComponent<RectTransform>();
            nextBtnRect.anchorMin = new Vector2(1, 0.5f);
            nextBtnRect.anchorMax = new Vector2(1, 0.5f);
            nextBtnRect.pivot = new Vector2(1, 0.5f);
            nextBtnRect.anchoredPosition = new Vector2(-35, 0);
            nextBtnRect.sizeDelta = new Vector2(220, 55);

            var nextBtnImg = nextBtnGo.AddComponent<Image>();
            nextBtnImg.color = new Color(0.0f, 0.55f, 0.85f, 1f);

            var nextBtn = nextBtnGo.AddComponent<Button>();
            nextBtn.targetGraphic = nextBtnImg;
            var btnColors = nextBtn.colors;
            btnColors.highlightedColor = new Color(0.1f, 0.7f, 1.0f, 1f);
            btnColors.pressedColor = new Color(0.0f, 0.4f, 0.7f, 1f);
            nextBtn.colors = btnColors;

            var nextBtnTextGo = new GameObject("Text", typeof(RectTransform));
            nextBtnTextGo.transform.SetParent(nextBtnGo.transform, false);
            var nextBtnText = nextBtnTextGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) nextBtnText.font = fontAsset;
            nextBtnText.text = "<b>NEXT  ></b>";
            nextBtnText.fontSize = 22;
            nextBtnText.alignment = TextAlignmentOptions.Center;
            nextBtnText.color = Color.white;
            var nextBtnTextRect = nextBtnTextGo.GetComponent<RectTransform>();
            nextBtnTextRect.anchorMin = Vector2.zero;
            nextBtnTextRect.anchorMax = Vector2.one;
            nextBtnTextRect.sizeDelta = Vector2.zero;

            // ── 7. TRAINING FRAMEWORK ─────────────────────────────────
            var trainingFramework = new GameObject("TrainingFramework");

            // 7.1 Manager GameObject
            var managerGo = new GameObject("Manager");
            managerGo.transform.SetParent(trainingFramework.transform, false);

            var potManager = managerGo.AddComponent<PotLeakageManager>();
            var uiController = managerGo.AddComponent<PotLeakageUIController>();
            var valueDisplay = managerGo.AddComponent<PotLeakageValueDisplay>();

            valueDisplay.config = config;
            valueDisplay.normalProductionPotVoltage = config.normalProductionPotVoltage;
            valueDisplay.normalProductionCBTTemperature = config.normalProductionCBTTemperature;
            
            var valDispSO = new SerializedObject(valueDisplay);
            valDispSO.FindProperty("titleText").objectReferenceValue = titleText;
            valDispSO.FindProperty("descriptionText").objectReferenceValue = descText;
            valDispSO.FindProperty("valueText").objectReferenceValue = valueText;
            valDispSO.FindProperty("statusText").objectReferenceValue = statusText;
            valDispSO.FindProperty("progressText").objectReferenceValue = progressText;
            valDispSO.ApplyModifiedProperties();

            uiController.headerPanel = headerGo;
            uiController.welcomePanel = welcomePanelGo;
            uiController.titleText = titleText;
            uiController.descriptionText = descText;
            uiController.progressText = progressText;
            uiController.companyLogo = logoImg;
            uiController.nextButton = nextBtn;
            uiController.valueDisplay = valueDisplay;
            uiController.potMachineHighlightTarget = potMachineHighlightGo;
            uiController.cbtHighlightTarget = fireGo;
            uiController.fireEffect = fireEffectGo;

            // 7.2 Sequence GameObject
            var sequenceGo = new GameObject("Sequence");
            sequenceGo.transform.SetParent(trainingFramework.transform, false);
            var sequence = sequenceGo.AddComponent<Sequence>();
            sequence.SequenceName = "PotLeakage_Introduction";
            sequence.TaskList = new List<Task>();

            // 7.3 Sequence Handler GameObject
            var seqHandlerGo = new GameObject("Sequence Handler");
            seqHandlerGo.transform.SetParent(trainingFramework.transform, false);
            var seqHandler = seqHandlerGo.AddComponent<SequenceHandler>();
            seqHandler.autostart = true;
            seqHandler.isSequenceMode = true;
            seqHandler.sequenceList = new List<Sequence> { sequence };

            // 7.4 Sequence Helper GameObject
            var seqHelperGo = new GameObject("Sequence Helper");
            seqHelperGo.transform.SetParent(trainingFramework.transform, false);
            var seqHelper = seqHelperGo.AddComponent<SequenceHelperFunctions>();
            seqHelper.nextButton = nextBtn;
            if (highlightMat != null)
            {
                seqHelper.HighlightMaterial = highlightMat;
            }

            // Wire Manager references
            potManager.mainCamera = cam;
            potManager.welcomeTarget = welcomeTarget.transform;
            potManager.normalPotOperationTarget = normalPotOperationTarget.transform;
            potManager.idealPotVoltageTarget = idealPotVoltageTarget.transform;
            potManager.idealCBTTemperatureTarget = idealCBTTemperatureTarget.transform;
            potManager.cameraController = cameraController;
            potManager.potMachineHighlightTarget = potMachineHighlightGo;
            potManager.cbtHighlightTarget = fireGo;
            potManager.fireEffect = fireEffectGo;
            potManager.welcomePanel = welcomePanelGo;
            potManager.panelTitle = titleText;
            potManager.panelDescription = descText;
            potManager.valueText = valueText;
            potManager.statusText = statusText;
            potManager.progressText = progressText;
            potManager.companyLogo = logoImg;
            potManager.nextButton = nextBtn;
            potManager.uiController = uiController;
            potManager.valueDisplay = valueDisplay;
            potManager.moltenAluminiumVFX = vfxController;
            potManager.sequenceHandler = seqHandler;
            potManager.sequenceHelper = seqHelper;
            potManager.sequenceAsset = sequence;
            potManager.potLeakageConfig = config;

            // Wire Next Button to SequenceHelperFunctions.CompleteCurrentTask
            UnityEventTools.AddPersistentListener(nextBtn.onClick, new UnityAction(seqHelper.CompleteCurrentTask));

            // ── 8. SEQUENCE TASKS (01 to 04) ─────────────────────────
            // TASK 01: Welcome
            var task01 = new Task
            {
                TaskName = "01 – Welcome",
                typeOfInteraction = Task.TypeOfInteraction.None,
                instructionText = "POT LEAKAGE",
                useTTS = false,
                completionMode = CompletionMode.Manual
            };
            UnityEventTools.AddObjectPersistentListener<Transform>(task01.EventsToFollow, new UnityAction<Transform>(cameraController.MoveToTarget), welcomeTarget.transform);
            UnityEventTools.AddPersistentListener(task01.EventsToFollow, new UnityAction(uiController.SetTask01UI));
            UnityEventTools.AddPersistentListener(task01.EventsToFollow, new UnityAction(seqHelper.ClearAllHighlights));
            UnityEventTools.AddPersistentListener(task01.EventsToFollow, new UnityAction(vfxController.DisableVFX));
            sequence.TaskList.Add(task01);

            // TASK 02: Normal Pot Operation
            var task02 = new Task
            {
                TaskName = "02 – Normal Pot Operation",
                typeOfInteraction = Task.TypeOfInteraction.None,
                instructionText = "This section introduces the normal operating condition of the pot before discussing abnormal conditions such as pot leakage.",
                useTTS = false,
                completionMode = CompletionMode.Manual
            };
            UnityEventTools.AddObjectPersistentListener<Transform>(task02.EventsToFollow, new UnityAction<Transform>(cameraController.MoveToTarget), normalPotOperationTarget.transform);
            if (potMachineHighlightGo != null)
            {
                UnityEventTools.AddObjectPersistentListener<GameObject>(task02.EventsToFollow, new UnityAction<GameObject>(seqHelper.HighlightObject), potMachineHighlightGo);
            }
            UnityEventTools.AddPersistentListener(task02.EventsToFollow, new UnityAction(uiController.SetTask02UI));
            UnityEventTools.AddPersistentListener(task02.EventsToFollow, new UnityAction(vfxController.DisableVFX));
            sequence.TaskList.Add(task02);

            // TASK 03: Ideal Pot Voltage
            var task03 = new Task
            {
                TaskName = "03 – Ideal Pot Voltage",
                typeOfInteraction = Task.TypeOfInteraction.None,
                instructionText = "Pot voltage is continuously monitored to ensure operational stability and cell thermal balance.",
                useTTS = false,
                completionMode = CompletionMode.Manual
            };
            UnityEventTools.AddObjectPersistentListener<Transform>(task03.EventsToFollow, new UnityAction<Transform>(cameraController.MoveToTarget), idealPotVoltageTarget.transform);
            if (potMachineHighlightGo != null)
            {
                UnityEventTools.AddObjectPersistentListener<GameObject>(task03.EventsToFollow, new UnityAction<GameObject>(seqHelper.HighlightObject), potMachineHighlightGo);
            }
            UnityEventTools.AddPersistentListener(task03.EventsToFollow, new UnityAction(uiController.SetTask03UI));
            UnityEventTools.AddPersistentListener(task03.EventsToFollow, new UnityAction(vfxController.DisableVFX));
            sequence.TaskList.Add(task03);

            // TASK 04: Ideal CBT Temperature
            var task04 = new Task
            {
                TaskName = "04 – Ideal CBT Temperature",
                typeOfInteraction = Task.TypeOfInteraction.None,
                instructionText = "Collector Bar Temperature (CBT) is an important parameter used when identifying abnormal pot conditions and possible collector bar leakage.",
                useTTS = false,
                completionMode = CompletionMode.Manual
            };
            UnityEventTools.AddObjectPersistentListener<Transform>(task04.EventsToFollow, new UnityAction<Transform>(cameraController.MoveToTarget), idealCBTTemperatureTarget.transform);
            if (potMachineHighlightGo != null)
            {
                UnityEventTools.AddObjectPersistentListener<GameObject>(task04.EventsToFollow, new UnityAction<GameObject>(seqHelper.RemoveHighlight), potMachineHighlightGo);
            }
            if (fireGo != null)
            {
                UnityEventTools.AddObjectPersistentListener<GameObject>(task04.EventsToFollow, new UnityAction<GameObject>(seqHelper.HighlightObject), fireGo);
            }
            UnityEventTools.AddPersistentListener(task04.EventsToFollow, new UnityAction(uiController.SetTask04UI));
            UnityEventTools.AddPersistentListener(task04.EventsToFollow, new UnityAction(vfxController.DisableVFX));
            sequence.TaskList.Add(task04);

            // Save scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[PotLeakageSceneBuilder] Successfully created and saved scene: {ScenePath}");
        }
    }
}
#endif
