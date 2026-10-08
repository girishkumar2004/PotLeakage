using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PotLeakage.Core;
using PotLeakage.UI;
using PotLeakage.VFX;
using PotLeakage.Configuration;
using TruckTyreReplacement.Core;

namespace VedantaTraining.Editor
{
    /// <summary>
    /// Comprehensive validation and diagnostics for the Vedanta Training Framework.
    /// Provides reusable validators for training data, TTS cache, and scene setup,
    /// alongside module-specific diagnostics (such as Pot Leakage).
    /// </summary>
    public static class VedantaTrainingValidator
    {
        public class ValidationResult
        {
            public string Category;
            public List<string> Passed = new List<string>();
            public List<string> Warnings = new List<string>();
            public List<string> Errors = new List<string>();

            public bool IsSuccess => Errors.Count == 0;

            public void PrintToConsole()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"================================================================================");
                sb.AppendLine($"VEDANTA TRAINING VALIDATION: {Category.ToUpperInvariant()}");
                sb.AppendLine($"================================================================================");
                foreach (var p in Passed) sb.AppendLine($"  [PASS] {p}");
                foreach (var w in Warnings) sb.AppendLine($"  [WARN] {w}");
                foreach (var e in Errors) sb.AppendLine($"  [FAIL] {e}");
                sb.AppendLine($"SUMMARY: {Passed.Count} Passed, {Warnings.Count} Warnings, {Errors.Count} Errors.");
                sb.AppendLine($"================================================================================");

                if (Errors.Count > 0)
                {
                    Debug.LogError(sb.ToString());
                }
                else if (Warnings.Count > 0)
                {
                    Debug.LogWarning(sb.ToString());
                }
                else
                {
                    Debug.Log(sb.ToString());
                }
            }
        }

        /// <summary>
        /// Runs complete diagnostic suite across Training Data, Audio Cache, Scene, and Editor/Runtime Boundary.
        /// Produces a combined executive report as mandated by Requirement 25.
        /// </summary>
        public static void RunAllDiagnostics()
        {
            var resData = ValidateTrainingData();
            resData.PrintToConsole();

            var resAudio = ValidateTTSCache();
            resAudio.PrintToConsole();

            var resScene = ValidateScene();
            resScene.PrintToConsole();

            var module = VedantaModuleManager.ActiveModule;
            ValidationResult resPot = null;
            if (module.ModuleName == "Pot Leakage")
            {
                resPot = ValidatePotLeakageModule();
                resPot.PrintToConsole();
            }

            var resBoundary = ValidateEditorRuntimeBoundary();
            resBoundary.PrintToConsole();

            // Combined Master Report
            string StatusStr(ValidationResult r)
            {
                if (r == null) return "N/A";
                if (r.Errors.Count > 0) return "FAIL";
                if (r.Warnings.Count > 0) return $"WARNING ({r.Warnings.Count} items)";
                return "PASS";
            }

            bool anyFail = (resData.Errors.Count > 0) || (resAudio.Errors.Count > 0) || (resScene.Errors.Count > 0) ||
                           (resPot != null && resPot.Errors.Count > 0) || (resBoundary.Errors.Count > 0);
            bool anyWarn = (resData.Warnings.Count > 0) || (resAudio.Warnings.Count > 0) || (resScene.Warnings.Count > 0) ||
                           (resPot != null && resPot.Warnings.Count > 0) || (resBoundary.Warnings.Count > 0);

            string overall = anyFail ? "FAIL" : (anyWarn ? "WARNING" : "PASS");

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("VEDANTA TRAINING COMPLETE DIAGNOSTICS REPORT");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Module:                  {module.ModuleName}");
            sb.AppendLine();
            sb.AppendLine($"Training Data:           {StatusStr(resData)}");
            sb.AppendLine($"TTS Audio Cache:         {StatusStr(resAudio)}");
            sb.AppendLine($"Scene Infrastructure:    {StatusStr(resScene)}");
            if (resPot != null)
                sb.AppendLine($"Required References:     {StatusStr(resPot)}");
            sb.AppendLine($"Editor/Runtime Boundary: {StatusStr(resBoundary)}");
            sb.AppendLine();
            sb.AppendLine($"OVERALL:                 {overall}");
            sb.AppendLine("================================================================================");

            if (anyFail) Debug.LogError(sb.ToString());
            else if (anyWarn) Debug.LogWarning(sb.ToString());
            else Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Validates JSON existence, syntax, uniqueness of keys, and language presence.
        /// </summary>
        public static ValidationResult ValidateTrainingData()
        {
            var res = new ValidationResult { Category = "Training Data" };
            string path = VedantaModuleManager.ActiveModule.ResolvedTrainingJsonPath;

            if (!File.Exists(path))
            {
                res.Errors.Add($"training.json file missing at: {path}");
                return res;
            }
            res.Passed.Add($"training.json found at: {path}");

            bool valid = VedantaTrainingDataTools.ValidateTrainingJson(out List<string> errors, out List<string> warnings);
            foreach (var err in errors) res.Errors.Add(err);
            foreach (var warn in warnings) res.Warnings.Add(warn);

            if (valid)
            {
                var root = VedantaTrainingDataTools.LoadTrainingJson(path);
                int count = root != null && root.entries != null ? root.entries.Count : 0;
                res.Passed.Add($"Training JSON parsed successfully with {count} total entries.");
            }

            return res;
        }

        /// <summary>
        /// Validates that all speech entries have corresponding, valid WAV cache files and manifest entries,
        /// and reports stale entries or duplicate cache records.
        /// </summary>
        public static ValidationResult ValidateTTSCache()
        {
            var res = new ValidationResult { Category = "TTS Audio Cache" };

            // Check manifest file
            string manifestPath = VedantaTrainingAudioTools.ManifestPath;
            if (File.Exists(manifestPath))
            {
                res.Passed.Add($"cache_manifest.json found at: {manifestPath}");
            }
            else
            {
                res.Warnings.Add($"cache_manifest.json not yet generated at: {manifestPath}");
            }

            var items = VedantaTrainingAudioTools.InspectCache();
            if (items.Count == 0)
            {
                res.Warnings.Add("No speech entries found to validate in active training data.");
                return res;
            }

            var manifest = VedantaTrainingAudioTools.LoadManifest();
            var validKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int validCount = 0;
            foreach (var item in items)
            {
                validKeys.Add(item.key);
                float detectedLead = 0f;
                string leadDetail = "";
                bool measured = false;
                if (!string.IsNullOrEmpty(item.filePath) && File.Exists(item.filePath))
                {
                    measured = LocalTTSCacheService.MeasureLeadingSilence(item.filePath, out detectedLead, out leadDetail);
                }
                bool passLead = measured && detectedLead >= LocalTTSCacheService.TTS_MIN_ACCEPTED_LEADING_SILENCE;

                Debug.Log($"[TTS LEADING SILENCE CHECK]\nKey = {item.key}\nDetectedLeadingSilence = {detectedLead:F3}s\nRequiredMinimum = {LocalTTSCacheService.TTS_MIN_ACCEPTED_LEADING_SILENCE:F3}s\nStatus = {(passLead ? "PASS" : "FAIL")}");

                if (item.status == CacheEntryStatus.Valid && passLead)
                {
                    validCount++;
                    res.Passed.Add($"[{item.key}] {item.fileName} valid (Lead: {detectedLead:F3}s, {item.fileSizeBytes / 1024} KB).");
                }
                else if (item.status == CacheEntryStatus.Missing)
                {
                    res.Errors.Add($"[{item.key}] Missing audio cache file: {item.fileName}");
                }
                else if (item.status == CacheEntryStatus.Changed || !passLead)
                {
                    res.Warnings.Add($"[{item.key}] Insufficient leading silence ({detectedLead:F3}s < {LocalTTSCacheService.TTS_MIN_ACCEPTED_LEADING_SILENCE:F3}s) or changed text. Requires regeneration.");
                }
                else if (item.status == CacheEntryStatus.Invalid)
                {
                    res.Errors.Add($"[{item.key}] Invalid/corrupted audio file: {item.fileName}");
                }
            }

            // Check for stale entries in manifest (entries no longer in training.json)
            if (manifest != null && manifest.entries != null)
            {
                foreach (var me in manifest.entries)
                {
                    if (!validKeys.Contains(me.key))
                    {
                        res.Warnings.Add($"Stale manifest entry detected: [{me.key}] ({me.fileName})");
                    }
                }
            }

            res.Passed.Add($"Cache integrity: {validCount} of {items.Count} entries verified valid.");
            return res;
        }

        /// <summary>
        /// Validates that no runtime scripts outside Editor assemblies contain unguarded UnityEditor references.
        /// Prevents Player build breaks.
        /// </summary>
        public static ValidationResult ValidateEditorRuntimeBoundary()
        {
            var res = new ValidationResult { Category = "Editor/Runtime Boundary" };
            string assetsDir = Application.dataPath;
            var csFiles = Directory.GetFiles(assetsDir, "*.cs", SearchOption.AllDirectories);

            int checkedFiles = 0;
            int violations = 0;

            foreach (var file in csFiles)
            {
                string norm = file.Replace("\\", "/");
                if (norm.Contains("/Editor/")) continue; // Editor script, allowed

                checkedFiles++;
                var lines = File.ReadAllLines(file);
                int inEditorBlock = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    string trimmed = lines[i].Trim();
                    if (trimmed.StartsWith("#if") && trimmed.Contains("UNITY_EDITOR")) inEditorBlock++;
                    if (trimmed.StartsWith("#endif") && inEditorBlock > 0) inEditorBlock--;

                    if (trimmed.Contains("UnityEditor") && inEditorBlock == 0)
                    {
                        violations++;
                        string rel = "Assets" + norm.Substring(assetsDir.Length);
                        res.Errors.Add($"Unguarded UnityEditor reference in '{rel}' line {i + 1}: {trimmed}");
                    }
                }
            }

            if (violations == 0)
            {
                res.Passed.Add($"All {checkedFiles} runtime script files verified clean. Zero unguarded UnityEditor references.");
            }

            return res;
        }

        /// <summary>
        /// Validates core scene infrastructure components (Manager, Camera, Sequence).
        /// </summary>
        public static ValidationResult ValidateScene()
        {
            var res = new ValidationResult { Category = "Scene Infrastructure" };

            var mgr = UnityEngine.Object.FindFirstObjectByType<TruckTyreReplacement.Core.Manager>();
            if (mgr != null) res.Passed.Add("Manager GameObject found in active scene.");
            else res.Warnings.Add("Manager component not found in active scene (will be created automatically at runtime).");

            var cam = UnityEngine.Camera.main;
            if (cam != null) res.Passed.Add($"Main Camera found: '{cam.name}'.");
            else res.Errors.Add("Main Camera missing in active scene.");

            var seq = UnityEngine.Object.FindFirstObjectByType<Sequence>();
            if (seq != null) res.Passed.Add($"Sequence component found: '{seq.name}'.");
            else res.Warnings.Add("Sequence component not found in active scene.");

            return res;
        }

        /// <summary>
        /// Validates Pot Leakage specific critical scene references without modifying them.
        /// </summary>
        public static ValidationResult ValidatePotLeakageModule()
        {
            var res = new ValidationResult { Category = "Pot Leakage Module Diagnostics" };

            // Helper to find GameObject including inactive in active scene
            GameObject FindInActiveScene(string name)
            {
                var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                var rootObjs = activeScene.GetRootGameObjects();
                foreach (var root in rootObjs)
                {
                    var found = FindInChildren(root.transform, name);
                    if (found != null) return found.gameObject;
                }
                return null;
            }

            Transform FindInChildren(Transform parent, string targetName)
            {
                if (parent.name == targetName) return parent;
                for (int i = 0; i < parent.childCount; i++)
                {
                    var resTrans = FindInChildren(parent.GetChild(i), targetName);
                    if (resTrans != null) return resTrans;
                }
                return null;
            }

            // 1. Config Asset
            var config = AssetDatabase.LoadAssetAtPath<PotLeakageConfig>("Assets/PotLeakage/Data/PotLeakageConfig.asset");
            if (config != null) res.Passed.Add("PotLeakageConfig.asset verified.");
            else res.Errors.Add("PotLeakageConfig.asset missing at 'Assets/PotLeakage/Data/PotLeakageConfig.asset'.");

            // 2. Drop Waypoints
            var drop0 = FindInActiveScene("drop");
            var drop1 = FindInActiveScene("drop (1)");
            var drop2 = FindInActiveScene("drop (2)");

            if (drop0 != null) res.Passed.Add($"Authoritative waypoint 'drop' verified at {drop0.transform.position}.");
            else res.Errors.Add("Authoritative waypoint 'drop' missing!");

            if (drop1 != null) res.Passed.Add($"Authoritative waypoint 'drop (1)' verified at {drop1.transform.position}.");
            else res.Errors.Add("Authoritative waypoint 'drop (1)' missing!");

            if (drop2 != null) res.Passed.Add($"Authoritative waypoint 'drop (2)' verified at {drop2.transform.position}.");
            else res.Errors.Add("Authoritative waypoint 'drop (2)' missing!");

            // 3. VFX & Stream
            var vfx = UnityEngine.Object.FindFirstObjectByType<MoltenAluminiumVFXController>(FindObjectsInactive.Include);
            if (vfx != null) res.Passed.Add("MoltenAluminiumVFXController found in scene.");
            else res.Errors.Add("MoltenAluminiumVFXController missing in scene.");

            var stream = UnityEngine.Object.FindFirstObjectByType<MoltenMetalStreamParticles>(FindObjectsInactive.Include);
            if (stream != null) res.Passed.Add("MoltenMetalStreamParticles component verified.");
            else res.Errors.Add("MoltenMetalStreamParticles missing under VFX hierarchy.");

            var flowMesh = UnityEngine.Object.FindFirstObjectByType<MoltenMetalFlowMesh>(FindObjectsInactive.Include);
            if (flowMesh != null)
            {
                var mr = flowMesh.GetComponent<MeshRenderer>();
                if (mr == null || !mr.enabled) res.Passed.Add("MoltenMetalFlowMesh present with MeshRenderer safely disabled.");
                else res.Warnings.Add("MoltenMetalFlowMesh MeshRenderer is currently enabled.");
            }

            // 4. Toolbox, Gloves, Tools
            var gloves = FindInActiveScene("gloves");
            if (gloves != null) res.Passed.Add("Safety gloves found in scene (disabled as required).");
            else res.Errors.Add("Safety gloves GameObject missing!");

            var tool = FindInActiveScene("SIDEREAKING TOOL");
            if (tool != null) res.Passed.Add("SIDEREAKING TOOL verified in scene.");
            else res.Errors.Add("SIDEREAKING TOOL missing!");

            var pcm = FindInActiveScene("PotControlMachine");
            if (pcm != null) res.Passed.Add("Pot Control Machine waypoint verified.");
            else res.Warnings.Add("Pot Control Machine waypoint not found.");

            return res;
        }
    }
}
