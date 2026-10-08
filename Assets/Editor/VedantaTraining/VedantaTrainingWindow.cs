using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VedantaTraining.Editor
{
    /// <summary>
    /// Interactive EditorWindow for managing the Vedanta Training Framework:
    /// TTS audio cache inspection, status diagnostics, selective regeneration,
    /// audio preview, and training JSON synchronization.
    /// </summary>
    public class VedantaTrainingWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private string searchFilter = "";
        private CacheEntryStatus? statusFilter = null;
        private List<AudioCacheItem> cachedItems = new List<AudioCacheItem>();
        private bool selectAll = false;
        private string currentlyPlayingFile = "";

        [MenuItem("Vedanta/Audio / TTS/TTS Cache Manager & Status Window %&t", priority = 200)]
        public static void ShowWindow()
        {
            var window = GetWindow<VedantaTrainingWindow>("Vedanta TTS Cache");
            window.minSize = new Vector2(820, 500);
            window.RefreshData();
            window.Show();
        }

        private void OnEnable()
        {
            RefreshData();
        }

        private void OnDisable()
        {
            VedantaTrainingAudioTools.StopPreview();
            currentlyPlayingFile = "";
        }

        public void RefreshData()
        {
            cachedItems = VedantaTrainingAudioTools.InspectCache();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawToolbar();
            DrawTable();
            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("VEDANTA TRAINING FRAMEWORK — AUDIO / TTS CACHE MANAGER", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // Module selector
            var modules = VedantaModuleManager.RegisteredModules;
            string[] moduleNames = new string[modules.Count];
            for (int i = 0; i < modules.Count; i++) moduleNames[i] = modules[i].ModuleName;

            int selectedIndex = EditorGUILayout.Popup("Active Module:", VedantaModuleManager.ActiveModuleIndex, moduleNames, GUILayout.Width(250));
            if (selectedIndex != VedantaModuleManager.ActiveModuleIndex)
            {
                VedantaModuleManager.ActiveModuleIndex = selectedIndex;
                RefreshData();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Refresh button
            if (GUILayout.Button("↻ Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshData();
            }

            // Selection controls
            if (GUILayout.Button("Select All", EditorStyles.toolbarButton, GUILayout.Width(75)))
            {
                selectAll = true;
                foreach (var item in cachedItems) item.isSelected = true;
            }

            if (GUILayout.Button("Clear Selection", EditorStyles.toolbarButton, GUILayout.Width(95)))
            {
                selectAll = false;
                foreach (var item in cachedItems) item.isSelected = false;
            }

            // Search field
            GUILayout.Label("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(160));

            // Status Filter
            GUILayout.Label("Filter:", GUILayout.Width(40));
            string[] filterOptions = new string[] { "All", "Valid", "Missing", "Changed", "Stale", "Invalid", "Error" };
            int currentFilterIdx = 0;
            if (statusFilter == CacheEntryStatus.Valid) currentFilterIdx = 1;
            else if (statusFilter == CacheEntryStatus.Missing) currentFilterIdx = 2;
            else if (statusFilter == CacheEntryStatus.Changed) currentFilterIdx = 3;
            else if (statusFilter == CacheEntryStatus.Stale) currentFilterIdx = 4;
            else if (statusFilter == CacheEntryStatus.Invalid) currentFilterIdx = 5;
            else if (statusFilter == CacheEntryStatus.Error) currentFilterIdx = 6;

            int newFilterIdx = EditorGUILayout.Popup(currentFilterIdx, filterOptions, EditorStyles.toolbarPopup, GUILayout.Width(80));
            if (newFilterIdx != currentFilterIdx)
            {
                switch (newFilterIdx)
                {
                    case 1: statusFilter = CacheEntryStatus.Valid; break;
                    case 2: statusFilter = CacheEntryStatus.Missing; break;
                    case 3: statusFilter = CacheEntryStatus.Changed; break;
                    case 4: statusFilter = CacheEntryStatus.Stale; break;
                    case 5: statusFilter = CacheEntryStatus.Invalid; break;
                    case 6: statusFilter = CacheEntryStatus.Error; break;
                    default: statusFilter = null; break;
                }
            }

            // Stop preview if playing
            if (!string.IsNullOrEmpty(currentlyPlayingFile))
            {
                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("■ Stop Audio", EditorStyles.toolbarButton, GUILayout.Width(90)))
                {
                    VedantaTrainingAudioTools.StopPreview();
                    currentlyPlayingFile = "";
                }
                GUI.backgroundColor = prevBg;
            }

            GUILayout.FlexibleSpace();

            // Action Buttons
            if (GUILayout.Button("Generate Missing", EditorStyles.toolbarButton))
            {
                VedantaTrainingAudioTools.GenerateMissingAudio();
                RefreshData();
            }

            if (GUILayout.Button("Update Changed", EditorStyles.toolbarButton))
            {
                VedantaTrainingAudioTools.UpdateChangedAudio();
                RefreshData();
            }

            if (GUILayout.Button("Regenerate Selected", EditorStyles.toolbarButton))
            {
                var selected = cachedItems.FindAll(i => i.isSelected);
                if (selected.Count > 0)
                {
                    VedantaTrainingAudioTools.RegenerateSelected(selected);
                    RefreshData();
                }
                else
                {
                    EditorUtility.DisplayDialog("No Selection", "Please check the box next to one or more entries to regenerate.", "OK");
                }
            }

            if (GUILayout.Button("Regenerate All...", EditorStyles.toolbarButton))
            {
                VedantaTrainingAudioTools.RegenerateAll();
                RefreshData();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTable()
        {
            // Table Header
            EditorGUILayout.BeginHorizontal("box");
            bool newSelectAll = EditorGUILayout.Toggle(selectAll, GUILayout.Width(22));
            if (newSelectAll != selectAll)
            {
                selectAll = newSelectAll;
                foreach (var item in cachedItems) item.isSelected = selectAll;
            }

            GUILayout.Label("Key", EditorStyles.boldLabel, GUILayout.Width(160));
            GUILayout.Label("Language", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("Status", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("Speech Text", EditorStyles.boldLabel);
            GUILayout.Label("File / Hash", EditorStyles.boldLabel, GUILayout.Width(130));
            GUILayout.Label("Action", EditorStyles.boldLabel, GUILayout.Width(110));
            EditorGUILayout.EndHorizontal();

            // Table Body
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            for (int i = 0; i < cachedItems.Count; i++)
            {
                var item = cachedItems[i];

                // Apply Filters
                if (statusFilter.HasValue && item.status != statusFilter.Value) continue;
                if (!string.IsNullOrEmpty(searchFilter))
                {
                    bool matchKey = item.key.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                    bool matchText = item.text.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!matchKey && !matchText) continue;
                }

                EditorGUILayout.BeginHorizontal(i % 2 == 0 ? "CN Box" : "box");

                // Selection Checkbox
                item.isSelected = EditorGUILayout.Toggle(item.isSelected, GUILayout.Width(22));

                // Key
                GUILayout.Label(item.key, GUILayout.Width(160));

                // Language
                GUILayout.Label(item.language, GUILayout.Width(70));

                // Status Badge
                DrawStatusBadge(item.status);

                // Speech Text with tooltip
                GUILayout.Label(new GUIContent(item.text, item.text));

                // File name & size
                string fileInfo = System.IO.File.Exists(item.filePath)
                    ? $"{item.fileSizeBytes / 1024} KB\n({item.hash.Substring(0, 8)}...)"
                    : "Not cached";
                GUILayout.Label(fileInfo, EditorStyles.miniLabel, GUILayout.Width(130));

                // Action / Audio Preview
                EditorGUILayout.BeginHorizontal(GUILayout.Width(110));
                bool isPlayingThis = (currentlyPlayingFile == item.filePath);

                if (System.IO.File.Exists(item.filePath))
                {
                    if (isPlayingThis)
                    {
                        if (GUILayout.Button("■ Stop", GUILayout.Width(50)))
                        {
                            VedantaTrainingAudioTools.StopPreview();
                            currentlyPlayingFile = "";
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("▶ Play", GUILayout.Width(50)))
                        {
                            VedantaTrainingAudioTools.PlayPreview(item.filePath);
                            currentlyPlayingFile = item.filePath;
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button("+ Gen", GUILayout.Width(50)))
                    {
                        VedantaTrainingAudioTools.RegenerateSelected(new List<AudioCacheItem> { item });
                        RefreshData();
                    }
                }

                if (GUILayout.Button("↻", GUILayout.Width(24)))
                {
                    VedantaTrainingAudioTools.RegenerateSelected(new List<AudioCacheItem> { item });
                    RefreshData();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawStatusBadge(CacheEntryStatus status)
        {
            Color prevColor = GUI.color;
            string text = status.ToString().ToUpperInvariant();

            switch (status)
            {
                case CacheEntryStatus.Valid:
                    GUI.color = new Color(0.3f, 0.95f, 0.4f, 1f);
                    break;
                case CacheEntryStatus.Missing:
                    GUI.color = new Color(1.0f, 0.35f, 0.35f, 1f);
                    break;
                case CacheEntryStatus.Changed:
                    GUI.color = new Color(1.0f, 0.85f, 0.25f, 1f);
                    break;
                case CacheEntryStatus.Stale:
                    GUI.color = new Color(1.0f, 0.6f, 0.1f, 1f);
                    break;
                case CacheEntryStatus.Invalid:
                case CacheEntryStatus.Error:
                    GUI.color = new Color(1.0f, 0.2f, 0.6f, 1f);
                    break;
                default:
                    GUI.color = Color.gray;
                    break;
            }

            GUILayout.Label(text, EditorStyles.boldLabel, GUILayout.Width(80));
            GUI.color = prevColor;
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            int valid = 0, missing = 0, changed = 0, invalid = 0;
            foreach (var item in cachedItems)
            {
                if (item.status == CacheEntryStatus.Valid) valid++;
                else if (item.status == CacheEntryStatus.Missing) missing++;
                else if (item.status == CacheEntryStatus.Changed) changed++;
                else invalid++;
            }

            GUILayout.Label($"Total: {cachedItems.Count}  |  Valid: {valid}  |  Missing: {missing}  |  Changed: {changed}  |  Invalid: {invalid}", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Deploy JSON", EditorStyles.miniButton, GUILayout.Width(90)))
            {
                VedantaTrainingDataTools.DeployTrainingJson();
            }

            if (GUILayout.Button("Validate Cache", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                VedantaTrainingValidator.ValidateTTSCache().PrintToConsole();
            }

            if (GUILayout.Button("Sync Manifest", EditorStyles.miniButton, GUILayout.Width(90)))
            {
                VedantaTrainingAudioTools.SyncManifestFiles();
                RefreshData();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }
}
