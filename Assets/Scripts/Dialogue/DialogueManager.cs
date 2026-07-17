// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Text;
// using UnityEngine;

// /// <summary>
// /// Central authority for running a dialogue conversation: typewriter text reveal, item awards,
// /// next-line/response branching, response option navigation, and (new) Type1 delivery actions -
// /// awarding and/or removing regular and quest items when a Type1 response option is chosen.
// /// Owns no UI directly - it fires events that DialogueUI subscribes to.
// ///
// /// Every node type types out its own lineText first, then branches based on its type - see the
// /// class-level comments in previous versions for the full flow; unchanged here.
// ///
// /// Sequence progression: once a node with advancesSequence finishes typing, the speaking NPC's
// /// AdvanceSequence() is called - see NPC.cs.
// ///
// /// Type1 response actions: on selection, any checked "removes" actions are attempted first (via
// /// InventoryManager/QuestInventoryManager.TryRemoveItem - these only validate and fire an event;
// /// they do NOT destroy the item yet, InventoryUI handles that after its animation). If any
// /// requested removal fails (item not held), no awards happen, nextNode is skipped, and
// /// itemMissingLineText is shown instead via a throwaway runtime DialogueNode (built with
// /// ScriptableObject.CreateInstance so it can flow through the exact same BeginNode() pipeline
// /// as every other node, without needing a real asset).
// /// </summary>
// public class DialogueManager : MonoBehaviour
// {
//     private enum DialoguePhase { Inactive, TypingLine, LineFinished, AwaitingResponse }

//     public static DialogueManager Instance { get; private set; }

//     [Header("Typing")]
//     [Tooltip("How many characters are revealed per second while a line is typing out.")]
//     [SerializeField] private float charactersPerSecond = 30f;

//     public event Action OnDialogueOpened;
//     public event Action OnDialogueClosed;
//     public event Action<DialogueSpeaker, Transform, string> OnLineWillStart;
//     public event Action<string> OnLineTextUpdated;
//     public event Action<bool> OnShowNextArrow;
//     public event Action<DialogueNodeType, List<string>> OnResponsePromptOpened;
//     public event Action OnResponsePromptClosed;
//     public event Action<int> OnResponseSelectionChanged;

//     public bool IsDialogueActive => phase != DialoguePhase.Inactive;

//     private DialoguePhase phase = DialoguePhase.Inactive;
//     private NPC currentNPC;
//     private DialogueNode currentNode;
//     private DialogueNode pendingAdvanceNode;
//     private DialogueNode currentResponseNode;
//     private ItemData pendingItemAward;
//     private DialogueNode pendingItemAwardNode;
//     private QuestItemData pendingQuestItemAward;
//     private DialogueNode pendingQuestItemAwardNode;
//     private string fullLineText = "";
//     private int selectedOptionIndex;
//     private Coroutine typingCoroutine;

//     private int dialogueStartedFrame = -1;

//     private readonly HashSet<DialogueNode> awardedItemNodes = new HashSet<DialogueNode>();
//     private readonly HashSet<DialogueNode> awardedQuestItemNodes = new HashSet<DialogueNode>();

//     private void Awake()
//     {
//         if (Instance != null && Instance != this)
//         {
//             Destroy(gameObject);
//             return;
//         }

//         Instance = this;
//         DontDestroyOnLoad(gameObject);
//     }

//     private void Update()
//     {
//         if (!IsDialogueActive) return;

//         if (phase == DialoguePhase.AwaitingResponse)
//         {
//             HandleResponseNavigationInput();
//         }

//         if (Time.frameCount != dialogueStartedFrame && Input.GetKeyDown(KeyCode.E))
//         {
//             HandleAdvancePressed();
//         }
//     }

//     // ---------- Public entry point ----------

//     public void StartDialogue(NPC npc, DialogueNode rootNode)
//     {
//         if (IsDialogueActive || npc == null || rootNode == null) return;

//         currentNPC = npc;
//         dialogueStartedFrame = Time.frameCount;

//         if (PlayerController2D.Instance != null)
//         {
//             PlayerController2D.Instance.CanMove = false;
//             npc.FaceTowardsPlayer(PlayerController2D.Instance.CurrentFacing);
//         }

//         npc.OnDialogueStarted();

//         BeginNode(rootNode);
//         OnDialogueOpened?.Invoke();
//     }

//     // ---------- Input handling ----------

//     private void HandleAdvancePressed()
//     {
//         switch (phase)
//         {
//             case DialoguePhase.TypingLine:
//                 SkipTyping();
//                 break;

//             case DialoguePhase.LineFinished:
//                 if (pendingAdvanceNode != null) BeginNode(pendingAdvanceNode);
//                 else EndDialogue();
//                 break;

//             case DialoguePhase.AwaitingResponse:
//                 ConfirmResponseSelection();
//                 break;
//         }
//     }

//     private void HandleResponseNavigationInput()
//     {
//         int optionCount = GetCurrentOptionCount();
//         if (optionCount <= 0) return;

//         if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
//         {
//             selectedOptionIndex = Mathf.Clamp(selectedOptionIndex - 1, 0, optionCount - 1);
//             OnResponseSelectionChanged?.Invoke(selectedOptionIndex);
//         }
//         else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
//         {
//             selectedOptionIndex = Mathf.Clamp(selectedOptionIndex + 1, 0, optionCount - 1);
//             OnResponseSelectionChanged?.Invoke(selectedOptionIndex);
//         }
//     }

//     private int GetCurrentOptionCount()
//     {
//         if (currentResponseNode == null) return 0;
//         var options = currentResponseNode.nodeType == DialogueNodeType.Type1Response
//             ? currentResponseNode.type1Options
//             : currentResponseNode.type2Options;
//         return options.Count;
//     }

//     // ---------- Node playback ----------

//     private void BeginNode(DialogueNode node)
//     {
//         currentNode = node;
//         pendingAdvanceNode = null;
//         pendingItemAward = null;
//         pendingItemAwardNode = null;
//         pendingQuestItemAward = null;
//         pendingQuestItemAwardNode = null;

//         string textToType = node.lineText;

//         if (node.awardsItem)
//         {
//             bool alreadyAwarded = awardedItemNodes.Contains(node);

//             if (alreadyAwarded)
//             {
//                 textToType = string.IsNullOrEmpty(node.alreadyAwardedLineText) ? textToType : node.alreadyAwardedLineText;
//             }
//             else
//             {
//                 bool inventoryFull = InventoryManager.Instance != null && InventoryManager.Instance.IsFull;
//                 if (inventoryFull)
//                 {
//                     textToType = string.IsNullOrEmpty(node.inventoryFullLineText) ? textToType : node.inventoryFullLineText;
//                 }
//                 else
//                 {
//                     pendingItemAward = node.itemToAward;
//                     pendingItemAwardNode = node;
//                 }
//             }
//         }

//         if (node.awardsQuestItem)
//         {
//             bool alreadyAwarded = awardedQuestItemNodes.Contains(node);

//             if (alreadyAwarded)
//             {
//                 textToType = string.IsNullOrEmpty(node.alreadyAwardedQuestItemLineText) ? textToType : node.alreadyAwardedQuestItemLineText;
//             }
//             else
//             {
//                 pendingQuestItemAward = node.questItemToAward;
//                 pendingQuestItemAwardNode = node;
//             }
//         }

//         fullLineText = textToType ?? "";
//         phase = DialoguePhase.TypingLine;
//         OnShowNextArrow?.Invoke(false);

//         Transform npcAnchor = (node.speaker == DialogueSpeaker.NPC && currentNPC != null) ? currentNPC.DialogueAnchor : null;
//         OnLineWillStart?.Invoke(node.speaker, npcAnchor, fullLineText);

//         OnLineTextUpdated?.Invoke("");

//         if (typingCoroutine != null) StopCoroutine(typingCoroutine);
//         typingCoroutine = StartCoroutine(TypeText(fullLineText));
//     }

//     private IEnumerator TypeText(string text)
//     {
//         StringBuilder sb = new StringBuilder();
//         float delay = charactersPerSecond > 0f ? 1f / charactersPerSecond : 0f;

//         foreach (char c in text)
//         {
//             sb.Append(c);
//             OnLineTextUpdated?.Invoke(sb.ToString());

//             if (delay > 0f) yield return new WaitForSeconds(delay);
//             else yield return null;
//         }

//         typingCoroutine = null;
//         FinishTyping();
//     }

//     private void SkipTyping()
//     {
//         if (typingCoroutine != null)
//         {
//             StopCoroutine(typingCoroutine);
//             typingCoroutine = null;
//         }

//         OnLineTextUpdated?.Invoke(fullLineText);
//         FinishTyping();
//     }

//     private void FinishTyping()
//     {
//         phase = DialoguePhase.LineFinished;

//         if (pendingItemAward != null && InventoryManager.Instance != null)
//         {
//             bool added = InventoryManager.Instance.TryAddItem(pendingItemAward);
//             if (added && pendingItemAwardNode != null)
//             {
//                 awardedItemNodes.Add(pendingItemAwardNode);
//             }
//             pendingItemAward = null;
//             pendingItemAwardNode = null;
//         }

//         if (pendingQuestItemAward != null && QuestInventoryManager.Instance != null)
//         {
//             QuestInventoryManager.Instance.AddItem(pendingQuestItemAward);
//             if (pendingQuestItemAwardNode != null)
//             {
//                 awardedQuestItemNodes.Add(pendingQuestItemAwardNode);
//             }
//             pendingQuestItemAward = null;
//             pendingQuestItemAwardNode = null;
//         }

//         if (currentNode.advancesSequence && currentNPC != null)
//         {
//             currentNPC.AdvanceSequence();
//         }

//         if (currentNode.opensShop && currentNode.shopToOpen != null)
//         {
//             OpenShopAndEndDialogue();
//             return;
//         }

//         switch (currentNode.nodeType)
//         {
//             case DialogueNodeType.Line:
//                 EvaluateNextAfterLine();
//                 break;

//             case DialogueNodeType.Type1Response:
//             case DialogueNodeType.Type2Response:
//                 BeginResponseOptions(currentNode);
//                 break;

//             case DialogueNodeType.End:
//                 pendingAdvanceNode = null;
//                 OnShowNextArrow?.Invoke(true);
//                 break;
//         }
//     }

//     private void EvaluateNextAfterLine()
//     {
//         DialogueNode next = currentNode.nextNode;
//         pendingAdvanceNode = next;
//         OnShowNextArrow?.Invoke(true);
//     }

//     /// <summary>
//     /// Ends the current conversation (fading out the dialogue bubble, restoring movement, etc,
//     /// same as a normal EndDialogue()) and immediately opens the shop UI, which re-freezes
//     /// movement itself. The conversation does not resume after the shop closes.
//     /// </summary>
//     private void OpenShopAndEndDialogue()
//     {
//         ShopData shop = currentNode.shopToOpen;
//         NPC npc = currentNPC;

//         EndDialogue();

//         if (ShopManager.Instance != null && shop != null)
//         {
//             ShopManager.Instance.OpenShop(shop, npc);
//         }
//     }

//     // ---------- Response prompts ----------

//     private void BeginResponseOptions(DialogueNode responseNode)
//     {
//         currentResponseNode = responseNode;
//         selectedOptionIndex = 0;
//         phase = DialoguePhase.AwaitingResponse;

//         var options = responseNode.nodeType == DialogueNodeType.Type1Response
//             ? responseNode.type1Options
//             : responseNode.type2Options;

//         List<string> optionTexts = new List<string>();
//         foreach (var option in options) optionTexts.Add(option.optionText);

//         OnResponsePromptOpened?.Invoke(responseNode.nodeType, optionTexts);
//         OnResponseSelectionChanged?.Invoke(selectedOptionIndex);
//     }

//     private void ConfirmResponseSelection()
//     {
//         if (currentResponseNode == null) return;

//         var options = currentResponseNode.nodeType == DialogueNodeType.Type1Response
//             ? currentResponseNode.type1Options
//             : currentResponseNode.type2Options;

//         if (selectedOptionIndex < 0 || selectedOptionIndex >= options.Count) return;

//         DialogueResponseOption chosen = options[selectedOptionIndex];
//         DialogueNodeType chosenType = currentResponseNode.nodeType;
//         DialogueSpeaker npcSpeakerFallback = currentNode != null ? currentNode.speaker : DialogueSpeaker.NPC;
//         currentResponseNode = null;

//         OnResponsePromptClosed?.Invoke();

//         if (chosenType == DialogueNodeType.Type1Response)
//         {
//             if (!string.IsNullOrEmpty(chosen.actionID))
//             {
//                 Debug.Log($"DialogueManager: Type1 action selected - '{chosen.optionText}' " +
//                           $"(actionID: '{chosen.actionID}'). Action handling not implemented yet.");
//             }

//             bool actionFailed = ProcessType1Actions(chosen);

//             if (actionFailed)
//             {
//                 if (!string.IsNullOrEmpty(chosen.itemMissingLineText))
//                 {
//                     DialogueNode missingNode = ScriptableObject.CreateInstance<DialogueNode>();
//                     missingNode.nodeType = DialogueNodeType.End;
//                     missingNode.lineText = chosen.itemMissingLineText;
//                     missingNode.speaker = npcSpeakerFallback;
//                     BeginNode(missingNode);
//                 }
//                 else
//                 {
//                     EndDialogue();
//                 }
//                 return;
//             }
//         }

//         if (chosen.nextNode == null)
//         {
//             EndDialogue();
//         }
//         else
//         {
//             BeginNode(chosen.nextNode);
//         }
//     }

//     /// <summary>
//     /// Attempts any removals requested by this option first. If any fail (player doesn't have
//     /// the item), returns true (failed) WITHOUT applying any awards. If all requested removals
//     /// succeed (or none were requested), applies any requested awards and returns false.
//     /// </summary>
//     private bool ProcessType1Actions(DialogueResponseOption option)
//     {
//         bool removalFailed = false;

//         if (option.removesItem && option.itemToRemove != null)
//         {
//             bool ok = InventoryManager.Instance != null && InventoryManager.Instance.TryRemoveItem(option.itemToRemove);
//             if (!ok) removalFailed = true;
//         }

//         if (option.removesQuestItem && option.questItemToRemove != null)
//         {
//             bool ok = QuestInventoryManager.Instance != null && QuestInventoryManager.Instance.TryRemoveItem(option.questItemToRemove);
//             if (!ok) removalFailed = true;
//         }

//         if (removalFailed) return true;

//         if (option.awardsItem && option.itemToAward != null && InventoryManager.Instance != null)
//         {
//             InventoryManager.Instance.TryAddItem(option.itemToAward);
//         }

//         if (option.awardsQuestItem && option.questItemToAward != null && QuestInventoryManager.Instance != null)
//         {
//             QuestInventoryManager.Instance.AddItem(option.questItemToAward);
//         }

//         return false;
//     }

//     // ---------- Ending ----------

//     private void EndDialogue()
//     {
//         phase = DialoguePhase.Inactive;

//         if (typingCoroutine != null)
//         {
//             StopCoroutine(typingCoroutine);
//             typingCoroutine = null;
//         }

//         OnShowNextArrow?.Invoke(false);
//         OnDialogueClosed?.Invoke();

//         if (PlayerController2D.Instance != null) PlayerController2D.Instance.CanMove = true;
//         if (currentNPC != null) currentNPC.OnDialogueEnded();

//         currentNPC = null;
//         currentNode = null;
//         pendingAdvanceNode = null;
//         currentResponseNode = null;
//     }
// }









using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Central authority for running a dialogue conversation: typewriter text reveal, item awards,
/// next-line/response branching, response option navigation, and (new) Type1 delivery actions -
/// awarding and/or removing regular and quest items when a Type1 response option is chosen.
/// Owns no UI directly - it fires events that DialogueUI subscribes to.
///
/// Every node type types out its own lineText first, then branches based on its type - see the
/// class-level comments in previous versions for the full flow; unchanged here.
///
/// Sequence progression: once a node with advancesSequence finishes typing, the speaking NPC's
/// AdvanceSequence() is called - see NPC.cs.
///
/// Type1 response actions: on selection, any checked "removes" actions are attempted first (via
/// InventoryManager/QuestInventoryManager.TryRemoveItem - these only validate and fire an event;
/// they do NOT destroy the item yet, InventoryUI handles that after its animation). If any
/// requested removal fails (item not held), no awards happen, nextNode is skipped, and
/// itemMissingLineText is shown instead via a throwaway runtime DialogueNode (built with
/// ScriptableObject.CreateInstance so it can flow through the exact same BeginNode() pipeline
/// as every other node, without needing a real asset).
/// </summary>
public class DialogueManager : MonoBehaviour
{
    private enum DialoguePhase { Inactive, TypingLine, LineFinished, AwaitingResponse }

    public static DialogueManager Instance { get; private set; }

    [Header("Typing")]
    [Tooltip("How many characters are revealed per second while a line is typing out.")]
    [SerializeField] private float charactersPerSecond = 30f;
    [Tooltip("Voice/typing-sound configuration used whenever the Player is the speaker (NPCs use their own NPC.VoiceData instead).")]
    [SerializeField] private DialogueVoiceData playerVoiceData;

    [Header("Sound")]
    [Tooltip("Played when the player presses E to advance from one finished line to the next.")]
    [SerializeField] private AudioClip continueSound;
    [Tooltip("Played when the player navigates between response options (W/S/Up/Down).")]
    [SerializeField] private AudioClip switchOptionSound;
    [Tooltip("Played when the player confirms a response option (E).")]
    [SerializeField] private AudioClip confirmOptionSound;

    public event Action OnDialogueOpened;
    public event Action OnDialogueClosed;
    public event Action<DialogueSpeaker, Transform, string> OnLineWillStart;
    public event Action<string> OnLineTextUpdated;
    public event Action<bool> OnShowNextArrow;
    public event Action<DialogueNodeType, List<string>> OnResponsePromptOpened;
    public event Action OnResponsePromptClosed;
    public event Action<int> OnResponseSelectionChanged;

    public bool IsDialogueActive => phase != DialoguePhase.Inactive;

    private DialoguePhase phase = DialoguePhase.Inactive;
    private NPC currentNPC;
    private DialogueNode currentNode;
    private DialogueNode pendingAdvanceNode;
    private DialogueNode currentResponseNode;
    private ItemData pendingItemAward;
    private DialogueNode pendingItemAwardNode;
    private QuestItemData pendingQuestItemAward;
    private DialogueNode pendingQuestItemAwardNode;
    private string fullLineText = "";
    private int selectedOptionIndex;
    private Coroutine typingCoroutine;
    private DialogueVoiceData activeVoiceData;
    private int typingSoundCharCounter;

    private int dialogueStartedFrame = -1;

    private readonly HashSet<DialogueNode> awardedItemNodes = new HashSet<DialogueNode>();
    private readonly HashSet<DialogueNode> awardedQuestItemNodes = new HashSet<DialogueNode>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!IsDialogueActive) return;

        if (phase == DialoguePhase.AwaitingResponse)
        {
            HandleResponseNavigationInput();
        }

        if (Time.frameCount != dialogueStartedFrame && Input.GetKeyDown(KeyCode.E))
        {
            HandleAdvancePressed();
        }
    }

    // ---------- Public entry point ----------

    public void StartDialogue(NPC npc, DialogueNode rootNode)
    {
        if (IsDialogueActive || npc == null || rootNode == null) return;

        currentNPC = npc;
        dialogueStartedFrame = Time.frameCount;

        if (PlayerController2D.Instance != null)
        {
            PlayerController2D.Instance.CanMove = false;
            npc.FaceTowardsPlayer(PlayerController2D.Instance.CurrentFacing);
        }

        npc.OnDialogueStarted();

        BeginNode(rootNode);
        OnDialogueOpened?.Invoke();
    }

    // ---------- Input handling ----------

    private void HandleAdvancePressed()
    {
        switch (phase)
        {
            case DialoguePhase.TypingLine:
                SkipTyping();
                break;

            case DialoguePhase.LineFinished:
                if (pendingAdvanceNode != null)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayUISound(continueSound);
                    BeginNode(pendingAdvanceNode);
                }
                else EndDialogue();
                break;

            case DialoguePhase.AwaitingResponse:
                ConfirmResponseSelection();
                break;
        }
    }

    private void HandleResponseNavigationInput()
    {
        int optionCount = GetCurrentOptionCount();
        if (optionCount <= 0) return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedOptionIndex = Mathf.Clamp(selectedOptionIndex - 1, 0, optionCount - 1);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUISound(switchOptionSound);
            OnResponseSelectionChanged?.Invoke(selectedOptionIndex);
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedOptionIndex = Mathf.Clamp(selectedOptionIndex + 1, 0, optionCount - 1);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUISound(switchOptionSound);
            OnResponseSelectionChanged?.Invoke(selectedOptionIndex);
        }
    }

    private int GetCurrentOptionCount()
    {
        if (currentResponseNode == null) return 0;
        var options = currentResponseNode.nodeType == DialogueNodeType.Type1Response
            ? currentResponseNode.type1Options
            : currentResponseNode.type2Options;
        return options.Count;
    }

    // ---------- Node playback ----------

    private void BeginNode(DialogueNode node)
    {
        currentNode = node;
        pendingAdvanceNode = null;
        pendingItemAward = null;
        pendingItemAwardNode = null;
        pendingQuestItemAward = null;
        pendingQuestItemAwardNode = null;

        ApplyMusicAction(node);

        string textToType = node.lineText;

        if (node.awardsItem)
        {
            bool alreadyAwarded = awardedItemNodes.Contains(node);

            if (alreadyAwarded)
            {
                textToType = string.IsNullOrEmpty(node.alreadyAwardedLineText) ? textToType : node.alreadyAwardedLineText;
            }
            else
            {
                bool inventoryFull = InventoryManager.Instance != null && InventoryManager.Instance.IsFull;
                if (inventoryFull)
                {
                    textToType = string.IsNullOrEmpty(node.inventoryFullLineText) ? textToType : node.inventoryFullLineText;
                }
                else
                {
                    pendingItemAward = node.itemToAward;
                    pendingItemAwardNode = node;
                }
            }
        }

        if (node.awardsQuestItem)
        {
            bool alreadyAwarded = awardedQuestItemNodes.Contains(node);

            if (alreadyAwarded)
            {
                textToType = string.IsNullOrEmpty(node.alreadyAwardedQuestItemLineText) ? textToType : node.alreadyAwardedQuestItemLineText;
            }
            else
            {
                pendingQuestItemAward = node.questItemToAward;
                pendingQuestItemAwardNode = node;
            }
        }

        fullLineText = textToType ?? "";
        phase = DialoguePhase.TypingLine;
        OnShowNextArrow?.Invoke(false);

        Transform npcAnchor = (node.speaker == DialogueSpeaker.NPC && currentNPC != null) ? currentNPC.DialogueAnchor : null;
        OnLineWillStart?.Invoke(node.speaker, npcAnchor, fullLineText);

        OnLineTextUpdated?.Invoke("");

        activeVoiceData = node.speaker == DialogueSpeaker.NPC && currentNPC != null ? currentNPC.VoiceData : playerVoiceData;
        typingSoundCharCounter = 0;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(fullLineText));
    }

    /// <summary>Applies this node's musicAction, if any, via AudioManager.</summary>
    private void ApplyMusicAction(DialogueNode node)
    {
        if (AudioManager.Instance == null) return;

        switch (node.musicAction)
        {
            case DialogueMusicAction.FadeOutCurrent:
                AudioManager.Instance.FadeOutMusic(node.musicFadeDuration);
                break;
            case DialogueMusicAction.FadeInCurrent:
                AudioManager.Instance.FadeInMusic(node.musicFadeDuration);
                break;
            case DialogueMusicAction.QueueNewTrack:
                if (node.musicTrackToQueue != null)
                {
                    AudioManager.Instance.PlayMusic(node.musicTrackToQueue, true, node.musicFadeDuration);
                }
                break;
        }
    }

    private IEnumerator TypeText(string text)
    {
        StringBuilder sb = new StringBuilder();
        float delay = charactersPerSecond > 0f ? 1f / charactersPerSecond : 0f;

        foreach (char c in text)
        {
            sb.Append(c);
            OnLineTextUpdated?.Invoke(sb.ToString());
            PlayTypingSoundIfDue(c);

            if (delay > 0f) yield return new WaitForSeconds(delay);
            else yield return null;
        }

        typingCoroutine = null;
        FinishTyping();
    }

    /// <summary>
    /// Plays a typing blip if this character is due one, per activeVoiceData's frequency
    /// (every Nth non-whitespace character). Whitespace is always skipped and never counts
    /// towards the frequency, so pauses between words don't throw off the rhythm.
    /// </summary>
    private void PlayTypingSoundIfDue(char c)
    {
        if (activeVoiceData == null || activeVoiceData.clips == null || activeVoiceData.clips.Length == 0) return;
        if (char.IsWhiteSpace(c)) return;

        typingSoundCharCounter++;
        if (typingSoundCharCounter < activeVoiceData.frequency) return;

        typingSoundCharCounter = 0;

        AudioClip clip = activeVoiceData.clips[UnityEngine.Random.Range(0, activeVoiceData.clips.Length)];
        float pitch = UnityEngine.Random.Range(activeVoiceData.minPitch, activeVoiceData.maxPitch);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayDialogueTypingSound(clip, pitch);
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        OnLineTextUpdated?.Invoke(fullLineText);
        FinishTyping();
    }

    private void FinishTyping()
    {
        phase = DialoguePhase.LineFinished;

        if (pendingItemAward != null && InventoryManager.Instance != null)
        {
            bool added = InventoryManager.Instance.TryAddItem(pendingItemAward);
            if (added && pendingItemAwardNode != null)
            {
                awardedItemNodes.Add(pendingItemAwardNode);
            }
            pendingItemAward = null;
            pendingItemAwardNode = null;
        }

        if (pendingQuestItemAward != null && QuestInventoryManager.Instance != null)
        {
            QuestInventoryManager.Instance.AddItem(pendingQuestItemAward);
            if (pendingQuestItemAwardNode != null)
            {
                awardedQuestItemNodes.Add(pendingQuestItemAwardNode);
            }
            pendingQuestItemAward = null;
            pendingQuestItemAwardNode = null;
        }

        if (currentNode.advancesSequence && currentNPC != null)
        {
            currentNPC.AdvanceSequence();
        }

        ApplyProgressionActions(currentNode);

        if (currentNode.opensShop && currentNode.shopToOpen != null)
        {
            OpenShopAndEndDialogue();
            return;
        }

        switch (currentNode.nodeType)
        {
            case DialogueNodeType.Line:
                EvaluateNextAfterLine();
                break;

            case DialogueNodeType.Type1Response:
            case DialogueNodeType.Type2Response:
                BeginResponseOptions(currentNode);
                break;

            case DialogueNodeType.End:
                pendingAdvanceNode = null;
                OnShowNextArrow?.Invoke(true);
                break;
        }
    }

    /// <summary>
    /// Applies a node's day-advancement and task-completion actions, if set. Both reference
    /// the shared GameData/TaskManager assets carried directly on the node itself (dragged in
    /// per-node, same pattern as everywhere else), so DialogueManager needs no wiring of its own.
    /// If a task is completed and it matches the day's primary task ID, and the node also has
    /// advanceSequenceByPrimaryTask checked, the current NPC's sequence jumps directly to
    /// advanceSequenceTarget.
    /// </summary>
    private void ApplyProgressionActions(DialogueNode node)
    {
        if (node.triggersDayAdvance && node.gameData != null)
        {
            node.gameData.AdvanceDay();
        }

        if (node.incrementsBaniFavour && node.gameData != null)
        {
            node.gameData.AddBaniFavour(node.baniFavourAmount);
        }

        if (node.completesTask && node.taskManager != null && !string.IsNullOrEmpty(node.taskIdToComplete))
        {
            node.taskManager.CompleteTask(node.taskIdToComplete);

            if (node.advanceSequenceByPrimaryTask && node.gameData != null && currentNPC != null)
            {
                string primaryTaskId = $"day{node.gameData.CurrentDay}_initial";
                if (node.taskIdToComplete == primaryTaskId)
                {
                    currentNPC.SetSequence(node.advanceSequenceTarget);
                }
            }
        }
    }

    private void EvaluateNextAfterLine()
    {
        DialogueNode next = currentNode.nextNode;
        pendingAdvanceNode = next;
        OnShowNextArrow?.Invoke(true);
    }

    /// <summary>
    /// Ends the current conversation (fading out the dialogue bubble, restoring movement, etc,
    /// same as a normal EndDialogue()) and immediately opens the shop UI, which re-freezes
    /// movement itself. The conversation does not resume after the shop closes.
    /// </summary>
    private void OpenShopAndEndDialogue()
    {
        ShopData shop = currentNode.shopToOpen;
        NPC npc = currentNPC;

        EndDialogue();

        if (ShopManager.Instance != null && shop != null)
        {
            ShopManager.Instance.OpenShop(shop, npc);
        }
    }

    // ---------- Response prompts ----------

    private void BeginResponseOptions(DialogueNode responseNode)
    {
        currentResponseNode = responseNode;
        selectedOptionIndex = 0;
        phase = DialoguePhase.AwaitingResponse;

        var options = responseNode.nodeType == DialogueNodeType.Type1Response
            ? responseNode.type1Options
            : responseNode.type2Options;

        List<string> optionTexts = new List<string>();
        foreach (var option in options) optionTexts.Add(option.optionText);

        OnResponsePromptOpened?.Invoke(responseNode.nodeType, optionTexts);
        OnResponseSelectionChanged?.Invoke(selectedOptionIndex);
    }

    private void ConfirmResponseSelection()
    {
        if (currentResponseNode == null) return;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayUISound(confirmOptionSound);

        var options = currentResponseNode.nodeType == DialogueNodeType.Type1Response
            ? currentResponseNode.type1Options
            : currentResponseNode.type2Options;

        if (selectedOptionIndex < 0 || selectedOptionIndex >= options.Count) return;

        DialogueResponseOption chosen = options[selectedOptionIndex];
        DialogueNodeType chosenType = currentResponseNode.nodeType;
        DialogueSpeaker npcSpeakerFallback = currentNode != null ? currentNode.speaker : DialogueSpeaker.NPC;
        currentResponseNode = null;

        OnResponsePromptClosed?.Invoke();

        if (chosenType == DialogueNodeType.Type1Response)
        {
            if (!string.IsNullOrEmpty(chosen.actionID))
            {
                Debug.Log($"DialogueManager: Type1 action selected - '{chosen.optionText}' " +
                          $"(actionID: '{chosen.actionID}'). Action handling not implemented yet.");
            }

            bool actionFailed = ProcessType1Actions(chosen);

            if (actionFailed)
            {
                if (!string.IsNullOrEmpty(chosen.itemMissingLineText))
                {
                    DialogueNode missingNode = ScriptableObject.CreateInstance<DialogueNode>();
                    missingNode.nodeType = DialogueNodeType.End;
                    missingNode.lineText = chosen.itemMissingLineText;
                    missingNode.speaker = npcSpeakerFallback;
                    BeginNode(missingNode);
                }
                else
                {
                    EndDialogue();
                }
                return;
            }
        }

        if (chosen.nextNode == null)
        {
            EndDialogue();
        }
        else
        {
            BeginNode(chosen.nextNode);
        }
    }

    /// <summary>
    /// Attempts any removals requested by this option first. If any fail (player doesn't have
    /// the item), returns true (failed) WITHOUT applying any awards. If all requested removals
    /// succeed (or none were requested), applies any requested awards and returns false.
    /// </summary>
    private bool ProcessType1Actions(DialogueResponseOption option)
    {
        bool removalFailed = false;

        if (option.removesItem && option.itemToRemove != null)
        {
            bool ok = InventoryManager.Instance != null && InventoryManager.Instance.TryRemoveItem(option.itemToRemove);
            if (!ok) removalFailed = true;
        }

        if (option.removesQuestItem && option.questItemToRemove != null)
        {
            bool ok = QuestInventoryManager.Instance != null && QuestInventoryManager.Instance.TryRemoveItem(option.questItemToRemove);
            if (!ok) removalFailed = true;
        }

        if (removalFailed) return true;

        if (option.awardsItem && option.itemToAward != null && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.TryAddItem(option.itemToAward);
        }

        if (option.awardsQuestItem && option.questItemToAward != null && QuestInventoryManager.Instance != null)
        {
            QuestInventoryManager.Instance.AddItem(option.questItemToAward);
        }

        return false;
    }

    // ---------- Ending ----------

    private void EndDialogue()
    {
        phase = DialoguePhase.Inactive;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        OnShowNextArrow?.Invoke(false);
        OnDialogueClosed?.Invoke();

        if (PlayerController2D.Instance != null) PlayerController2D.Instance.CanMove = true;
        if (currentNPC != null) currentNPC.OnDialogueEnded();

        currentNPC = null;
        currentNode = null;
        pendingAdvanceNode = null;
        currentResponseNode = null;
    }
}