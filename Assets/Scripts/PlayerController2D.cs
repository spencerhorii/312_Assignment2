// using UnityEngine;
// [RequireComponent(typeof(Rigidbody2D))]
// public class PlayerController2D : MonoBehaviour
// {
//     public enum FacingDirection { Left, Right }

//     [Header("Movement")]
//     [Tooltip("Horizontal movement speed in units/second.")]
//     [SerializeField] private float moveSpeed = 6f;

//     [Header("Jump")]
//     [Tooltip("Upward force applied when jumping.")]
//     [SerializeField] private float jumpForce = 12f;
//     [Tooltip("Transform positioned at the player's feet, used to detect ground contact.")]
//     [SerializeField] private Transform groundCheck;
//     [Tooltip("Radius of the ground check overlap circle.")]
//     [SerializeField] private float groundCheckRadius = 0.15f;
//     [Tooltip("Which layers count as 'ground' for jump purposes.")]
//     [SerializeField] private LayerMask groundLayer;

//     [Header("Facing")]
//     [Tooltip("SpriteRenderer to flip based on movement direction. Defaults to facing Right.")]
//     [SerializeField] private SpriteRenderer spriteRenderer;

//     [Header("State")]
//     [Tooltip("When false (e.g. during dialogue), all movement input is ignored. " +
//              "Other systems (DialogueManager, MerchantUI, etc.) toggle this.")]
//     public bool CanMove = true;

//     /// <summary>Current facing direction. Read by other systems (dialogue UI, prompts, animation).</summary>
//     public FacingDirection CurrentFacing { get; private set; } = FacingDirection.Right;

//     private Rigidbody2D rb;
//     private float horizontalInput;
//     private bool jumpRequested;
//     private bool isGrounded;

//     private void Awake()
//     {
//         rb = GetComponent<Rigidbody2D>();

//         if (spriteRenderer == null)
//         {
//             spriteRenderer = GetComponentInChildren<SpriteRenderer>();
//         }

//         ApplyFacing(); // ensure sprite matches default facing (Right) on start
//     }

//     private void Update()
//     {
//         // Ground check happens every frame regardless of CanMove so state stays accurate.
//         isGrounded = groundCheck != null &&
//                      Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

//         if (!CanMove)
//         {
//             horizontalInput = 0f;
//             return;
//         }

//         horizontalInput = Input.GetAxisRaw("Horizontal"); // A/D and Left/Right by default

//         UpdateFacing(horizontalInput);

//         if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
//         {
//             jumpRequested = true;
//         }
//     }

//     private void FixedUpdate()
//     {
//         // Preserve current vertical velocity (gravity/falling), only drive horizontal.
//         rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

//         if (jumpRequested)
//         {
//             rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // reset vertical before applying jump
//             rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
//             jumpRequested = false;
//         }
//     }

//     /// <summary>
//     /// Updates CurrentFacing based on horizontal input. Only changes on non-zero input,
//     /// so the player keeps facing their last direction while idle (rather than resetting).
//     /// </summary>
//     private void UpdateFacing(float horizontal)
//     {
//         if (horizontal > 0f && CurrentFacing != FacingDirection.Right)
//         {
//             CurrentFacing = FacingDirection.Right;
//             ApplyFacing();
//         }
//         else if (horizontal < 0f && CurrentFacing != FacingDirection.Left)
//         {
//             CurrentFacing = FacingDirection.Left;
//             ApplyFacing();
//         }
//     }

//     /// <summary>Applies CurrentFacing to the sprite via flipX (Right = not flipped, Left = flipped).</summary>
//     private void ApplyFacing()
//     {
//         if (spriteRenderer != null)
//         {
//             spriteRenderer.flipX = CurrentFacing == FacingDirection.Left;
//         }
//     }

//     private void OnDrawGizmosSelected()
//     {
//         if (groundCheck == null) return;
//         Gizmos.color = Color.green;
//         Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
//     }
// }

using UnityEngine;

/// <summary>
/// Handles core 2D platforming movement for the player: horizontal movement (A/D)
/// and jumping (Spacebar). Also tracks and exposes which way the player is facing,
/// and owns the inventorySlots setting used to size the player's inventory.
///
/// This script intentionally only handles MOVEMENT + FACING + inventory sizing.
/// Interaction (E key), the inventory popup, and dialogue-freeze logic live in
/// separate scripts and communicate with this one via the public CanMove flag and
/// the static Instance reference, keeping responsibilities modular.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    public enum FacingDirection { Left, Right }

    /// <summary>
    /// Static reference to the active player, used by other systems (InventoryUI, DialogueManager,
    /// MerchantUI, etc.) that need to freeze/unfreeze movement or query facing direction without
    /// needing a manually-wired Inspector reference. Not a persistent DontDestroyOnLoad singleton -
    /// just a convenience lookup for the single player in the current scene.
    /// </summary>
    public static PlayerController2D Instance { get; private set; }

    [Header("Movement")]
    [Tooltip("Horizontal movement speed in units/second.")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Jump")]
    [Tooltip("Upward force applied when jumping.")]
    [SerializeField] private float jumpForce = 12f;
    [Tooltip("Transform positioned at the player's feet, used to detect ground contact.")]
    [SerializeField] private Transform groundCheck;
    [Tooltip("Radius of the ground check overlap circle.")]
    [SerializeField] private float groundCheckRadius = 0.15f;
    [Tooltip("Which layers count as 'ground' for jump purposes.")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Facing")]
    [Tooltip("SpriteRenderer to flip based on movement direction. Defaults to facing Right.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Inventory")]
    [Tooltip("Number of inventory slots this player has. Drives the size of the inventory UI. " +
             "Change this to add more slots later - the UI rebuilds itself to match.")]
    [SerializeField] private int inventorySlots = 4;

    [Header("State")]
    [Tooltip("When false (e.g. during dialogue or while the inventory is open), all movement " +
             "input is ignored. Other systems toggle this.")]
    public bool CanMove = true;

    /// <summary>Current facing direction. Read by other systems (dialogue UI, prompts, animation).</summary>
    public FacingDirection CurrentFacing { get; private set; } = FacingDirection.Right;

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool jumpRequested;
    private bool isGrounded;

    private void Awake()
    {
        Instance = this;

        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        ApplyFacing(); // ensure sprite matches default facing (Right) on start
    }

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.Initialize(inventorySlots);
        }
        else
        {
            Debug.LogWarning("PlayerController2D: InventoryManager.Instance is null. " +
                              "Make sure InventoryManager exists in the scene.");
        }
    }

    private void Update()
    {
        // Ground check happens every frame regardless of CanMove so state stays accurate.
        isGrounded = groundCheck != null &&
                     Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (!CanMove)
        {
            horizontalInput = 0f;
            return;
        }

        horizontalInput = GetHorizontalInput(); // A/D only - arrow keys are reserved for UI navigation

        UpdateFacing(horizontalInput);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        // Preserve current vertical velocity (gravity/falling), only drive horizontal.
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // reset vertical before applying jump
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpRequested = false;
        }
    }

    /// <summary>
    /// Reads horizontal movement input from A/D only. Deliberately not using
    /// Input.GetAxisRaw("Horizontal"), since that axis is bound to both A/D and the
    /// Left/Right arrow keys by default in Unity's Input Manager - arrow keys are
    /// reserved exclusively for UI navigation (inventory, dialogue choices, etc).
    /// </summary>
    private float GetHorizontalInput()
    {
        float value = 0f;
        if (Input.GetKey(KeyCode.A)) value -= 1f;
        if (Input.GetKey(KeyCode.D)) value += 1f;
        return value;
    }

    /// <summary>
    /// Updates CurrentFacing based on horizontal input. Only changes on non-zero input,
    /// so the player keeps facing their last direction while idle (rather than resetting).
    /// </summary>
    private void UpdateFacing(float horizontal)
    {
        if (horizontal > 0f && CurrentFacing != FacingDirection.Right)
        {
            CurrentFacing = FacingDirection.Right;
            ApplyFacing();
        }
        else if (horizontal < 0f && CurrentFacing != FacingDirection.Left)
        {
            CurrentFacing = FacingDirection.Left;
            ApplyFacing();
        }
    }

    /// <summary>Applies CurrentFacing to the sprite via flipX (Right = not flipped, Left = flipped).</summary>
    private void ApplyFacing()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = CurrentFacing == FacingDirection.Left;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}