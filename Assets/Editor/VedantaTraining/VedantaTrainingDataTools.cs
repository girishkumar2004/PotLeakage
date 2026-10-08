using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VedantaTraining.Editor
{
    /// <summary>
    /// Reusable training data operations for the Vedanta Industrial Training Framework.
    /// Manages training.json deployment, validation, and runtime synchronization.
    /// </summary>
    public static class VedantaTrainingDataTools
    {
        [Serializable]
        public class TrainingJsonRoot
        {
            public List<TrainingJsonEntry> entries = new List<TrainingJsonEntry>();
        }

        [Serializable]
        public class TrainingJsonEntry
        {
            public string key;
            public string voiceProfile;
            public LanguageTextMap display;
            public LanguageTextMap speech;
        }

        [Serializable]
        public class LanguageTextMap
        {
            public string en;
            public string hi;
            public string or;

            public string GetText(string languageCode)
            {
                if (string.IsNullOrEmpty(languageCode)) return en ?? "";
                string lower = languageCode.ToLowerInvariant().Trim();
                if (lower == "hi" || lower == "hindi") return hi ?? "";
                if (lower == "or" || lower == "odia" || lower == "oriya") return or ?? "";
                return en ?? "";
            }
        }

        public static string GetPersistentTrainingJsonPath()
        {
            return Path.Combine(Application.persistentDataPath, "TrainingData", "training.json");
        }

        /// <summary>
        /// Deploys the active module's source training.json to the authoritative persistent runtime location.
        /// </summary>
        public static bool DeployTrainingJson(string customSourcePath = null, bool notifyRuntime = true)
        {
            var module = VedantaModuleManager.ActiveModule;
            string sourcePath = !string.IsNullOrEmpty(customSourcePath) ? customSourcePath : module.ResolvedTrainingJsonPath;
            string targetPath = GetPersistentTrainingJsonPath();

            if (!File.Exists(sourcePath))
            {
                Debug.LogError($"[VedantaTraining] Cannot deploy: Source training.json not found at '{sourcePath}'");
                return false;
            }

            try
            {
                string targetDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                File.Copy(sourcePath, targetPath, true);
                Debug.Log($"[VedantaTraining] Successfully deployed training.json from '{sourcePath}' to '{targetPath}'");

                if (notifyRuntime && Application.isPlaying)
                {
                    var mgr = TruckTyreReplacement.Core.Manager.Instance;
                    if (mgr != null)
                    {
                        mgr.ReloadTrainingData();
                        Debug.Log("[VedantaTraining] Runtime Manager reloaded training data successfully.");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VedantaTraining] Error deploying training.json: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Triggers reload of training data in the active runtime Manager if in play mode,
        /// or validates in-memory parsing in edit mode.
        /// </summary>
        public static void ReloadTrainingData()
        {
            if (Application.isPlaying)
            {
                var mgr = TruckTyreReplacement.Core.Manager.Instance;
                if (mgr != null)
                {
                    mgr.ReloadTrainingData();
                    Debug.Log("[VedantaTraining] Runtime Manager training data reloaded.");
                }
                else
                {
                    Debug.LogWarning("[VedantaTraining] Manager.Instance not found in active scene.");
                }
            }
            else
            {
                string path = VedantaModuleManager.ActiveModule.ResolvedTrainingJsonPath;
                if (File.Exists(path))
                {
                    var root = LoadTrainingJson(path);
                    int count = root != null && root.entries != null ? root.entries.Count : 0;
                    Debug.Log($"[VedantaTraining] Training data parsed successfully from '{path}' ({count} entries).");
                }
                else
                {
                    Debug.LogWarning($"[VedantaTraining] Training data not found at '{path}'.");
                }
            }
        }

        /// <summary>
        /// Opens the current module's training.json file in the OS default editor.
        /// </summary>
        public static void OpenTrainingJsonInEditor()
        {
            string path = VedantaModuleManager.ActiveModule.ResolvedTrainingJsonPath;
            if (File.Exists(path))
            {
                EditorUtility.OpenWithDefaultApp(Path.GetFullPath(path));
            }
            else
            {
                Debug.LogError($"[VedantaTraining] Training JSON file does not exist at '{path}'.");
            }
        }

        /// <summary>
        /// Loads and parses the training.json file into strongly-typed data.
        /// </summary>
        public static TrainingJsonRoot LoadTrainingJson(string path = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = VedantaModuleManager.ActiveModule.ResolvedTrainingJsonPath;
            }

            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                return JsonUtility.FromJson<TrainingJsonRoot>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VedantaTraining] Failed to parse training JSON at '{path}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validates the training JSON structure, duplicate keys, and multilingual completeness.
        /// </summary>
        public static bool ValidateTrainingJson(out List<string> errors, out List<string> warnings)
        {
            errors = new List<string>();
            warnings = new List<string>();

            string path = VedantaModuleManager.ActiveModule.ResolvedTrainingJsonPath;
            if (!File.Exists(path))
            {
                errors.Add($"Training JSON not found at path: {path}");
                return false;
            }

            TrainingJsonRoot root = null;
            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                root = JsonUtility.FromJson<TrainingJsonRoot>(json);
            }
            catch (Exception ex)
            {
                errors.Add($"JSON Parsing Exception: {ex.Message}");
                return false;
            }

            if (root == null || root.entries == null || root.entries.Count == 0)
            {
                errors.Add("Training JSON contains no entries or is empty.");
                return false;
            }

            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < root.entries.Count; i++)
            {
                var entry = root.entries[i];
                if (string.IsNullOrWhiteSpace(entry.key))
                {
                    errors.Add($"Entry at index {i} has empty or missing 'key'.");
                    continue;
                }

                if (!seenKeys.Add(entry.key))
                {
                    warnings.Add($"Duplicate key detected: '{entry.key}' (found multiple times in training.json).");
                }

                bool hasDisplay = entry.display != null && !string.IsNullOrWhiteSpace(entry.display.en);
                bool hasSpeech = entry.speech != null && !string.IsNullOrWhiteSpace(entry.speech.en);

                if (!hasDisplay && !hasSpeech)
                {
                    warnings.Add($"Key '{entry.key}' has both empty English display and speech text.");
                }
            }

            return errors.Count == 0;
        }
    }
}
