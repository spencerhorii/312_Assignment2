using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneController : MonoBehaviour
{
    [SerializeField] private GameObject gameContent;
    [SerializeField] private GameData gd;
    private int currDay;
    [SerializeField] private CanvasScaler uiCanvasScaler; // drag the Canvas (with CanvasScaler) here

    [Header("Transition Settings")]
    [Tooltip("How long the scale transition takes, in seconds.")]
    [SerializeField] private float transitionDuration = 0.5f;

    [Tooltip("Scale the game content shrinks to before the scene loads.")]
    [SerializeField] private Vector3 gameContentTargetScale = Vector3.zero;

    [Tooltip("Target Scale Factor on the CanvasScaler. Only used when the canvas is Screen Space - Camera.")]
    [SerializeField] private float uiCanvasTargetScaleFactor = 1.5f;

    [Tooltip("Optional easing curve for the transition (default: ease in-out).")]
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Fade Settings (used instead of scaling when Canvas is Screen Space - Overlay)")]
    [Tooltip("CanvasGroup on the UI canvas, used to fade it out instead of scaling. Auto-fetched from the canvas if left empty.")]
    [SerializeField] private CanvasGroup uiCanvasGroup;

    [Tooltip("Alpha the UI fades to before the scene loads.")]
    [SerializeField] private float uiFadeTargetAlpha = 0f;

    [Header("Entry Transition (on Awake)")]
    [Tooltip("If true, this scene starts in the 'transitioned' state (small content / faded or big UI) and eases back to normal on Awake — mirrors the exit transition.")]
    [SerializeField] private bool playEntryTransition = true;

    [Tooltip("How long the entry (reverse) transition takes, in seconds.")]
    [SerializeField] private float entryTransitionDuration = 0.5f;

    private bool isTransitioning;
    private Vector3 normalGameContentScale;
    private float normalUiCanvasScaleFactor;
    private float normalUiAlpha;

    private bool useFadeForUi; // true when canvas is Screen Space - Overlay

    private void Awake()
    {
        // Detect whether the canvas is Screen Space - Overlay, in which case we fade instead of scale.
        Canvas uiCanvas = uiCanvasScaler.GetComponent<Canvas>();
        useFadeForUi = uiCanvas != null && uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay;

        if (useFadeForUi && uiCanvasGroup == null)
        {
            uiCanvasGroup = uiCanvasScaler.GetComponent<CanvasGroup>();
            if (uiCanvasGroup == null)
            {
                uiCanvasGroup = uiCanvasScaler.gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Capture whatever scale/scaleFactor/alpha was authored in the Inspector/scene as "normal".
        normalGameContentScale = gameContent.transform.localScale;
        normalUiCanvasScaleFactor = uiCanvasScaler.scaleFactor;
        normalUiAlpha = useFadeForUi ? uiCanvasGroup.alpha : 1f;

        if (playEntryTransition)
        {
            // Freeze player movement until the scale-in transition below finishes.
            if (PlayerController2D.Instance != null)
            {
                PlayerController2D.Instance.CanMove = false;
            }

            // Snap into the "just transitioned out" state, then ease back to normal.
            gameContent.transform.localScale = gameContentTargetScale;

            if (useFadeForUi)
            {
                uiCanvasGroup.alpha = uiFadeTargetAlpha;
            }
            else
            {
                uiCanvasScaler.scaleFactor = uiCanvasTargetScaleFactor;
            }

            StartCoroutine(PlayEntryTransition());
        }

        currDay = gd.getDay();
    }

    private IEnumerator PlayEntryTransition()
    {
        isTransitioning = true;

        Vector3 gameContentStartScale = gameContent.transform.localScale;
        float uiCanvasStartScaleFactor = uiCanvasScaler.scaleFactor;
        float uiStartAlpha = useFadeForUi ? uiCanvasGroup.alpha : 1f;

        float elapsed = 0f;

        while (elapsed < entryTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / entryTransitionDuration);
            float easedT = Mathf.Clamp01(easeCurve.Evaluate(t));

            gameContent.transform.localScale = Vector3.Lerp(gameContentStartScale, normalGameContentScale, easedT);

            if (useFadeForUi)
            {
                uiCanvasGroup.alpha = Mathf.Lerp(uiStartAlpha, normalUiAlpha, easedT);
            }
            else
            {
                uiCanvasScaler.scaleFactor = Mathf.Lerp(uiCanvasStartScaleFactor, normalUiCanvasScaleFactor, easedT);
            }

            yield return null;
        }

        gameContent.transform.localScale = normalGameContentScale;

        if (useFadeForUi)
        {
            uiCanvasGroup.alpha = normalUiAlpha;
        }
        else
        {
            uiCanvasScaler.scaleFactor = normalUiCanvasScaleFactor;
        }

        isTransitioning = false;

        // Entry transition finished — hand movement control back to the player.
        if (PlayerController2D.Instance != null)
        {
            PlayerController2D.Instance.CanMove = true;
        }
    }

    public void ChangeScene(string sceneName)
    {
        if (isTransitioning) return; // prevent double-triggering
        StartCoroutine(TransitionAndLoad(sceneName));
    }

    public void EnterHouse(string sceneName)
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isTransitioning) return; // prevent double-triggering
            StartCoroutine(TransitionAndLoad(sceneName));
        }
    }

    private IEnumerator TransitionAndLoad(string sceneName)
    {
        isTransitioning = true;

        // Freeze player movement for the duration of the scale-out transition.
        // Not explicitly re-enabled at the end here since the scene is about to unload —
        // the new scene's own entry transition (if playEntryTransition is true there) will
        // re-enable it once IT finishes, assuming the player persists across scenes.
        if (PlayerController2D.Instance != null)
        {
            PlayerController2D.Instance.CanMove = false;
        }

        Vector3 gameContentStartScale = gameContent.transform.localScale;
        float uiCanvasStartScaleFactor = uiCanvasScaler.scaleFactor;
        float uiStartAlpha = useFadeForUi ? uiCanvasGroup.alpha : 1f;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float easedT = Mathf.Clamp01(easeCurve.Evaluate(t));

            gameContent.transform.localScale = Vector3.Lerp(gameContentStartScale, gameContentTargetScale, easedT);

            if (useFadeForUi)
            {
                uiCanvasGroup.alpha = Mathf.Lerp(uiStartAlpha, uiFadeTargetAlpha, easedT);
            }
            else
            {
                uiCanvasScaler.scaleFactor = Mathf.Lerp(uiCanvasStartScaleFactor, uiCanvasTargetScaleFactor, easedT);
            }

            yield return null;
        }

        // Snap to exact final values in case of floating point drift.
        gameContent.transform.localScale = gameContentTargetScale;

        if (useFadeForUi)
        {
            uiCanvasGroup.alpha = uiFadeTargetAlpha;
        }
        else
        {
            uiCanvasScaler.scaleFactor = uiCanvasTargetScaleFactor;
        }

        SceneManager.LoadScene(sceneName);
    }

    private void Update()
    {
        if(currDay < gd.getDay())
        {
            currDay = gd.getDay();
            ChangeScene("EndOfDay");
        }
    }
}