using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneController : MonoBehaviour
{
    [SerializeField] private GameObject gameContent;
    [SerializeField] private CanvasScaler uiCanvasScaler; // drag the Canvas (with CanvasScaler) here

    [Header("Transition Settings")]
    [Tooltip("How long the scale transition takes, in seconds.")]
    [SerializeField] private float transitionDuration = 0.5f;

    [Tooltip("Scale the game content shrinks to before the scene loads.")]
    [SerializeField] private Vector3 gameContentTargetScale = Vector3.zero;

    [Tooltip("Target Scale Factor on the CanvasScaler (grows the UI since it's Screen Space - Overlay).")]
    [SerializeField] private float uiCanvasTargetScaleFactor = 1.5f;

    [Tooltip("Optional easing curve for the transition (default: ease in-out).")]
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Entry Transition (on Awake)")]
    [Tooltip("If true, this scene starts in the 'transitioned' state (small content / big UI) and eases back to normal on Awake — mirrors the exit transition.")]
    [SerializeField] private bool playEntryTransition = true;

    [Tooltip("How long the entry (reverse) transition takes, in seconds.")]
    [SerializeField] private float entryTransitionDuration = 0.5f;

    private bool isTransitioning;
    private Vector3 normalGameContentScale;
    private float normalUiCanvasScaleFactor;

    private void Awake()
    {
        // Capture whatever scale/scaleFactor was authored in the Inspector/scene as "normal".
        normalGameContentScale = gameContent.transform.localScale;
        normalUiCanvasScaleFactor = uiCanvasScaler.scaleFactor;

        if (playEntryTransition)
        {
            // Snap into the "just transitioned out" state, then ease back to normal.
            gameContent.transform.localScale = gameContentTargetScale;
            uiCanvasScaler.scaleFactor = uiCanvasTargetScaleFactor;

            StartCoroutine(PlayEntryTransition());
        }
    }

    private IEnumerator PlayEntryTransition()
    {
        isTransitioning = true;

        Vector3 gameContentStartScale = gameContent.transform.localScale;
        float uiCanvasStartScaleFactor = uiCanvasScaler.scaleFactor;

        float elapsed = 0f;

        while (elapsed < entryTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / entryTransitionDuration);
            float easedT = Mathf.Clamp01(easeCurve.Evaluate(t));

            gameContent.transform.localScale = Vector3.Lerp(gameContentStartScale, normalGameContentScale, easedT);
            uiCanvasScaler.scaleFactor = Mathf.Lerp(uiCanvasStartScaleFactor, normalUiCanvasScaleFactor, easedT);

            yield return null;
        }

        gameContent.transform.localScale = normalGameContentScale;
        uiCanvasScaler.scaleFactor = normalUiCanvasScaleFactor;

        isTransitioning = false;
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

        Vector3 gameContentStartScale = gameContent.transform.localScale;
        float uiCanvasStartScaleFactor = uiCanvasScaler.scaleFactor;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float easedT = Mathf.Clamp01(easeCurve.Evaluate(t));

            gameContent.transform.localScale = Vector3.Lerp(gameContentStartScale, gameContentTargetScale, easedT);
            uiCanvasScaler.scaleFactor = Mathf.Lerp(uiCanvasStartScaleFactor, uiCanvasTargetScaleFactor, easedT);

            yield return null;
        }

        // Snap to exact final values in case of floating point drift.
        gameContent.transform.localScale = gameContentTargetScale;
        uiCanvasScaler.scaleFactor = uiCanvasTargetScaleFactor;

        SceneManager.LoadScene(sceneName);
    }
}