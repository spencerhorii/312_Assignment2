// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using TMPro;

// /// <summary>
// /// Renders the dialogue bubble and response bubble by subscribing to DialogueManager's
// /// events. Owns no dialogue state itself - purely a view layer, same split as InventoryUI.
// ///
// /// The main dialogue bubble does two extra things per node, both driven by
// /// DialogueManager.OnLineWillStart (fired before typing begins):
// ///   1. Resizes "Bubble Shape" (which holds a RoundedRectangle) to fit the upcoming text,
// ///      via TMP's GetPreferredValues, capped at Max Bubble Width, plus Bubble Padding.
// ///   2. Repositions the bubble above whichever world Transform is speaking - the NPC's
// ///      DialogueAnchor for NPC lines, or the Player's own transform for Player lines -
// ///      converted from world space to UI space, each with its own vertical offset field.
// ///
// /// IMPORTANT HIERARCHY REQUIREMENT: dialogueBubbleRoot must be a DIRECT CHILD of the Canvas
// /// (not nested inside another panel with its own offset/scale), since the anchor's screen
// /// position is converted directly into dialogueBubbleRoot's anchoredPosition space relative
// /// to the Canvas's own RectTransform.
// ///
// /// Both bubbles still use the same fade + slight vertical ease animation (opacity 0->1 while
// /// easing into its resting position, reversed when hiding), and visibility is controlled via
// /// CanvasGroup - both bubble GameObjects must stay active at all times, same reasoning as
// /// InventoryUI's panel.
// /// </summary>
// public class DialogueUI : MonoBehaviour
// {
//     [Header("Dialogue Bubble")]
//     [Tooltip("Root RectTransform of the dialogue bubble (handles fade/slide + repositioning). Must be a direct child of the Canvas and stay active at all times.")]
//     [SerializeField] private RectTransform dialogueBubbleRoot;
//     [SerializeField] private CanvasGroup dialogueBubbleCanvasGroup;
//     [Tooltip("The shape object holding the RoundedRectangle background, resized to fit the text each node. " +
//              "Its anchors are forced to a single centre point at Start() so sizeDelta reliably means 'exact width/height' " +
//              "regardless of how it was left in the Inspector - stretched anchors would make sizeDelta ADD to a stretched " +
//              "size instead of defining an absolute one, which is a common cause of unconstrained-looking text.")]
//     [SerializeField] private RectTransform bubbleShape;
//     [SerializeField] private TextMeshProUGUI dialogueText;
//     [Tooltip("The 'next line' arrow icon. Anchor it to the bottom-right of Bubble Shape (anchor min/max = (1,0)) " +
//              "so it tracks resizing - but its Pivot is forced to centre (0.5, 0.5) at Start() so the rocking " +
//              "animation rotates around its own centre rather than hinging off a corner.")]
//     [SerializeField] private RectTransform nextArrowIcon;

//     [Header("Bubble Sizing")]
//     [Tooltip("Maximum width (UI units) a line of dialogue text can reach before wrapping to a new line. " +
//              "This is the direct control for line-break length - lower it to force shorter lines.")]
//     [SerializeField] private float maxBubbleWidth = 500f;
//     [Tooltip("Padding between the bubble's edge and the text, in UI units (x = left/right, y = top/bottom).")]
//     [SerializeField] private Vector2 bubblePadding = new Vector2(24f, 16f);

//     [Header("Speaker Positioning")]
//     [Tooltip("Extra vertical offset (UI units) added above the NPC's DialogueAnchor screen position.")]
//     [SerializeField] private float npcSpeakerVerticalOffset = 40f;
//     [Tooltip("Extra vertical offset (UI units) added above the Player's screen position, so the bubble sits above their head instead of on top of them.")]
//     [SerializeField] private float playerSpeakerVerticalOffset = 60f;
//     [Tooltip("Used only if a world position can't be resolved (e.g. Player/NPC reference missing) - a plain screen-space fallback.")]
//     [SerializeField] private Vector2 fallbackAnchoredPosition = Vector2.zero;

//     [Header("Bubble Fade/Slide Animation")]
//     [SerializeField] private float bubbleFadeDuration = 0.2f;
//     [Tooltip("How far (in UI units) the bubble slides while fading. It rests slightly above " +
//              "its resting spot while hidden, and eases down into place as it appears.")]
//     [SerializeField] private float bubbleSlideDistance = 20f;
//     [SerializeField] private AnimationCurve bubbleFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

//     [Header("Next Arrow Rock Animation")]
//     [Tooltip("How far (in degrees) the arrow rocks side to side around its centre.")]
//     [SerializeField] private float arrowRockAmplitude = 10f;
//     [Tooltip("How fast the arrow rocks, in full back-and-forth cycles per second.")]
//     [SerializeField] private float arrowRockSpeed = 1f;

//     [Header("Response Bubble")]
//     [Tooltip("Root RectTransform of the response bubble, anchored to the middle of the screen. Must stay active at all times.")]
//     [SerializeField] private RectTransform responseBubbleRoot;
//     [SerializeField] private CanvasGroup responseBubbleCanvasGroup;
//     [Tooltip("Container with a Vertical Layout Group that holds the instantiated option rows, opening downward.")]
//     [SerializeField] private RectTransform responseOptionsContainer;
//     [Tooltip("Prefab for a single response option row, must have a DialogueResponseOptionUI component.")]
//     [SerializeField] private GameObject responseOptionPrefab;
//     [Tooltip("Selection icon moved to whichever option row is currently highlighted.")]
//     [SerializeField] private RectTransform responseSelectionIcon;

//     [Header("Response Option Opacity")]
//     [Range(0f, 1f)]
//     [SerializeField] private float dimmedAlpha = 0.5f;

//     private Vector2 dialogueBubbleBasePos;
//     private Vector2 responseBubbleBasePos;
//     private Coroutine dialogueBubbleAnim;
//     private Coroutine responseBubbleAnim;
//     private Coroutine arrowRockCoroutine;
//     private readonly List<DialogueResponseOptionUI> optionUIs = new List<DialogueResponseOptionUI>();
//     private bool isSubscribed;
//     private Canvas parentCanvas;

//     private void Start()
//     {
//         parentCanvas = GetComponentInParent<Canvas>();
//         if (parentCanvas == null)
//         {
//             Debug.LogWarning("DialogueUI: no parent Canvas found - bubble positioning will fall back to fallbackAnchoredPosition.");
//         }

//         if (dialogueBubbleRoot != null)
//         {
//             dialogueBubbleBasePos = dialogueBubbleRoot.anchoredPosition;
//             dialogueBubbleRoot.anchoredPosition = dialogueBubbleBasePos + new Vector2(0f, bubbleSlideDistance);
//         }

//         if (responseBubbleRoot != null)
//         {
//             responseBubbleBasePos = responseBubbleRoot.anchoredPosition;
//             responseBubbleRoot.anchoredPosition = responseBubbleBasePos + new Vector2(0f, bubbleSlideDistance);
//         }

//         // Force a single-point anchor so sizeDelta always means "exact width/height" - see the
//         // field tooltip above for why a stretched anchor here would break dynamic sizing.
//         if (bubbleShape != null)
//         {
//             Vector2 currentAnchor = bubbleShape.anchorMin; // preserve whatever point it's centred on (e.g. (0.5,0.5))
//             bubbleShape.anchorMin = currentAnchor;
//             bubbleShape.anchorMax = currentAnchor;
//         }

//         // Force centre pivot so the arrow's rock animation rotates around its own centre,
//         // not whatever corner it happens to be anchored to for positioning purposes.
//         if (nextArrowIcon != null)
//         {
//             nextArrowIcon.pivot = new Vector2(0.5f, 0.5f);
//         }

//         // Text is centre-anchored and explicitly sized/positioned each line (see ResizeBubbleForText),
//         // rather than stretched to fill the bubble - this avoids relying on Unity's parent-child
//         // stretch timing, and keeps wrapping calculation unambiguous.
//         if (dialogueText != null)
//         {
//             RectTransform textRect = dialogueText.rectTransform;
//             textRect.anchorMin = new Vector2(0.5f, 0.5f);
//             textRect.anchorMax = new Vector2(0.5f, 0.5f);
//             textRect.pivot = new Vector2(0.5f, 0.5f);
//             textRect.anchoredPosition = Vector2.zero;

//             dialogueText.enableWordWrapping = true;
//             dialogueText.alignment = TextAlignmentOptions.Center;
//         }

//         SetGroupVisible(dialogueBubbleCanvasGroup, false);
//         SetGroupVisible(responseBubbleCanvasGroup, false);

//         if (nextArrowIcon != null) nextArrowIcon.gameObject.SetActive(false);

//         TrySubscribe();
//     }

//     private void OnEnable()
//     {
//         TrySubscribe();
//     }

//     private void OnDisable()
//     {
//         if (DialogueManager.Instance != null && isSubscribed)
//         {
//             DialogueManager.Instance.OnDialogueOpened -= HandleDialogueOpened;
//             DialogueManager.Instance.OnDialogueClosed -= HandleDialogueClosed;
//             DialogueManager.Instance.OnLineWillStart -= HandleLineWillStart;
//             DialogueManager.Instance.OnLineTextUpdated -= HandleLineTextUpdated;
//             DialogueManager.Instance.OnShowNextArrow -= HandleShowNextArrow;
//             DialogueManager.Instance.OnResponsePromptOpened -= HandleResponsePromptOpened;
//             DialogueManager.Instance.OnResponsePromptClosed -= HandleResponsePromptClosed;
//             DialogueManager.Instance.OnResponseSelectionChanged -= HandleResponseSelectionChanged;
//             isSubscribed = false;
//         }
//     }

//     private void TrySubscribe()
//     {
//         if (isSubscribed) return;

//         if (DialogueManager.Instance == null)
//         {
//             Debug.LogWarning("DialogueUI: DialogueManager.Instance is null. Make sure DialogueManager exists in the scene.");
//             return;
//         }

//         DialogueManager.Instance.OnDialogueOpened += HandleDialogueOpened;
//         DialogueManager.Instance.OnDialogueClosed += HandleDialogueClosed;
//         DialogueManager.Instance.OnLineWillStart += HandleLineWillStart;
//         DialogueManager.Instance.OnLineTextUpdated += HandleLineTextUpdated;
//         DialogueManager.Instance.OnShowNextArrow += HandleShowNextArrow;
//         DialogueManager.Instance.OnResponsePromptOpened += HandleResponsePromptOpened;
//         DialogueManager.Instance.OnResponsePromptClosed += HandleResponsePromptClosed;
//         DialogueManager.Instance.OnResponseSelectionChanged += HandleResponseSelectionChanged;
//         isSubscribed = true;
//     }

//     // ---------- Dialogue bubble ----------

//     private void HandleDialogueOpened()
//     {
//         AnimateBubble(dialogueBubbleRoot, dialogueBubbleCanvasGroup, dialogueBubbleBasePos, true, ref dialogueBubbleAnim);
//     }

//     private void HandleDialogueClosed()
//     {
//         AnimateBubble(dialogueBubbleRoot, dialogueBubbleCanvasGroup, dialogueBubbleBasePos, false, ref dialogueBubbleAnim);
//         HandleShowNextArrow(false);
//     }

//     private void HandleLineWillStart(DialogueSpeaker speaker, Transform npcAnchor, string fullText)
//     {
//         ResizeBubbleForText(fullText);
//         PositionBubble(speaker, npcAnchor);
//     }

//     private void HandleLineTextUpdated(string text)
//     {
//         if (dialogueText != null) dialogueText.text = text;
//     }

//     private void HandleShowNextArrow(bool show)
//     {
//         if (nextArrowIcon == null) return;

//         if (arrowRockCoroutine != null)
//         {
//             StopCoroutine(arrowRockCoroutine);
//             arrowRockCoroutine = null;
//         }

//         nextArrowIcon.gameObject.SetActive(show);
//         nextArrowIcon.localEulerAngles = Vector3.zero;

//         if (show) arrowRockCoroutine = StartCoroutine(RockArrow());
//     }

//     private IEnumerator RockArrow()
//     {
//         float elapsed = 0f;
//         while (true)
//         {
//             elapsed += Time.deltaTime;
//             float angle = Mathf.Sin(elapsed * arrowRockSpeed * Mathf.PI * 2f) * arrowRockAmplitude;
//             nextArrowIcon.localEulerAngles = new Vector3(0f, 0f, angle);
//             yield return null;
//         }
//     }

//     // ---------- Bubble sizing ----------

//     private void ResizeBubbleForText(string text)
//     {
//         if (dialogueText == null || bubbleShape == null) return;

//         float maxContentWidth = Mathf.Max(10f, maxBubbleWidth - bubblePadding.x * 2f);

//         // Force the text box to the max width first, then make TMP actually lay out the real
//         // mesh and read back its true rendered size - more reliable across TMP versions than
//         // asking GetPreferredValues to predict a wrapped size without touching the RectTransform.
//         RectTransform textRect = dialogueText.rectTransform;
//         textRect.sizeDelta = new Vector2(maxContentWidth, textRect.sizeDelta.y);

//         dialogueText.text = text;
//         dialogueText.ForceMeshUpdate();

//         float contentWidth = Mathf.Min(dialogueText.preferredWidth, maxContentWidth);
//         float contentHeight = dialogueText.preferredHeight;

//         // Shrink the text box down to exactly the content it ended up needing (still centred,
//         // since anchor/pivot/anchoredPosition are all (0.5,0.5)/(0,0) - see Start()).
//         textRect.sizeDelta = new Vector2(contentWidth, contentHeight);

//         bubbleShape.sizeDelta = new Vector2(
//             contentWidth + bubblePadding.x * 2f,
//             contentHeight + bubblePadding.y * 2f
//         );
//     }

//     // ---------- Speaker-based positioning ----------

//     private void PositionBubble(DialogueSpeaker speaker, Transform npcAnchor)
//     {
//         if (dialogueBubbleRoot == null) return;

//         Vector2 newPos = ComputeBubblePosition(speaker, npcAnchor);
//         dialogueBubbleBasePos = newPos;

//         // If the bubble is already visible (mid-conversation, e.g. speaker changed between
//         // lines), reposition it instantly. If it's not yet visible (the very first node of a
//         // new conversation), leave the RectTransform where it is - HandleDialogueOpened's
//         // fade-in coroutine will read dialogueBubbleBasePos as its target and slide down into it.
//         bool bubbleCurrentlyVisible = dialogueBubbleCanvasGroup != null && dialogueBubbleCanvasGroup.alpha > 0.01f;
//         if (bubbleCurrentlyVisible)
//         {
//             dialogueBubbleRoot.anchoredPosition = newPos;
//         }
//     }

//     private Vector2 ComputeBubblePosition(DialogueSpeaker speaker, Transform npcAnchor)
//     {
//         Transform worldAnchor = null;
//         float verticalOffset = 0f;

//         if (speaker == DialogueSpeaker.NPC && npcAnchor != null)
//         {
//             worldAnchor = npcAnchor;
//             verticalOffset = npcSpeakerVerticalOffset;
//         }
//         else if (speaker == DialogueSpeaker.Player && PlayerController2D.Instance != null)
//         {
//             worldAnchor = PlayerController2D.Instance.transform;
//             verticalOffset = playerSpeakerVerticalOffset;
//         }

//         if (worldAnchor == null || parentCanvas == null)
//         {
//             return fallbackAnchoredPosition;
//         }

//         Camera worldCamera = Camera.main;
//         if (worldCamera == null) return fallbackAnchoredPosition;

//         Vector2 screenPoint = worldCamera.WorldToScreenPoint(worldAnchor.position);

//         Camera uiCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
//         RectTransform canvasRect = parentCanvas.transform as RectTransform;

//         if (canvasRect != null &&
//             RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 localPoint))
//         {
//             return localPoint + new Vector2(0f, verticalOffset);
//         }

//         return fallbackAnchoredPosition;
//     }

//     // ---------- Response bubble ----------

//     private void HandleResponsePromptOpened(DialogueNodeType type, List<string> optionTexts)
//     {
//         BuildOptions(optionTexts);
//         AnimateBubble(responseBubbleRoot, responseBubbleCanvasGroup, responseBubbleBasePos, true, ref responseBubbleAnim);
//     }

//     private void HandleResponsePromptClosed()
//     {
//         AnimateBubble(responseBubbleRoot, responseBubbleCanvasGroup, responseBubbleBasePos, false, ref responseBubbleAnim);
//     }

//     private void HandleResponseSelectionChanged(int selectedIndex)
//     {
//         for (int i = 0; i < optionUIs.Count; i++)
//         {
//             optionUIs[i].SetOpacity(i == selectedIndex ? 1f : dimmedAlpha);
//         }

//         if (responseSelectionIcon != null && selectedIndex >= 0 && selectedIndex < optionUIs.Count)
//         {
//             Canvas.ForceUpdateCanvases();
//             responseSelectionIcon.position = optionUIs[selectedIndex].RectTransform.position;
//         }
//     }

//     private void BuildOptions(List<string> optionTexts)
//     {
//         foreach (Transform child in responseOptionsContainer)
//         {
//             Destroy(child.gameObject);
//         }
//         optionUIs.Clear();

//         foreach (string text in optionTexts)
//         {
//             GameObject instance = Instantiate(responseOptionPrefab, responseOptionsContainer);
//             DialogueResponseOptionUI optionUI = instance.GetComponent<DialogueResponseOptionUI>();

//             if (optionUI == null)
//             {
//                 Debug.LogError("DialogueUI: responseOptionPrefab is missing a DialogueResponseOptionUI component.");
//                 continue;
//             }

//             optionUI.SetText(text);
//             optionUIs.Add(optionUI);
//         }

//         Canvas.ForceUpdateCanvases();
//     }

//     // ---------- Shared fade/slide animation ----------

//     private void AnimateBubble(RectTransform rect, CanvasGroup group, Vector2 basePos, bool show, ref Coroutine coroutineRef)
//     {
//         if (rect == null || group == null) return;

//         if (coroutineRef != null) StopCoroutine(coroutineRef);
//         coroutineRef = StartCoroutine(FadeSlide(rect, group, basePos, show));
//     }

//     private IEnumerator FadeSlide(RectTransform rect, CanvasGroup group, Vector2 basePos, bool show)
//     {
//         float startAlpha = group.alpha;
//         float targetAlpha = show ? 1f : 0f;
//         Vector2 startPos = rect.anchoredPosition;
//         Vector2 targetPos = show ? basePos : basePos + new Vector2(0f, bubbleSlideDistance);

//         float elapsed = 0f;
//         while (elapsed < bubbleFadeDuration)
//         {
//             elapsed += Time.deltaTime;
//             float normalized = Mathf.Clamp01(elapsed / bubbleFadeDuration);
//             float curved = bubbleFadeCurve.Evaluate(normalized);
//             group.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, curved);
//             rect.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, curved);
//             yield return null;
//         }

//         group.alpha = targetAlpha;
//         rect.anchoredPosition = targetPos;
//         group.interactable = show;
//         group.blocksRaycasts = show;
//     }

//     private void SetGroupVisible(CanvasGroup group, bool visible)
//     {
//         if (group == null) return;

//         group.alpha = visible ? 1f : 0f;
//         group.interactable = visible;
//         group.blocksRaycasts = visible;
//     }
// }


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Renders the dialogue bubble and response bubble by subscribing to DialogueManager's
/// events. Owns no dialogue state itself - purely a view layer, same split as InventoryUI.
///
/// The main dialogue bubble does two extra things per node, both driven by
/// DialogueManager.OnLineWillStart (fired before typing begins):
///   1. Resizes "Bubble Shape" (which holds a RoundedRectangle) to fit the upcoming text,
///      via TMP's GetPreferredValues, capped at Max Bubble Width, plus Bubble Padding.
///   2. Repositions the bubble above whichever world Transform is speaking - the NPC's
///      DialogueAnchor for NPC lines, or the Player's own transform for Player lines -
///      converted from world space to UI space, each with its own vertical offset field.
///
/// IMPORTANT HIERARCHY REQUIREMENT: dialogueBubbleRoot must be a DIRECT CHILD of the Canvas
/// (not nested inside another panel with its own offset/scale), since the anchor's screen
/// position is converted directly into dialogueBubbleRoot's anchoredPosition space relative
/// to the Canvas's own RectTransform.
///
/// Both bubbles still use the same fade + slight vertical ease animation (opacity 0->1 while
/// easing into its resting position, reversed when hiding), and visibility is controlled via
/// CanvasGroup - both bubble GameObjects must stay active at all times, same reasoning as
/// InventoryUI's panel.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    [Header("Dialogue Bubble")]
    [Tooltip("Root RectTransform of the dialogue bubble (handles fade/slide + repositioning). Must be a direct child of the Canvas and stay active at all times.")]
    [SerializeField] private RectTransform dialogueBubbleRoot;
    [SerializeField] private CanvasGroup dialogueBubbleCanvasGroup;
    [Tooltip("The shape object holding the RoundedRectangle background, resized to fit the text each node. " +
             "Its anchors are forced to a single centre point at Start() so sizeDelta reliably means 'exact width/height' " +
             "regardless of how it was left in the Inspector - stretched anchors would make sizeDelta ADD to a stretched " +
             "size instead of defining an absolute one, which is a common cause of unconstrained-looking text.")]
    [SerializeField] private RectTransform bubbleShape;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [Tooltip("The 'next line' arrow icon. Anchor it to the bottom-right of Bubble Shape (anchor min/max = (1,0)) " +
             "so it tracks resizing - but its Pivot is forced to centre (0.5, 0.5) at Start() so the rocking " +
             "animation rotates around its own centre rather than hinging off a corner.")]
    [SerializeField] private RectTransform nextArrowIcon;

    [Header("Bubble Sizing")]
    [Tooltip("Maximum width (UI units) a line of dialogue text can reach before wrapping to a new line. " +
             "This is the direct control for line-break length - lower it to force shorter lines.")]
    [SerializeField] private float maxBubbleWidth = 500f;
    [Tooltip("Padding between the bubble's edge and the text, in UI units (x = left/right, y = top/bottom).")]
    [SerializeField] private Vector2 bubblePadding = new Vector2(24f, 16f);

    [Header("Speaker Positioning")]
    [Tooltip("Extra vertical offset (UI units) added above the NPC's DialogueAnchor screen position.")]
    [SerializeField] private float npcSpeakerVerticalOffset = 40f;
    [Tooltip("Extra vertical offset (UI units) added above the Player's screen position, so the bubble sits above their head instead of on top of them.")]
    [SerializeField] private float playerSpeakerVerticalOffset = 60f;
    [Tooltip("Used only if a world position can't be resolved (e.g. Player/NPC reference missing) - a plain screen-space fallback.")]
    [SerializeField] private Vector2 fallbackAnchoredPosition = Vector2.zero;

    [Header("Bubble Fade/Slide Animation")]
    [SerializeField] private float bubbleFadeDuration = 0.2f;
    [Tooltip("How far (in UI units) the bubble slides while fading. It rests slightly above " +
             "its resting spot while hidden, and eases down into place as it appears.")]
    [SerializeField] private float bubbleSlideDistance = 20f;
    [SerializeField] private AnimationCurve bubbleFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Next Arrow Rock Animation")]
    [Tooltip("How far (in degrees) the arrow rocks side to side around its centre.")]
    [SerializeField] private float arrowRockAmplitude = 10f;
    [Tooltip("How fast the arrow rocks, in full back-and-forth cycles per second.")]
    [SerializeField] private float arrowRockSpeed = 1f;

    [Header("Response Bubble")]
    [Tooltip("Root RectTransform of the response bubble, anchored to the middle of the screen. Must stay active at all times.")]
    [SerializeField] private RectTransform responseBubbleRoot;
    [SerializeField] private CanvasGroup responseBubbleCanvasGroup;
    [Tooltip("Container with a Vertical Layout Group that holds the instantiated option rows, opening downward.")]
    [SerializeField] private RectTransform responseOptionsContainer;
    [Tooltip("Prefab for a single response option row, must have a DialogueResponseOptionUI component.")]
    [SerializeField] private GameObject responseOptionPrefab;
    [Tooltip("Selection icon moved to whichever option row is currently highlighted.")]
    [SerializeField] private RectTransform responseSelectionIcon;

    [Header("Response Option Opacity")]
    [Range(0f, 1f)]
    [SerializeField] private float dimmedAlpha = 0.5f;

    private Vector2 dialogueBubbleBasePos;
    private Vector2 responseBubbleBasePos;
    private Coroutine dialogueBubbleAnim;
    private Coroutine responseBubbleAnim;
    private Coroutine arrowRockCoroutine;
    private readonly List<DialogueResponseOptionUI> optionUIs = new List<DialogueResponseOptionUI>();
    private bool isSubscribed;
    private Canvas parentCanvas;

    private void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogWarning("DialogueUI: no parent Canvas found - bubble positioning will fall back to fallbackAnchoredPosition.");
        }

        if (dialogueBubbleRoot != null)
        {
            dialogueBubbleBasePos = dialogueBubbleRoot.anchoredPosition;
            dialogueBubbleRoot.anchoredPosition = dialogueBubbleBasePos + new Vector2(0f, bubbleSlideDistance);
        }

        if (responseBubbleRoot != null)
        {
            responseBubbleBasePos = responseBubbleRoot.anchoredPosition;
            responseBubbleRoot.anchoredPosition = responseBubbleBasePos + new Vector2(0f, bubbleSlideDistance);
        }

        // Force a single-point anchor so sizeDelta always means "exact width/height" - see the
        // field tooltip above for why a stretched anchor here would break dynamic sizing.
        if (bubbleShape != null)
        {
            Vector2 currentAnchor = bubbleShape.anchorMin; // preserve whatever point it's centred on (e.g. (0.5,0.5))
            bubbleShape.anchorMin = currentAnchor;
            bubbleShape.anchorMax = currentAnchor;
        }

        // Force centre pivot so the arrow's rock animation rotates around its own centre,
        // not whatever corner it happens to be anchored to for positioning purposes.
        if (nextArrowIcon != null)
        {
            nextArrowIcon.pivot = new Vector2(0.5f, 0.5f);
        }

        // Text is centre-anchored and explicitly sized/positioned each line (see ResizeBubbleForText),
        // rather than stretched to fill the bubble - this avoids relying on Unity's parent-child
        // stretch timing, and keeps wrapping calculation unambiguous.
        if (dialogueText != null)
        {
            RectTransform textRect = dialogueText.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;

            dialogueText.enableWordWrapping = true;
            dialogueText.alignment = TextAlignmentOptions.Center;
        }

        SetGroupVisible(dialogueBubbleCanvasGroup, false);
        SetGroupVisible(responseBubbleCanvasGroup, false);

        if (nextArrowIcon != null) nextArrowIcon.gameObject.SetActive(false);

        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (DialogueManager.Instance != null && isSubscribed)
        {
            DialogueManager.Instance.OnDialogueOpened -= HandleDialogueOpened;
            DialogueManager.Instance.OnDialogueClosed -= HandleDialogueClosed;
            DialogueManager.Instance.OnLineWillStart -= HandleLineWillStart;
            DialogueManager.Instance.OnLineTextUpdated -= HandleLineTextUpdated;
            DialogueManager.Instance.OnShowNextArrow -= HandleShowNextArrow;
            DialogueManager.Instance.OnResponsePromptOpened -= HandleResponsePromptOpened;
            DialogueManager.Instance.OnResponsePromptClosed -= HandleResponsePromptClosed;
            DialogueManager.Instance.OnResponseSelectionChanged -= HandleResponseSelectionChanged;
            isSubscribed = false;
        }
    }

    private void TrySubscribe()
    {
        if (isSubscribed) return;

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("DialogueUI: DialogueManager.Instance is null. Make sure DialogueManager exists in the scene.");
            return;
        }

        DialogueManager.Instance.OnDialogueOpened += HandleDialogueOpened;
        DialogueManager.Instance.OnDialogueClosed += HandleDialogueClosed;
        DialogueManager.Instance.OnLineWillStart += HandleLineWillStart;
        DialogueManager.Instance.OnLineTextUpdated += HandleLineTextUpdated;
        DialogueManager.Instance.OnShowNextArrow += HandleShowNextArrow;
        DialogueManager.Instance.OnResponsePromptOpened += HandleResponsePromptOpened;
        DialogueManager.Instance.OnResponsePromptClosed += HandleResponsePromptClosed;
        DialogueManager.Instance.OnResponseSelectionChanged += HandleResponseSelectionChanged;
        isSubscribed = true;
    }

    // ---------- Dialogue bubble ----------

    private void HandleDialogueOpened()
    {
        AnimateBubble(dialogueBubbleRoot, dialogueBubbleCanvasGroup, dialogueBubbleBasePos, true, ref dialogueBubbleAnim);
    }

    private void HandleDialogueClosed()
    {
        AnimateBubble(dialogueBubbleRoot, dialogueBubbleCanvasGroup, dialogueBubbleBasePos, false, ref dialogueBubbleAnim);
        HandleShowNextArrow(false);
    }

    private void HandleLineWillStart(DialogueSpeaker speaker, Transform npcAnchor, string fullText)
    {
        ResizeBubbleForText(fullText);
        PositionBubble(speaker, npcAnchor);
    }

    private void HandleLineTextUpdated(string text)
    {
        if (dialogueText != null) dialogueText.text = text;
    }

    private void HandleShowNextArrow(bool show)
    {
        if (nextArrowIcon == null) return;

        if (arrowRockCoroutine != null)
        {
            StopCoroutine(arrowRockCoroutine);
            arrowRockCoroutine = null;
        }

        nextArrowIcon.gameObject.SetActive(show);
        nextArrowIcon.localEulerAngles = Vector3.zero;

        if (show) arrowRockCoroutine = StartCoroutine(RockArrow());
    }

    private IEnumerator RockArrow()
    {
        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Sin(elapsed * arrowRockSpeed * Mathf.PI * 2f) * arrowRockAmplitude;
            nextArrowIcon.localEulerAngles = new Vector3(0f, 0f, angle);
            yield return null;
        }
    }

    // ---------- Bubble sizing ----------

    private void ResizeBubbleForText(string text)
    {
        if (dialogueText == null || bubbleShape == null) return;

        float maxContentWidth = Mathf.Max(10f, maxBubbleWidth - bubblePadding.x * 2f);

        // Force the text box to the max width first, then make TMP actually lay out the real
        // mesh and read back its true rendered size - more reliable across TMP versions than
        // asking GetPreferredValues to predict a wrapped size without touching the RectTransform.
        RectTransform textRect = dialogueText.rectTransform;
        textRect.sizeDelta = new Vector2(maxContentWidth, textRect.sizeDelta.y);

        dialogueText.text = text;
        dialogueText.ForceMeshUpdate();

        float contentWidth = Mathf.Min(dialogueText.preferredWidth, maxContentWidth);
        float contentHeight = dialogueText.preferredHeight;

        // Shrink the text box down to exactly the content it ended up needing (still centred,
        // since anchor/pivot/anchoredPosition are all (0.5,0.5)/(0,0) - see Start()).
        textRect.sizeDelta = new Vector2(contentWidth, contentHeight);

        bubbleShape.sizeDelta = new Vector2(
            contentWidth + bubblePadding.x * 2f,
            contentHeight + bubblePadding.y * 2f
        );
    }

    // ---------- Speaker-based positioning ----------

    private void PositionBubble(DialogueSpeaker speaker, Transform npcAnchor)
    {
        if (dialogueBubbleRoot == null) return;

        Vector2 newPos = ComputeBubblePosition(speaker, npcAnchor);
        dialogueBubbleBasePos = newPos;

        // If the bubble is already visible (mid-conversation, e.g. speaker changed between
        // lines), reposition it instantly. If it's not yet visible (the very first node of a
        // new conversation), leave the RectTransform where it is - HandleDialogueOpened's
        // fade-in coroutine will read dialogueBubbleBasePos as its target and slide down into it.
        bool bubbleCurrentlyVisible = dialogueBubbleCanvasGroup != null && dialogueBubbleCanvasGroup.alpha > 0.01f;
        if (bubbleCurrentlyVisible)
        {
            dialogueBubbleRoot.anchoredPosition = newPos;
        }
    }

    private Vector2 ComputeBubblePosition(DialogueSpeaker speaker, Transform npcAnchor)
    {
        Transform worldAnchor = null;
        float verticalOffset = 0f;

        if (speaker == DialogueSpeaker.NPC && npcAnchor != null)
        {
            worldAnchor = npcAnchor;
            verticalOffset = npcSpeakerVerticalOffset;
        }
        else if (speaker == DialogueSpeaker.Player && PlayerController2D.Instance != null)
        {
            worldAnchor = PlayerController2D.Instance.transform;
            verticalOffset = playerSpeakerVerticalOffset;
        }

        if (worldAnchor == null || parentCanvas == null)
        {
            return fallbackAnchoredPosition;
        }

        Camera worldCamera = Camera.main;
        if (worldCamera == null) return fallbackAnchoredPosition;

        Vector2 screenPoint = worldCamera.WorldToScreenPoint(worldAnchor.position);

        Camera uiCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
        RectTransform canvasRect = parentCanvas.transform as RectTransform;

        if (canvasRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 localPoint))
        {
            return localPoint + new Vector2(0f, verticalOffset);
        }

        return fallbackAnchoredPosition;
    }

    // ---------- Response bubble ----------

    private void HandleResponsePromptOpened(DialogueNodeType type, List<string> optionTexts)
    {
        BuildOptions(optionTexts);
        AnimateBubble(responseBubbleRoot, responseBubbleCanvasGroup, responseBubbleBasePos, true, ref responseBubbleAnim);
    }

    private void HandleResponsePromptClosed()
    {
        AnimateBubble(responseBubbleRoot, responseBubbleCanvasGroup, responseBubbleBasePos, false, ref responseBubbleAnim);
    }

    private void HandleResponseSelectionChanged(int selectedIndex)
    {
        for (int i = 0; i < optionUIs.Count; i++)
        {
            optionUIs[i].SetOpacity(i == selectedIndex ? 1f : dimmedAlpha);
        }

        if (responseSelectionIcon != null && selectedIndex >= 0 && selectedIndex < optionUIs.Count)
        {
            Canvas.ForceUpdateCanvases();
            responseSelectionIcon.position = optionUIs[selectedIndex].RectTransform.position;
        }
    }

    private void BuildOptions(List<string> optionTexts)
    {
        // Deactivate before destroying: Destroy() is deferred to end-of-frame, so without this,
        // the Vertical Layout Group would briefly "see" both the old (not-yet-removed) and new
        // rows when we force a layout update a few lines below - producing a stale position read
        // for the selection icon. Deactivating removes them from layout calculations immediately.
        foreach (Transform child in responseOptionsContainer)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        optionUIs.Clear();

        foreach (string text in optionTexts)
        {
            GameObject instance = Instantiate(responseOptionPrefab, responseOptionsContainer);
            DialogueResponseOptionUI optionUI = instance.GetComponent<DialogueResponseOptionUI>();

            if (optionUI == null)
            {
                Debug.LogError("DialogueUI: responseOptionPrefab is missing a DialogueResponseOptionUI component.");
                continue;
            }

            optionUI.SetText(text);
            optionUIs.Add(optionUI);
        }

        Canvas.ForceUpdateCanvases();
    }

    // ---------- Shared fade/slide animation ----------

    private void AnimateBubble(RectTransform rect, CanvasGroup group, Vector2 basePos, bool show, ref Coroutine coroutineRef)
    {
        if (rect == null || group == null) return;

        if (coroutineRef != null) StopCoroutine(coroutineRef);
        coroutineRef = StartCoroutine(FadeSlide(rect, group, basePos, show));
    }

    private IEnumerator FadeSlide(RectTransform rect, CanvasGroup group, Vector2 basePos, bool show)
    {
        float startAlpha = group.alpha;
        float targetAlpha = show ? 1f : 0f;
        Vector2 startPos = rect.anchoredPosition;
        Vector2 targetPos = show ? basePos : basePos + new Vector2(0f, bubbleSlideDistance);

        float elapsed = 0f;
        while (elapsed < bubbleFadeDuration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / bubbleFadeDuration);
            float curved = bubbleFadeCurve.Evaluate(normalized);
            group.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, curved);
            rect.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, curved);
            yield return null;
        }

        group.alpha = targetAlpha;
        rect.anchoredPosition = targetPos;
        group.interactable = show;
        group.blocksRaycasts = show;
    }

    private void SetGroupVisible(CanvasGroup group, bool visible)
    {
        if (group == null) return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}