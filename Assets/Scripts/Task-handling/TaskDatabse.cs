using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authored data defining, for each day, one initial task and the follow-up tasks
/// that get added once the initial task is completed. Each task carries a money
/// reward (paid via GameData.addMoney() when completed) and an NPC it's for.
///
/// SETUP: Right-click in the Project window -> Create -> Game Data -> Task Database.
/// Fill in one entry per day (1 through however many days you're tracking) in the Inspector.
/// </summary>
[CreateAssetMenu(fileName = "TaskDatabase", menuName = "Game Data/Task Database")]
public class TaskDatabase : ScriptableObject
{
    [Serializable]
    public class TaskDefinition
    {
        public string description;
        public int moneyReward;

        [Tooltip("Which NPC this task is for.")]
        public TaskItem.NPC npc;
    }

    [Serializable]
    public class DayTaskSet
    {
        [Tooltip("Which day this task set applies to (1, 2, 3...).")]
        public int day;

        [Tooltip("The single task assigned at the start of this day.")]
        public TaskDefinition initialTask;

        [Tooltip("Tasks added to the list once the initial task is completed.")]
        public List<TaskDefinition> followUpTasks;
    }

    [SerializeField] private List<DayTaskSet> daySets;

    /// <summary>
    /// Returns the task set for the given day, or null if none is defined.
    /// </summary>
    public DayTaskSet GetTasksForDay(int day)
    {
        return daySets.Find(d => d.day == day);
    }
}