using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TruckTyreReplacement.Core
{
    public enum LocalTTSLanguage
    {
        English = 0,
        Hindi = 1,
        Odia = 2
    }

    [System.Serializable]
    public class TranslationEntry
    {
        public string key;

        [TextArea(2, 5)]
        public string englishDisplay;

        [TextArea(2, 5)]
        public string englishSpeech;

        [TextArea(2, 5)]
        public string hindiDisplay;

        [TextArea(2, 5)]
        public string hindiSpeech;

        [TextArea(2, 5)]
        public string odiaDisplay;

        [TextArea(2, 5)]
        public string odiaSpeech;
    }

    [System.Serializable]
    public class LanguageFontEntry
    {
        public LocalTTSLanguage language;
        public TMP_FontAsset font;
    }

    /// <summary>
    /// Generic Manager for VR Training Modules.
    /// Handles VR Configuration, Multilingual Localization (driven by an
    /// external training.json), Language Selection, generic Inspector-driven
    /// fonts, and externally-cached offline TTS audio. Contains no
    /// module-specific (tyre/vehicle/etc.) assumptions - reusable across
    /// future training modules by changing external data and Inspector
    /// configuration only.
    /// </summary>
    [AddComponentMenu("Vedanta/Manager")]
    public class Manager : MonoBehaviour
    {
        private static Manager _instance;
        public static Manager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindFirstObjectByType<Manager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("Manager");
                        _instance = go.AddComponent<Manager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }
        public static Manager instance => Instance;

        [Header("VR CONFIGURATION")]
        [Tooltip("[REQUIRED] The XR Origin (VR) GameObject in the scene")]
        [SerializeField] private GameObject xrOrigin;

        [Tooltip("[REQUIRED] The Main Camera inside the XR Origin")]
        [SerializeField] private Camera mainCamera;

        [Tooltip("[REQUIRED] Left Controller GameObject")]
        [SerializeField] private GameObject leftController;

        [Tooltip("[REQUIRED] Right Controller GameObject")]
        [SerializeField] private GameObject rightController;

        [Header("LANGUAGE CONFIGURATION")]
        [SerializeField] private LocalTTSLanguage currentLanguage = LocalTTSLanguage.English;
        [SerializeField] private bool alwaysAskLanguageOnStartup = true;
#pragma warning disable 0414
        [SerializeField] private bool speakOnStartup = false;
#pragma warning restore 0414

        [Header("AUDIO CONFIGURATION")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;
        [Range(0f, 1f)]
        [SerializeField] private float voiceVolume = 1.0f;
        [SerializeField] private bool interruptCurrentSpeech = true;

        [Header("FONTS")]
        [Tooltip("Generic language -> font mapping. Order does not matter; the language value determines the font. Add/reorder/replace entries freely.")]
        [SerializeField] private List<LanguageFontEntry> languageFonts = new List<LanguageFontEntry>();
        [Tooltip("Used when a language has no assigned font. May be left empty.")]
        [SerializeField] private TMP_FontAsset fallbackFont;

        [Header("LOCALIZED UI TARGETS")]
        [Tooltip("TMP_Text components whose font is updated on language change. Populated automatically at startup from the current scene; register/unregister dynamically-created UI via RegisterLocalizedText/UnregisterLocalizedText.")]
        [SerializeField] private List<TMP_Text> localizedTextTargets = new List<TMP_Text>();

        [Header("TRAINING DATA (runtime representation of external training.json)")]
        [Tooltip("Populated at runtime from Application.persistentDataPath/TrainingData/training.json. Not authoritative - editing here does not persist.")]
        [SerializeField] private List<TranslationEntry> translationDatabase = new List<TranslationEntry>();

        [Header("JSON MONITORING")]
        [SerializeField] private bool monitorExternalTrainingJson = true;
        [SerializeField] private float jsonCheckInterval = 2f;

        [Header("DEBUG")]
        [SerializeField] private bool debugMode = false;

        // ── TTS Cache Service ────────────────────────────────
        private readonly LocalTTSCacheService ttsCache = new LocalTTSCacheService();

        private struct SpeechRequest
        {
            public string key;
            public string text;
            public string voiceProfile;
            public float pitch;
            public int version;
        }

        private int speechVersion = 0;
        private readonly Queue<SpeechRequest> speechQueue = new Queue<SpeechRequest>();
        private Coroutine speechCoroutine;
        private bool isSpeaking = false;
        private string lastSpokenText = "";
        private string lastSpokenKey = "";
        public string LastSpokenKey => lastSpokenKey;

        // ── JSON state ───────────────────────────────────────
        private string lastTrainingJsonContentHash = "";

        // ── UI State ─────────────────────────────────────────
        private GameObject languageSelectionPanel;
        private bool sequenceStarted = false;
        public bool IsLanguageSelected { get; private set; } = false;
        public LocalTTSLanguage CurrentLanguage => currentLanguage;

        // ── Training preflight/preparation ──────────────────
        private TrainingPreflightManager preflightManager;
        public bool IsTrainingStarted { get; private set; } = false;
        public TrainingPreflightState PreflightState => preflightManager != null ? preflightManager.State : TrainingPreflightState.Idle;

        // Default is the safe, always-unavailable stub - no Player build may ever
        // depend on Python/subprocess/ONNX. A future real on-device backend can be
        // assigned here (e.g. by another component in Awake) without any change to
        // Manager or TrainingPreflightManager.
        private IRuntimeTTSService runtimeTTSService = new NullRuntimeTTSService();
        public IRuntimeTTSService RuntimeTTSService
        {
            get => runtimeTTSService;
            set => runtimeTTSService = value ?? new NullRuntimeTTSService();
        }
        public bool IsSpeaking => isSpeaking;

        public event Action<LocalTTSLanguage> OnLanguageChanged;
        public static event Action<string, string> OnSpeechCompleted;

        public string TrainingJsonPath => Path.Combine(Application.persistentDataPath, "TrainingData", "training.json");

        public static string GetLanguageCode(LocalTTSLanguage lang)
        {
            switch (lang)
            {
                case LocalTTSLanguage.Hindi: return "hi";
                case LocalTTSLanguage.Odia: return "or";
                default: return "en";
            }
        }

        private void Awake()
        {
            if (_instance == null || _instance == this)
            {
                _instance = this;
                if (transform.parent != null) transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
                InitializeManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeManager()
        {
#if UNITY_EDITOR
            if (runtimeTTSService is NullRuntimeTTSService)
            {
                runtimeTTSService = new EditorRuntimeTTSService();
            }
#endif
            preflightManager = GetComponent<TrainingPreflightManager>();
            if (preflightManager == null)
            {
                preflightManager = gameObject.AddComponent<TrainingPreflightManager>();
            }

            // Setup voice AudioSource
            if (voiceAudioSource == null)
            {
                voiceAudioSource = GetComponent<AudioSource>();
                if (voiceAudioSource == null)
                {
                    voiceAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            voiceAudioSource.spatialBlend = 0.0f; // 2D speech
            voiceAudioSource.volume = voiceVolume;
            voiceAudioSource.playOnAwake = false;
            voiceAudioSource.loop = false;

            EnsureLanguageFonts();

            // Populate translation database from external JSON if empty
            if (translationDatabase == null) translationDatabase = new List<TranslationEntry>();
            if (translationDatabase.Count == 0)
            {
                if (!File.Exists(TrainingJsonPath))
                {
                    string fallbackAsset = Path.Combine(Application.dataPath, "PotLeakage", "Data", "training.json");
                    if (File.Exists(fallbackAsset))
                    {
                        string dir = Path.GetDirectoryName(TrainingJsonPath);
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        File.Copy(fallbackAsset, TrainingJsonPath, true);
                    }
                }
                LoadTrainingDataFromDisk(logIfMissing: true);
            }

            PrepopulateFonts();

            // Ensure player is at IntroductionPoint from the very start
            var introPoint = GameObject.Find("IntroductionPoint")?.transform;
            if (introPoint != null)
            {
                MovePlayerTo(introPoint);
            }
        }

        private void Start()
        {
            // Deferred from Awake/InitializeManager: at Awake time other scene
            // objects are not yet reliably reported as scene-loaded, which made
            // this silently discover 0 targets. Start() runs after every
            // Awake() in the scene, so the scan is reliable here.
            AutoDiscoverLocalizedTargets();

            StartCoroutine(StartupLanguageRoutine());

            if (monitorExternalTrainingJson)
            {
                StartCoroutine(JsonMonitorRoutine());
            }
        }

        private IEnumerator StartupLanguageRoutine()
        {
            // Ensure player is at IntroductionPoint while selecting language
            var introPoint = GameObject.Find("IntroductionPoint")?.transform;
            if (introPoint != null)
            {
                MovePlayerTo(introPoint);
            }

            bool hasLanguageKey = PlayerPrefs.HasKey("TrainingLanguage");
            int savedLangIndex = PlayerPrefs.GetInt("TrainingLanguage", (int)currentLanguage);

            if (alwaysAskLanguageOnStartup || !hasLanguageKey)
            {
                IsLanguageSelected = false;
                CreateLanguageSelectionPanel();
                if (languageSelectionPanel != null)
                {
                    languageSelectionPanel.SetActive(true);
                }
            }
            else
            {
                SetLanguage((LocalTTSLanguage)savedLangIndex);
            }
            yield return null;
        }

        private IEnumerator JsonMonitorRoutine()
        {
            while (monitorExternalTrainingJson)
            {
                yield return new WaitForSeconds(Mathf.Max(0.5f, jsonCheckInterval));
                ReloadTrainingDataIfChanged();
            }
        }

        // ─────────────────────────────────────────────────────
        // LANGUAGE & FONT MANAGEMENT
        // ─────────────────────────────────────────────────────

        public void SetLanguage(LocalTTSLanguage lang)
        {
            currentLanguage = lang;
            PlayerPrefs.SetInt("TrainingLanguage", (int)lang);
            PlayerPrefs.Save();
            IsLanguageSelected = true;

            if (debugMode) Debug.Log($"[Manager] Selected Language: {lang} (code: {GetLanguageCode(lang)})");

            ApplyFontForLanguage(lang);

            // Preload audio into memory cache for instant playback
            PreloadLanguageAudio(lang);

            if (languageSelectionPanel != null)
            {
                languageSelectionPanel.SetActive(false);
            }

            var canvas = FindTrainingInstructionCanvas();
            if (canvas != null)
            {
                var panelTrans = canvas.transform.Find("Panel");
                if (panelTrans != null)
                {
                    panelTrans.gameObject.SetActive(true);
                }
            }

            OnLanguageChanged?.Invoke(lang);

            // The sequence no longer starts immediately here - a training
            // preflight/preparation check must report Ready first. See
            // BeginTrainingPreflightIfNeeded(), triggered by the NextButton.
            if (!sequenceStarted)
            {
                var anchor = FindTrainingHologramAnchor();
                if (anchor != null)
                {
                    anchor.ShowPrestartPrompt();
                }
            }
        }

        // ─────────────────────────────────────────────────────
        // TRAINING PREFLIGHT / PREPARATION
        // ─────────────────────────────────────────────────────

        private TruckTyreReplacement.UI.TrainingHologramAnchor FindTrainingHologramAnchor()
        {
            return UnityEngine.Object.FindFirstObjectByType<TruckTyreReplacement.UI.TrainingHologramAnchor>();
        }

        /// <summary>
        /// Entry point for the NextButton once a language has been selected but
        /// training has not yet started. Runs the preflight check and, only on
        /// success, starts SequenceHandler. Never touches SequenceHandler itself
        /// before that - SequenceHandler.Init() is the sole progression entry
        /// point, called exactly once, only after Ready.
        /// </summary>
        public void BeginTrainingPreflightIfNeeded()
        {
            if (IsTrainingStarted) return;

            var anchor = FindTrainingHologramAnchor();

            if (preflightManager == null)
            {
                // No preflight component available - do not block training entirely.
                StartTrainingSequence();
                return;
            }

            if (anchor != null)
            {
                preflightManager.OnPreflightUpdated -= anchor.ShowPreflightState;
                preflightManager.OnPreflightUpdated += anchor.ShowPreflightState;
            }

            preflightManager.RunPreflight(this, StartTrainingSequence);
        }

        private void StartTrainingSequence()
        {
            if (IsTrainingStarted) return;
            if (!sequenceStarted && SequenceHandler.instance != null)
            {
                sequenceStarted = true;
                IsTrainingStarted = true;

                // The preflight UI temporarily used the title field ("READY", etc.);
                // clear it before real task content begins, since no task in the
                // normal flow otherwise manages the title.
                var anchor = FindTrainingHologramAnchor();
                if (anchor != null) anchor.SetTitle("");

                SequenceHandler.instance.Init();
            }
        }

        /// <summary>
        /// All (key, language) pairs in the current translation database that
        /// have non-empty speech text - the generic checklist a preflight check
        /// iterates. Contains no module-specific keys.
        /// </summary>
        public List<(string key, string language)> GetTrainingContentCheckItems()
        {
            var items = new List<(string key, string language)>();
            if (translationDatabase == null) return items;

            foreach (var entry in translationDatabase)
            {
                foreach (LocalTTSLanguage lang in Enum.GetValues(typeof(LocalTTSLanguage)))
                {
                    string speech = GetSpeechTextForLanguage(entry, lang);
                    if (string.IsNullOrEmpty(speech)) continue;
                    items.Add((entry.key, lang.ToString()));
                }
            }
            return items;
        }

        /// <summary>
        /// Resolves the current cache status for one (key, language) pair using
        /// the same hashing/manifest logic Speak() uses - no second implementation.
        /// </summary>
        public TTSCacheStatus CheckOneEntry(string key, string language)
        {
            var entry = translationDatabase?.Find(e => string.Equals(e.key, key, StringComparison.OrdinalIgnoreCase));
            if (entry == null || !Enum.TryParse<LocalTTSLanguage>(language, out var lang)) return TTSCacheStatus.Missing;

            string speech = GetSpeechTextForLanguage(entry, lang);
            string clean = LocalTTSCacheService.NormalizeSpeechText(speech);
            string hash = LocalTTSCacheService.ComputeSpeechHash(language, clean);
            return ttsCache.GetStatus(key, language, hash, out _);
        }

        /// <summary>One (key, language) entry's full audio-readiness data, used by the preflight repair pipeline.</summary>
        public struct TrainingAudioCheckItem
        {
            public string key;
            public string language;
            public string normalizedSpeech;
            public string hash;
            public string expectedPath;
            public TTSCacheStatus status;
        }

        /// <summary>
        /// Richer version of GetTrainingContentCheckItems/CheckOneEntry that
        /// returns hash/expected-path/status in one pass, for the preflight
        /// repair pipeline. Reuses the exact same hashing/status logic - no
        /// second implementation.
        /// </summary>
        public List<TrainingAudioCheckItem> GetTrainingAudioCheckItems()
        {
            var items = new List<TrainingAudioCheckItem>();
            if (translationDatabase == null) return items;

            foreach (var entry in translationDatabase)
            {
                foreach (LocalTTSLanguage lang in Enum.GetValues(typeof(LocalTTSLanguage)))
                {
                    string speech = GetSpeechTextForLanguage(entry, lang);
                    if (string.IsNullOrEmpty(speech)) continue;

                    string langName = lang.ToString();
                    string norm = LocalTTSCacheService.NormalizeSpeechText(speech);
                    string hash = LocalTTSCacheService.ComputeSpeechHash(langName, norm);
                    var status = ttsCache.GetStatus(entry.key, langName, hash, out string expectedPath);

                    items.Add(new TrainingAudioCheckItem
                    {
                        key = entry.key,
                        language = langName,
                        normalizedSpeech = norm,
                        hash = hash,
                        expectedPath = expectedPath,
                        status = status
                    });
                }
            }
            return items;
        }

        /// <summary>Scratch directory for in-progress runtime generation, under the same cache root used at runtime.</summary>
        public string GetTempGenerationDirectory()
        {
            string dir = Path.Combine(ttsCache.CacheRootPath, "_generating");
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Validates a freshly generated WAV and, only if valid, commits it into
        /// the cache and updates the manifest for every key that shares this
        /// exact (language, hash) - preserving content-addressed deduplication
        /// (e.g. welcome/step0 sharing identical text must both become ready
        /// from the one physical file, without generating it twice).
        /// </summary>
        public bool TryCommitGeneratedAudio(IEnumerable<string> keysSharingThisHash, string language, string hash, string tempWavPath, string speechText, out string failureReason)
        {
            failureReason = null;

            if (!File.Exists(tempWavPath))
            {
                failureReason = "Generated file not found.";
                return false;
            }

            // Ensure PCM silence padding is applied before cache commit
            LocalTTSCacheService.ApplyPcmSilencePadding(tempWavPath);

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(tempWavPath);
            }
            catch (Exception ex)
            {
                failureReason = "Failed to read generated file: " + ex.Message;
                return false;
            }

            var clip = ttsCache.WavToAudioClip(bytes, $"{language}_{hash}", out string wavFailure);
            if (clip == null)
            {
                failureReason = "Generated WAV failed validation: " + wavFailure;
                return false;
            }
            if (clip.length <= 0f)
            {
                failureReason = "Generated WAV validated but has zero length.";
                return false;
            }

            ttsCache.WriteClipBytes(language, hash, bytes);
            string fileName = ttsCache.GetCacheFileName(language, hash);

            foreach (var key in keysSharingThisHash)
            {
                ttsCache.UpsertManifestEntry(key, language, hash, fileName, speechText);
            }
            ttsCache.SaveManifest();

            foreach (var key in keysSharingThisHash)
            {
                var verify = ttsCache.GetStatus(key, language, hash, out _);
                if (verify != TTSCacheStatus.Valid)
                {
                    failureReason = $"Post-generation verification for '{key}' reported {verify} instead of Valid.";
                    return false;
                }
            }

            return true;
        }

        private void EnsureLanguageFonts()
        {
            if (languageFonts != null && languageFonts.Count > 0) return;
            if (languageFonts == null) languageFonts = new List<LanguageFontEntry>();

            TMP_FontAsset english = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            TMP_FontAsset hindi = null;
            TMP_FontAsset odia = null;

#if UNITY_EDITOR
            if (english == null)
            {
                english = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }
            hindi = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LocalTTS/Fonts/NotoSansDevanagari SDF.asset");
            odia = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LocalTTS/Fonts/NotoSansOdia SDF.asset");
#endif

            languageFonts.Add(new LanguageFontEntry { language = LocalTTSLanguage.English, font = english });
            languageFonts.Add(new LanguageFontEntry { language = LocalTTSLanguage.Hindi, font = hindi });
            languageFonts.Add(new LanguageFontEntry { language = LocalTTSLanguage.Odia, font = odia });

            if (debugMode) Debug.Log("[FONTS] languageFonts list was empty - auto-populated default entries.");
        }

        /// <summary>
        /// Resolves the font for a language by searching languageFonts by
        /// VALUE, never by list index/order. Never throws: falls back to
        /// fallbackFont (which may itself be null) and logs a warning
        /// instead of crashing.
        /// </summary>
        public TMP_FontAsset GetFontForLanguage(LocalTTSLanguage language)
        {
            EnsureLanguageFonts();

            TMP_FontAsset resolved = null;
            int matchCount = 0;

            if (languageFonts != null)
            {
                foreach (var entry in languageFonts)
                {
                    if (entry == null || entry.language != language) continue;
                    matchCount++;
                    if (resolved == null && entry.font != null)
                    {
                        resolved = entry.font;
                    }
                }
            }

            if (matchCount > 1)
            {
                Debug.LogWarning($"[FONTS] Duplicate languageFonts entries found for language '{language}'. Using the first assigned font found.");
            }

            if (resolved == null)
            {
                Debug.LogWarning($"[FONTS] No font assigned for language '{language}'.{(fallbackFont != null ? " Using fallbackFont." : " No fallbackFont configured either.")}");
                resolved = fallbackFont;
            }

            return resolved;
        }

        private void PrepopulateFonts()
        {
            if (translationDatabase == null) return;

            var sb = new StringBuilder(" SELECT LANGUAGE English हिन्दी ଓଡ଼ିଆ ");
            foreach (var entry in translationDatabase)
            {
                sb.Append(entry.hindiDisplay).Append(" ").Append(entry.hindiSpeech).Append(" ");
                sb.Append(entry.odiaDisplay).Append(" ").Append(entry.odiaSpeech).Append(" ");
            }

            string fullText = sb.ToString();

            var hindiFontAsset = GetFontForLanguage(LocalTTSLanguage.Hindi);
            var odiaFontAsset = GetFontForLanguage(LocalTTSLanguage.Odia);

            if (hindiFontAsset != null)
            {
                hindiFontAsset.TryAddCharacters(fullText, true);
            }
            if (odiaFontAsset != null)
            {
                odiaFontAsset.TryAddCharacters(fullText, true);
            }
        }

        /// <summary>
        /// Applies the font for the given language to every registered
        /// localized text target. Does not scan the whole scene - see
        /// AutoDiscoverLocalizedTargets/RegisterLocalizedText.
        /// </summary>
        public void ApplyFontForLanguage(LocalTTSLanguage lang)
        {
            TMP_FontAsset targetFont = GetFontForLanguage(lang);

            if (targetFont == null)
            {
                Debug.LogWarning($"[FONTS] ApplyFontForLanguage: no font resolved for {lang}; registered targets keep their current font.");
                return;
            }

            int applied = 0;
            for (int i = localizedTextTargets.Count - 1; i >= 0; i--)
            {
                var txt = localizedTextTargets[i];
                if (txt == null)
                {
                    localizedTextTargets.RemoveAt(i);
                    continue;
                }
                txt.font = targetFont;
                applied++;
            }

            Debug.Log($"[FONTS]\nLanguage = {lang}\nFont = {targetFont.name}\nTargets updated = {applied}");
        }

        private void AutoDiscoverLocalizedTargets()
        {
            var allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            foreach (var txt in allTexts)
            {
                if (txt == null || !txt.gameObject.scene.IsValid() || !txt.gameObject.scene.isLoaded) continue;

                if (txt.transform.parent != null &&
                    (txt.transform.parent.name == "EnglishButton" ||
                     txt.transform.parent.name == "HindiButton" ||
                     txt.transform.parent.name == "OdiaButton" ||
                     txt.transform.parent.name == "LanguageSelectionPanel"))
                {
                    continue;
                }

                RegisterLocalizedText(txt);
            }

            if (debugMode) Debug.Log($"[FONTS] Auto-discovered {localizedTextTargets.Count} localized text target(s) at startup.");
        }

        public void RegisterLocalizedText(TMP_Text text)
        {
            if (text == null) return;
            if (localizedTextTargets.Contains(text)) return;
            localizedTextTargets.Add(text);
        }

        public void UnregisterLocalizedText(TMP_Text text)
        {
            if (text == null) return;
            localizedTextTargets.Remove(text);
        }

        // ─────────────────────────────────────────────────────
        // TRANSLATION LOOKUP API
        // ─────────────────────────────────────────────────────

        public void GetTranslation(string key, out string displayText, out string speechText)
        {
            displayText = "";
            speechText = "";

            if (string.IsNullOrEmpty(key) || translationDatabase == null) return;
            if (string.Equals(key, "correct_pressed", StringComparison.OrdinalIgnoreCase)) key = "correct_pressure";

            var entry = translationDatabase.Find(e => string.Equals(e.key, key, StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                switch (currentLanguage)
                {
                    case LocalTTSLanguage.Hindi:
                        displayText = !string.IsNullOrEmpty(entry.hindiDisplay) ? entry.hindiDisplay : entry.englishDisplay;
                        speechText = !string.IsNullOrEmpty(entry.hindiSpeech) ? entry.hindiSpeech : entry.englishSpeech;
                        break;
                    case LocalTTSLanguage.Odia:
                        displayText = !string.IsNullOrEmpty(entry.odiaDisplay) ? entry.odiaDisplay : entry.englishDisplay;
                        speechText = !string.IsNullOrEmpty(entry.odiaSpeech) ? entry.odiaSpeech : entry.englishSpeech;
                        break;
                    default:
                        displayText = entry.englishDisplay;
                        speechText = entry.englishSpeech;
                        break;
                }
            }
            else
            {
                displayText = key;
                speechText = key;
            }
        }

        public string GetDisplayText(string key)
        {
            GetTranslation(key, out string disp, out _);
            return disp;
        }

        public string GetSpeechText(string key)
        {
            GetTranslation(key, out _, out string speech);
            return speech;
        }

        private static string GetSpeechTextForLanguage(TranslationEntry entry, LocalTTSLanguage lang)
        {
            switch (lang)
            {
                case LocalTTSLanguage.Hindi: return entry.hindiSpeech;
                case LocalTTSLanguage.Odia: return entry.odiaSpeech;
                default: return entry.englishSpeech;
            }
        }

        // ─────────────────────────────────────────────────────
        // EXTERNAL TRAINING JSON (persistentDataPath)
        // ─────────────────────────────────────────────────────

        [ContextMenu("Import Training JSON")]
        public void ImportTrainingJson()
        {
            LoadTrainingDataFromDisk(logIfMissing: true);
        }

        private bool LoadTrainingDataFromDisk(bool logIfMissing)
        {
            string path = TrainingJsonPath;

            if (!File.Exists(path))
            {
                try
                {
                    string dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[JSON] Failed to create TrainingData directory.\nPath = {path}\nException = {ex.Message}");
                }

                if (logIfMissing)
                {
                    Debug.LogWarning($"[JSON] training.json not found.\nExpected path = {path}\nUse 'Vedanta Training Data > Deploy Training JSON' in the Editor to deploy it.");
                }
                return false;
            }

            string jsonText;
            try
            {
                jsonText = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JSON] Failed to read training.json.\nPath = {path}\nException = {ex.Message}");
                return false;
            }

            if (string.IsNullOrEmpty(jsonText))
            {
                Debug.LogWarning($"[JSON] training.json is empty.\nPath = {path}");
                return false;
            }

            try
            {
                var root = JsonUtility.FromJson<JsonRootV2>(jsonText);
                if (root == null || root.entries == null)
                {
                    Debug.LogError($"[JSON] training.json parsed but contained no entries.\nPath = {path}");
                    return false;
                }

                translationDatabase.Clear();
                foreach (var raw in root.entries)
                {
                    if (raw == null || string.IsNullOrEmpty(raw.key)) continue;
                    translationDatabase.Add(new TranslationEntry
                    {
                        key = raw.key,
                        englishDisplay = raw.display?.en ?? "",
                        englishSpeech = raw.speech?.en ?? "",
                        hindiDisplay = raw.display?.hi ?? "",
                        hindiSpeech = raw.speech?.hi ?? "",
                        odiaDisplay = raw.display?.or ?? "",
                        odiaSpeech = raw.speech?.or ?? ""
                    });
                }

                lastTrainingJsonContentHash = ComputeContentHash(jsonText);

                Debug.Log($"[JSON]\nLoaded: {path}\n[LOCALIZATION]\nLoaded {translationDatabase.Count} translation entries.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JSON] Failed to parse training.json.\nPath = {path}\nException = {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Forces a reload from disk, rebuilds the translation database and
        /// re-applies fonts/audio preload for the current language. Does not
        /// restart SequenceHandler and does not change the current language.
        /// </summary>
        public void ReloadTrainingData()
        {
            var previousHashes = SnapshotSpeechHashes();

            bool loaded = LoadTrainingDataFromDisk(logIfMissing: true);
            if (!loaded) return;

            // The TTS cache manifest is memoized in-memory; an external Editor
            // tool (or another process) may have regenerated WAVs and updated
            // cache_manifest.json on disk since we last read it. Force a fresh
            // read here so status/preload reflect the current disk state.
            ttsCache.LoadManifest(forceReload: true);

            var newHashes = SnapshotSpeechHashes();
            var changedKeys = new List<string>();
            foreach (var kvp in newHashes)
            {
                if (previousHashes.TryGetValue(kvp.Key, out string oldHash) && oldHash != kvp.Value)
                {
                    int sep = kvp.Key.LastIndexOf('|');
                    string entryKey = sep >= 0 ? kvp.Key.Substring(0, sep) : kvp.Key;
                    string langName = sep >= 0 ? kvp.Key.Substring(sep + 1) : "";
                    changedKeys.Add(kvp.Key);

                    Debug.Log($"[TTS INVALIDATED]\nKey = {entryKey}\nLanguage = {langName}\nOldHash = {oldHash}\nNewHash = {kvp.Value}");

                    // The old AudioClip (keyed by language+oldHash) must never be returned
                    // again for this key now that the speech text has changed. This affects
                    // CONTENT/cache state only - it must never call into SequenceHandler or
                    // any task-completion API.
                    ttsCache.InvalidateMemoryClip(langName, oldHash);
                }
            }

            if (changedKeys.Count > 0)
            {
                Debug.Log($"[JSON RELOAD]\nChangedKeys = {string.Join(", ", changedKeys)}");
            }

            PrepopulateFonts();
            ApplyFontForLanguage(currentLanguage);
            PreloadLanguageAudio(currentLanguage);
        }

        /// <summary>
        /// Cheap periodic check: compares a content hash of the external
        /// training.json to the last-loaded hash and only reparses when it
        /// actually changed. Intended to be called on an interval, not per-frame.
        /// </summary>
        public void ReloadTrainingDataIfChanged()
        {
            string path = TrainingJsonPath;
            if (!File.Exists(path)) return;

            string jsonText;
            try
            {
                jsonText = File.ReadAllText(path, Encoding.UTF8);
            }
            catch
            {
                return;
            }

            string currentHash = ComputeContentHash(jsonText);
            if (currentHash == lastTrainingJsonContentHash) return;

            if (debugMode) Debug.Log("[JSON] External training.json change detected. Reloading.");
            ReloadTrainingData();
        }

        private Dictionary<string, string> SnapshotSpeechHashes()
        {
            var map = new Dictionary<string, string>();
            if (translationDatabase == null) return map;
            foreach (var entry in translationDatabase)
            {
                foreach (LocalTTSLanguage lang in Enum.GetValues(typeof(LocalTTSLanguage)))
                {
                    string langName = lang.ToString();
                    string speech = GetSpeechTextForLanguage(entry, lang);
                    map[entry.key + "|" + langName] = LocalTTSCacheService.ComputeSpeechHash(langName, LocalTTSCacheService.NormalizeSpeechText(speech));
                }
            }
            return map;
        }

        private static string ComputeContentHash(string content)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes) sb.Append(b.ToString("X2"));
                return sb.ToString();
            }
        }

        [Serializable] private class JsonLangText { public string en; public string hi; public string or; }
        [Serializable] private class JsonEntryRaw { public string key; public JsonLangText display; public JsonLangText speech; }
        [Serializable] private class JsonRootV2 { public List<JsonEntryRaw> entries; }

        // ─────────────────────────────────────────────────────
        // OFFLINE TTS AUDIO (externally cached, SHA-256 keyed)
        // ─────────────────────────────────────────────────────

        private void PreloadLanguageAudio(LocalTTSLanguage lang)
        {
            if (translationDatabase == null) return;
            string langName = lang.ToString();

            foreach (var entry in translationDatabase)
            {
                string speechText = GetSpeechTextForLanguage(entry, lang);
                if (string.IsNullOrEmpty(speechText)) continue;

                string clean = LocalTTSCacheService.NormalizeSpeechText(speechText);
                string hash = LocalTTSCacheService.ComputeSpeechHash(langName, clean);

                var status = ttsCache.GetStatus(entry.key, langName, hash, out string expectedPath);

                if (debugMode)
                {
                    Debug.Log($"[TTS CACHE]\nKey = {entry.key}\nLanguage = {langName}\nHash = {hash}\nFile = {Path.GetFileName(expectedPath)}\nStatus = {status.ToString().ToUpperInvariant()}");
                }

                if (status == TTSCacheStatus.Valid && !ttsCache.TryGetMemoryClip(langName, hash, out _))
                {
                    var clip = ttsCache.LoadClipFromDisk(langName, hash, $"{langName}_{hash}");
                    if (clip != null)
                    {
                        ttsCache.SetMemoryClip(langName, hash, clip);
                    }
                }
            }
        }

        public void SpeakText(string text, string key = "")
        {
            string profile = LocalTTSCacheService.IsMaleTrainingSentence(text, key) ? "Technical_Mark" : "";
            SpeakText(text, key, profile, 1.0f);
        }

        public void SpeakText(string text, string key, string voiceProfile, float pitch = 1.0f)
        {
            if (string.IsNullOrEmpty(text)) return;
            string keyToUse = !string.IsNullOrEmpty(key) ? key : text;
            if (string.IsNullOrEmpty(voiceProfile) && LocalTTSCacheService.IsMaleTrainingSentence(text, keyToUse))
            {
                voiceProfile = "Technical_Mark";
            }
            SpeakDirect(text, keyToUse, voiceProfile, pitch);
        }

        public void Speak(string textOrKey)
        {
            if (string.IsNullOrEmpty(textOrKey)) return;

            string resolvedKey = textOrKey;
            string textToSpeak = textOrKey;
            string voiceProfile = "";
            var entry = translationDatabase?.Find(e => string.Equals(e.key, textOrKey, StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                textToSpeak = GetSpeechText(textOrKey);
            }
            if (LocalTTSCacheService.IsMaleTrainingSentence(textToSpeak, resolvedKey))
            {
                voiceProfile = "Technical_Mark";
            }
            SpeakDirect(textToSpeak, resolvedKey, voiceProfile, 1.0f);
        }

        private void SpeakDirect(string textToSpeak, string key, string voiceProfile = "", float pitch = 1.0f)
        {
            if (string.IsNullOrEmpty(textToSpeak)) return;
            if (string.IsNullOrEmpty(voiceProfile) && LocalTTSCacheService.IsMaleTrainingSentence(textToSpeak, key))
            {
                voiceProfile = "Technical_Mark";
            }

            if (!IsLanguageSelected)
            {
                IsLanguageSelected = true;
                PreloadLanguageAudio(currentLanguage);
            }

            string cleanText = LocalTTSCacheService.NormalizeSpeechText(textToSpeak);
            if (string.IsNullOrEmpty(cleanText)) return;

            if (!string.IsNullOrEmpty(lastSpokenKey) && string.Equals(key, lastSpokenKey, StringComparison.OrdinalIgnoreCase) && cleanText == lastSpokenText)
            {
                if (isSpeaking || (voiceAudioSource != null && voiceAudioSource.isPlaying) || speechQueue.Count > 0)
                {
                    Debug.Log($"[Manager][TTS] Suppressing duplicate speech trigger for key '{key}'");
                    return;
                }
            }

            lastSpokenKey = key;
            lastSpokenText = cleanText;

            Debug.Log($"[AIR TTS]\nKey = {key}\nLanguage = {currentLanguage}\nVoice = {voiceProfile}\nSpeechText = {cleanText}\nAudioSource = {(voiceAudioSource != null ? voiceAudioSource.name : "null")}\nPlayRequested = TRUE");
            Debug.Log($"[TTS] START: Key = \"{key}\", Text = \"{cleanText}\"");

            if (interruptCurrentSpeech)
            {
                StopSpeech();
            }

            int myVersion = ++speechVersion;
            speechQueue.Enqueue(new SpeechRequest { key = key, text = cleanText, voiceProfile = voiceProfile, pitch = pitch, version = myVersion });
            isSpeaking = true;

            if (Application.isPlaying)
            {
                if (speechCoroutine == null)
                {
                    speechCoroutine = StartCoroutine(ProcessSpeechQueue());
                }
            }
            else
            {
                // In Edit Mode, verify and preload audio clip synchronously
                string langName = currentLanguage.ToString();
                string hash = LocalTTSCacheService.ComputeSpeechHash(langName, cleanText, voiceProfile);
                ResolveClipForPlayback(langName, hash, cleanText, voiceProfile, out TTSCacheStatus status, out _);
                isSpeaking = false;
                Debug.Log($"[Manager][EditMode] Speak validated: '{cleanText}' (key='{key}', voice='{voiceProfile}', status={status})");
                Debug.Log("[TTS] Completed");
                OnSpeechCompleted?.Invoke(key, cleanText);
            }
        }

        public void StopSpeech()
        {
            speechVersion++;
            speechQueue.Clear();
            if (voiceAudioSource != null)
            {
                if (voiceAudioSource.isPlaying) voiceAudioSource.Stop();
                voiceAudioSource.clip = null;
            }
            isSpeaking = false;
            if (speechCoroutine != null)
            {
                StopCoroutine(speechCoroutine);
                speechCoroutine = null;
            }
            lastSpokenKey = "";
            lastSpokenText = "";
            Debug.Log($"[TTS] STOP: speechVersion = {speechVersion}");
        }

        public AudioSource GetVoiceAudioSource()
        {
            if (voiceAudioSource == null)
            {
                voiceAudioSource = GetComponent<AudioSource>();
                if (voiceAudioSource == null)
                {
                    voiceAudioSource = gameObject.AddComponent<AudioSource>();
                }
                voiceAudioSource.spatialBlend = 0.0f;
                voiceAudioSource.volume = voiceVolume;
                voiceAudioSource.playOnAwake = false;
                voiceAudioSource.loop = false;
            }
            return voiceAudioSource;
        }

        public AudioSource GetSFXAudioSource()
        {
            if (sfxAudioSource == null)
            {
                sfxAudioSource = GetComponent<AudioSource>();
                if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
            }
            return sfxAudioSource;
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            var src = GetSFXAudioSource();
            if (src != null)
            {
                src.PlayOneShot(clip);
                Debug.Log($"[Manager][SFX] Playing SFX '{clip.name}' on {src.gameObject.name}");
            }
        }

        private IEnumerator ProcessSpeechQueue()
        {
            isSpeaking = true;
            string langName = currentLanguage.ToString();

            while (speechQueue.Count > 0)
            {
                var request = speechQueue.Dequeue();
                if (request.version != speechVersion)
                {
                    Debug.Log($"[TTS] IGNORE STALE: Request version {request.version} != Current {speechVersion}");
                    continue;
                }

                string hash = LocalTTSCacheService.ComputeSpeechHash(langName, request.text, request.voiceProfile);

                AudioClip clipToPlay = ResolveClipForPlayback(langName, hash, request.text, request.voiceProfile, out TTSCacheStatus status, out string expectedPath);

                if (voiceAudioSource == null)
                {
                    voiceAudioSource = GetVoiceAudioSource();
                }

                if (request.key.IndexOf("welcome", StringComparison.OrdinalIgnoreCase) >= 0 || request.key.Contains("01") || request.text.IndexOf("Welcome to the Vedanta", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Debug.Log("[PotLeakage TTS] Task 01 Welcome requested");
                    Debug.Log($"[PotLeakage TTS] SpeechText = \"{request.text}\"");
                    Debug.Log($"[PotLeakage TTS] Audio loaded = {(clipToPlay != null ? "TRUE" : "FALSE")}");
                    Debug.Log($"[PotLeakage TTS] Playback started = {(voiceAudioSource != null && clipToPlay != null ? "TRUE" : "FALSE")}");
                }

                Debug.Log($"[TTS CACHE]\nKey = {request.key}\nLanguage = {langName}\nVoice = {request.voiceProfile}\nHash = {hash}\nFile = {Path.GetFileName(expectedPath)}\nStatus = {status.ToString().ToUpperInvariant()}");

                if (clipToPlay != null && voiceAudioSource != null)
                {
                    if (request.version != speechVersion)
                    {
                        Debug.Log($"[TTS] IGNORE STALE: Clip resolved but version changed ({request.version} != {speechVersion})");
                        yield break;
                    }

                    Debug.Log($"[TTS] AUDIO READY: Key = \"{request.key}\", File = \"{clipToPlay.name}\"");
                    Debug.Log($"[TTS RESOLVED]\nKey = {request.key}\nLanguage = {langName}\nVoice = {request.voiceProfile}\nHash = {hash}\nStatus = VALID");

                    voiceAudioSource.Stop();
                    voiceAudioSource.clip = clipToPlay;
                    voiceAudioSource.volume = voiceVolume;
                    voiceAudioSource.pitch = request.pitch > 0.1f ? request.pitch : 1.0f;
                    voiceAudioSource.time = 0f;
                    yield return null; // 1-frame settle buffer to let Unity audio engine prime the clip

                    if (request.version != speechVersion)
                    {
                        voiceAudioSource.Stop();
                        voiceAudioSource.clip = null;
                        yield break;
                    }

                    voiceAudioSource.Play();

                    Debug.Log($"[TTS PLAY START]\nKey = {request.key}\nLanguage = {langName}\nVoice = {request.voiceProfile}\nHash = {hash}\nClip = {clipToPlay.name}");

                    // Wait for playback to actually begin, then for it to finish -
                    // never assume completion purely from clip.length, and never treat
                    // isPlaying==false immediately after Play() as completion.
                    float safetyTimeout = clipToPlay.length * 2f + 1f;
                    float waited = 0f;
                    while (!voiceAudioSource.isPlaying && waited < 1f)
                    {
                        if (request.version != speechVersion)
                        {
                            voiceAudioSource.Stop();
                            voiceAudioSource.clip = null;
                            yield break;
                        }
                        waited += Time.deltaTime;
                        yield return null;
                    }

                    if (!voiceAudioSource.isPlaying)
                    {
                        Debug.LogWarning($"[TTS PLAY FAILED]\nKey = {request.key}\nLanguage = {langName}\nHash = {hash}\nReason = AudioSource never reported isPlaying after Play()");
                    }
                    else
                    {
                        waited = 0f;
                        while (voiceAudioSource.isPlaying && waited < safetyTimeout)
                        {
                            if (request.version != speechVersion)
                            {
                                voiceAudioSource.Stop();
                                voiceAudioSource.clip = null;
                                yield break;
                            }
                            waited += Time.deltaTime;
                            yield return null;
                        }
                        Debug.Log($"[TTS PLAY COMPLETE]\nKey = {request.key}\nLanguage = {langName}\nHash = {hash}");
                        Debug.Log("[TTS] Completed");
                    }

                    if (request.version != speechVersion)
                    {
                        voiceAudioSource.Stop();
                        voiceAudioSource.clip = null;
                        yield break;
                    }

                    voiceAudioSource.pitch = 1.0f;
                    OnSpeechCompleted?.Invoke(request.key, request.text);
                }
                else
                {
                    Debug.LogWarning($"[TTS BLOCKED]\nKey = {request.key}\nLanguage = {langName}\nStatus = {status.ToString().ToUpperInvariant()}");
                    Debug.LogWarning($"[TTS MISSING]\nKey = {request.key}\nLanguage = {langName}\nSpeech = {request.text}\nExpectedPath = {expectedPath}");
                    yield return new WaitForSeconds(1.0f);
                    if (request.version == speechVersion)
                    {
                        OnSpeechCompleted?.Invoke(request.key, request.text);
                    }
                }
            }

            isSpeaking = false;
            speechCoroutine = null;
        }

        public AudioClip ResolveClipForPlayback(string langName, string hash, out TTSCacheStatus status, out string expectedPath)
        {
            return ResolveClipForPlayback(langName, hash, null, "", out status, out expectedPath);
        }

        public AudioClip ResolveClipForPlayback(string langName, string hash, string speechText, out TTSCacheStatus status, out string expectedPath)
        {
            return ResolveClipForPlayback(langName, hash, speechText, "", out status, out expectedPath);
        }

        public AudioClip ResolveClipForPlayback(string langName, string hash, string speechText, string voiceProfile, out TTSCacheStatus status, out string expectedPath)
        {
            expectedPath = ttsCache.GetCacheFilePath(langName, hash);
            if (!File.Exists(expectedPath))
            {
                string saPath = Path.Combine(Application.streamingAssetsPath, "TTSCache", ttsCache.GetCacheFileName(langName, hash));
                if (File.Exists(saPath)) expectedPath = saPath;
                else
                {
                    string projPath = Path.Combine(Application.dataPath, "PotLeakage", "Audio", "TTS", ttsCache.GetCacheFileName(langName, hash));
                    if (File.Exists(projPath)) expectedPath = projPath;
                    else if (!string.IsNullOrEmpty(speechText))
                    {
                        // Fallback check between plain hash and Technical_Mark hash
                        string altHash = string.IsNullOrEmpty(voiceProfile)
                            ? LocalTTSCacheService.ComputeSpeechHash(langName, speechText, "Technical_Mark")
                            : LocalTTSCacheService.ComputeSpeechHash(langName, speechText, "");
                        string altSaPath = Path.Combine(Application.streamingAssetsPath, "TTSCache", ttsCache.GetCacheFileName(langName, altHash));
                        if (File.Exists(altSaPath)) expectedPath = altSaPath;
                        else
                        {
                            string altProjPath = Path.Combine(Application.dataPath, "PotLeakage", "Audio", "TTS", ttsCache.GetCacheFileName(langName, altHash));
                            if (File.Exists(altProjPath)) expectedPath = altProjPath;
                        }
                    }
                }
            }

            if (ttsCache.TryGetMemoryClip(langName, hash, out AudioClip memClip) && memClip != null)
            {
                status = TTSCacheStatus.Valid;
                return memClip;
            }

            if (!File.Exists(expectedPath) && !string.IsNullOrEmpty(speechText))
            {
                LocalTTSCacheService.GenerateWav(expectedPath, speechText, voiceProfile);
            }

            status = File.Exists(expectedPath) ? TTSCacheStatus.Valid : TTSCacheStatus.Missing;

            if (status == TTSCacheStatus.Valid)
            {
                var clip = ttsCache.LoadClipFromPath(expectedPath, $"{langName}_{hash}")
                           ?? ttsCache.LoadClipFromDisk(langName, hash, $"{langName}_{hash}");
                if (clip != null)
                {
                    ttsCache.SetMemoryClip(langName, hash, clip);
                    return clip;
                }
                status = TTSCacheStatus.Missing;
            }

            return null;
        }

        public void RegisterPreloadedClip(string langName, string hash, AudioClip clip)
        {
            if (clip != null && ttsCache != null)
            {
                ttsCache.SetMemoryClip(langName, hash, clip);
            }
        }

        public TTSCacheStatus GetSpeechCacheStatus(string key)
        {
            string langName = currentLanguage.ToString();
            string speech = key;
            var entry = translationDatabase?.Find(e => string.Equals(e.key, key, StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                speech = GetSpeechTextForLanguage(entry, currentLanguage);
            }
            string clean = LocalTTSCacheService.NormalizeSpeechText(speech);
            string hash = LocalTTSCacheService.ComputeSpeechHash(langName, clean);
            return ttsCache.GetStatus(key, langName, hash, out _);
        }

        public bool IsSpeechCached(string key) => GetSpeechCacheStatus(key) == TTSCacheStatus.Valid;

        public string GetTrainingJsonPath() => TrainingJsonPath;

        public string GetTTSCachePath() => ttsCache.CacheRootPath;

        // ─────────────────────────────────────────────────────
        // VR TELEPORTATION & MOVEMENT
        // ─────────────────────────────────────────────────────

        public void MovePlayerTo(Transform destination)
        {
            if (destination == null)
            {
                Debug.LogWarning("[Manager] MovePlayerTo: Destination transform is null!");
                return;
            }

            if (xrOrigin == null)
            {
                xrOrigin = GameObject.Find("XR Origin (VR)");
            }

            if (xrOrigin != null)
            {
                xrOrigin.transform.SetPositionAndRotation(destination.position, destination.rotation);
                if (debugMode) Debug.Log($"[Manager] Player moved to: {destination.name}");
            }
        }

        // ─────────────────────────────────────────────────────
        // VR LANGUAGE SELECTION PANEL
        // ─────────────────────────────────────────────────────

        private Canvas FindTrainingInstructionCanvas()
        {
            var canvasGo = GameObject.Find("TrainingInstructionCanvas");
            if (canvasGo != null)
            {
                var c = canvasGo.GetComponent<Canvas>();
                if (c != null) return c;
            }
            var anchor = UnityEngine.Object.FindFirstObjectByType<TruckTyreReplacement.UI.TrainingHologramAnchor>();
            if (anchor != null)
            {
                var c = anchor.GetComponentInChildren<Canvas>(true);
                if (c != null) return c;
            }
            return null;
        }

        private void CreateLanguageSelectionPanel()
        {
            var canvas = FindTrainingInstructionCanvas();
            if (canvas == null) return;

            var existing = canvas.transform.Find("LanguageSelectionPanel");
            if (existing != null)
            {
                languageSelectionPanel = existing.gameObject;
                languageSelectionPanel.SetActive(true);
                var p = canvas.transform.Find("Panel");
                if (p != null) p.gameObject.SetActive(false);
                return;
            }

            var panelTrans = canvas.transform.Find("Panel");
            if (panelTrans == null) return;
            panelTrans.gameObject.SetActive(false);

            languageSelectionPanel = new GameObject("LanguageSelectionPanel", typeof(RectTransform));
            languageSelectionPanel.transform.SetParent(canvas.transform, false);

            var rect = languageSelectionPanel.GetComponent<RectTransform>();
            var panelRect = panelTrans.GetComponent<RectTransform>();
            rect.anchorMin = panelRect.anchorMin;
            rect.anchorMax = panelRect.anchorMax;
            rect.pivot = panelRect.pivot;
            rect.sizeDelta = panelRect.sizeDelta;
            rect.anchoredPosition = panelRect.anchoredPosition;

            var panelImg = panelTrans.GetComponent<Image>();
            if (panelImg != null)
            {
                var img = languageSelectionPanel.AddComponent<Image>();
                img.sprite = panelImg.sprite;
                img.color = panelImg.color;
                img.type = panelImg.type;
            }

            // Title
            var titleGo = new GameObject("TitleText", typeof(RectTransform));
            titleGo.transform.SetParent(languageSelectionPanel.transform, false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            var hindiTitleFont = GetFontForLanguage(LocalTTSLanguage.Hindi);
            if (hindiTitleFont != null) titleTmp.font = hindiTitleFont;
            titleTmp.text = "SELECT LANGUAGE\n<size=24>भाषा चुनें | ଭାଷା ଚୟନ କରନ୍ତୁ</size>";
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.fontSize = 28;
            titleTmp.color = Color.white;
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 160);
            titleRect.sizeDelta = new Vector2(480, 80);

            // Buttons with native fonts, resolved generically via GetFontForLanguage
            CreateLanguageBtn("EnglishButton", "ENGLISH", new Vector2(0, 50), LocalTTSLanguage.English, GetFontForLanguage(LocalTTSLanguage.English));
            CreateLanguageBtn("HindiButton", "हिन्दी", new Vector2(0, -40), LocalTTSLanguage.Hindi, GetFontForLanguage(LocalTTSLanguage.Hindi));
            CreateLanguageBtn("OdiaButton", "ଓଡ଼ିଆ", new Vector2(0, -130), LocalTTSLanguage.Odia, GetFontForLanguage(LocalTTSLanguage.Odia));
        }

        private void CreateLanguageBtn(string goName, string label, Vector2 pos, LocalTTSLanguage lang, TMP_FontAsset font)
        {
            var btnGo = new GameObject(goName, typeof(RectTransform));
            btnGo.transform.SetParent(languageSelectionPanel.transform, false);
            var rect = btnGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 65);
            rect.anchoredPosition = pos;

            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.12f, 0.45f, 0.75f, 1f);

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(btnGo.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 22;
            tmp.color = Color.white;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            btn.onClick.AddListener(() => SetLanguage(lang));
        }

        // ─────────────────────────────────────────────────────
        // FRAMEWORK VALIDATION (Editor + runtime diagnostics)
        // ─────────────────────────────────────────────────────

        public string BuildFrameworkValidationReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[VEDANTA FRAMEWORK]");
            sb.AppendLine();
            sb.AppendLine("FONTS");
            foreach (LocalTTSLanguage lang in Enum.GetValues(typeof(LocalTTSLanguage)))
            {
                var f = GetFontForLanguage(lang);
                sb.AppendLine($"{lang} -> {(f != null ? "assigned (" + f.name + ")" : "MISSING")}");
            }
            sb.AppendLine();
            sb.AppendLine("JSON");
            sb.AppendLine($"Path -> {TrainingJsonPath}");
            sb.AppendLine($"Exists -> {(File.Exists(TrainingJsonPath) ? "yes" : "no")}");
            sb.AppendLine($"Entries -> {(translationDatabase != null ? translationDatabase.Count : 0)}");
            sb.AppendLine();
            sb.AppendLine("TTS CACHE");
            sb.AppendLine($"Path -> {ttsCache.CacheRootPath}");
            sb.AppendLine($"Manifest -> {(File.Exists(ttsCache.ManifestPath) ? "valid" : "missing")}");

            int valid = 0, outdated = 0, missing = 0;
            if (translationDatabase != null)
            {
                foreach (var entry in translationDatabase)
                {
                    foreach (LocalTTSLanguage lang in Enum.GetValues(typeof(LocalTTSLanguage)))
                    {
                        string langName = lang.ToString();
                        string speech = GetSpeechTextForLanguage(entry, lang);
                        string hash = LocalTTSCacheService.ComputeSpeechHash(langName, LocalTTSCacheService.NormalizeSpeechText(speech));
                        var status = ttsCache.GetStatus(entry.key, langName, hash, out _);
                        if (status == TTSCacheStatus.Valid) valid++;
                        else if (status == TTSCacheStatus.Outdated) outdated++;
                        else missing++;
                    }
                }
            }
            sb.AppendLine($"Valid -> {valid}");
            sb.AppendLine($"Outdated -> {outdated}");
            sb.AppendLine($"Missing -> {missing}");
            sb.AppendLine();
            sb.AppendLine("UI");
            sb.AppendLine($"TrainingInstructionCanvas -> {(FindTrainingInstructionCanvas() != null ? "assigned" : "missing")}");
            sb.AppendLine($"Localized targets -> {localizedTextTargets.Count}");
            sb.AppendLine($"Voice AudioSource -> {(voiceAudioSource != null ? "assigned" : "missing")}");
            sb.AppendLine($"SFX AudioSource -> {(sfxAudioSource != null ? "assigned" : "missing")}");
            return sb.ToString();
        }

        [ContextMenu("Validate Framework")]
        public void LogFrameworkValidationReport()
        {
            Debug.Log(BuildFrameworkValidationReport());
        }
    }
}
