using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace TruckTyreReplacement.Core
{
    public enum TTSCacheStatus
    {
        Valid,
        Outdated,
        Missing
    }

    public enum TrainingPreflightState
    {
        Idle,
        Checking,
        Repairing,
        Ready,
        Failed
    }

    public interface IRuntimeTTSService { }

    public class NullRuntimeTTSService : IRuntimeTTSService { }

    public class EditorRuntimeTTSService : IRuntimeTTSService { }

    public class LocalTTSCacheService
    {
        public string CacheRootPath => Path.Combine(Application.persistentDataPath, "TTSCache");
        public string ManifestPath => Path.Combine(CacheRootPath, "cache_manifest.json");

        private readonly Dictionary<string, AudioClip> memoryClips = new Dictionary<string, AudioClip>();

        public static string NormalizeSpeechText(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return System.Text.RegularExpressions.Regex.Replace(input, @"\s+", " ").Trim();
        }

        public static string ComputeSpeechHash(string lang, string text, string voiceProfile = "")
        {
            if (string.IsNullOrEmpty(text)) return "";
            using (var sha = SHA256.Create())
            {
                string input = string.IsNullOrEmpty(voiceProfile) ? (lang + ":" + text) : (lang + ":" + voiceProfile + ":" + text);
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public TTSCacheStatus GetStatus(string key, string lang, string hash, out string expectedPath)
        {
            expectedPath = GetCacheFilePath(lang, hash);
            if (File.Exists(expectedPath)) return TTSCacheStatus.Valid;
            string saPath = Path.Combine(Application.streamingAssetsPath, "TTSCache", GetCacheFileName(lang, hash));
            if (File.Exists(saPath))
            {
                expectedPath = saPath;
                return TTSCacheStatus.Valid;
            }
            string projPath = Path.Combine(Application.dataPath, "PotLeakage", "Audio", "TTS", GetCacheFileName(lang, hash));
            if (File.Exists(projPath))
            {
                expectedPath = projPath;
                return TTSCacheStatus.Valid;
            }
            return TTSCacheStatus.Missing;
        }

        public string GetCacheFileName(string lang, string hash) => $"{lang}_{hash}.wav";

        public string GetCacheFilePath(string lang, string hash) => Path.Combine(CacheRootPath, GetCacheFileName(lang, hash));

        public bool TryGetMemoryClip(string lang, string hash, out AudioClip clip)
        {
            return memoryClips.TryGetValue($"{lang}_{hash}", out clip);
        }

        public void SetMemoryClip(string lang, string hash, AudioClip clip)
        {
            memoryClips[$"{lang}_{hash}"] = clip;
        }

        public void InvalidateMemoryClip(string lang, string hash)
        {
            memoryClips.Remove($"{lang}_{hash}");
        }

        public AudioClip LoadClipFromDisk(string lang, string hash, string clipName)
        {
            string path = GetCacheFilePath(lang, hash);
            if (!File.Exists(path))
            {
                string saPath = Path.Combine(Application.streamingAssetsPath, "TTSCache", GetCacheFileName(lang, hash));
                if (File.Exists(saPath)) path = saPath;
            }
            if (!File.Exists(path))
            {
                string projPath = Path.Combine(Application.dataPath, "PotLeakage", "Audio", "TTS", GetCacheFileName(lang, hash));
                if (File.Exists(projPath)) path = projPath;
            }
            if (!File.Exists(path)) return null;
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                return WavToAudioClip(bytes, clipName, out _);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TTS CACHE] Failed to read clip from disk: {ex.Message}");
                return null;
            }
        }

        public AudioClip WavToAudioClip(byte[] bytes, string clipName, out string failureReason)
        {
            failureReason = null;
            if (bytes == null || bytes.Length < 44)
            {
                failureReason = "Byte array too small for WAV header.";
                return null;
            }
            try
            {
                int channels = BitConverter.ToInt16(bytes, 22);
                int sampleRate = BitConverter.ToInt32(bytes, 24);
                int bitsPerSample = BitConverter.ToInt16(bytes, 34);

                int pos = 12;
                while (pos < bytes.Length - 8)
                {
                    string id = Encoding.ASCII.GetString(bytes, pos, 4);
                    int size = BitConverter.ToInt32(bytes, pos + 4);
                    if (id == "data")
                    {
                        pos += 8;
                        int bytesPerSample = bitsPerSample / 8;
                        if (bytesPerSample <= 0) bytesPerSample = 2;
                        int sampleCount = size / bytesPerSample;
                        float[] samples = new float[sampleCount];
                        if (bitsPerSample == 16)
                        {
                            for (int i = 0; i < sampleCount; i++)
                            {
                                short val = BitConverter.ToInt16(bytes, pos + i * 2);
                                samples[i] = val / 32768f;
                            }
                        }
                        else if (bitsPerSample == 32)
                        {
                            for (int i = 0; i < sampleCount; i++)
                            {
                                samples[i] = BitConverter.ToSingle(bytes, pos + i * 4);
                            }
                        }
                        else
                        {
                            failureReason = $"Unsupported bitsPerSample: {bitsPerSample}";
                            return null;
                        }

                        // Ensure a clean lead-in silence (50-70ms) to eliminate initial clipping from DSP ramp-in.
                        int leadCheckSamples = Mathf.Min(sampleCount, Mathf.RoundToInt(sampleRate * 0.040f) * channels);
                        bool hasInitialSilence = true;
                        for (int i = 0; i < leadCheckSamples; i++)
                        {
                            if (Mathf.Abs(samples[i]) > 0.015f)
                            {
                                hasInitialSilence = false;
                                break;
                            }
                        }

                        if (!hasInitialSilence)
                        {
                            int paddingSamplesPerChannel = Mathf.RoundToInt(sampleRate * 0.065f); // 65ms silence padding
                            int totalPaddingSamples = paddingSamplesPerChannel * channels;
                            float[] paddedSamples = new float[totalPaddingSamples + sampleCount];
                            Array.Copy(samples, 0, paddedSamples, totalPaddingSamples, sampleCount);

                            var clip = AudioClip.Create(clipName, (sampleCount / channels) + paddingSamplesPerChannel, channels, sampleRate, false);
                            clip.SetData(paddedSamples, 0);
                            return clip;
                        }
                        else
                        {
                            var clip = AudioClip.Create(clipName, sampleCount / channels, channels, sampleRate, false);
                            clip.SetData(samples, 0);
                            return clip;
                        }
                    }
                    pos += 8 + size;
                }
                failureReason = "data chunk not found";
                return null;
            }
            catch (Exception ex)
            {
                failureReason = ex.Message;
                return null;
            }
        }

        public static bool GenerateWav(string filePath, string text, string voiceProfile = "")
        {
            if (string.IsNullOrEmpty(text)) return false;
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string escapedText = text.Replace("'", "''").Replace("\"", "`\"");
                string normalizedPath = filePath.Replace("\\", "/");

                bool isShift = (!string.IsNullOrEmpty(voiceProfile) && 
                    voiceProfile.IndexOf("Shift", StringComparison.OrdinalIgnoreCase) >= 0);

                bool isMark = (!string.IsNullOrEmpty(voiceProfile) && 
                    (voiceProfile.IndexOf("Mark", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     voiceProfile.IndexOf("Technical", StringComparison.OrdinalIgnoreCase) >= 0));

                bool isDavid = (!string.IsNullOrEmpty(voiceProfile) && 
                    (voiceProfile.IndexOf("David", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     voiceProfile.IndexOf("Male", StringComparison.OrdinalIgnoreCase) >= 0));

                bool isZira = !isMark && !isDavid;

                string targetVoice = isMark ? "Microsoft Mark Desktop" : (isDavid ? "Microsoft David Desktop" : "Microsoft Zira Desktop");
                string genderHint = (isMark || isDavid) ? "[System.Speech.Synthesis.VoiceGender]::Male" : "[System.Speech.Synthesis.VoiceGender]::Female";

                string script = 
                    "Add-Type -AssemblyName System.Speech; " +
                    "$s = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
                    "$s.Rate = -1; " +
                    "try { $s.SelectVoice('" + targetVoice + "'); } catch { " +
                        "try { $s.SelectVoiceByHints(" + genderHint + "); } catch {} " +
                    "}; " +
                    "$s.SetOutputToWaveFile('" + normalizedPath + "'); " +
                    "$s.Speak('" + escapedText + "'); " +
                    "$s.Dispose();";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + script + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var proc = System.Diagnostics.Process.Start(psi))
                {
                    proc.WaitForExit(5000);
                    return File.Exists(filePath) && new FileInfo(filePath).Length > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TTS GENERATION FAILED] {ex.Message}");
                return false;
            }
        }

        public void WriteClipBytes(string lang, string hash, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return;
            string dir = CacheRootPath;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(GetCacheFilePath(lang, hash), bytes);
        }

        [Serializable]
        private class ManifestRoot
        {
            public List<ManifestEntry> entries = new List<ManifestEntry>();
        }

        [Serializable]
        private class ManifestEntry
        {
            public string key;
            public string language;
            public string hash;
            public string fileName;
            public string text;
        }

        private ManifestRoot manifestCache;

        public void UpsertManifestEntry(string key, string lang, string hash, string fileName, string text)
        {
            LoadManifest();
            if (manifestCache == null) manifestCache = new ManifestRoot();
            var existing = manifestCache.entries.Find(e => e.key == key && e.language == lang);
            if (existing != null)
            {
                existing.hash = hash;
                existing.fileName = fileName;
                existing.text = text;
            }
            else
            {
                manifestCache.entries.Add(new ManifestEntry { key = key, language = lang, hash = hash, fileName = fileName, text = text });
            }
            SaveManifest();
        }

        public void LoadManifest(bool forceReload = false)
        {
            if (manifestCache != null && !forceReload) return;
            string path = ManifestPath;
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    manifestCache = JsonUtility.FromJson<ManifestRoot>(json);
                }
                catch
                {
                    manifestCache = new ManifestRoot();
                }
            }
            else
            {
                manifestCache = new ManifestRoot();
            }
        }

        public void SaveManifest()
        {
            if (manifestCache == null) return;
            try
            {
                string dir = CacheRootPath;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string json = JsonUtility.ToJson(manifestCache, true);
                File.WriteAllText(ManifestPath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TTS CACHE] Failed to save manifest: {ex.Message}");
            }
        }
    }

    public class TrainingPreflightManager : MonoBehaviour
    {
        public TrainingPreflightState State { get; private set; } = TrainingPreflightState.Ready;
        public event Action<TrainingPreflightState, string, float> OnPreflightUpdated;

        public void RunPreflight(Manager manager, Action onReady)
        {
            State = TrainingPreflightState.Ready;
            OnPreflightUpdated?.Invoke(State, "Ready", 1f);
            onReady?.Invoke();
        }
    }
}

namespace TruckTyreReplacement.UI
{
    public class TrainingHologramAnchor : MonoBehaviour
    {
        public void SetTitle(string title) { }
        public void SetDescription(string desc) { }
        public void ShowPrestartPrompt() { }
        public void ShowPreflightState(TruckTyreReplacement.Core.TrainingPreflightState state, string message, float progress) { }
    }

    public class TrainingInstructionPanel : MonoBehaviour
    {
        public void SetInstruction(string text) { }
    }
}

namespace Switch
{
    public class SwitchController : MonoBehaviour
    {
        public void ToggleSwitch() { }
    }
}

public class GrabDetect : MonoBehaviour
{
    public void ActivateGrab() { }
    public void DeactivateGrab() { }
}

public class NumericVariableController : MonoBehaviour
{
    public AudioClip targetConfirmedSFX;
    public Material defaultSelectMaterial;
}

namespace UnityEngine.XR.Interaction.Toolkit
{
    public interface IXRSelectInteractable { }

    public interface IXRInteractable
    {
        Transform transform { get; }
    }

    public class XRInteractionManager : MonoBehaviour
    {
        public void CancelInteractableSelection(IXRSelectInteractable interactable) { }
    }

    public class HoverEnterEventArgs
    {
        public IXRInteractable interactableObject { get; set; }
    }

    public class HoverExitEventArgs
    {
        public IXRInteractable interactableObject { get; set; }
    }

    public class SelectEnterEventArgs
    {
        public IXRInteractable interactableObject { get; set; }
    }

    public class SelectExitEventArgs
    {
        public IXRInteractable interactableObject { get; set; }
    }
}

namespace UnityEngine.XR.Interaction.Toolkit.Interactables
{
    public class XRBaseInteractable : MonoBehaviour, UnityEngine.XR.Interaction.Toolkit.IXRInteractable, UnityEngine.XR.Interaction.Toolkit.IXRSelectInteractable
    {
        public enum MovementType { Instantaneous, Kinematic, VelocityTracking }
        public bool isSelected { get; set; }
        public int interactionLayers { get; set; }
        public MovementType movementType { get; set; }
        public bool trackPosition { get; set; }
        public bool trackRotation { get; set; }
        public bool throwOnDetach { get; set; }
        public UnityEngine.XR.Interaction.Toolkit.XRInteractionManager interactionManager { get; set; }
        public List<Collider> colliders { get; set; } = new List<Collider>();
    }

    public class XRGrabInteractable : XRBaseInteractable { }
}
