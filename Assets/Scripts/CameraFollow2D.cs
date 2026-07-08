using UnityEngine;

/// <summary>
/// Smoothly follows a target transform (typically the Player) using SmoothDamp,
/// so the camera eases toward the target instead of snapping instantly.
/// Attach this to the Main Camera.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The transform the camera should follow (drag in the Player).")]
    [SerializeField] private Transform target;

    [Header("Follow Behaviour")]
    [Tooltip("Approximate time (seconds) for the camera to catch up to the target. " +
             "Lower = snappier, Higher = more lag/easing.")]
    [SerializeField] private float smoothTime = 0.15f;

    [Tooltip("Constant offset from the target, e.g. (0, 0, -10) to keep the camera in front in 2D.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    [Tooltip("Optional clamp on max camera movement speed. Set to a large value to effectively disable.")]
    [SerializeField] private float maxSpeed = Mathf.Infinity;

    private Vector3 currentVelocity; // used internally by SmoothDamp, not manually set

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            smoothTime,
            maxSpeed
        );
    }

    /// <summary>
    /// Allows other systems (e.g. a teleport gate) to reassign the follow target at runtime,
    /// which is useful once multiple rooms/scenes are involved.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// Snaps the camera instantly to the target with no easing. Useful right after
    /// a teleport/scene transition so the camera doesn't visibly "catch up" from
    /// the old room's position.
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = target.position + offset;
        currentVelocity = Vector3.zero;
    }
}
