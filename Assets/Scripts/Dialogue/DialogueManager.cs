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
                if (pendingAdvanceNode != null) BeginNode(pendingAdvanceNode);
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
            OnResponseSelectionChanged?.Invoke(selectedOptionIndex);
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedOptionIndex = Mathf.Clamp(selectedOptionIndex + 1, 0, optionCount - 1);
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

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(fullLineText));
    }

    private IEnumerator TypeText(string text)
    {
        StringBuilder sb = new StringBuilder();
        float delay = charactersPerSecond > 0f ? 1f / charactersPerSecond : 0f;

        foreach (char c in text)
        {
            sb.Append(c);
            OnLineTextUpdated?.Invoke(sb.ToString());

            if (delay > 0f) yield return new WaitForSeconds(delay);
            else yield return null;
        }

        typingCoroutine = null;
        FinishTyping();
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

//     [Header("Dialogue Audio")]

//     [Tooltip("Played whenever the player advances dialogue with E.")]
//     [SerializeField] private AudioClip continueDialogueSound;

//     [Tooltip("Played when moving between dialogue response options.")]
//     [SerializeField] private AudioClip navigateResponseSound;

//     [Tooltip("Played when confirming a dialogue response.")]
//     [SerializeField] private AudioClip confirmResponseSound;

//     [Tooltip("Fallback voice used if an NPC has no DialogueVoice assigned.")]
//     [SerializeField] private DialogueVoiceData defaultDialogueVoice;

//     [Tooltip("Voice used whenever the player speaks.")]
//     [SerializeField] private DialogueVoiceData playerDialogueVoice;

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

//     // new
//     private DialogueVoiceData currentVoiceData;
//     private int visibleCharacterCounter;

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
        
//         // New
//         visibleCharacterCounter = 0;

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

//         //New block
//         if (node.speaker == DialogueSpeaker.Player)
//         {
//             currentVoiceData = playerDialogueVoice;
//         }
//         else if (currentNPC != null && currentNPC.DialogueVoice != null)
//         {
//             currentVoiceData = currentNPC.DialogueVoice;
//         }
//         else
//         {
//             currentVoiceData = defaultDialogueVoice;
//         }

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

//             // New
//             if (!char.IsWhiteSpace(c))
//             {
//                 visibleCharacterCounter++;

//                 if (currentVoiceData != null &&
//                     currentVoiceData.clips.Length > 0 &&
//                     visibleCharacterCounter % currentVoiceData.frequency == 0)
//                 {
//                     AudioClip clip =
//                         currentVoiceData.clips[
//                             Random.Range(0, currentVoiceData.clips.Length)];

//                     float pitch =
//                         Random.Range(
//                             currentVoiceData.minPitch,
//                             currentVoiceData.maxPitch);

//                     AudioManager.Instance?.PlayDialogueTypingSound(
//                         clip,
//                         pitch);
//                 }
//             }

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