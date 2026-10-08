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
        public const float TTS_LEADING_SILENCE_SECONDS = 0.20f;
        public const float TTS_TRAILING_SILENCE_SECONDS = 0.10f;
        public const float TTS_MIN_ACCEPTED_LEADING_SILENCE = 0.15f;
        public const int AUDIO_PADDING_VERSION = 2;

        public string CacheRootPath => Path.Combine(Application.persistentDataPath, "TTSCache");
        public string ManifestPath => Path.Combine(CacheRootPath, "cache_manifest.json");

        private readonly Dictionary<string, AudioClip> memoryClips = new Dictionary<string, AudioClip>();

        public static bool MeasureLeadingSilence(byte[] bytes, out float leadingSilenceSec, out string detail)
        {
            leadingSilenceSec = 0f;
            detail = string.Empty;
            if (bytes == null || bytes.Length < 44)
            {
                detail = "File smaller than 44 bytes";
                return false;
            }

            try
            {
                int channels = BitConverter.ToInt16(bytes, 22);
                int sampleRate = BitConverter.ToInt32(bytes, 24);
                int bitsPerSample = BitConverter.ToInt16(bytes, 34);
                if (channels <= 0 || sampleRate <= 0 || bitsPerSample <= 0)
                {
                    detail = "Invalid WAV header parameters";
                    return false;
                }

                int pos = 12;
                while (pos < bytes.Length - 8)
                {
                    string id = Encoding.ASCII.GetString(bytes, pos, 4);
                    int size = BitConverter.ToInt32(bytes, pos + 4);
                    if (id == "data")
                    {
                        int dataOffset = pos + 8;
                        int sampleCount = size / (bitsPerSample / 8);
                        int firstSpeechSample = -1;

                        if (bitsPerSample == 16)
                        {
                            int totalShorts = size / 2;
                            for (int i = 0; i < totalShorts; i++)
                            {
                                short s = BitConverter.ToInt16(bytes, dataOffset + i * 2);
                                if (Math.Abs((int)s) > 250)
                                {
                                    firstSpeechSample = i / channels;
                                    break;
                                }
                            }
                        }
                        else if (bitsPerSample == 32)
                        {
                            int totalFloats = size / 4;
                            for (int i = 0; i < totalFloats; i++)
                            {
                                float f = BitConverter.ToSingle(bytes, dataOffset + i * 4);
                                if (Math.Abs(f) > 0.008f)
                                {
                                    firstSpeechSample = i / channels;
                                    break;
                                }
                            }
                        }

                        if (firstSpeechSample >= 0)
                        {
                            leadingSilenceSec = firstSpeechSample / (float)sampleRate;
                        }
                        else
                        {
                            leadingSilenceSec = sampleCount / (float)(sampleRate * channels);
                        }

                        detail = $"SampleRate={sampleRate}, Ch={channels}, Bits={bitsPerSample}, FirstSpeechSample={firstSpeechSample}";
                        return true;
                    }
                    pos += 8 + size;
                }

                detail = "No data chunk found";
                return false;
            }
            catch (Exception ex)
            {
                detail = ex.Message;
                return false;
            }
        }

        public static bool MeasureLeadingSilence(string filePath, out float leadingSilenceSec, out string detail)
        {
            leadingSilenceSec = 0f;
            detail = string.Empty;
            if (!File.Exists(filePath))
            {
                detail = "File not found";
                return false;
            }
            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                return MeasureLeadingSilence(bytes, out leadingSilenceSec, out detail);
            }
            catch (Exception ex)
            {
                detail = ex.Message;
                return false;
            }
        }

        public static bool ApplyPcmSilencePadding(byte[] originalBytes, out byte[] paddedBytes, string key = "")
        {
            paddedBytes = originalBytes;
            if (originalBytes == null || originalBytes.Length < 44) return false;

            try
            {
                int channels = BitConverter.ToInt16(originalBytes, 22);
                int sampleRate = BitConverter.ToInt32(originalBytes, 24);
                int bitsPerSample = BitConverter.ToInt16(originalBytes, 34);
                if (channels <= 0 || sampleRate <= 0 || bitsPerSample <= 0) return false;

                int bytesPerSample = bitsPerSample / 8;
                int blockAlign = channels * bytesPerSample;

                int pos = 12;
                int dataHeaderPos = -1;
                int dataSize = -1;
                while (pos < originalBytes.Length - 8)
                {
                    string id = Encoding.ASCII.GetString(originalBytes, pos, 4);
                    int size = BitConverter.ToInt32(originalBytes, pos + 4);
                    if (id == "data")
                    {
                        dataHeaderPos = pos;
                        dataSize = size;
                        break;
                    }
                    pos += 8 + size;
                }

                if (dataHeaderPos < 0 || dataSize <= 0) return false;

                int origDataOffset = dataHeaderPos + 8;
                if (origDataOffset + dataSize > originalBytes.Length) return false;

                // Idempotency check: if the audio already has >= 180ms of leading silence, do not double-pad!
                if (MeasureLeadingSilence(originalBytes, out float existingLead, out _))
                {
                    if (existingLead >= 0.180f)
                    {
                        return false;
                    }
                }

                int leadingBytes = Mathf.RoundToInt(sampleRate * TTS_LEADING_SILENCE_SECONDS) * blockAlign;
                int trailingBytes = Mathf.RoundToInt(sampleRate * TTS_TRAILING_SILENCE_SECONDS) * blockAlign;

                int trailingChunksOffset = origDataOffset + dataSize;
                int trailingChunksSize = originalBytes.Length - trailingChunksOffset;

                int newTotalSize = originalBytes.Length + leadingBytes + trailingBytes;
                byte[] result = new byte[newTotalSize];

                // Copy header up to data chunk start
                Buffer.BlockCopy(originalBytes, 0, result, 0, origDataOffset);

                // Update RIFF chunk size at byte 4
                int origRiffSize = BitConverter.ToInt32(originalBytes, 4);
                int newRiffSize = origRiffSize + leadingBytes + trailingBytes;
                Buffer.BlockCopy(BitConverter.GetBytes(newRiffSize), 0, result, 4, 4);

                // Update data chunk size at dataHeaderPos + 4
                int newDataSize = dataSize + leadingBytes + trailingBytes;
                Buffer.BlockCopy(BitConverter.GetBytes(newDataSize), 0, result, dataHeaderPos + 4, 4);

                // Leading silence: new byte[] is already zeroed (0x00 PCM silence)
                int destPos = origDataOffset + leadingBytes;

                // Copy original audio samples
                Buffer.BlockCopy(originalBytes, origDataOffset, result, destPos, dataSize);
                destPos += dataSize;

                // Trailing silence: already zeroed
                destPos += trailingBytes;

                // Copy any trailing chunks
                if (trailingChunksSize > 0)
                {
                    Buffer.BlockCopy(originalBytes, trailingChunksOffset, result, destPos, trailingChunksSize);
                }

                paddedBytes = result;

                float origDuration = (dataSize / (float)blockAlign) / sampleRate;
                float finalDuration = (newDataSize / (float)blockAlign) / sampleRate;

                Debug.Log($"[TTS AUDIO PADDING]\nKey = {(!string.IsNullOrEmpty(key) ? key : "AudioClip")}\nLeadingSilence = {TTS_LEADING_SILENCE_SECONDS:F3}s\nTrailingSilence = {TTS_TRAILING_SILENCE_SECONDS:F3}s\nSampleRate = {sampleRate}\nChannels = {channels}\nBitsPerSample = {bitsPerSample}\nOriginalDuration = {origDuration:F3}s\nFinalDuration = {finalDuration:F3}s");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TTS AUDIO PADDING ERROR] {ex.Message}");
                return false;
            }
        }

        public static bool ApplyPcmSilencePadding(string filePath, string key = "")
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;
            try
            {
                byte[] raw = File.ReadAllBytes(filePath);
                if (ApplyPcmSilencePadding(raw, out byte[] padded, !string.IsNullOrEmpty(key) ? key : Path.GetFileNameWithoutExtension(filePath)))
                {
                    File.WriteAllBytes(filePath, padded);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TTS AUDIO PADDING FILE ERROR] {filePath}: {ex.Message}");
                return false;
            }
        }

        public static string NormalizeSpeechText(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return System.Text.RegularExpressions.Regex.Replace(input, @"\s+", " ").Trim();
        }

        public static bool IsMaleTrainingSentence(string text, string key = "")
        {
            if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(key)) return false;
            if (!string.IsNullOrEmpty(key))
            {
                string uKey = key.ToUpperInvariant();
                if (uKey == "TASK_01" || uKey == "TASK_01_WELCOME" ||
                    uKey == "TASK_02" || uKey == "TASK_02_NORMAL_POT_OPERATION" ||
                    uKey == "TASK_03" || uKey == "TASK_04" || uKey == "TASK_05" || 
                    uKey == "TASK_05_POT_CONTROL_OBSERVATION" || uKey == "TASK_06" || 
                    uKey == "TASK_06_POT_CONTROL_WARNING" || uKey == "TASK_07" || 
                    uKey == "TASK_07_BEAM_SCALE_VERIFICATION" || uKey == "TASK_08" || 
                    uKey == "TASK_08_SAFE" || uKey == "TASK_08_SAFE_VOLTAGE" || 
                    uKey == "TASK_09" || uKey == "TASK_09_TOOLBOX" ||
                    uKey == "TASK_10" || uKey == "TASK_10_APPLY_STOPPER" || 
                    uKey == "TASK_10_STOPPER_APPLIED" || uKey.Contains("STOPPER") ||
                    uKey.Contains("COOLING") || uKey.Contains("PIPE") ||
                    uKey.Contains("FURTHER_TOOLS") || uKey.Contains("CRANE") ||
                    uKey.Contains("PTM") || uKey.Contains("POT_BREAKING") ||
                    uKey.Contains("BATH_BIN") || uKey.Contains("SIDE_BREAKING") ||
                    uKey.Contains("RED_SHELL") || uKey.Contains("HOES") || uKey.Contains("HOSE") ||
                    uKey.Contains("VOLTAGE_CHECK") || uKey.Contains("SAFER_LIMIT") ||
                    uKey.Contains("VALVE") || uKey.Contains("COOL_AIR") || uKey.Contains("FLR"))
                    return true;
            }
            if (!string.IsNullOrEmpty(text))
            {
                string clean = NormalizeSpeechText(text);
                if (clean.Contains("Welcome to the Vedanta Pot Leakage") ||
                    clean.Contains("Emergency Response Procedure for Side Shell Leakage") ||
                    clean.Contains("reduction pots operate continuously") ||
                    clean.Contains("Normal Pot Operation") ||
                    clean.Contains("Pot leakage has occurred in Pot 69") ||
                    clean.Contains("call the Superintendent in such situation") ||
                    clean.Contains("Observe the voltage value") ||
                    clean.Contains("Verify the beam level") ||
                    clean.Contains("Select the Side Breaking Tool") ||
                    clean.Contains("carefully apply the stopper") ||
                    clean.Contains("arrange the cooling pipes") ||
                    clean.Contains("further tools in order to reduce or stop") ||
                    clean.Contains("voltage is now at an appropriate level") ||
                    clean.Contains("stopper has been applied") ||
                    clean.Contains("complete the leakage-control step") ||
                    clean.Contains("Click the highlighted cooling pipes") ||
                    clean.Contains("Cooling gas is now being directed") ||
                    clean.Contains("reducing the intensity of the molten metal") ||
                    clean.Contains("intensity is reduced but not stopped") ||
                    clean.Contains("arrange PTM crane") ||
                    clean.Contains("pot breaking process") ||
                    clean.Contains("Bath bin is also arranged") ||
                    clean.Contains("Side breaking above the leakage") ||
                    clean.Contains("Observe the red shell formation") ||
                    clean.Contains("use hoes to reduce the red shell") ||
                    clean.Contains("Pick up the hoes") ||
                    clean.Contains("Pick up the hose") ||
                    clean.Contains("voltage is within the safer limit") ||
                    clean.Contains("Place the hoes pipe near the red shell area") ||
                    clean.Contains("Open the FLR block valve") ||
                    clean.Contains("Now that the valve is open, cool air is released") ||
                    clean.Contains("Let us observe the red shell. It still requires more cooling") ||
                    clean.Contains("Let us start bath packing") ||
                    clean.Contains("Maintain safe distance to avoid splashing") ||
                    clean.Contains("Packing is done successfully") ||
                    clean.Contains("Check if the voltage is within the safer limit range") ||
                    clean.Contains("Check if the leakage and the shell formation is completely disappeared"))
                {
                    return true;
                }
            }
            return false;
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

        public AudioClip LoadClipFromPath(string path, string clipName)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                return WavToAudioClip(bytes, clipName, out _);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TTS CACHE] Failed to read clip from path '{path}': {ex.Message}");
                return null;
            }
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
            return LoadClipFromPath(path, clipName);
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
                if (string.IsNullOrEmpty(voiceProfile) && IsMaleTrainingSentence(text))
                {
                    voiceProfile = "Technical_Mark";
                }
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
                    proc.WaitForExit(7000);
                    if (File.Exists(filePath) && new FileInfo(filePath).Length > 44)
                    {
                        string key = Path.GetFileNameWithoutExtension(filePath);
                        ApplyPcmSilencePadding(filePath, key);
                        return true;
                    }
                    return false;
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
            public int audioPaddingVersion = AUDIO_PADDING_VERSION;
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
            public int paddingVersion = AUDIO_PADDING_VERSION;
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
                existing.paddingVersion = AUDIO_PADDING_VERSION;
            }
            else
            {
                manifestCache.entries.Add(new ManifestEntry { key = key, language = lang, hash = hash, fileName = fileName, text = text, paddingVersion = AUDIO_PADDING_VERSION });
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
