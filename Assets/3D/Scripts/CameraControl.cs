using UnityEngine;

/// <summary>
/// Adds a subtle idle bounce/sway to the camera, useful for giving a
/// static or slow-moving camera a bit of life (breathing/floating feel).
/// Attach this to your Camera GameObject (or a rig that holds it).
/// </summary>
public class CameraControl : MonoBehaviour
{
    [Header("Bounce Settings")]
    [Tooltip("How far the camera moves up/down from its base position.")]
    [SerializeField] private float bounceHeight = 0.05f;

    [Tooltip("How fast the up/down bounce cycles per second.")]
    [SerializeField] private float bounceSpeed = 1.5f;

    [Header("Sway Settings (optional horizontal drift)")]
    [Tooltip("How far the camera drifts side to side.")]
    [SerializeField] private float swayAmount = 0.02f;

    [Tooltip("How fast the side-to-side sway cycles per second.")]
    [SerializeField] private float swaySpeed = 0.75f;

    [Header("Tilt Settings (optional subtle rotation)")]
    [Tooltip("How many degrees the camera tilts back and forth.")]
    [SerializeField] private float tiltAmount = 0.5f;

    [Tooltip("How fast the tilt cycles per second.")]
    [SerializeField] private float tiltSpeed = 1f;

    [Header("General")]
    [Tooltip("If true, uses unscaled time so the bounce ignores Time.timeScale (e.g. during pause).")]
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Target Zoom/Follow")]
    [Tooltip("The object the camera zooms toward and follows while W/A/S/D is held.")]
    [SerializeField] private Transform target;

    [Tooltip("How far (0-1) the camera moves from its default position toward the target when fully zoomed in.")]
    [Range(0f, 1f)]
    [SerializeField] private float followAmount = 0.3f;

    [Tooltip("How quickly the camera eases toward the zoomed-in state.")]
    [SerializeField] private float zoomInSpeed = 4f;

    [Tooltip("How quickly the camera eases back to its default position when no key is held.")]
    [SerializeField] private float zoomOutSpeed = 3f;

    [Tooltip("Shapes the zoom transition over time. X axis = raw progress (0-1), Y axis = eased blend actually applied. Default is linear.")]
    [SerializeField] private AnimationCurve zoomEaseCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Tooltip("If true, also pulls in the camera's field of view while zoomed (requires a Camera component).")]
    [SerializeField] private bool affectFieldOfView = true;

    [Tooltip("Field of view to use at full zoom-in. Only used if affectFieldOfView is true.")]
    [SerializeField] private float zoomedFieldOfView = 45f;

    [Tooltip("If true, tilts the camera's X rotation (pitch) while zoomed in, then returns to normal when zoomed out.")]
    [SerializeField] private bool affectXRotation = true;

    [Tooltip("X rotation (pitch, in degrees) to reach at full zoom-in. Applied on top of the camera's base rotation.")]
    [SerializeField] private float zoomedXRotationDegrees = 20f;

    private Vector3 basePosition;
    private Quaternion baseRotation;
    private float timeOffset;

    private Camera cam;
    private float defaultFieldOfView;
    private float zoomProgress; // 0 = default, 1 = fully zoomed toward target — raw linear progress, driven by zoomInSpeed/zoomOutSpeed

    /// <summary>
    /// Current zoom state after easing: 0 = fully zoomed out (default/idle), 1 = fully zoomed in toward target.
    /// Other scripts (e.g. world-space popups) can read this to react to zoom level.
    /// </summary>
    public float ZoomBlend => zoomEaseCurve != null ? Mathf.Clamp01(zoomEaseCurve.Evaluate(zoomProgress)) : zoomProgress;

    private void Awake()
    {
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;

        // Randomize the starting phase so multiple cameras don't bounce in sync.
        timeOffset = Random.Range(0f, 100f);

        cam = GetComponent<Camera>();
        if (cam != null)
        {
            defaultFieldOfView = cam.fieldOfView;
        }
    }

    private void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float t = (useUnscaledTime ? Time.unscaledTime : Time.time) + timeOffset;

        // --- Idle bounce/sway/tilt (relative to whatever the base position currently is) ---
        float yOffset = Mathf.Sin(t * bounceSpeed * Mathf.PI * 2f) * bounceHeight;
        float xOffset = Mathf.Sin(t * swaySpeed * Mathf.PI * 2f) * swayAmount;
        float tiltZ = Mathf.Sin(t * tiltSpeed * Mathf.PI * 2f) * tiltAmount;

        // --- Zoom/follow toward target while W/A/S/D is held ---
        bool movementKeyHeld = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                                Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

        float targetBlend = movementKeyHeld ? 1f : 0f;
        float blendSpeed = movementKeyHeld ? zoomInSpeed : zoomOutSpeed;
        zoomProgress = Mathf.MoveTowards(zoomProgress, targetBlend, blendSpeed * dt);
        float easedBlend = ZoomBlend;

        Vector3 desiredPosition = basePosition + new Vector3(xOffset, yOffset, 0f);

        if (target != null && easedBlend > 0f)
        {
            Vector3 followPoint = Vector3.Lerp(basePosition, target.position, followAmount);
            desiredPosition = Vector3.Lerp(desiredPosition, followPoint, easedBlend);
        }

        transform.localPosition = desiredPosition;

        float pitchX = affectXRotation ? Mathf.Lerp(0f, zoomedXRotationDegrees, easedBlend) : 0f;
        transform.localRotation = baseRotation * Quaternion.Euler(pitchX, 0f, tiltZ);

        if (affectFieldOfView && cam != null)
        {
            cam.fieldOfView = Mathf.Lerp(defaultFieldOfView, zoomedFieldOfView, easedBlend);
        }
    }

    /// <summary>
    /// Call this if you move the camera elsewhere at runtime and want the
    /// bounce to recenter around the new position instead of the old Awake() one.
    /// </summary>
    public void ResetBaseTransform()
    {
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
    }
}