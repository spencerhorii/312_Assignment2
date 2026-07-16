using UnityEngine;

/// <summary>
/// Same show/hide + camera-zoom-scaling system as BuildingPopUp, but stripped down to just
/// the popup visuals - no sprite cycling, no E-key interaction, no scene-change logic.
/// Call SetListening(true/false) externally to show/hide it.
/// </summary>
public class HoverHandler : MonoBehaviour
{
    [Header("Show/Hide Transition")]
    [Tooltip("How far below its normal position the popup hides to.")]
    [SerializeField] private Vector3 hiddenOffset = new Vector3(0f, -2f, 0f);

    [Tooltip("Higher = snappier transition, lower = slower/smoother.")]
    [SerializeField] private float transitionSpeed = 8f;

    [Header("Camera Zoom Scaling")]
    [Tooltip("The camera's CameraControl script (tracks zoom state).")]
    [SerializeField] private CameraControl cameraControl;

    [Tooltip("How much bigger the popup gets (multiplied by its default scale) when the camera is fully zoomed out (idle).")]
    [SerializeField] private float maxScaleMultiplier = 2f;

    private bool listening;

    private Vector3 shownPosition;
    private Vector3 hiddenPosition;
    private Vector3 shownScale;

    private void Start()
    {
        listening = false;

        // Capture this popup's authored local position/scale (relative to its parent building) as its "shown" state.
        shownPosition = transform.localPosition;
        shownScale = transform.localScale;
        hiddenPosition = shownPosition + hiddenOffset;

        // Start hidden.
        transform.localPosition = hiddenPosition;
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        UpdatePopupTransform();
    }

    private void UpdatePopupTransform()
    {
        float scaleMultiplier = GetDistanceScaleMultiplier();

        Vector3 targetPosition = listening ? GetShownPositionWithRise(scaleMultiplier) : hiddenPosition;
        Vector3 targetScale = listening ? shownScale * scaleMultiplier : Vector3.zero;

        float t = 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime); // frame-rate independent smoothing
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, t);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, t);
    }

    private Vector3 GetShownPositionWithRise(float scaleMultiplier)
    {
        // Scale the Y offset by the same multiplier as the popup's scale, so it rises
        // proportionally as it grows (keeps it visually anchored above the building
        // instead of appearing to sink in as it scales up from its pivot).
        Vector3 result = shownPosition;
        result.y = shownPosition.y * scaleMultiplier;
        return result;
    }

    private float GetDistanceScaleMultiplier()
    {
        if (cameraControl == null) return 1f;

        // zoomBlend: 0 = zoomed out/idle (popup should be BIG), 1 = zoomed in (popup at default size)
        return Mathf.Lerp(maxScaleMultiplier, 1f, cameraControl.ZoomBlend);
    }

    /// <summary>
    /// Call this to show or hide the popup.
    /// </summary>
    public void SetListening(bool value)
    {
        listening = value;
    }
}