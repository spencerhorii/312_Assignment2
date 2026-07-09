using UnityEngine;
using UnityEngine.InputSystem;

public class Driving : MonoBehaviour
{
    [SerializeField] private float maxSpeed;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 40f;   // much higher than acceleration = snappier stop

    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // Clamp so diagonal input doesn't exceed magnitude 1
        Vector2 clampedInput = Vector2.ClampMagnitude(moveInput, 1f);
        Vector2 targetVelocity = clampedInput * maxSpeed;

        float rate = clampedInput.sqrMagnitude > 0.01f ? acceleration : deceleration;

        // rb.linearVelocity = Vector2.MoveTowards(
        //     rb.linearVelocity,
        //     targetVelocity,
        //     rate * Time.fixedDeltaTime
        // );

        if (clampedInput.sqrMagnitude > 0.01f)
  {
      rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
  }
  else
  {
      rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
  }
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
};