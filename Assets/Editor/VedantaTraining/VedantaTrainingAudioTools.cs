using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TruckTyreReplacement.Core;

namespace VedantaTraining.Editor
{
    public enum CacheEntryStatus
    {
        Valid,
        Missing,
        Changed,
        Stale,
        Invalid,
        Error
    }

    [Serializable]
    public class AudioCacheItem
    {
        public string key;
        public string language = "English";
        public string text;
        public string voiceProfile = "";
        public string hash;
        public string fileName;
        public string filePath;
        public CacheEntryStatus status;
        public bool isSelected = false;
        public long fileSizeBytes = 0;
    }

    /// <summary>
    /// Content-aware audio cache management for the Vedanta Training Framework.
    /// Operates directly on the existing LocalTTSCacheService architecture,
    /// supporting missing generation, changed-audio updates, atomic regeneration,
    /// manifest synchronization, and Editor audio preview.
    /// </summary>
    public static class VedantaTrainingAudioTools
    {
        public static string PrimaryCacheDirectory => Path.Combine(Application.streamingAssetsPath, "TTSCache");
        public static string PersistentCacheDirectory => Path.Combine(Application.persistentDataPath, "TTSCache");

        public static string ManifestPath => Path.Combine(PrimaryCacheDirectory, "cache_manifest.json");
        public static string PersistentManifestPath => Path.Combine(PersistentCacheDirectory, "cache_manifest.json");

        [Serializable]
        public class ManifestRoot
        {
            public int audioPaddingVersion = LocalTTSCacheService.AUDIO_PADDING_VERSION;
            public List<ManifestEntry> entries = new List<ManifestEntry>();
        }

        [Serializable]
        public class ManifestEntry
        {
            public string key;
            public string language;
            public string hash;
            public string fileName;
            public string text;
            public int paddingVersion = LocalTTSCacheService.AUDIO_PADDING_VERSION;
        }

        /// <summary>
        /// Derives the appropriate voice profile based on entry voiceProfile, training sentence, or key naming conventions.
        /// </summary>
        public static string ResolveVoiceProfile(string key, string text = "", string explicitProfile = "")
        {
            if (!string.IsNullOrEmpty(explicitProfile)) return explicitProfile;
            if (LocalTTSCacheService.IsMaleTrainingSentence(text, key)) return "Technical_Mark";
            if (string.IsNullOrEmpty(key)) return "";
            string upper = key.ToUpperInvariant();
            if (upper.Contains("SHIFT")) return "Shift_InCharge";
            if (upper.Contains("TECH") || upper.Contains("MARK")) return "Technical_Mark";
            if (upper.Contains("DAVID") || upper.Contains("MALE")) return "David";
            return "";
        }

        /// <summary>
        /// Evaluates all training speech items against the current cache and manifest.
        /// </summary>
        public static List<AudioCacheItem> InspectCache()
        {
            var results = new List<AudioCacheItem>();
            var jsonRoot = VedantaTrainingDataTools.LoadTrainingJson();
            if (jsonRoot == null || jsonRoot.entries == null) return results;

            var manifest = LoadManifest();
            var manifestMap = new Dictionary<string, ManifestEntry>(StringComparer.OrdinalIgnoreCase);
            if (manifest != null && manifest.entries != null)
            {
                foreach (var me in manifest.entries)
                {
                    string mKey = $"{me.language}_{me.key}";
                    manifestMap[mKey] = me;
                }
            }

            string primaryDir = PrimaryCacheDirectory;
            if (!Directory.Exists(primaryDir)) Directory.CreateDirectory(primaryDir);

            // Supported languages
            string[] languages = new string[] { "English" };

            foreach (var entry in jsonRoot.entries)
            {
                if (string.IsNullOrEmpty(entry.key)) continue;

                foreach (var lang in languages)
                {
                    string speechText = entry.speech != null ? entry.speech.GetText(lang) : "";
                    if (string.IsNullOrWhiteSpace(speechText))
                    {
                        speechText = entry.display != null ? entry.display.GetText(lang) : "";
                    }

                    if (string.IsNullOrWhiteSpace(speechText)) continue;

                    string voiceProfile = ResolveVoiceProfile(entry.key, speechText, entry.voiceProfile);
                    string cleanText = LocalTTSCacheService.NormalizeSpeechText(speechText);
                    string hash = LocalTTSCacheService.ComputeSpeechHash(lang, cleanText, voiceProfile);
                    string fileName = $"{lang}_{hash}.wav";
                    string expectedPath = Path.Combine(primaryDir, fileName);

                    var item = new AudioCacheItem
                    {
                        key = entry.key,
                        language = lang,
                        text = cleanText,
                        voiceProfile = voiceProfile,
                        hash = hash,
                        fileName = fileName,
                        filePath = expectedPath,
                        isSelected = false
                    };

                    string mKey = $"{lang}_{entry.key}";
                    bool hasManifest = manifestMap.TryGetValue(mKey, out ManifestEntry me);

                    bool existsPrimary = File.Exists(expectedPath);
                    string persistentPath = Path.Combine(PersistentCacheDirectory, fileName);
                    bool existsPersistent = File.Exists(persistentPath);

                    if (existsPrimary || existsPersistent)
                    {
                        string foundPath = existsPrimary ? expectedPath : persistentPath;
                        item.filePath = foundPath;
                        var fi = new FileInfo(foundPath);
                        item.fileSizeBytes = fi.Length;

                        if (fi.Length < 44)
                        {
                            item.status = CacheEntryStatus.Invalid;
                        }
                        else
                        {
                            float detectedLead;
                            string leadDetail;
                            bool measured = LocalTTSCacheService.MeasureLeadingSilence(foundPath, out detectedLead, out leadDetail);
                            if (measured && detectedLead < LocalTTSCacheService.TTS_MIN_ACCEPTED_LEADING_SILENCE)
                            {
                                item.status = CacheEntryStatus.Changed; // Insufficient leading silence, requires regeneration
                            }
                            else
                            {
                                item.status = CacheEntryStatus.Valid;
                            }
                        }
                    }
                    else
                    {
                        if (hasManifest && me.hash != hash)
                        {
                            item.status = CacheEntryStatus.Changed;
                        }
                        else
                        {
                            item.status = CacheEntryStatus.Missing;
                        }
                    }

                    results.Add(item);
                }
            }

            return results;
        }

        /// <summary>
        /// Generates only missing WAV files. Does not regenerate existing valid audio.
        /// </summary>
        public static string GenerateMissingAudio(Action<float, string> progress = null)
        {
            var items = InspectCache();
            int total = items.Count;
            int existing = 0;
            int missing = 0;
            int generated = 0;
            int failed = 0;

            var toGenerate = new List<AudioCacheItem>();
            foreach (var item in items)
            {
                if (item.status == CacheEntryStatus.Missing)
                {
                    missing++;
                    toGenerate.Add(item);
                }
                else if (item.status == CacheEntryStatus.Valid)
                {
                    existing++;
                }
            }

            for (int i = 0; i < toGenerate.Count; i++)
            {
                var item = toGenerate[i];
                float p = (float)(i + 1) / Mathf.Max(1, toGenerate.Count);
                progress?.Invoke(p, $"Generating [{item.key}] ({i + 1}/{toGenerate.Count})...");

                if (GenerateAudioAtomically(item))
                {
                    generated++;
                    UpsertManifest(item.key, item.language, item.hash, item.fileName, item.text);
                }
                else
                {
                    failed++;
                }
            }

            SyncManifestFiles();
            AssetDatabase.Refresh();

            string report = $"TTS CACHE REPORT — GENERATE MISSING\n" +
                            $"Total entries: {total}\n" +
                            $"Existing valid: {existing}\n" +
                            $"Missing detected: {missing}\n" +
                            $"Successfully generated: {generated}\n" +
                            $"Failed: {failed}";

            Debug.Log($"[VedantaTraining] {report}");
            return report;
        }

        /// <summary>
        /// Identifies changed speech texts, generates replacement audio safely without deleting
        /// the old audio prematurely, and updates the manifest.
        /// </summary>
        public static string UpdateChangedAudio(Action<float, string> progress = null)
        {
            var items = InspectCache();
            var changedList = new List<AudioCacheItem>();

            foreach (var item in items)
            {
                if (item.status == CacheEntryStatus.Changed)
                {
                    changedList.Add(item);
                }
            }

            int changed = changedList.Count;
            int regenerated = 0;
            int failed = 0;

            for (int i = 0; i < changedList.Count; i++)
            {
                var item = changedList[i];
                float p = (float)(i + 1) / Mathf.Max(1, changedList.Count);
                progress?.Invoke(p, $"Updating [{item.key}] ({i + 1}/{changedList.Count})...");

                if (GenerateAudioAtomically(item))
                {
                    regenerated++;
                    UpsertManifest(item.key, item.language, item.hash, item.fileName, item.text);
                }
                else
                {
                    failed++;
                }
            }

            SyncManifestFiles();
            AssetDatabase.Refresh();

            string report = $"TTS CACHE REPORT — UPDATE CHANGED\n" +
                            $"Total entries: {items.Count}\n" +
                            $"Changed detected: {changed}\n" +
                            $"Regenerated: {regenerated}\n" +
                            $"Failed: {failed}";

            Debug.Log($"[VedantaTraining] {report}");
            return report;
        }

        /// <summary>
        /// Regenerates specifically selected items from the cache inspection list.
        /// </summary>
        public static string RegenerateSelected(List<AudioCacheItem> selectedItems, Action<float, string> progress = null)
        {
            if (selectedItems == null || selectedItems.Count == 0)
            {
                return "No audio items selected for regeneration.";
            }

            int regenerated = 0;
            int failed = 0;

            for (int i = 0; i < selectedItems.Count; i++)
            {
                var item = selectedItems[i];
                float p = (float)(i + 1) / selectedItems.Count;
                progress?.Invoke(p, $"Regenerating [{item.key}] ({i + 1}/{selectedItems.Count})...");

                if (GenerateAudioAtomically(item))
                {
                    regenerated++;
                    UpsertManifest(item.key, item.language, item.hash, item.fileName, item.text);
                }
                else
                {
                    failed++;
                }
            }

            SyncManifestFiles();
            AssetDatabase.Refresh();

            string report = $"TTS CACHE REPORT — REGENERATE SELECTED\n" +
                            $"Selected: {selectedItems.Count}\n" +
                            $"Successfully regenerated: {regenerated}\n" +
                            $"Failed: {failed}";

            Debug.Log($"[VedantaTraining] {report}");
            return report;
        }

        /// <summary>
        /// Regenerates all audio with an explicit confirmation guard.
        /// </summary>
        public static string RegenerateAll(bool skipConfirmation = false, Action<float, string> progress = null)
        {
            if (!skipConfirmation)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Regenerate All Audio",
                    "Regenerate all TTS audio clips for the current training module?\n\nThis will re-synthesize all speech entries. Existing valid files will be safely replaced only upon successful synthesis.",
                    "Yes, Regenerate All",
                    "Cancel"
                );

                if (!confirmed) return "Regeneration canceled by user.";
            }

            var items = InspectCache();
            return RegenerateSelected(items, progress);
        }

        /// <summary>
        /// Atomically synthesizes speech into a temporary file first, verifies file integrity,
        /// and only then copies it to the target location. This guarantees the old valid WAV
        /// is never lost if synthesis fails.
        /// </summary>
        private static bool GenerateAudioAtomically(AudioCacheItem item)
        {
            string targetPath = Path.Combine(PrimaryCacheDirectory, item.fileName);
            string tempPath = Path.Combine(PrimaryCacheDirectory, item.fileName + ".tmp");

            try
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);

                bool success = LocalTTSCacheService.GenerateWav(tempPath, item.text, item.voiceProfile);
                if (success && File.Exists(tempPath) && new FileInfo(tempPath).Length > 44)
                {
                    // Copy to primary StreamingAssets/TTSCache
                    File.Copy(tempPath, targetPath, true);
                    File.Delete(tempPath);

                    // Also mirror to persistentDataPath/TTSCache
                    string persistentTarget = Path.Combine(PersistentCacheDirectory, item.fileName);
                    string persistentDir = Path.GetDirectoryName(persistentTarget);
                    if (!Directory.Exists(persistentDir)) Directory.CreateDirectory(persistentDir);
                    File.Copy(targetPath, persistentTarget, true);

                    return true;
                }
                else
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                    Debug.LogWarning($"[VedantaTraining] Synthesis failed for key '{item.key}'. Previous file retained.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
                Debug.LogError($"[VedantaTraining] Synthesis exception for '{item.key}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reads cache_manifest.json from StreamingAssets or persistentDataPath.
        /// </summary>
        public static ManifestRoot LoadManifest()
        {
            string path = ManifestPath;
            if (!File.Exists(path)) path = PersistentManifestPath;
            if (!File.Exists(path)) return new ManifestRoot();

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                return JsonUtility.FromJson<ManifestRoot>(json) ?? new ManifestRoot();
            }
            catch
            {
                return new ManifestRoot();
            }
        }

        /// <summary>
        /// Upserts an entry into cache_manifest.json.
        /// </summary>
        public static void UpsertManifest(string key, string lang, string hash, string fileName, string text)
        {
            var root = LoadManifest();
            root.audioPaddingVersion = LocalTTSCacheService.AUDIO_PADDING_VERSION;
            var entry = root.entries.Find(e => e.key == key && e.language == lang && (e.hash == hash || e.text == text))
                     ?? root.entries.Find(e => e.key == key && e.language == lang);
            if (entry != null)
            {
                entry.hash = hash;
                entry.fileName = fileName;
                entry.text = text;
                entry.paddingVersion = LocalTTSCacheService.AUDIO_PADDING_VERSION;
            }
            else
            {
                root.entries.Add(new ManifestEntry
                {
                    key = key,
                    language = lang,
                    hash = hash,
                    fileName = fileName,
                    text = text,
                    paddingVersion = LocalTTSCacheService.AUDIO_PADDING_VERSION
                });
            }

            SaveManifest(root);
        }

        public static void SaveManifest(ManifestRoot root)
        {
            if (root == null) return;
            string json = JsonUtility.ToJson(root, true);

            string dir1 = Path.GetDirectoryName(ManifestPath);
            if (!Directory.Exists(dir1)) Directory.CreateDirectory(dir1);
            File.WriteAllText(ManifestPath, json, Encoding.UTF8);

            string dir2 = Path.GetDirectoryName(PersistentManifestPath);
            if (!Directory.Exists(dir2)) Directory.CreateDirectory(dir2);
            File.WriteAllText(PersistentManifestPath, json, Encoding.UTF8);
        }

        public static void SyncManifestFiles()
        {
            var manifest = LoadManifest();
            var items = InspectCache();
            foreach (var item in items)
            {
                if (item.status == CacheEntryStatus.Valid)
                {
                    var existing = manifest.entries.Find(e => e.key == item.key && e.language == item.language && (e.hash == item.hash || e.text == item.text))
                                ?? manifest.entries.Find(e => e.key == item.key && e.language == item.language && e.hash == item.hash);
                    if (existing != null)
                    {
                        existing.hash = item.hash;
                        existing.fileName = item.fileName;
                        existing.text = item.text;
                    }
                    else
                    {
                        manifest.entries.Add(new ManifestEntry
                        {
                            key = item.key,
                            language = item.language,
                            hash = item.hash,
                            fileName = item.fileName,
                            text = item.text
                        });
                    }
                }
            }
            SaveManifest(manifest);
        }

        /// <summary>
        /// Editor audio preview using internal AudioUtil without affecting scene audio sources.
        /// </summary>
        public static void PlayPreview(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[VedantaTraining] Cannot preview audio: File does not exist at '{filePath}'");
                return;
            }

            StopPreview();

            // Load as AudioClip asset or convert bytes
            string relPath = filePath.Replace("\\", "/");
            if (relPath.StartsWith(Application.dataPath.Replace("\\", "/")))
            {
                relPath = "Assets" + relPath.Substring(Application.dataPath.Length);
            }

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(relPath);
            if (clip == null)
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(filePath);
                    var svc = new LocalTTSCacheService();
                    clip = svc.WavToAudioClip(bytes, Path.GetFileNameWithoutExtension(filePath), out string err);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[VedantaTraining] Error decoding preview WAV: {ex.Message}");
                }
            }

            if (clip != null)
            {
                PlayAudioClipEditor(clip);
            }
        }

        public static void StopPreview()
        {
            StopAudioClipEditor();
        }

        private static void PlayAudioClipEditor(AudioClip clip)
        {
            try
            {
                var assembly = typeof(AudioImporter).Assembly;
                var audioUtil = assembly.GetType("UnityEditor.AudioUtil");
                if (audioUtil != null)
                {
                    var playMethod = audioUtil.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)
                                  ?? audioUtil.GetMethod("PlayClip", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);

                    if (playMethod != null)
                    {
                        playMethod.Invoke(null, new object[] { clip, 0, false });
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VedantaTraining] AudioUtil preview reflection notice: {ex.Message}");
            }
        }

        private static void StopAudioClipEditor()
        {
            try
            {
                var assembly = typeof(AudioImporter).Assembly;
                var audioUtil = assembly.GetType("UnityEditor.AudioUtil");
                if (audioUtil != null)
                {
                    var stopMethod = audioUtil.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public)
                                  ?? audioUtil.GetMethod("StopAllClips", BindingFlags.Static | BindingFlags.Public);

                    if (stopMethod != null)
                    {
                        stopMethod.Invoke(null, null);
                    }
                }
            }
            catch
            {
                // Fallback quiet
            }
        }
    }
}
