using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the active task list for the current day, as a ScriptableObject asset
/// (like GameData and TaskDatabase) so it can be referenced directly from any scene —
/// no singleton or DontDestroyOnLoad needed. Any script in any scene that needs to
/// complete a task just drags in this same asset and calls CompleteTask(taskId).
///
/// SETUP: Right-click in the Project window -> Create -> Game Data -> Task Manager.
/// This creates one TaskManager.asset. Drag that SAME asset into every script (in
/// every scene) that needs to read the task list or complete a task.
///
/// Listens to GameData.OnDayChanged to reset tasks each day, seeds a single initial
/// task, and adds follow-up tasks once that initial task is completed. Only runs up
/// through maxTaskDay.
///
/// Also controls GameData.canAdvance: locked (true) when a new day's initial task is
/// assigned, unlocked (false) once that task is completed — so AdvanceDay() only
/// succeeds after the required task is done.
/// </summary>
[CreateAssetMenu(fileName = "TaskManager", menuName = "Game Data/Task Manager")]
public class TaskManager : ScriptableObject
{
    [Tooltip("Same GameData asset used by the rest of the game — provides day/money state.")]
    [SerializeField] private GameData gameData;

    [Tooltip("Authored task definitions per day.")]
    [SerializeField] private TaskDatabase taskDatabase;

    [Tooltip("Tasks stop being assigned after this day.")]
    [SerializeField] private int maxTaskDay = 6;

    public List<TaskItem> ActiveTasks { get; private set; } = new List<TaskItem>();

    /// <summary>Fired whenever the task list changes (tasks added or completed) — UI can subscribe to refresh.</summary>
    public event Action OnTaskListChanged;

    private TaskDatabase.DayTaskSet currentDaySet;
    private bool isSubscribed;

    /// <summary>
    /// Called when Unity loads this asset (roughly once, whenever it's first referenced —
    /// similar timing to GameData.OnEnable, since ScriptableObjects share that lifecycle).
    ///
    /// IMPORTANT: explicitly resets ALL runtime state here rather than trusting default
    /// values. With "Domain Reload" disabled in Enter Play Mode Settings (a common Editor
    /// speed optimization), this asset's fields can otherwise carry over stale values —
    /// leftover tasks, a stale currentDaySet, or a duplicate event subscription — from the
    /// previous Play session.
    /// </summary>
    private void OnEnable()
    {
        ActiveTasks = new List<TaskItem>();
        currentDaySet = null;

        if (isSubscribed)
        {
            // Unsubscribe first in case OnEnable fires again without a matching OnDisable
            // in between (can happen with Domain Reload disabled) — prevents double-firing.
            gameData.OnDayChanged -= HandleDayChanged;
        }
        gameData.OnDayChanged += HandleDayChanged;
        isSubscribed = true;

        SetupTasksForDay(gameData.CurrentDay);
    }

    private void OnDisable()
    {
        if (isSubscribed)
        {
            gameData.OnDayChanged -= HandleDayChanged;
            isSubscribed = false;
        }
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

        // A new day's initial task locks advancing until it's completed.
        gameData.SetCanAdvance(true);

        // Assign the single starting task for the day.
        ActiveTasks.Add(new TaskItem(
            $"day{day}_initial",
            currentDaySet.initialTask.description,
            currentDaySet.initialTask.moneyReward,
            currentDaySet.initialTask.npc));

        OnTaskListChanged?.Invoke();
    }

    /// <summary>
    /// Call this when the player completes a task — from a UI button's OnClick, a trigger
    /// collider, or a script in a completely different scene. Since this is a shared asset,
    /// any script that has this same TaskManager reference can call it directly.
    /// </summary>
    public void CompleteTask(string taskId)
    {

        Debug.Log($"Trying to complete: {taskId}");

        TaskItem task = ActiveTasks.Find(t => t.id == taskId);

        if (task == null)
        {
            Debug.LogWarning("Task not found.");

            foreach (TaskItem t in ActiveTasks)
            {
                Debug.Log($"Existing task: {t.id}");
            }

            return;
        }

        if (task == null)
        {
            Debug.LogWarning($"Task '{taskId}' not found.");
            return;
        }
        if (task == null || task.isCompleted) return;

        task.isCompleted = true;
        // gameData.addMoney(task.moneyReward);
        gameData.AddCurrency(task.moneyReward);

        bool wasInitialTask = currentDaySet != null && taskId == $"day{gameData.CurrentDay}_initial";

        // Remove the completed task so it no longer shows up in the visible list.
        ActiveTasks.Remove(task);

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
                    followUp.npc));
            }
        }

        OnTaskListChanged?.Invoke();
    }
}