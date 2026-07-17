using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One entry in a Type1 or Type2 response prompt. optionText is what's displayed;
/// nextNode is where the conversation continues if this option is chosen.
///
/// The award/remove fields below are primarily meant for Type1 (action) responses - e.g. a
/// "Yes, here's your delivery" option that removes a quest item and awards currency/an item
/// in return. They'll also appear on Type2 options in the Inspector since both share this
/// class, but are simply ignored if left unchecked there.
///
/// Removal happens via InventoryManager/QuestInventoryManager's two-phase TryRemoveItem() ->
/// (animation plays) -> ConfirmRemoval() flow, so the item isn't actually destroyed until
/// InventoryUI's shrink animation finishes - see those managers and InventoryUI for details.
/// </summary>
[System.Serializable]
public class DialogueResponseOption
{
    [Tooltip("Text shown for this option in the response bubble.")]
    public string optionText;

    [Tooltip("Where the conversation continues if this option is picked (and all requested " +
             "removals below succeeded). Leave empty to end the dialogue.")]
    public DialogueNode nextNode;

    [Tooltip("Free-form identifier read by future systems (Shop, etc) to know which action was chosen.")]
    public string actionID;

    [Header("Regular Item Actions")]
    public bool awardsItem;
    public ItemData itemToAward;
    public bool removesItem;
    public ItemData itemToRemove;

    [Header("Quest Item Actions")]
    public bool awardsQuestItem;
    public QuestItemData questItemToAward;
    public bool removesQuestItem;
    public QuestItemData questItemToRemove;

    [Header("Missing Item Fallback")]
    [TextArea(2, 3)]
    [Tooltip("If any 'removes' action above fails because the player doesn't actually have that " +
             "item, this message is shown instead of proceeding to nextNode, and no awards/removals " +
             "happen at all. Leave empty to just proceed to nextNode regardless (not recommended if " +
             "any removal is checked).")]
    public string itemMissingLineText;
}

public enum DialogueNodeType
{
    Line,
    Type1Response,
    Type2Response,
    End
}

/// <summary>
/// Who is "speaking" a node's text - determines where the main dialogue bubble appears.
/// Only affects the main dialogue bubble; the response options bubble always stays
/// centre-screen regardless of speaker.
/// </summary>
public enum DialogueSpeaker
{
    NPC,
    Player
}

/// <summary>
/// What this node should do to the background music when it's reached. None = leave music
/// alone. FadeOutCurrent = fade the currently playing track down. FadeInCurrent = fade the
/// currently playing track back up (resumes a previously faded-out track). QueueNewTrack =
/// crossfade into musicTrackToQueue.
/// </summary>
public enum DialogueMusicAction
{
    None,
    FadeOutCurrent,
    FadeInCurrent,
    QueueNewTrack
}

/// <summary>
/// One node in a dialogue tree, saved as its own asset (Create > Dialogue > Dialogue Node).
/// Build a conversation by creating a chain of these and linking them via nextNode /
/// response option nextNode fields - the whole tree is just asset references.
///
/// Every node type types out its own lineText before branching based on its nodeType -
/// see DialogueManager for the exact flow. A single node can award a regular item, a quest
/// item, or both - see DialogueManager for how the two interact if both are set on one node.
///
/// Day advancement and task completion (below) reference the SAME shared GameData/TaskManager
/// ScriptableObject assets used everywhere else in the project - drag in those same assets here.
/// This node is pure data - DialogueManager is what actually calls AdvanceDay()/CompleteTask()
/// when it processes a node with these fields set, since a ScriptableObject has no Update() of
/// its own to act on its own data.
/// </summary>
[CreateAssetMenu(fileName = "NewDialogueNode", menuName = "Dialogue/Dialogue Node")]
public class DialogueNode : ScriptableObject
{
    [Tooltip("What kind of node this is. Line = a spoken line of dialogue. Type1Response/Type2Response = " +
             "a response prompt. End = explicitly terminates the conversation.")]
    public DialogueNodeType nodeType = DialogueNodeType.Line;

    [Header("Text (all node types)")]
    [TextArea(2, 5)]
    [Tooltip("The text typed out character-by-character for this node.")]
    public string lineText;

    [Tooltip("Who is speaking this line - determines where the dialogue bubble appears. " +
             "NPC = positioned above the NPC's head. Player = positioned at the centre of the screen.")]
    public DialogueSpeaker speaker = DialogueSpeaker.NPC;

    [Header("Sequence Progression")]
    [Tooltip("If checked, reaching this node advances the speaking NPC's dialogue sequence by 1 " +
             "once its text finishes typing. Use this on whichever node marks the natural end of a " +
             "sequence, so the NPC starts from a different Sequence Starter next time you talk to them.")]
    public bool advancesSequence;

    [Header("Music (optional, any node type)")]
    [Tooltip("What to do to the background music when this node is reached.")]
    public DialogueMusicAction musicAction = DialogueMusicAction.None;
    [Tooltip("QueueNewTrack only: the track to crossfade into.")]
    public AudioClip musicTrackToQueue;
    [Tooltip("How long the fade/crossfade takes, in seconds.")]
    public float musicFadeDuration = 1f;

    [Header("Line (nodeType = Line)")]
    [Tooltip("What happens after this line finishes: another Line, a response prompt, an End node, " +
             "or leave empty to end the conversation here.")]
    public DialogueNode nextNode;

    [Header("Item Award (optional, any node type)")]
    [Tooltip("If checked, the item below is given to the player once this node's text finishes typing " +
             "- but only the first time. Repeat visits show alreadyAwardedLineText instead and do not re-award.")]
    public bool awardsItem;
    [Tooltip("The item to award.")]
    public ItemData itemToAward;
    [TextArea(2, 3)]
    [Tooltip("If the player's inventory is full (and the item hasn't been awarded yet), this text is shown " +
             "instead of lineText, and the item is NOT awarded. Leave empty to just show lineText as normal.")]
    public string inventoryFullLineText;
    [TextArea(2, 3)]
    [Tooltip("Shown instead of lineText if this item has already been awarded once before (in this play " +
             "session). The item is never given twice. Leave empty to just show lineText again without re-awarding.")]
    public string alreadyAwardedLineText;

    [Header("Quest Item Award (optional, any node type)")]
    [Tooltip("If checked, the quest item below is given to the player once this node's text finishes " +
             "typing - but only the first time. Quest inventory is unbounded, so there's no 'full' case " +
             "to handle here, unlike regular item awards.")]
    public bool awardsQuestItem;
    [Tooltip("The quest item to award.")]
    public QuestItemData questItemToAward;
    [TextArea(2, 3)]
    [Tooltip("Shown instead of lineText if this quest item has already been awarded once before. " +
             "If both awardsItem and awardsQuestItem are set and both are already-awarded, this text " +
             "takes priority over alreadyAwardedLineText. Leave empty to just show lineText again.")]
    public string alreadyAwardedQuestItemLineText;

    [Header("Opens Shop (optional, any node type)")]
    [Tooltip("If checked, once this node's text finishes typing (or immediately, if lineText is " +
             "empty), the conversation ends and the shop UI opens automatically using shopToOpen.")]
    public bool opensShop;
    [Tooltip("Which shop's stock to open. Required if opensShop is checked.")]
    public ShopData shopToOpen;

    [Header("Day Advancement (optional, any node type)")]
    [Tooltip("If checked, GameData.AdvanceDay() is called once this node's text finishes typing. " +
             "This is blocked internally by GameData's own canAdvance lock if the day's primary task " +
             "hasn't been completed yet, so it's safe to place this on a node the player can reach " +
             "early - nothing happens until the task is actually done.")]
    public bool triggersDayAdvance;
    [Tooltip("The shared GameData asset - drag in the SAME asset used everywhere else in the project.")]
    public GameData gameData;

    [Header("Bani Favour (optional, any node type)")]
    [Tooltip("If checked, gameData.AddBaniFavour(baniFavourAmount) is called once this node's text " +
            "finishes typing. Uses the same shared GameData asset dragged in above (Day Advancement's " +
            "gameData field) - no separate reference needed.")]
    public bool incrementsBaniFavour;
    [Tooltip("Amount to add to Bani Favour when this node fires. Can be negative to decrease it.")]
    public int baniFavourAmount = 1;


    [Header("Task Completion (optional, any node type)")]
    [Tooltip("If checked, taskManager.CompleteTask(taskIdToComplete) is called once this node's " +
             "text finishes typing.")]
    public bool completesTask;
    [Tooltip("The shared TaskManager asset - drag in the SAME asset used everywhere else in the project.")]
    public TaskManager taskManager;
    [Tooltip("The exact task ID to complete, matching TaskManager's own ID convention: " +
             "\"day{N}_initial\" for a day's primary task, or \"day{N}_followup{index}\" for a " +
             "follow-up task (e.g. \"day3_initial\", \"day3_followup0\"). Must match exactly, " +
             "including the day number - a mismatch here silently does nothing (TaskManager just " +
             "won't find a matching task).")]
    public string taskIdToComplete;

    [Header("Advance NPC Sequence on Primary Task Completion")]
    [Tooltip("If checked, and taskIdToComplete above matches the CURRENT day's primary task ID " +
             "(\"day{CurrentDay}_initial\"), the NPC identified by targetNpcID below has its dialogue " +
             "sequence set DIRECTLY to advanceSequenceTarget (not just +1) once the task completes. " +
             "Only takes effect if completesTask is also checked.")]
    public bool advanceSequenceByPrimaryTask;
    [Tooltip("The npcID of the NPC whose sequence should be updated. This does NOT have to be the " +
             "NPC currently being spoken to - e.g. completing a delivery via NPC A can update NPC B's " +
             "dialogue state. Must match that NPC's npcID field exactly. Works even if that NPC isn't " +
             "loaded in the current scene, since this writes directly to GameData. Leave empty to fall " +
             "back to whichever NPC is currently being spoken to (old behavior).")]
    public string targetNpcID;
    [Tooltip("The sequence number to jump the NPC to (e.g. 5).")]
    public int advanceSequenceTarget = 1;

    [Header("Type1 Responses (nodeType = Type1Response) - action options, e.g. Shop/Deliver/Leave")]
    public List<DialogueResponseOption> type1Options = new List<DialogueResponseOption>();

    [Header("Type2 Responses (nodeType = Type2Response) - branching dialogue options, e.g. Yes/No")]
    public List<DialogueResponseOption> type2Options = new List<DialogueResponseOption>();
}

// // using System.Collections.Generic;
// // using UnityEngine;

// // /// <summary>
// // /// One entry in a Type1 or Type2 response prompt. optionText is what's displayed;
// // /// nextNode is where the conversation continues if this option is chosen.
// // ///
// // /// The award/remove fields below are primarily meant for Type1 (action) responses - e.g. a
// // /// "Yes, here's your delivery" option that removes a quest item and awards currency/an item
// // /// in return. They'll also appear on Type2 options in the Inspector since both share this
// // /// class, but are simply ignored if left unchecked there.
// // ///
// // /// Removal happens via InventoryManager/QuestInventoryManager's two-phase TryRemoveItem() ->
// // /// (animation plays) -> ConfirmRemoval() flow, so the item isn't actually destroyed until
// // /// InventoryUI's shrink animation finishes - see those managers and InventoryUI for details.
// // /// </summary>
// // [System.Serializable]
// // public class DialogueResponseOption
// // {
// //     [Tooltip("Text shown for this option in the response bubble.")]
// //     public string optionText;

// //     [Tooltip("Where the conversation continues if this option is picked (and all requested " +
// //              "removals below succeeded). Leave empty to end the dialogue.")]
// //     public DialogueNode nextNode;

// //     [Tooltip("Free-form identifier read by future systems (Shop, etc) to know which action was chosen.")]
// //     public string actionID;

// //     [Header("Regular Item Actions")]
// //     public bool awardsItem;
// //     public ItemData itemToAward;
// //     public bool removesItem;
// //     public ItemData itemToRemove;

// //     [Header("Quest Item Actions")]
// //     public bool awardsQuestItem;
// //     public QuestItemData questItemToAward;
// //     public bool removesQuestItem;
// //     public QuestItemData questItemToRemove;

// //     [Header("Missing Item Fallback")]
// //     [TextArea(2, 3)]
// //     [Tooltip("If any 'removes' action above fails because the player doesn't actually have that " +
// //              "item, this message is shown instead of proceeding to nextNode, and no awards/removals " +
// //              "happen at all. Leave empty to just proceed to nextNode regardless (not recommended if " +
// //              "any removal is checked).")]
// //     public string itemMissingLineText;
// // }

// // public enum DialogueNodeType
// // {
// //     Line,
// //     Type1Response,
// //     Type2Response,
// //     End
// // }

// // /// <summary>
// // /// Who is "speaking" a node's text - determines where the main dialogue bubble appears.
// // /// Only affects the main dialogue bubble; the response options bubble always stays
// // /// centre-screen regardless of speaker.
// // /// </summary>
// // public enum DialogueSpeaker
// // {
// //     NPC,
// //     Player
// // }

// // /// <summary>
// // /// One node in a dialogue tree, saved as its own asset (Create > Dialogue > Dialogue Node).
// // /// Build a conversation by creating a chain of these and linking them via nextNode /
// // /// response option nextNode fields - the whole tree is just asset references.
// // ///
// // /// Every node type types out its own lineText before branching based on its nodeType -
// // /// see DialogueManager for the exact flow. A single node can award a regular item, a quest
// // /// item, or both - see DialogueManager for how the two interact if both are set on one node.
// // /// </summary>
// // [CreateAssetMenu(fileName = "NewDialogueNode", menuName = "Dialogue/Dialogue Node")]
// // public class DialogueNode : ScriptableObject
// // {
// //     [Tooltip("What kind of node this is. Line = a spoken line of dialogue. Type1Response/Type2Response = " +
// //              "a response prompt. End = explicitly terminates the conversation.")]
// //     public DialogueNodeType nodeType = DialogueNodeType.Line;

// //     [Header("Text (all node types)")]
// //     [TextArea(2, 5)]
// //     [Tooltip("The text typed out character-by-character for this node.")]
// //     public string lineText;

// //     [Tooltip("Who is speaking this line - determines where the dialogue bubble appears. " +
// //              "NPC = positioned above the NPC's head. Player = positioned at the centre of the screen.")]
// //     public DialogueSpeaker speaker = DialogueSpeaker.NPC;

// //     [Header("Sequence Progression")]
// //     [Tooltip("If checked, reaching this node advances the speaking NPC's dialogue sequence by 1 " +
// //              "once its text finishes typing. Use this on whichever node marks the natural end of a " +
// //              "sequence, so the NPC starts from a different Sequence Starter next time you talk to them.")]
// //     public bool advancesSequence;

// //     [Header("Line (nodeType = Line)")]
// //     [Tooltip("What happens after this line finishes: another Line, a response prompt, an End node, " +
// //              "or leave empty to end the conversation here.")]
// //     public DialogueNode nextNode;

// //     [Header("Item Award (optional, any node type)")]
// //     [Tooltip("If checked, the item below is given to the player once this node's text finishes typing " +
// //              "- but only the first time. Repeat visits show alreadyAwardedLineText instead and do not re-award.")]
// //     public bool awardsItem;
// //     [Tooltip("The item to award.")]
// //     public ItemData itemToAward;
// //     [TextArea(2, 3)]
// //     [Tooltip("If the player's inventory is full (and the item hasn't been awarded yet), this text is shown " +
// //              "instead of lineText, and the item is NOT awarded. Leave empty to just show lineText as normal.")]
// //     public string inventoryFullLineText;
// //     [TextArea(2, 3)]
// //     [Tooltip("Shown instead of lineText if this item has already been awarded once before (in this play " +
// //              "session). The item is never given twice. Leave empty to just show lineText again without re-awarding.")]
// //     public string alreadyAwardedLineText;

// //     [Header("Quest Item Award (optional, any node type)")]
// //     [Tooltip("If checked, the quest item below is given to the player once this node's text finishes " +
// //              "typing - but only the first time. Quest inventory is unbounded, so there's no 'full' case " +
// //              "to handle here, unlike regular item awards.")]
// //     public bool awardsQuestItem;
// //     [Tooltip("The quest item to award.")]
// //     public QuestItemData questItemToAward;
// //     [TextArea(2, 3)]
// //     [Tooltip("Shown instead of lineText if this quest item has already been awarded once before. " +
// //              "If both awardsItem and awardsQuestItem are set and both are already-awarded, this text " +
// //              "takes priority over alreadyAwardedLineText. Leave empty to just show lineText again.")]
// //     public string alreadyAwardedQuestItemLineText;

// //     [Header("Type1 Responses (nodeType = Type1Response) - action options, e.g. Shop/Deliver/Leave")]
// //     public List<DialogueResponseOption> type1Options = new List<DialogueResponseOption>();

// //     [Header("Type2 Responses (nodeType = Type2Response) - branching dialogue options, e.g. Yes/No")]
// //     public List<DialogueResponseOption> type2Options = new List<DialogueResponseOption>();
// // }


// // using System.Collections.Generic;
// // using UnityEngine;

// // /// <summary>
// // /// One entry in a Type1 or Type2 response prompt. optionText is what's displayed;
// // /// nextNode is where the conversation continues if this option is chosen.
// // ///
// // /// The award/remove fields below are primarily meant for Type1 (action) responses - e.g. a
// // /// "Yes, here's your delivery" option that removes a quest item and awards currency/an item
// // /// in return. They'll also appear on Type2 options in the Inspector since both share this
// // /// class, but are simply ignored if left unchecked there.
// // ///
// // /// Removal happens via InventoryManager/QuestInventoryManager's two-phase TryRemoveItem() ->
// // /// (animation plays) -> ConfirmRemoval() flow, so the item isn't actually destroyed until
// // /// InventoryUI's shrink animation finishes - see those managers and InventoryUI for details.
// // /// </summary>
// // [System.Serializable]
// // public class DialogueResponseOption
// // {
// //     [Tooltip("Text shown for this option in the response bubble.")]
// //     public string optionText;

// //     [Tooltip("Where the conversation continues if this option is picked (and all requested " +
// //              "removals below succeeded). Leave empty to end the dialogue.")]
// //     public DialogueNode nextNode;

// //     [Tooltip("Free-form identifier read by future systems (Shop, etc) to know which action was chosen.")]
// //     public string actionID;

// //     [Header("Regular Item Actions")]
// //     public bool awardsItem;
// //     public ItemData itemToAward;
// //     public bool removesItem;
// //     public ItemData itemToRemove;

// //     [Header("Quest Item Actions")]
// //     public bool awardsQuestItem;
// //     public QuestItemData questItemToAward;
// //     public bool removesQuestItem;
// //     public QuestItemData questItemToRemove;

// //     [Header("Missing Item Fallback")]
// //     [TextArea(2, 3)]
// //     [Tooltip("If any 'removes' action above fails because the player doesn't actually have that " +
// //              "item, this message is shown instead of proceeding to nextNode, and no awards/removals " +
// //              "happen at all. Leave empty to just proceed to nextNode regardless (not recommended if " +
// //              "any removal is checked).")]
// //     public string itemMissingLineText;
// // }

// // public enum DialogueNodeType
// // {
// //     Line,
// //     Type1Response,
// //     Type2Response,
// //     End
// // }

// // /// <summary>
// // /// Who is "speaking" a node's text - determines where the main dialogue bubble appears.
// // /// Only affects the main dialogue bubble; the response options bubble always stays
// // /// centre-screen regardless of speaker.
// // /// </summary>
// // public enum DialogueSpeaker
// // {
// //     NPC,
// //     Player
// // }

// // /// <summary>
// // /// One node in a dialogue tree, saved as its own asset (Create > Dialogue > Dialogue Node).
// // /// Build a conversation by creating a chain of these and linking them via nextNode /
// // /// response option nextNode fields - the whole tree is just asset references.
// // ///
// // /// Every node type types out its own lineText before branching based on its nodeType -
// // /// see DialogueManager for the exact flow. A single node can award a regular item, a quest
// // /// item, or both - see DialogueManager for how the two interact if both are set on one node.
// // /// </summary>
// // [CreateAssetMenu(fileName = "NewDialogueNode", menuName = "Dialogue/Dialogue Node")]
// // public class DialogueNode : ScriptableObject
// // {
// //     [Tooltip("What kind of node this is. Line = a spoken line of dialogue. Type1Response/Type2Response = " +
// //              "a response prompt. End = explicitly terminates the conversation.")]
// //     public DialogueNodeType nodeType = DialogueNodeType.Line;

// //     [Header("Text (all node types)")]
// //     [TextArea(2, 5)]
// //     [Tooltip("The text typed out character-by-character for this node.")]
// //     public string lineText;

// //     [Tooltip("Who is speaking this line - determines where the dialogue bubble appears. " +
// //              "NPC = positioned above the NPC's head. Player = positioned at the centre of the screen.")]
// //     public DialogueSpeaker speaker = DialogueSpeaker.NPC;

// //     [Header("Sequence Progression")]
// //     [Tooltip("If checked, reaching this node advances the speaking NPC's dialogue sequence by 1 " +
// //              "once its text finishes typing. Use this on whichever node marks the natural end of a " +
// //              "sequence, so the NPC starts from a different Sequence Starter next time you talk to them.")]
// //     public bool advancesSequence;

// //     [Header("Line (nodeType = Line)")]
// //     [Tooltip("What happens after this line finishes: another Line, a response prompt, an End node, " +
// //              "or leave empty to end the conversation here.")]
// //     public DialogueNode nextNode;

// //     [Header("Item Award (optional, any node type)")]
// //     [Tooltip("If checked, the item below is given to the player once this node's text finishes typing " +
// //              "- but only the first time. Repeat visits show alreadyAwardedLineText instead and do not re-award.")]
// //     public bool awardsItem;
// //     [Tooltip("The item to award.")]
// //     public ItemData itemToAward;
// //     [TextArea(2, 3)]
// //     [Tooltip("If the player's inventory is full (and the item hasn't been awarded yet), this text is shown " +
// //              "instead of lineText, and the item is NOT awarded. Leave empty to just show lineText as normal.")]
// //     public string inventoryFullLineText;
// //     [TextArea(2, 3)]
// //     [Tooltip("Shown instead of lineText if this item has already been awarded once before (in this play " +
// //              "session). The item is never given twice. Leave empty to just show lineText again without re-awarding.")]
// //     public string alreadyAwardedLineText;

// //     [Header("Quest Item Award (optional, any node type)")]
// //     [Tooltip("If checked, the quest item below is given to the player once this node's text finishes " +
// //              "typing - but only the first time. Quest inventory is unbounded, so there's no 'full' case " +
// //              "to handle here, unlike regular item awards.")]
// //     public bool awardsQuestItem;
// //     [Tooltip("The quest item to award.")]
// //     public QuestItemData questItemToAward;
// //     [TextArea(2, 3)]
// //     [Tooltip("Shown instead of lineText if this quest item has already been awarded once before. " +
// //              "If both awardsItem and awardsQuestItem are set and both are already-awarded, this text " +
// //              "takes priority over alreadyAwardedLineText. Leave empty to just show lineText again.")]
// //     public string alreadyAwardedQuestItemLineText;

// //     [Header("Opens Shop (optional, any node type)")]
// //     [Tooltip("If checked, once this node's text finishes typing (or immediately, if lineText is " +
// //              "empty), the conversation ends and the shop UI opens automatically using shopToOpen.")]
// //     public bool opensShop;
// //     [Tooltip("Which shop's stock to open. Required if opensShop is checked.")]
// //     public ShopData shopToOpen;

// //     [Header("Type1 Responses (nodeType = Type1Response) - action options, e.g. Shop/Deliver/Leave")]
// //     public List<DialogueResponseOption> type1Options = new List<DialogueResponseOption>();

// //     [Header("Type2 Responses (nodeType = Type2Response) - branching dialogue options, e.g. Yes/No")]
// //     public List<DialogueResponseOption> type2Options = new List<DialogueResponseOption>();
// // }




























// // using System.Collections.Generic;
// // using UnityEngine;

// // /// <summary>
// // /// One entry in a Type1 or Type2 response prompt. optionText is what's displayed;
// // /// nextNode is where the conversation continues if this option is chosen.
// // ///
// // /// The award/remove fields below are primarily meant for Type1 (action) responses - e.g. a
// // /// "Yes, here's your delivery" option that removes a quest item and awards currency/an item
// // /// in return. They'll also appear on Type2 options in the Inspector since both share this
// // /// class, but are simply ignored if left unchecked there.
// // ///
// // /// Removal happens via InventoryManager/QuestInventoryManager's two-phase TryRemoveItem() ->
// // /// (animation plays) -> ConfirmRemoval() flow, so the item isn't actually destroyed until
// // /// InventoryUI's shrink animation finishes - see those managers and InventoryUI for details.
// // /// </summary>
// // [System.Serializable]
// // public class DialogueResponseOption
// // {
// //     [Tooltip("Text shown for this option in the response bubble.")]
// //     public string optionText;

// //     [Tooltip("Where the conversation continues if this option is picked (and all requested " +
// //              "removals below succeeded). Leave empty to end the dialogue.")]
// //     public DialogueNode nextNode;

// //     [Tooltip("Free-form identifier read by future systems (Shop, etc) to know which action was chosen.")]
// //     public string actionID;

// //     [Header("Regular Item Actions")]
// //     public bool awardsItem;
// //     public ItemData itemToAward;
// //     public bool removesItem;
// //     public ItemData itemToRemove;

// //     [Header("Quest Item Actions")]
// //     public bool awardsQuestItem;
// //     public QuestItemData questItemToAward;
// //     public bool removesQuestItem;
// //     public QuestItemData questItemToRemove;

// //     [Header("Missing Item Fallback")]
// //     [TextArea(2, 3)]
// //     [Tooltip("If any 'removes' action above fails because the player doesn't actually have that " +
// //              "item, this message is shown instead of proceeding to nextNode, and no awards/removals " +
// //              "happen at all. Leave empty to just proceed to nextNode regardless (not recommended if " +
// //              "any removal is checked).")]
// //     public string itemMissingLineText;
// // }

// // public enum DialogueNodeType
// // {
// //     Line,
// //     Type1Response,
// //     Type2Response,
// //     End
// // }

// // /// <summary>
// // /// Who is "speaking" a node's text - determines where the main dialogue bubble appears.
// // /// Only affects the main dialogue bubble; the response options bubble always stays
// // /// centre-screen regardless of speaker.
// // /// </summary>
// // public enum DialogueSpeaker
// // {
// //     NPC,
// //     Player
// // }

// // public enum DialogueMusicAction
// // {
// //     None,
// //     FadeOutCurrent,
// //     ResumeCurrent,
// //     ChangeTrack
// // }

// // /// <summary>
// // /// One node in a dialogue tree, saved as its own asset (Create > Dialogue > Dialogue Node).
// // /// Build a conversation by creating a chain of these and linking them via nextNode /
// // /// response option nextNode fields - the whole tree is just asset references.
// // ///
// // /// Every node type types out its own lineText before branching based on its nodeType -
// // /// see DialogueManager for the exact flow. A single node can award a regular item, a quest
// // /// item, or both - see DialogueManager for how the two interact if both are set on one node.
// // /// </summary>
// // [CreateAssetMenu(fileName = "NewDialogueNode", menuName = "Dialogue/Dialogue Node")]
// // public class DialogueNode : ScriptableObject
// // {
// //     [Tooltip("What kind of node this is. Line = a spoken line of dialogue. Type1Response/Type2Response = " +
// //              "a response prompt. End = explicitly terminates the conversation.")]
// //     public DialogueNodeType nodeType = DialogueNodeType.Line;

// //     [Header("Text (all node types)")]
// //     [TextArea(2, 5)]
// //     [Tooltip("The text typed out character-by-character for this node.")]
// //     public string lineText;

// //     [Tooltip("Who is speaking this line - determines where the dialogue bubble appears. " +
// //              "NPC = positioned above the NPC's head. Player = positioned at the centre of the screen.")]
// //     public DialogueSpeaker speaker = DialogueSpeaker.NPC;

// //     [Header("Sequence Progression")]
// //     [Tooltip("If checked, reaching this node advances the speaking NPC's dialogue sequence by 1 " +
// //              "once its text finishes typing. Use this on whichever node marks the natural end of a " +
// //              "sequence, so the NPC starts from a different Sequence Starter next time you talk to them.")]
// //     public bool advancesSequence;

// //     [Header("Line (nodeType = Line)")]
// //     [Tooltip("What happens after this line finishes: another Line, a response prompt, an End node, " +
// //              "or leave empty to end the conversation here.")]
// //     public DialogueNode nextNode;

// //     [Header("Item Award (optional, any node type)")]
// //     [Tooltip("If checked, the item below is given to the player once this node's text finishes typing " +
// //              "- but only the first time. Repeat visits show alreadyAwardedLineText instead and do not re-award.")]
// //     public bool awardsItem;
// //     [Tooltip("The item to award.")]
// //     public ItemData itemToAward;
// //     [TextArea(2, 3)]
// //     [Tooltip("If the player's inventory is full (and the item hasn't been awarded yet), this text is shown " +
// //              "instead of lineText, and the item is NOT awarded. Leave empty to just show lineText as normal.")]
// //     public string inventoryFullLineText;
// //     [TextArea(2, 3)]
// //     [Tooltip("Shown instead of lineText if this item has already been awarded once before (in this play " +
// //              "session). The item is never given twice. Leave empty to just show lineText again without re-awarding.")]
// //     public string alreadyAwardedLineText;

// //     [Header("Quest Item Award (optional, any node type)")]
// //     [Tooltip("If checked, the quest item below is given to the player once this node's text finishes " +
// //              "typing - but only the first time. Quest inventory is unbounded, so there's no 'full' case " +
// //              "to handle here, unlike regular item awards.")]
// //     public bool awardsQuestItem;
// //     [Tooltip("The quest item to award.")]
// //     public QuestItemData questItemToAward;
// //     [TextArea(2, 3)]
// //     [Tooltip("Shown instead of lineText if this quest item has already been awarded once before. " +
// //              "If both awardsItem and awardsQuestItem are set and both are already-awarded, this text " +
// //              "takes priority over alreadyAwardedLineText. Leave empty to just show lineText again.")]
// //     public string alreadyAwardedQuestItemLineText;

// //     [Header("Opens Shop (optional, any node type)")]
// //     [Tooltip("If checked, once this node's text finishes typing (or immediately, if lineText is " +
// //              "empty), the conversation ends and the shop UI opens automatically using shopToOpen.")]
// //     public bool opensShop;
// //     [Tooltip("Which shop's stock to open. Required if opensShop is checked.")]
// //     public ShopData shopToOpen;

// //     [Header("Music (optional, any node type)")]

// //     [Tooltip("Optional music behaviour that occurs as soon as this dialogue node becomes active.")]
// //     public DialogueMusicAction musicAction = DialogueMusicAction.None;

// //     [Tooltip("Music clip used when Music Action is Change Track.")]
// //     public AudioClip musicTrack;

// //     [Tooltip("How long the music transition should take.")]
// //     [Min(0f)]
// //     public float musicFadeDuration = 1f;

// //     [Header("Type1 Responses (nodeType = Type1Response) - action options, e.g. Shop/Deliver/Leave")]
// //     public List<DialogueResponseOption> type1Options = new List<DialogueResponseOption>();

// //     [Header("Type2 Responses (nodeType = Type2Response) - branching dialogue options, e.g. Yes/No")]
// //     public List<DialogueResponseOption> type2Options = new List<DialogueResponseOption>();
// // }




















// using System.Collections.Generic;
// using UnityEngine;

// /// <summary>
// /// One entry in a Type1 or Type2 response prompt. optionText is what's displayed;
// /// nextNode is where the conversation continues if this option is chosen.
// ///
// /// The award/remove fields below are primarily meant for Type1 (action) responses - e.g. a
// /// "Yes, here's your delivery" option that removes a quest item and awards currency/an item
// /// in return. They'll also appear on Type2 options in the Inspector since both share this
// /// class, but are simply ignored if left unchecked there.
// ///
// /// Removal happens via InventoryManager/QuestInventoryManager's two-phase TryRemoveItem() ->
// /// (animation plays) -> ConfirmRemoval() flow, so the item isn't actually destroyed until
// /// InventoryUI's shrink animation finishes - see those managers and InventoryUI for details.
// /// </summary>
// [System.Serializable]
// public class DialogueResponseOption
// {
//     [Tooltip("Text shown for this option in the response bubble.")]
//     public string optionText;

//     [Tooltip("Where the conversation continues if this option is picked (and all requested " +
//              "removals below succeeded). Leave empty to end the dialogue.")]
//     public DialogueNode nextNode;

//     [Tooltip("Free-form identifier read by future systems (Shop, etc) to know which action was chosen.")]
//     public string actionID;

//     [Header("Regular Item Actions")]
//     public bool awardsItem;
//     public ItemData itemToAward;
//     public bool removesItem;
//     public ItemData itemToRemove;

//     [Header("Quest Item Actions")]
//     public bool awardsQuestItem;
//     public QuestItemData questItemToAward;
//     public bool removesQuestItem;
//     public QuestItemData questItemToRemove;

//     [Header("Missing Item Fallback")]
//     [TextArea(2, 3)]
//     [Tooltip("If any 'removes' action above fails because the player doesn't actually have that " +
//              "item, this message is shown instead of proceeding to nextNode, and no awards/removals " +
//              "happen at all. Leave empty to just proceed to nextNode regardless (not recommended if " +
//              "any removal is checked).")]
//     public string itemMissingLineText;
// }

// public enum DialogueNodeType
// {
//     Line,
//     Type1Response,
//     Type2Response,
//     End
// }

// /// <summary>
// /// Who is "speaking" a node's text - determines where the main dialogue bubble appears.
// /// Only affects the main dialogue bubble; the response options bubble always stays
// /// centre-screen regardless of speaker.
// /// </summary>
// public enum DialogueSpeaker
// {
//     NPC,
//     Player
// }

// /// <summary>
// /// What this node should do to the background music when it's reached. None = leave music
// /// alone. FadeOutCurrent = fade the currently playing track down. FadeInCurrent = fade the
// /// currently playing track back up (resumes a previously faded-out track). QueueNewTrack =
// /// crossfade into musicTrackToQueue.
// /// </summary>
// public enum DialogueMusicAction
// {
//     None,
//     FadeOutCurrent,
//     FadeInCurrent,
//     QueueNewTrack
// }

// /// <summary>
// /// One node in a dialogue tree, saved as its own asset (Create > Dialogue > Dialogue Node).
// /// Build a conversation by creating a chain of these and linking them via nextNode /
// /// response option nextNode fields - the whole tree is just asset references.
// ///
// /// Every node type types out its own lineText before branching based on its nodeType -
// /// see DialogueManager for the exact flow. A single node can award a regular item, a quest
// /// item, or both - see DialogueManager for how the two interact if both are set on one node.
// ///
// /// Day advancement and task completion (below) reference the SAME shared GameData/TaskManager
// /// ScriptableObject assets used everywhere else in the project - drag in those same assets here.
// /// This node is pure data - DialogueManager is what actually calls AdvanceDay()/CompleteTask()
// /// when it processes a node with these fields set, since a ScriptableObject has no Update() of
// /// its own to act on its own data.
// /// </summary>
// [CreateAssetMenu(fileName = "NewDialogueNode", menuName = "Dialogue/Dialogue Node")]
// public class DialogueNode : ScriptableObject
// {
//     [Tooltip("What kind of node this is. Line = a spoken line of dialogue. Type1Response/Type2Response = " +
//              "a response prompt. End = explicitly terminates the conversation.")]
//     public DialogueNodeType nodeType = DialogueNodeType.Line;

//     [Header("Text (all node types)")]
//     [TextArea(2, 5)]
//     [Tooltip("The text typed out character-by-character for this node.")]
//     public string lineText;

//     [Tooltip("Who is speaking this line - determines where the dialogue bubble appears. " +
//              "NPC = positioned above the NPC's head. Player = positioned at the centre of the screen.")]
//     public DialogueSpeaker speaker = DialogueSpeaker.NPC;

//     [Header("Sequence Progression")]
//     [Tooltip("If checked, reaching this node advances the speaking NPC's dialogue sequence by 1 " +
//              "once its text finishes typing. Use this on whichever node marks the natural end of a " +
//              "sequence, so the NPC starts from a different Sequence Starter next time you talk to them.")]
//     public bool advancesSequence;

//     [Header("Music (optional, any node type)")]
//     [Tooltip("What to do to the background music when this node is reached.")]
//     public DialogueMusicAction musicAction = DialogueMusicAction.None;
//     [Tooltip("QueueNewTrack only: the track to crossfade into.")]
//     public AudioClip musicTrackToQueue;
//     [Tooltip("How long the fade/crossfade takes, in seconds.")]
//     public float musicFadeDuration = 1f;

//     [Header("Line (nodeType = Line)")]
//     [Tooltip("What happens after this line finishes: another Line, a response prompt, an End node, " +
//              "or leave empty to end the conversation here.")]
//     public DialogueNode nextNode;

//     [Header("Item Award (optional, any node type)")]
//     [Tooltip("If checked, the item below is given to the player once this node's text finishes typing " +
//              "- but only the first time. Repeat visits show alreadyAwardedLineText instead and do not re-award.")]
//     public bool awardsItem;
//     [Tooltip("The item to award.")]
//     public ItemData itemToAward;
//     [TextArea(2, 3)]
//     [Tooltip("If the player's inventory is full (and the item hasn't been awarded yet), this text is shown " +
//              "instead of lineText, and the item is NOT awarded. Leave empty to just show lineText as normal.")]
//     public string inventoryFullLineText;
//     [TextArea(2, 3)]
//     [Tooltip("Shown instead of lineText if this item has already been awarded once before (in this play " +
//              "session). The item is never given twice. Leave empty to just show lineText again without re-awarding.")]
//     public string alreadyAwardedLineText;

//     [Header("Quest Item Award (optional, any node type)")]
//     [Tooltip("If checked, the quest item below is given to the player once this node's text finishes " +
//              "typing - but only the first time. Quest inventory is unbounded, so there's no 'full' case " +
//              "to handle here, unlike regular item awards.")]
//     public bool awardsQuestItem;
//     [Tooltip("The quest item to award.")]
//     public QuestItemData questItemToAward;
//     [TextArea(2, 3)]
//     [Tooltip("Shown instead of lineText if this quest item has already been awarded once before. " +
//              "If both awardsItem and awardsQuestItem are set and both are already-awarded, this text " +
//              "takes priority over alreadyAwardedLineText. Leave empty to just show lineText again.")]
//     public string alreadyAwardedQuestItemLineText;

//     [Header("Opens Shop (optional, any node type)")]
//     [Tooltip("If checked, once this node's text finishes typing (or immediately, if lineText is " +
//              "empty), the conversation ends and the shop UI opens automatically using shopToOpen.")]
//     public bool opensShop;
//     [Tooltip("Which shop's stock to open. Required if opensShop is checked.")]
//     public ShopData shopToOpen;

//     [Header("Day Advancement (optional, any node type)")]
//     [Tooltip("If checked, GameData.AdvanceDay() is called once this node's text finishes typing. " +
//              "This is blocked internally by GameData's own canAdvance lock if the day's primary task " +
//              "hasn't been completed yet, so it's safe to place this on a node the player can reach " +
//              "early - nothing happens until the task is actually done.")]
//     public bool triggersDayAdvance;
//     [Tooltip("The shared GameData asset - drag in the SAME asset used everywhere else in the project.")]
//     public GameData gameData;

//     [Header("Task Completion (optional, any node type)")]
//     [Tooltip("If checked, taskManager.CompleteTask(taskIdToComplete) is called once this node's " +
//              "text finishes typing.")]
//     public bool completesTask;
//     [Tooltip("The shared TaskManager asset - drag in the SAME asset used everywhere else in the project.")]
//     public TaskManager taskManager;
//     [Tooltip("The exact task ID to complete, matching TaskManager's own ID convention: " +
//              "\"day{N}_initial\" for a day's primary task, or \"day{N}_followup{index}\" for a " +
//              "follow-up task (e.g. \"day3_initial\", \"day3_followup0\"). Must match exactly, " +
//              "including the day number - a mismatch here silently does nothing (TaskManager just " +
//              "won't find a matching task).")]
//     public string taskIdToComplete;

//     [Header("Advance NPC Sequence on Primary Task Completion")]
//     [Tooltip("If checked, and taskIdToComplete above matches the CURRENT day's primary task ID " +
//              "(\"day{CurrentDay}_initial\"), the NPC being spoken to has its dialogue sequence set " +
//              "DIRECTLY to advanceSequenceTarget (not just +1) once the task completes. Only takes " +
//              "effect if completesTask is also checked.")]
//     public bool advanceSequenceByPrimaryTask;
//     [Tooltip("The sequence number to jump the NPC to (e.g. 5).")]
//     public int advanceSequenceTarget = 1;

//     [Header("Type1 Responses (nodeType = Type1Response) - action options, e.g. Shop/Deliver/Leave")]
//     public List<DialogueResponseOption> type1Options = new List<DialogueResponseOption>();

//     [Header("Type2 Responses (nodeType = Type2Response) - branching dialogue options, e.g. Yes/No")]
//     public List<DialogueResponseOption> type2Options = new List<DialogueResponseOption>();
// }