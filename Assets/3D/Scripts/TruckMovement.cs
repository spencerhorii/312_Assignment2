using UnityEngine;
using UnityEngine.InputSystem;

public class TruckMovement : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 40f;

    [SerializeField] private float turnSpeed = 120f;

    [Header("Traction")]
    [SerializeField] private float tractionGrip = 10f; // higher = less sideways sliding (snappier grip)
    [Range(0f, 1f)]
    [SerializeField] private float driftFactor = 0.15f; // 0 = no drift, 1 = ice

    private Rigidbody rb;
    private Vector2 moveInput;

    private bool moving;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        moving = false;
    }

    void FixedUpdate()
    {
        float throttle = moveInput.y;
        float steer = moveInput.x;

        // --- Forward/backward movement ---
        Vector3 forwardDir = transform.forward;
        Vector3 targetVelocity = forwardDir * throttle * maxSpeed;
        targetVelocity.y = rb.linearVelocity.y;

        float rate = Mathf.Abs(throttle) > 0.01f ? acceleration : deceleration;

        rb.linearVelocity = Vector3.MoveTowards(
            rb.linearVelocity,
            targetVelocity,
            rate * Time.fixedDeltaTime
        );

        // --- Traction: kill sideways sliding so the car "grips" the road ---
        Vector3 rightDir = transform.right;
        Vector3 currentVel = rb.linearVelocity;

        float lateralSpeed = Vector3.Dot(currentVel, rightDir);
        Vector3 lateralVelocity = rightDir * lateralSpeed;

        // Reduce lateral velocity based on grip (driftFactor lets some slide through)
        Vector3 lateralCorrection = -lateralVelocity * (1f - driftFactor) * tractionGrip * Time.fixedDeltaTime;
        rb.linearVelocity += lateralCorrection;

        // --- Steering / rotation ---
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speedFactor = Mathf.Clamp01(flatVel.magnitude / maxSpeed);
        float moveDirection = Vector3.Dot(flatVel, forwardDir) < 0f ? -1f : 1f;

        float turnAmount = steer * turnSpeed * speedFactor * moveDirection * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnAmount, 0f));
        if(moving)
        {
            foreach (Transform childTransform in this.transform){
                Animator anim = childTransform.GetComponent<Animator>();
                anim.SetBool("isDriving", true);
            }
        }
        else
        {
            foreach (Transform childTransform in this.transform){
                Animator anim = childTransform.GetComponent<Animator>();
                anim.SetBool("isDriving", false);
            }

        }

        if(throttle == 0)
        {
            moving = false;
        }
    }


    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        moving = true;
    }
}