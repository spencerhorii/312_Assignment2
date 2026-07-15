using System;

/// <summary>
/// A single task instance. Plain C# class (not a ScriptableObject) since these are
/// created/tracked at runtime, not authored as assets.
///
/// Each task has two parts: reaching/finishing at startLocation (Part 1), then
/// finishing at endLocation (Part 2 — this is what completes the task).
/// </summary>
[Serializable]
public class TaskItem
{
    public string id;
    public string description;
    public int moneyReward;

    public string startLocation;
    public string endLocation;

    public bool isPart1Completed;
    public bool isCompleted; // true once Part 2 / the full task is done

    public TaskItem(string id, string description, int moneyReward, string startLocation, string endLocation)
    {
        this.id = id;
        this.description = description;
        this.moneyReward = moneyReward;
        this.startLocation = startLocation;
        this.endLocation = endLocation;
        isPart1Completed = false;
        isCompleted = false;
    }
}