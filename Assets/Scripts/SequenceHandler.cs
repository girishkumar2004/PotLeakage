using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SequenceHandler : MonoBehaviour
{
    public static SequenceHandler instance;
    public bool autostart = false;

    // ── C# EVENT DELEGATES FOR CLEAN SCRIPT INTEGRATION ──────────────────────
#pragma warning disable CS0067
    public static event System.Action<int, Task> OnTaskStarted;
    public static event System.Action<int, Task> OnTaskCompletedEvent;
    public static event System.Action<int> OnSequenceStarted;
    public static event System.Action<int> OnSequenceCompleted;
#pragma warning restore CS0067

    [Tooltip("The sequence list is referred from the scriptable objects here.")]
    public List<Sequence> sequenceList = new List<Sequence>();
    [SerializeField]
    public int currentSequence, currentTask;
    public bool WaitForTrigger;
    public bool isSequenceMode;
    public bool lockSequence = false;

    private int currentSpeakingTaskIndex = -1;
    private int currentSpeakingGeneration = -1;
    private int taskGeneration = 0;
    private bool taskCompletionHandled = false;

    private void OnEnable()
    {
        TruckTyreReplacement.Core.Manager.OnSpeechCompleted += OnSpeechFinished;
    }

    private void OnDisable()
    {
        TruckTyreReplacement.Core.Manager.OnSpeechCompleted -= OnSpeechFinished;
    }

    private void OnSpeechFinished(string key, string text)
    {
        HandleVoiceOverCompleted(currentSpeakingTaskIndex, currentSpeakingGeneration);
    }

    public void HandleVoiceOverCompleted(int taskIndex, int generation = -1)
    {
        // NO AUTO-ADVANCE: TTS / Voice completion NEVER advances the training sequence.
        // The ONLY progression mechanism is user clicking Next / Continue or explicit physical interaction.
        Debug.Log($"[SequenceHandler] Voice-over finished for task {taskIndex}. Progression requires manual Next / Continue click.");
    }

    public void UpdateContinueButtonVisibility(bool show)
    {
        if (SequenceHelperFunctions.instance != null && SequenceHelperFunctions.instance.nextButton != null)
        {
            SequenceHelperFunctions.instance.nextButton.gameObject.SetActive(show);
        }
        var ui = PotLeakage.UI.PotLeakageUIController.Instance ?? UnityEngine.Object.FindFirstObjectByType<PotLeakage.UI.PotLeakageUIController>();
        if (ui != null && ui.nextButton != null)
        {
            ui.nextButton.gameObject.SetActive(show);
        }
    }

    public void Awake()
    {
        if (autostart)
        {
            instance = this;
        }
        else if (instance == null || !instance.autostart)
        {
            if (sequenceList != null && sequenceList.Count > 0)
            {
                instance = this;
            }
            else if (instance == null)
            {
                instance = this;
            }
        }
    }
    public void Start()
    {
        if (autostart)
            Init();
    }
    public void DelayedStart(float s)
    {
        Invoke(nameof(Init), s);
    }

    public void Init()
    {
        currentSequence = 0;
        currentTask = 0;
        currentSpeakingTaskIndex = -1;
        currentSpeakingGeneration = -1;
        taskGeneration = 0;
        taskCompletionHandled = false;
        if (sequenceList != null)
        {
            foreach (var seq in sequenceList)
            {
                if (seq != null && seq.TaskList != null)
                {
                    foreach (var t in seq.TaskList)
                    {
                        if (t != null)
                        {
                            t.TaskCompleted = false;
                            t.TriggerCompleted = false;
                        }
                    }
                }
            }
        }
        OnSequenceStarted?.Invoke(currentSequence);
        //if (isSequenceMode)
            NextTask();
    }

    public void TaskCompleted()
    {
        if (sequenceList == null || currentSequence < 0 || currentSequence >= sequenceList.Count)
        {
            Debug.Log("[SequenceHandler] All sequences completed or sequence index out of bounds.");
            return;
        }

        if (sequenceList[currentSequence].TaskList == null || currentTask < 0 || currentTask >= sequenceList[currentSequence].TaskList.Count)
            return;

        if (taskCompletionHandled && sequenceList[currentSequence].TaskList[currentTask].TaskCompleted)
        {
            Debug.LogWarning($"[SequenceHandler] Task {currentTask} is already completed. Rejects duplicate completion call.");
            return;
        }

        taskCompletionHandled = true;
        taskGeneration++;

        // Stop current speech playback immediately if advancing early via Next click or other pathway
        try
        {
            TruckTyreReplacement.Core.Manager.Instance?.StopSpeech();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SequenceHandler] Error stopping speech: {ex.Message}");
        }

        string completedTaskName = sequenceList[currentSequence].TaskList[currentTask].TaskName;
        Task completedTask = sequenceList[currentSequence].TaskList[currentTask];
        completedTask.TaskCompleted = true;
        Debug.Log($"[SEQUENCE COMPLETE]\nTask = {completedTaskName}");
        Debug.Log("Current task num : " + currentTask + " completed by " + gameObject.name);
        Debug.Log($"[PotLeakage] Completing Task {(currentTask + 1):D2} (Index={currentTask}, Name='{completedTaskName}')");
        OnTaskCompletedEvent?.Invoke(currentTask, completedTask);
        currentTask++;
        NextTask();
    }

    public void CurrentTaskCompleted()
    {
        if (sequenceList == null || currentSequence < 0 || currentSequence >= sequenceList.Count) return;
        if (sequenceList[currentSequence].TaskList == null || currentTask < 0 || currentTask >= sequenceList[currentSequence].TaskList.Count) return;

        sequenceList[currentSequence].TaskList[currentTask].TaskCompleted = true;
        currentTask++;
    }

    /*public void SkipSequence()
    {
        for (int i = 0; i < sequenceList[currentSequence].TaskList.Count; i++)
        {
            sequenceList[currentSequence].TaskList[i].TaskCompleted = true;
        }
        currentTask = sequenceList[currentSequence].TaskList.Count;
        lockSequence = true;
        this.GetComponent<AudioSource>().Stop();
    }*/

    public void SkipSequence()
    {
        // Complete all remaining tasks in current sequence
        for (int i = 0; i < sequenceList[currentSequence].TaskList.Count; i++)
        {
            sequenceList[currentSequence].TaskList[i].TaskCompleted = true;
            sequenceList[currentSequence].TaskList[i].TriggerCompleted = true;
        }

        // Stop audio if available
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        Debug.Log("Skipping Sequence : " + currentSequence);

        // Move directly to next sequence
        currentSequence++;

        if (currentSequence >= sequenceList.Count)
        {
            Debug.Log("All sequences completed.");
            return;
        }

        currentTask = 0;

        // Start next sequence
        NextTask();
    }

    public void SequenceSelect(int n)
    {
        currentSequence = n;
        currentTask = 0;
        NextTask();
    }

    public void NextTask()
    {
        Debug.Log(currentSequence.CompareTo(currentTask));
        if (currentTask >= sequenceList[currentSequence].TaskList.Count)
        {
            if (!lockSequence)
            {
                NextSequence();
            }
            else
            {
                Debug.Log("Sequence is locked. Not moving to next sequence");
            }
            return;
        }

        taskGeneration++;
        currentSpeakingTaskIndex = currentTask;
        currentSpeakingGeneration = taskGeneration;
        taskCompletionHandled = false;

        Task activeTask = sequenceList[currentSequence].TaskList[currentTask];
        activeTask.TaskCompleted = false;

        bool isAuto = activeTask.AutoAdvanceAfterVoiceOver || activeTask.completionMode == CompletionMode.AudioComplete;
        UpdateContinueButtonVisibility(true);

        Debug.Log($"[SEQUENCE START]\nSequence = {currentSequence}\nTask Index = {currentTask}\nTask Name = {activeTask.TaskName}\nAutoAdvance = {isAuto}");
        Debug.Log($"[PotLeakage] Starting Task {(currentTask + 1):D2} (Index={currentTask}, Name='{activeTask.TaskName}')");

        ExecuteTaskInstructionAndTTS(activeTask);

        OnTaskStarted?.Invoke(currentTask, activeTask);

        switch (activeTask.typeOfInteraction)
        {
            case Task.TypeOfInteraction.None:
                WaitForTrigger = false;
                break;
            default:
                break;
        }

        if (!WaitForTrigger)
        {
            InvokeTaskEvents(activeTask);
        }
    }

    private void PlayCurrentTask()
    {
        if (currentSequence >= 0 && currentSequence < sequenceList.Count &&
            currentTask >= 0 && currentTask < sequenceList[currentSequence].TaskList.Count)
        {
            taskGeneration++;
            currentSpeakingTaskIndex = currentTask;
            currentSpeakingGeneration = taskGeneration;
            taskCompletionHandled = false;
            Task activeTask = sequenceList[currentSequence].TaskList[currentTask];
            activeTask.TaskCompleted = false;

            bool isAuto = activeTask.AutoAdvanceAfterVoiceOver || activeTask.completionMode == CompletionMode.AudioComplete;
            UpdateContinueButtonVisibility(true);

            Debug.Log($"Playing task {currentTask} in sequence {currentSequence}");

            ExecuteTaskInstructionAndTTS(activeTask);

            switch (activeTask.typeOfInteraction)
            {
                case Task.TypeOfInteraction.None:
                    WaitForTrigger = false;
                    break;
                default:
                    break;
            }

            if (!WaitForTrigger)
            {
                InvokeTaskEvents(activeTask);
            }
        }
        else
        {
            Debug.LogWarning($"Attempted to play invalid task: Sequence {currentSequence}, Task {currentTask}");
        }
    }

    private void InvokeTaskEvents(Task activeTask)
    {
        if (activeTask == null || activeTask.EventsToFollow == null) return;
        activeTask.EventsToFollow.Invoke();
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            for (int i = 0; i < activeTask.EventsToFollow.GetPersistentEventCount(); i++)
            {
                var target = activeTask.EventsToFollow.GetPersistentTarget(i);
                var method = activeTask.EventsToFollow.GetPersistentMethodName(i);
                if (target != null && !string.IsNullOrEmpty(method))
                {
                    var mInfo = target.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (mInfo != null && mInfo.GetParameters().Length == 0)
                    {
                        mInfo.Invoke(target, null);
                    }
                }
            }
        }
#endif
    }

    private void ExecuteTaskInstructionAndTTS(Task task)
    {
        if (task == null) return;

        string displayTextKey = !string.IsNullOrEmpty(task.instructionText) ? task.instructionText : task.TaskName;
        string speechKeyOrText = !string.IsNullOrEmpty(task.ttsTextOrKey) ? task.ttsTextOrKey : task.instructionText;

        var manager = TruckTyreReplacement.Core.Manager.Instance;
        if (manager == null) manager = UnityEngine.Object.FindFirstObjectByType<TruckTyreReplacement.Core.Manager>();

        // 1. Display Instruction Text on TrainingHologramAnchor / Instruction Panel
        if (!string.IsNullOrEmpty(displayTextKey))
        {
            string resolvedDisplay = displayTextKey;
            if (manager != null)
            {
                string localized = manager.GetDisplayText(displayTextKey);
                if (!string.IsNullOrEmpty(localized)) resolvedDisplay = localized;
            }

            var anchor = UnityEngine.Object.FindFirstObjectByType<TruckTyreReplacement.UI.TrainingHologramAnchor>();
            if (anchor != null)
            {
                anchor.SetDescription(resolvedDisplay);
            }
            else
            {
                var panel = UnityEngine.Object.FindFirstObjectByType<TruckTyreReplacement.UI.TrainingInstructionPanel>();
                if (panel != null) panel.SetInstruction(resolvedDisplay);
            }
        }

        // 2. Play LocalTTS Audio
        if (task.useTTS)
        {
            currentSpeakingTaskIndex = currentTask;
            currentSpeakingGeneration = taskGeneration;
            int capturedIndex = currentTask;
            int capturedGen = taskGeneration;

            if (task.audioClipOverride != null)
            {
                if (SequenceHelperFunctions.instance != null)
                {
                    SequenceHelperFunctions.instance.PlayVoiceAudioClipWithCallback(task.audioClipOverride, () =>
                    {
                        HandleVoiceOverCompleted(capturedIndex, capturedGen);
                    });
                }
            }
            else if (!string.IsNullOrEmpty(speechKeyOrText))
            {
                Debug.Log($"[SequenceHandler][TTS] Task '{task.TaskName}' (AutoAdvance: {task.AutoAdvanceAfterVoiceOver}, Mode: {task.completionMode}) speaking: '{speechKeyOrText}'");
                if (manager != null)
                {
                    manager.SpeakText(speechKeyOrText, speechKeyOrText);
                }
                else if (SequenceHelperFunctions.instance != null)
                {
                    SequenceHelperFunctions.instance.PlayLocaleAudio(speechKeyOrText);
                }
                else
                {
                    Debug.LogWarning($"[SequenceHandler][TTS] LocalTTS / Manager unavailable for task '{task.TaskName}'. Continuing without audio.");
                    HandleVoiceOverCompleted(capturedIndex, capturedGen);
                }
            }
        }
    }

    public void NextSequence()
    {
        string fromName = (sequenceList != null && currentSequence >= 0 && currentSequence < sequenceList.Count)
            ? sequenceList[currentSequence].SequenceName : currentSequence.ToString();
        OnSequenceCompleted?.Invoke(currentSequence);
        currentSequence++;
        if (currentSequence >= sequenceList.Count)
        {
            Debug.Log($"[SEQUENCE ADVANCE]\nFrom = {fromName}\nTo = (none - all sequences completed)");
            return;
        }
        string toName = sequenceList[currentSequence].SequenceName;
        Debug.Log($"[SEQUENCE ADVANCE]\nFrom = {fromName}\nTo = {toName}");
        currentTask = 0;
        currentSpeakingTaskIndex = -1;
        currentSpeakingGeneration = -1;
        taskCompletionHandled = false;
        OnSequenceStarted?.Invoke(currentSequence);
        NextTask();
    }

    public void PreviousSequence()
    {
        currentSequence--;
        if (currentSequence < 0)
        {
            return;
        }
        currentTask = 0;
        currentSpeakingTaskIndex = -1;
        currentSpeakingGeneration = -1;
        taskCompletionHandled = false;
        NextTask();
    }

    public void ReloadSequence()
    {
        if (currentSequence >= 0 && currentSequence < sequenceList.Count)
        {
            currentTask = 0;
            currentSpeakingTaskIndex = -1;
            currentSpeakingGeneration = -1;
            taskCompletionHandled = false;
            Debug.Log("Reload Sequence" + currentSequence);
            PlayCurrentTask();
        }
    }

    public void TriggerForTaskDone()
    {
        switch (sequenceList[currentSequence].TaskList[currentTask].typeOfInteraction)
        {
            case Task.TypeOfInteraction.None:
                WaitForTrigger = false;
                break;
            default:
                break;

        }
        sequenceList[currentSequence].TaskList[currentTask].TriggerCompleted = true;

        sequenceList[currentSequence].TaskList[currentTask].EventsToFollow.Invoke();

        WaitForTrigger = false;
    }

    public Task GetCurrentTask()
    {
        if (sequenceList != null && currentSequence >= 0 && currentSequence < sequenceList.Count)
        {
            var seq = sequenceList[currentSequence];
            if (seq != null && seq.TaskList != null && currentTask >= 0 && currentTask < seq.TaskList.Count)
            {
                return seq.TaskList[currentTask];
            }
        }
        return null;
    }

    public int GetCurrentTaskIndex() => currentTask;

    public int GetTotalTasksInCurrentSequence()
    {
        if (sequenceList != null && currentSequence >= 0 && currentSequence < sequenceList.Count)
        {
            var seq = sequenceList[currentSequence];
            if (seq != null && seq.TaskList != null)
            {
                return seq.TaskList.Count;
            }
        }
        return 0;
    }

    public void LoadScene(string name)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(name);
    }

    public void ApplicationExit()
    {
        Application.Quit();
    }
}