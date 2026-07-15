using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Populates a content box with one TaskEntryUI row per active task, refreshing
/// automatically as tasks progress. No panel show/hide — always visible/active.
/// </summary>
public class TaskListUI : MonoBehaviour
{
    [Tooltip("The TaskManager tracking the current day's tasks.")]
    [SerializeField] private TaskManager taskManager;

    [Tooltip("Prefab with a TaskEntryUI component.")]
    [SerializeField] private TaskEntryUI taskEntryPrefab;

    [Tooltip("Parent transform task rows get instantiated under — ideally has a layout group (e.g. Vertical Layout Group).")]
    [SerializeField] private Transform contentParent;

    [Tooltip("The content box GameObject to show/hide. If left empty, this GameObject itself is toggled.")]
    [SerializeField] private GameObject contentBox;

    [Tooltip("If true, the content box starts hidden when the scene loads.")]
    [SerializeField] private bool startHidden = true;

    private readonly List<TaskEntryUI> spawnedEntries = new List<TaskEntryUI>();

    private void Awake()
    {
        if (contentBox == null)
        {
            contentBox = gameObject;
        }

        if (startHidden)
        {
            contentBox.SetActive(false);
        }
    }

    private void OnEnable()
    {
        taskManager.OnTaskListChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDisable()
    {
        taskManager.OnTaskListChanged -= RefreshUI;
    }

    /// <summary>
    /// Hook this up to your button's OnClick() in the Inspector.
    /// Opens the content box if it's closed, closes it if it's open.
    /// </summary>
    public void ToggleContentBox()
    {
        bool newState = !contentBox.activeSelf;
        contentBox.SetActive(newState);

        if (newState)
        {
            RefreshUI(); // make sure it's showing the latest state the moment it opens
        }
    }

    private void RefreshUI()
    {
        foreach (TaskEntryUI entry in spawnedEntries)
        {
            if (entry != null)
            {
                Destroy(entry.gameObject);
            }
        }
        spawnedEntries.Clear();

        foreach (TaskItem task in taskManager.ActiveTasks)
        {
            TaskEntryUI newEntry = Instantiate(taskEntryPrefab, contentParent);
            newEntry.Setup(task);
            spawnedEntries.Add(newEntry);
        }
    }
}