using System;
using UnityEngine;
using UnityEditor;

namespace VedantaTraining.Editor
{
    /// <summary>
    /// Consolidated menu system for the Vedanta Industrial Training Visualization Framework.
    /// Unifies previously fragmented top-level menus (PotLeakage, Vedanta Pot Leakage,
    /// Vedanta Training Data, Vedanta) into a coherent, organized hierarchy.
    /// </summary>
    public static class VedantaTrainingMenu
    {
        // =========================================================================
        // 1. TRAINING DATA
        // =========================================================================
        [MenuItem("Vedanta/Training Data/Open Training Data (JSON)", priority = 100)]
        public static void OpenTrainingData()
        {
            VedantaTrainingDataTools.OpenTrainingJsonInEditor();
        }

        [MenuItem("Vedanta/Training Data/Deploy Training JSON", priority = 101)]
        public static void DeployTrainingJson()
        {
            VedantaTrainingDataTools.DeployTrainingJson();
        }

        [MenuItem("Vedanta/Training Data/Reload Training Data", priority = 102)]
        public static void ReloadTrainingData()
        {
            VedantaTrainingDataTools.ReloadTrainingData();
        }

        [MenuItem("Vedanta/Training Data/Validate Training JSON", priority = 103)]
        public static void ValidateTrainingJson()
        {
            var res = VedantaTrainingValidator.ValidateTrainingData();
            res.PrintToConsole();
        }

        // =========================================================================
        // 2. AUDIO / TTS
        // =========================================================================
        [MenuItem("Vedanta/Audio / TTS/Generate Missing Audio", priority = 201)]
        public static void GenerateMissingAudio()
        {
            VedantaTrainingAudioTools.GenerateMissingAudio();
        }

        [MenuItem("Vedanta/Audio / TTS/Update Changed Audio", priority = 202)]
        public static void UpdateChangedAudio()
        {
            VedantaTrainingAudioTools.UpdateChangedAudio();
        }

        [MenuItem("Vedanta/Audio / TTS/Regenerate All Audio...", priority = 203)]
        public static void RegenerateAllAudio()
        {
            VedantaTrainingAudioTools.RegenerateAll(skipConfirmation: false);
        }

        [MenuItem("Vedanta/Audio / TTS/Refresh Cache Manifest", priority = 204)]
        public static void RefreshCacheManifest()
        {
            VedantaTrainingAudioTools.SyncManifestFiles();
            Debug.Log("[VedantaTraining] TTS cache manifest synchronized.");
        }

        // =========================================================================
        // 3. DIAGNOSTICS & VALIDATION
        // =========================================================================
        [MenuItem("Vedanta/Diagnostics & Validation/Run Complete Training Diagnostics", priority = 300)]
        public static void RunAllDiagnostics()
        {
            VedantaTrainingValidator.RunAllDiagnostics();
        }

        [MenuItem("Vedanta/Diagnostics & Validation/Validate Training JSON", priority = 301)]
        public static void ValidateJsonFromDiag()
        {
            VedantaTrainingValidator.ValidateTrainingData().PrintToConsole();
        }

        [MenuItem("Vedanta/Diagnostics & Validation/Validate TTS Audio Cache", priority = 302)]
        public static void ValidateTTSFromDiag()
        {
            VedantaTrainingValidator.ValidateTTSCache().PrintToConsole();
        }

        [MenuItem("Vedanta/Diagnostics & Validation/Validate Active Scene", priority = 303)]
        public static void ValidateSceneFromDiag()
        {
            VedantaTrainingValidator.ValidateScene().PrintToConsole();
        }

        // =========================================================================
        // 4. POT LEAKAGE MODULE TOOLS
        // =========================================================================
        [MenuItem("Vedanta/Modules/Pot Leakage/Open Pot Leakage Configuration", priority = 400)]
        public static void OpenPotLeakageConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<PotLeakage.Configuration.PotLeakageConfig>("Assets/PotLeakage/Data/PotLeakageConfig.asset");
            if (config != null)
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }
            else
            {
                Debug.LogError("[VedantaTraining] PotLeakageConfig.asset not found at 'Assets/PotLeakage/Data/PotLeakageConfig.asset'.");
            }
        }

        [MenuItem("Vedanta/Modules/Pot Leakage/Run Canonical Progression Test", priority = 401)]
        public static void RunPotLeakageCanonicalTest()
        {
            PotLeakage.Editor.PotLeakageFlowArchitectureTest.RunIntegrationTestMenu();
        }

        [MenuItem("Vedanta/Modules/Pot Leakage/Run Complete Verification Test", priority = 402)]
        public static void RunPotLeakageVerificationTest()
        {
            PotLeakage.Editor.PotLeakageVerificationTest.RunTest();
        }

        [MenuItem("Vedanta/Modules/Pot Leakage/Run Task 11 Manual Mode Test", priority = 403)]
        public static void RunPotLeakageTask11Test()
        {
            PotLeakage.Editor.PotLeakageTask11ManualModeTest.RunTest();
        }

        [MenuItem("Vedanta/Modules/Pot Leakage/Build Pot Leakage Scene", priority = 404)]
        public static void BuildPotLeakageScene()
        {
            PotLeakage.Editor.PotLeakageSceneBuilder.BuildScene();
        }

        [MenuItem("Vedanta/Modules/Pot Leakage/Build Pot Control Display", priority = 405)]
        public static void BuildPotControlDisplay()
        {
            PotLeakage.Editor.BuildPotControlDisplay.Build();
        }

        [MenuItem("Vedanta/Modules/Pot Leakage/Build Walkie Communication Canvas", priority = 406)]
        public static void BuildWalkieCommunicationCanvas()
        {
            PotLeakage.Editor.BuildWalkieCommunicationCanvas.Build();
        }

        [MenuItem("Vedanta/Modules/Pot Leakage/Generate Molten Flow Textures", priority = 407)]
        public static void GenerateMoltenFlowTextures()
        {
            PotLeakage.Editor.PotLeakageTextureGenerator.GenerateTextures();
        }
    }
}
