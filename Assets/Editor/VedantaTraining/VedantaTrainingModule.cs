using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace VedantaTraining.Editor
{
    /// <summary>
    /// Descriptor for an industrial training visualization module.
    /// Enables the Vedanta Training Framework to support multiple training modules
    /// (e.g. Pot Leakage, Truck Tyre Replacement, etc.) without hard-coding paths.
    /// </summary>
    public class VedantaTrainingModule
    {
        public string ModuleName { get; set; }
        public string ModuleRootPath { get; set; }
        public string ScenePath { get; set; }
        public string SourceTrainingJsonPath { get; set; }
        public string ConfigAssetPath { get; set; }
        public string AudioCachePath { get; set; }
        public Action OnRunVerification { get; set; }
        public Action OnBuildScene { get; set; }

        public string ResolvedTrainingJsonPath
        {
            get
            {
                if (!string.IsNullOrEmpty(SourceTrainingJsonPath) && File.Exists(SourceTrainingJsonPath))
                {
                    return SourceTrainingJsonPath;
                }
                string persistent = Path.Combine(Application.persistentDataPath, "TrainingData", "training.json");
                if (File.Exists(persistent)) return persistent;
                return SourceTrainingJsonPath;
            }
        }
    }

    /// <summary>
    /// Registry managing available Vedanta training modules.
    /// Defaults to Pot Leakage while allowing seamless extension for future modules.
    /// </summary>
    public static class VedantaModuleManager
    {
        private static readonly List<VedantaTrainingModule> registeredModules = new List<VedantaTrainingModule>();
        private static int activeModuleIndex = 0;

        static VedantaModuleManager()
        {
            RegisterDefaultModules();
        }

        public static void RegisterDefaultModules()
        {
            registeredModules.Clear();

            // 1. Current Canonical Module: Pot Leakage
            registeredModules.Add(new VedantaTrainingModule
            {
                ModuleName = "Pot Leakage",
                ModuleRootPath = "Assets/PotLeakage",
                ScenePath = "Assets/PotLeakage/Scenes/PotLeakage_Main.unity",
                SourceTrainingJsonPath = "Assets/PotLeakage/Data/training.json",
                ConfigAssetPath = "Assets/PotLeakage/Data/PotLeakageConfig.asset",
                AudioCachePath = "Assets/StreamingAssets/TTSCache",
                OnRunVerification = () => PotLeakage.Editor.PotLeakageVerificationTest.RunTest(),
                OnBuildScene = () => PotLeakage.Editor.PotLeakageSceneBuilder.BuildScene()
            });

            // 2. Extensible Module Slot: Truck Tyre Replacement (ready for future modules)
            string ttrScene = "Assets/Scenes/TruckTyreReplacement_Main.unity";
            if (File.Exists(ttrScene))
            {
                registeredModules.Add(new VedantaTrainingModule
                {
                    ModuleName = "Truck Tyre Replacement",
                    ModuleRootPath = "Assets",
                    ScenePath = ttrScene,
                    SourceTrainingJsonPath = "Assets/Data/training.json",
                    ConfigAssetPath = "",
                    AudioCachePath = "Assets/StreamingAssets/TTSCache"
                });
            }
        }

        public static IReadOnlyList<VedantaTrainingModule> RegisteredModules
        {
            get
            {
                if (registeredModules.Count == 0) RegisterDefaultModules();
                return registeredModules;
            }
        }

        public static VedantaTrainingModule ActiveModule
        {
            get
            {
                if (registeredModules.Count == 0) RegisterDefaultModules();
                if (activeModuleIndex < 0 || activeModuleIndex >= registeredModules.Count)
                {
                    activeModuleIndex = 0;
                }
                return registeredModules[activeModuleIndex];
            }
        }

        public static int ActiveModuleIndex
        {
            get => activeModuleIndex;
            set
            {
                if (value >= 0 && value < registeredModules.Count)
                {
                    activeModuleIndex = value;
                }
            }
        }

        public static void SetActiveModuleByName(string name)
        {
            for (int i = 0; i < registeredModules.Count; i++)
            {
                if (string.Equals(registeredModules[i].ModuleName, name, StringComparison.OrdinalIgnoreCase))
                {
                    activeModuleIndex = i;
                    return;
                }
            }
        }
    }
}
