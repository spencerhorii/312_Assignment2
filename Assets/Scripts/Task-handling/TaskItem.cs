using System;

/// <summary>
/// A single task instance. Plain C# class (not a ScriptableObject) since these are
/// created/tracked at runtime, not authored as assets.
/// </summary>
[Serializable]
public class TaskItem
{
    public string id;
    public string description;
    public int moneyReward;
    public bool isCompleted;

    public enum NPC
    {
        Pendi,
        Pip
    }
    public NPC npc;

    public TaskItem(string id, string description, int moneyReward, NPC npc)
    {
        this.id = id;
        this.description = description;
        this.moneyReward = moneyReward;
        this.npc = npc;
        isCompleted = false;
    }
}