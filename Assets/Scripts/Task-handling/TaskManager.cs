using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the active task list for the current day. Listens to GameData.OnDayChanged
/// to reset tasks each day, seeds a single initial task, and adds the follow-up tasks
/// once that initial task is fully completed. Only runs up through maxTaskDay.
///
/// Also controls GameData.canAdvance: locked (true) when a new day's initial task is
/// assigned, unlocked (false) once that task is fully completed (Part 2 done) — so
/// AdvanceDay() only succeeds after the required task is done.
///
/// Each task has two parts: Part 1 (reaching/finishing at startLocation) and
/// Part 2 (reaching/finishing at endLocation — this is what completes the task).
/// </summary>
public class TaskManager : MonoBehaviour
{
    [Tooltip("Same GameData asset used by the rest of the game — provides day/money state.")]
    [SerializeField] private GameData gameData;

    [Tooltip("Authored task definitions per day.")]
    [SerializeField] private TaskDatabase taskDatabase;

    [Tooltip("Tasks stop being assigned after this day.")]
    [SerializeField] private int maxTaskDay = 6;

    public List<TaskItem> ActiveTasks { get; private set; } = new List<TaskItem>();

    /// <summary>Fired whenever the task list changes (tasks added, Part 1 done, or fully completed) — UI can subscribe to refresh.</summary>
    public event Action OnTaskListChanged;

    private TaskDatabase.DayTaskSet currentDaySet;

    private void OnEnable()
    {
        gameData.OnDayChanged += HandleDayChanged;

        // Seed tasks for whatever day it currently is when this scene/manager starts.
        SetupTasksForDay(gameData.CurrentDay);
    }

    private void OnDisable()
    {
        gameData.OnDayChanged -= HandleDayChanged;
    }

    private void HandleDayChanged(int newDay)
    {
        SetupTasksForDay(newDay);
    }

    private void SetupTasksForDay(int day)
    {
        // Any tasks left over from the previous day (e.g. uncompleted follow-ups)
        // are discarded here — a fresh day always starts with a clean list.
        ActiveTasks.Clear();

        if (day > maxTaskDay)
        {
            currentDaySet = null;
            // No task requirement beyond maxTaskDay — leave advancing unlocked.
            gameData.SetCanAdvance(false);
            OnTaskListChanged?.Invoke();
            return;
        }

        currentDaySet = taskDatabase.GetTasksForDay(day);

        if (currentDaySet == null)
        {
            Debug.LogWarning($"[{nameof(TaskManager)}] No task set defined in TaskDatabase for day {day}.");
            gameData.SetCanAdvance(false);
            OnTaskListChanged?.Invoke();
            return;
        }

        // A new day's initial task locks advancing until it's fully completed.
        gameData.SetCanAdvance(true);

        // Assign the single starting task for the day.
        ActiveTasks.Add(new TaskItem(
            $"day{day}_initial",
            currentDaySet.initialTask.description,
            currentDaySet.initialTask.moneyReward,
            currentDaySet.initialTask.startLocation,
            currentDaySet.initialTask.endLocation));

        OnTaskListChanged?.Invoke();
    }

    /// <summary>
    /// Call this when the player finishes Part 1 of a task (arrives/completes something at startLocation).
    /// Does not award money or unlock day advancement — that happens on CompletePart2.
    /// </summary>
    public void CompletePart1(string taskId)
    {
        TaskItem task = ActiveTasks.Find(t => t.id == taskId);
        if (task == null || task.isPart1Completed || task.isCompleted) return;

        task.isPart1Completed = true;
        OnTaskListChanged?.Invoke();
    }

    /// <summary>
    /// Call this when the player finishes Part 2 of a task (arrives/completes something at endLocation).
    /// This is what actually completes the task: awards money, unlocks day advancement if this
    /// was the day's initial task, and adds follow-up tasks if applicable.
    /// Requires Part 1 to already be completed.
    /// </summary>
    public void CompletePart2(string taskId)
    {
        TaskItem task = ActiveTasks.Find(t => t.id == taskId);
        if (task == null || task.isCompleted) return;

        if (!task.isPart1Completed)
        {
            Debug.LogWarning($"[{nameof(TaskManager)}] Tried to complete Part 2 of task '{taskId}' before Part 1 was done.");
            return;
        }

        task.isCompleted = true;
        gameData.addMoney(task.moneyReward);

        bool wasInitialTask = currentDaySet != null && taskId == $"day{gameData.CurrentDay}_initial";

        if (wasInitialTask)
        {
            // Required task done — unlock advancing to the next day.
            gameData.SetCanAdvance(false);

            for (int i = 0; i < currentDaySet.followUpTasks.Count; i++)
            {
                TaskDatabase.TaskDefinition followUp = currentDaySet.followUpTasks[i];
                string followUpId = $"day{gameData.CurrentDay}_followup{i}";
                ActiveTasks.Add(new TaskItem(
                    followUpId,
                    followUp.description,
                    followUp.moneyReward,
                    followUp.startLocation,
                    followUp.endLocation));
            }
        }

        OnTaskListChanged?.Invoke();
    }
}