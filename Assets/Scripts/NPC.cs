// using System.Collections.Generic;
// using UnityEngine;

// /// <summary>
// /// One sequence's starting node for a given day. An NPC's Day Settings entry holds a list of
// /// these - the NPC starts from whichever entry matches its CURRENT sequence number (see NPC's
// /// currentSequence field), not necessarily the first one in the list.
// /// </summary>
// [System.Serializable]
// public class SequenceStarter
// {
//     [Tooltip("Which sequence number this entry starts. Sequence numbering starts at 1.")]
//     public int sequence = 1;

//     [Tooltip("The dialogue tree root node played when the NPC is at this sequence.")]
//     public DialogueNode startNode;
// }

// /// <summary>
// /// Per-day dialogue configuration for one NPC: a list of sequence starters (see SequenceStarter).
// /// Delivery item acceptance now lives on individual dialogue response options (see
// /// DialogueResponseOption) rather than here - this keeps "what can be delivered" defined right
// /// next to the dialogue that handles the exchange, instead of a separate list that has to stay
// /// in sync with it.
// /// </summary>
// [System.Serializable]
// public class NPCDaySettings
// {
//     [Tooltip("Which day (matches DayManager.CurrentDay) this entry applies to.")]
//     public int day = 1;

//     [Tooltip("One entry per sequence this NPC can be at on this day. The NPC starts from " +
//              "whichever entry's 'sequence' matches its current sequence number.")]
//     public List<SequenceStarter> sequenceStarters = new List<SequenceStarter>();
// }

// /// <summary>
// /// A world NPC: detects the player via a trigger range, shows a "Speak (E)" tooltip
// /// (reusing WorldTooltip, same as Item's pickup tooltip), and starts a dialogue conversation
// /// through DialogueManager using whichever dialogue tree matches the current day AND the NPC's
// /// current sequence number.
// ///
// /// Sequence progression: starts at 1, advances by 1 whenever DialogueManager reaches a node
// /// with advancesSequence checked, and automatically resets to 1 whenever DayManager reports a
// /// new day - so a fresh day always starts every NPC back at their first sequence for that day.
// ///
// /// Requires the Player GameObject to be tagged "Player".
// /// </summary>
// [RequireComponent(typeof(SpriteRenderer))]
// public class NPC : MonoBehaviour
// {
//     [Header("Identity")]
//     [Tooltip("Unique identifier for this NPC, for later reference in code (save data, quest checks, etc).")]
//     public string npcID;
//     public string npcName;

//     [Header("References")]
//     [Tooltip("Sprite renderer for this NPC. Auto-found on this object if left empty.")]
//     [SerializeField] private SpriteRenderer spriteRenderer;
//     [Tooltip("Trigger collider defining the interaction range. Must have 'Is Trigger' enabled.")]
//     [SerializeField] private Collider2D interactionRange;
//     [Tooltip("WorldTooltip component showing 'Speak (E)' when the player is in range.")]
//     [SerializeField] private WorldTooltip worldTooltip;
//     [Tooltip("World-space point (usually just above the NPC's head) that the dialogue bubble " +
//              "positions itself above when this NPC is the speaker. Falls back to this object's " +
//              "own transform if left empty.")]
//     [SerializeField] private Transform dialogueAnchor;

//     /// <summary>The world-space point DialogueUI should position the bubble above for this NPC.</summary>
//     public Transform DialogueAnchor => dialogueAnchor != null ? dialogueAnchor : transform;

//     [Header("Per-Day Settings")]
//     [Tooltip("One entry per day this NPC has configured dialogue for. Avoid duplicate 'day' " +
//              "values - if two entries share the same day, whichever comes first in the list wins.")]
//     [SerializeField] private List<NPCDaySettings> daySettings = new List<NPCDaySettings>();

//     [Header("Merchant")]
//     [Tooltip("If checked, this NPC can be bought from / sold to via the shop UI (built later).")]
//     public bool isMerchant;
//     [Tooltip("Items this NPC has for sale, each using its own ItemData.purchasePrice. Only relevant if isMerchant is checked.")]
//     public List<ItemData> purchasableItems = new List<ItemData>();

//     /// <summary>Current dialogue sequence for this NPC. Starts at 1, advances via AdvanceSequence(), resets to 1 each new day.</summary>
//     public int CurrentSequence { get; private set; } = 1;

//     private bool playerInRange;
//     private bool isSubscribedToDayManager;

//     private void Awake()
//     {
//         if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
//         if (interactionRange == null) interactionRange = GetComponent<Collider2D>();

//         if (interactionRange != null && !interactionRange.isTrigger)
//         {
//             Debug.LogWarning($"NPC '{name}': interactionRange collider should have 'Is Trigger' enabled.");
//         }

//         if (worldTooltip != null) worldTooltip.SetText("Speak (E)");
//     }

//     private void OnEnable()
//     {
//         if (DayManager.Instance != null && !isSubscribedToDayManager)
//         {
//             DayManager.Instance.OnDayChanged += HandleDayChanged;
//             isSubscribedToDayManager = true;
//         }
//     }

//     private void OnDisable()
//     {
//         if (DayManager.Instance != null && isSubscribedToDayManager)
//         {
//             DayManager.Instance.OnDayChanged -= HandleDayChanged;
//             isSubscribedToDayManager = false;
//         }
//     }

//     private void HandleDayChanged(int newDay)
//     {
//         CurrentSequence = 1;
//     }

//     private void Update()
//     {
//         if (!playerInRange) return;
//         if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

//         if (Input.GetKeyDown(KeyCode.E))
//         {
//             TryStartDialogue();
//         }
//     }

//     private void TryStartDialogue()
//     {
//         DialogueNode startNode = GetCurrentSequenceStartNode();

//         if (startNode == null)
//         {
//             Debug.LogWarning($"NPC '{name}': no dialogue configured for day {CurrentDay()}, sequence {CurrentSequence}.");
//             return;
//         }

//         if (DialogueManager.Instance != null)
//         {
//             DialogueManager.Instance.StartDialogue(this, startNode);
//         }
//     }

//     /// <summary>Advances this NPC's dialogue sequence by 1. Called by DialogueManager when a node with advancesSequence is reached.</summary>
//     public void AdvanceSequence()
//     {
//         CurrentSequence++;
//     }

//     private int CurrentDay()
//     {
//         return DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
//     }

//     private DialogueNode GetCurrentSequenceStartNode()
//     {
//         NPCDaySettings settings = GetTodaysSettings();
//         if (settings == null) return null;

//         foreach (var starter in settings.sequenceStarters)
//         {
//             if (starter.sequence == CurrentSequence) return starter.startNode;
//         }
//         return null;
//     }

//     /// <summary>Returns this NPC's configured settings for the current day, or null if none exist for it.</summary>
//     public NPCDaySettings GetTodaysSettings()
//     {
//         int day = CurrentDay();
//         foreach (var settings in daySettings)
//         {
//             if (settings.day == day) return settings;
//         }
//         return null;
//     }

//     /// <summary>
//     /// Called by DialogueManager when a conversation starts, to make this NPC face the player.
//     /// The NPC's default (unflipped) art is assumed to face Right, same convention as the player -
//     /// so if the player is facing Right (an NPC to their right), the NPC must face Left to look
//     /// back at them, and vice versa.
//     /// </summary>
//     public void FaceTowardsPlayer(PlayerController2D.FacingDirection playerFacing)
//     {
//         if (spriteRenderer == null) return;
//         spriteRenderer.flipX = playerFacing == PlayerController2D.FacingDirection.Right;
//     }

//     /// <summary>Called by DialogueManager when a conversation with this NPC begins.</summary>
//     public void OnDialogueStarted()
//     {
//         if (worldTooltip != null) worldTooltip.Hide();
//     }

//     /// <summary>Called by DialogueManager when a conversation with this NPC ends.</summary>
//     public void OnDialogueEnded()
//     {
//         if (playerInRange && worldTooltip != null) worldTooltip.Show();
//     }

//     private void OnTriggerEnter2D(Collider2D other)
//     {
//         if (!other.CompareTag("Player")) return;

//         playerInRange = true;
//         if (worldTooltip != null) worldTooltip.Show();
//     }

//     private void OnTriggerExit2D(Collider2D other)
//     {
//         if (!other.CompareTag("Player")) return;

//         playerInRange = false;
//         if (worldTooltip != null) worldTooltip.Hide();
//     }
// }


using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One sequence's starting node for a given day. An NPC's Day Settings entry holds a list of
/// these - the NPC starts from whichever entry matches its CURRENT sequence number (see NPC's
/// currentSequence field), not necessarily the first one in the list.
/// </summary>
[System.Serializable]
public class SequenceStarter
{
    [Tooltip("Which sequence number this entry starts. Sequence numbering starts at 1.")]
    public int sequence = 1;

    [Tooltip("The dialogue tree root node played when the NPC is at this sequence.")]
    public DialogueNode startNode;
}

/// <summary>
/// Per-day dialogue configuration for one NPC: a list of sequence starters (see SequenceStarter).
/// Delivery item acceptance now lives on individual dialogue response options (see
/// DialogueResponseOption) rather than here - this keeps "what can be delivered" defined right
/// next to the dialogue that handles the exchange, instead of a separate list that has to stay
/// in sync with it.
/// </summary>
[System.Serializable]
public class NPCDaySettings
{
    [Tooltip("Which day (matches DayManager.CurrentDay) this entry applies to.")]
    public int day = 1;

    [Tooltip("One entry per sequence this NPC can be at on this day. The NPC starts from " +
             "whichever entry's 'sequence' matches its current sequence number.")]
    public List<SequenceStarter> sequenceStarters = new List<SequenceStarter>();
}

/// <summary>
/// A world NPC: detects the player via a trigger range, shows a "Speak (E)" tooltip
/// (reusing WorldTooltip, same as Item's pickup tooltip), and starts a dialogue conversation
/// through DialogueManager using whichever dialogue tree matches the current day AND the NPC's
/// current sequence number.
///
/// Sequence progression: starts at 1, advances by 1 whenever DialogueManager reaches a node
/// with advancesSequence checked, and automatically resets to 1 whenever DayManager reports a
/// new day - so a fresh day always starts every NPC back at their first sequence for that day.
///
/// Requires the Player GameObject to be tagged "Player".
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class NPC : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Unique identifier for this NPC, for later reference in code (save data, quest checks, etc).")]
    public string npcID;
    public string npcName;

    [Header("References")]
    [Tooltip("Sprite renderer for this NPC. Auto-found on this object if left empty.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Trigger collider defining the interaction range. Must have 'Is Trigger' enabled.")]
    [SerializeField] private Collider2D interactionRange;
    [Tooltip("WorldTooltip component showing 'Speak (E)' when the player is in range.")]
    [SerializeField] private WorldTooltip worldTooltip;
    [Tooltip("World-space point (usually just above the NPC's head) that the dialogue bubble " +
             "positions itself above when this NPC is the speaker. Falls back to this object's " +
             "own transform if left empty.")]
    [SerializeField] private Transform dialogueAnchor;

    /// <summary>The world-space point DialogueUI should position the bubble above for this NPC.</summary>
    public Transform DialogueAnchor => dialogueAnchor != null ? dialogueAnchor : transform;

    [Header("Per-Day Settings")]
    [Tooltip("One entry per day this NPC has configured dialogue for. Avoid duplicate 'day' " +
             "values - if two entries share the same day, whichever comes first in the list wins.")]
    [SerializeField] private List<NPCDaySettings> daySettings = new List<NPCDaySettings>();

    [Header("Merchant")]
    [Tooltip("If checked, this NPC can be bought from / sold to via the shop UI (built later).")]
    public bool isMerchant;
    [Tooltip("Items this NPC has for sale, each using its own ItemData.purchasePrice. Only relevant if isMerchant is checked.")]
    public List<ItemData> purchasableItems = new List<ItemData>();

    /// <summary>Current dialogue sequence for this NPC. Starts at 1, advances via AdvanceSequence(), resets to 1 each new day.</summary>
    public int CurrentSequence { get; private set; } = 1;

    private bool playerInRange;
    private bool isSubscribedToDayManager;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (interactionRange == null) interactionRange = GetComponent<Collider2D>();

        if (interactionRange != null && !interactionRange.isTrigger)
        {
            Debug.LogWarning($"NPC '{name}': interactionRange collider should have 'Is Trigger' enabled.");
        }

        if (worldTooltip != null) worldTooltip.SetText("Speak (E)");
    }

    private void OnEnable()
    {
        if (DayManager.Instance != null && !isSubscribedToDayManager)
        {
            DayManager.Instance.OnDayChanged += HandleDayChanged;
            isSubscribedToDayManager = true;
        }
    }

    private void OnDisable()
    {
        if (DayManager.Instance != null && isSubscribedToDayManager)
        {
            DayManager.Instance.OnDayChanged -= HandleDayChanged;
            isSubscribedToDayManager = false;
        }
    }

    private void HandleDayChanged(int newDay)
    {
        CurrentSequence = 1;
    }

    private void Update()
    {
        if (!playerInRange) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (ShopManager.Instance != null && ShopManager.Instance.IsShopOpen) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryStartDialogue();
        }
    }

    private void TryStartDialogue()
    {
        DialogueNode startNode = GetCurrentSequenceStartNode();

        if (startNode == null)
        {
            Debug.LogWarning($"NPC '{name}': no dialogue configured for day {CurrentDay()}, sequence {CurrentSequence}.");
            return;
        }

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(this, startNode);
        }
    }

    /// <summary>Advances this NPC's dialogue sequence by 1. Called by DialogueManager when a node with advancesSequence is reached.</summary>
    public void AdvanceSequence()
    {
        CurrentSequence++;
    }

    private int CurrentDay()
    {
        return DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
    }

    private DialogueNode GetCurrentSequenceStartNode()
    {
        NPCDaySettings settings = GetTodaysSettings();
        if (settings == null) return null;

        foreach (var starter in settings.sequenceStarters)
        {
            if (starter.sequence == CurrentSequence) return starter.startNode;
        }
        return null;
    }

    /// <summary>Returns this NPC's configured settings for the current day, or null if none exist for it.</summary>
    public NPCDaySettings GetTodaysSettings()
    {
        int day = CurrentDay();
        foreach (var settings in daySettings)
        {
            if (settings.day == day) return settings;
        }
        return null;
    }

    /// <summary>
    /// Called by DialogueManager when a conversation starts, to make this NPC face the player.
    /// The NPC's default (unflipped) art is assumed to face Right, same convention as the player -
    /// so if the player is facing Right (an NPC to their right), the NPC must face Left to look
    /// back at them, and vice versa.
    /// </summary>
    public void FaceTowardsPlayer(PlayerController2D.FacingDirection playerFacing)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.flipX = playerFacing == PlayerController2D.FacingDirection.Right;
    }

    /// <summary>Called by DialogueManager when a conversation with this NPC begins.</summary>
    public void OnDialogueStarted()
    {
        if (worldTooltip != null) worldTooltip.Hide();
    }

    /// <summary>Called by DialogueManager when a conversation with this NPC ends.</summary>
    public void OnDialogueEnded()
    {
        if (playerInRange && worldTooltip != null) worldTooltip.Show();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        if (worldTooltip != null) worldTooltip.Show();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        if (worldTooltip != null) worldTooltip.Hide();
    }
}