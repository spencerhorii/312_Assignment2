// using UnityEngine;

// /// <summary>
// /// Handles core 2D platforming movement for the player: horizontal movement (A/D)
// /// and jumping (Spacebar). Also tracks and exposes which way the player is facing,
// /// and owns the inventorySlots setting used to size the player's inventory.
// ///
// /// This script intentionally only handles MOVEMENT + FACING + inventory sizing.
// /// Interaction (E key), the inventory popup, and dialogue live in separate scripts and
// /// communicate with this one via the public CanMove flag, the static Instance reference,
// /// and SetFacing() (used by DialogueManager to turn the player towards an NPC).
// /// </summary>
// [RequireComponent(typeof(Rigidbody2D))]
// public class PlayerController2D : MonoBehaviour
// {
//     public enum FacingDirection { Left, Right }

//     /// <summary>
//     /// Static reference to the active player, used by other systems (InventoryUI, DialogueManager,
//     /// MerchantUI, etc.) that need to freeze/unfreeze movement or query/set facing direction without
//     /// needing a manually-wired Inspector reference. Not a persistent DontDestroyOnLoad singleton -
//     /// just a convenience lookup for the single player in the current scene.
//     /// </summary>
//     public static PlayerController2D Instance { get; private set; }

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

//     [Header("Inventory")]
//     [Tooltip("Number of inventory slots this player has. Drives the size of the inventory UI. " +
//              "Change this to add more slots later - the UI rebuilds itself to match.")]
//     [SerializeField] private int inventorySlots = 4;

//     [Header("State")]
//     [Tooltip("When false (e.g. during dialogue or while the inventory is open), all movement " +
//              "input is ignored. Other systems toggle this.")]
//     public bool CanMove = true;

//     /// <summary>Current facing direction. Read by other systems (dialogue UI, prompts, animation).</summary>
//     public FacingDirection CurrentFacing { get; private set; } = FacingDirection.Right;

//     private Rigidbody2D rb;
//     private float horizontalInput;
//     private bool jumpRequested;
//     private bool isGrounded;

//     private void Awake()
//     {
//         Instance = this;

//         rb = GetComponent<Rigidbody2D>();

//         if (spriteRenderer == null)
//         {
//             spriteRenderer = GetComponentInChildren<SpriteRenderer>();
//         }

//         ApplyFacing(); // ensure sprite matches default facing (Right) on start
//     }

//     private void Start()
//     {
//         if (InventoryManager.Instance != null)
//         {
//             InventoryManager.Instance.Initialize(inventorySlots);
//         }
//         else
//         {
//             Debug.LogWarning("PlayerController2D: InventoryManager.Instance is null. " +
//                               "Make sure InventoryManager exists in the scene.");
//         }
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

//         horizontalInput = GetHorizontalInput(); // A/D only - arrow keys are reserved for UI navigation

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
//     /// Reads horizontal movement input from A/D only. Deliberately not using
//     /// Input.GetAxisRaw("Horizontal"), since that axis is bound to both A/D and the
//     /// Left/Right arrow keys by default in Unity's Input Manager - arrow keys are
//     /// reserved exclusively for UI navigation (inventory, dialogue choices, etc).
//     /// </summary>
//     private float GetHorizontalInput()
//     {
//         float value = 0f;
//         if (Input.GetKey(KeyCode.A)) value -= 1f;
//         if (Input.GetKey(KeyCode.D)) value += 1f;
//         return value;
//     }

//     /// <summary>
//     /// Updates CurrentFacing based on horizontal input. Only changes on non-zero input,
//     /// so the player keeps facing their last direction while idle (rather than resetting).
//     /// </summary>
//     private void UpdateFacing(float horizontal)
//     {
//         if (horizontal > 0f && CurrentFacing != FacingDirection.Right)
//         {
//             SetFacing(FacingDirection.Right);
//         }
//         else if (horizontal < 0f && CurrentFacing != FacingDirection.Left)
//         {
//             SetFacing(FacingDirection.Left);
//         }
//     }

//     /// <summary>
//     /// Forces the player to face a specific direction. Used internally by movement input,
//     /// and externally by DialogueManager to turn the player towards an NPC when a
//     /// conversation starts.
//     /// </summary>
//     public void SetFacing(FacingDirection direction)
//     {
//         CurrentFacing = direction;
//         ApplyFacing();
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









































// using UnityEngine;

// /// <summary>
// /// Handles core 2D platforming movement for the player: horizontal movement (A/D)
// /// and jumping (Spacebar). Also tracks and exposes which way the player is facing,
// /// and owns the inventorySlots setting used to size the player's inventory.
// ///
// /// This script intentionally only handles MOVEMENT + FACING + inventory sizing.
// /// Interaction (E key), the inventory popup, and dialogue live in separate scripts and
// /// communicate with this one via the public CanMove flag, the static Instance reference,
// /// and SetFacing() (used by DialogueManager to turn the player towards an NPC).
// /// </summary>
// [RequireComponent(typeof(Rigidbody2D))]
// public class PlayerController2D : MonoBehaviour
// {
//     public enum FacingDirection { Left, Right }

//     /// <summary>
//     /// Static reference to the active player, used by other systems (InventoryUI, DialogueManager,
//     /// MerchantUI, etc.) that need to freeze/unfreeze movement or query/set facing direction without
//     /// needing a manually-wired Inspector reference. Not a persistent DontDestroyOnLoad singleton -
//     /// just a convenience lookup for the single player in the current scene.
//     /// </summary>
//     public static PlayerController2D Instance { get; private set; }

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

//     [Header("Inventory")]
//     [Tooltip("Number of inventory slots this player has. Drives the size of the inventory UI. " +
//              "Change this to add more slots later - the UI rebuilds itself to match.")]
//     [SerializeField] private int inventorySlots = 4;

//     [Header("State")]
//     [Tooltip("When false (e.g. during dialogue or while the inventory is open), all movement " +
//              "input is ignored. Other systems toggle this.")]
//     public bool CanMove = true;

//     /// <summary>Current facing direction. Read by other systems (dialogue UI, prompts, animation).</summary>
//     public FacingDirection CurrentFacing { get; private set; } = FacingDirection.Right;


//     /// <summary>True while standing on the ground.</summary>
//     public bool IsGrounded => isGrounded;

//     /// <summary>Current horizontal input (-1 to 1).</summary>
//     public float HorizontalInput => horizontalInput;

//     // /// <summary>Current vertical velocity.</summary>
//     // public float VerticalVelocity => rb != null ? rb.linearVelocity.y : 0f;

//     private Rigidbody2D rb;
//     public float horizontalInput;
//     public bool jumpRequested;
//     public bool isGrounded;
    
//     public float VerticalVelocity => rb != null ? rb.linearVelocity.y : 0f;

//     private void Awake()
//     {
//         Instance = this;

//         rb = GetComponent<Rigidbody2D>();

//         if (spriteRenderer == null)
//         {
//             spriteRenderer = GetComponentInChildren<SpriteRenderer>();
//         }

//         ApplyFacing(); // ensure sprite matches default facing (Right) on start
//     }

//     private void Start()
//     {
//         if (InventoryManager.Instance != null)
//         {
//             InventoryManager.Instance.Initialize(inventorySlots);
//         }
//         else
//         {
//             Debug.LogWarning("PlayerController2D: InventoryManager.Instance is null. " +
//                               "Make sure InventoryManager exists in the scene.");
//         }
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

//         horizontalInput = GetHorizontalInput(); // A/D only - arrow keys are reserved for UI navigation

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
//     /// Reads horizontal movement input from A/D only. Deliberately not using
//     /// Input.GetAxisRaw("Horizontal"), since that axis is bound to both A/D and the
//     /// Left/Right arrow keys by default in Unity's Input Manager - arrow keys are
//     /// reserved exclusively for UI navigation (inventory, dialogue choices, etc).
//     /// </summary>
//     private float GetHorizontalInput()
//     {
//         float value = 0f;
//         if (Input.GetKey(KeyCode.A)) value -= 1f;
//         if (Input.GetKey(KeyCode.D)) value += 1f;
//         return value;
//     }

//     /// <summary>
//     /// Updates CurrentFacing based on horizontal input. Only changes on non-zero input,
//     /// so the player keeps facing their last direction while idle (rather than resetting).
//     /// </summary>
//     private void UpdateFacing(float horizontal)
//     {
//         if (horizontal > 0f && CurrentFacing != FacingDirection.Right)
//         {
//             SetFacing(FacingDirection.Right);
//         }
//         else if (horizontal < 0f && CurrentFacing != FacingDirection.Left)
//         {
//             SetFacing(FacingDirection.Left);
//         }
//     }

//     /// <summary>
//     /// Forces the player to face a specific direction. Used internally by movement input,
//     /// and externally by DialogueManager to turn the player towards an NPC when a
//     /// conversation starts.
//     /// </summary>
//     public void SetFacing(FacingDirection direction)
//     {
//         CurrentFacing = direction;
//         ApplyFacing();
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
/// Interaction (E key), the inventory popup, and dialogue live in separate scripts and
/// communicate with this one via the public CanMove flag, the static Instance reference,
/// and SetFacing() (used by DialogueManager to turn the player towards an NPC).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    public enum FacingDirection { Left, Right }

    /// <summary>
    /// Static reference to the active player, used by other systems (InventoryUI, DialogueManager,
    /// MerchantUI, etc.) that need to freeze/unfreeze movement or query/set facing direction without
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
    [Tooltip("Number of inventory slots this player has. Drives the size of the inventory UI.")]
    [SerializeField] private int inventorySlots = 4;

    [Header("Audio")]
    [Tooltip("AudioSource used for the looping footstep sound.")]
    [SerializeField] private AudioSource walkAudioSource;

    [Tooltip("Looping footstep/walking clip.")]
    [SerializeField] private AudioClip walkClip;

    [Tooltip("Volume the walking sound fades up to while moving on the ground.")]
    [SerializeField] private float walkVolume = 0.6f;

    [Tooltip("How quickly the walk sound fades in/out (volume units per second).")]
    [SerializeField] private float walkVolumeLerpSpeed = 6f;

    [Tooltip("One-shot sound played when the player jumps.")]
    [SerializeField] private AudioClip jumpClip;

    [Tooltip("Volume for the jump sound.")]
    [SerializeField] private float jumpVolume = 0.8f;

    [Header("State")]
    [Tooltip("When false (e.g. during dialogue or while the inventory is open), all movement input is ignored.")]
    public bool CanMove = true;

    /// <summary>Current facing direction.</summary>
    public FacingDirection CurrentFacing { get; private set; } = FacingDirection.Right;

    // ===========================================================
    // Read-only properties exposed for PlayerAnimator.
    // ===========================================================

    /// <summary>True while standing on the ground.</summary>
    public bool IsGrounded => isGrounded;

    /// <summary>Current horizontal movement input (-1 to 1).</summary>
    public float HorizontalInput => horizontalInput;

    /// <summary>Current vertical velocity.</summary>
    public float VerticalVelocity => rb != null ? rb.linearVelocity.y : 0f;

    // ===========================================================

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
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        ApplyFacing();

        if (walkAudioSource != null)
        {
            walkAudioSource.clip = walkClip;
            walkAudioSource.loop = true;
            walkAudioSource.volume = 0f;
        }
    }

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.Initialize(inventorySlots);
        }
        else
        {
            Debug.LogWarning(
                "PlayerController2D: InventoryManager.Instance is null. " +
                "Make sure InventoryManager exists in the scene.");
        }
    }

    private void Update()
    {
        // Always update grounded state.
        isGrounded =
            groundCheck != null &&
            Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer);

        if (!CanMove)
        {
            horizontalInput = 0f;
            UpdateWalkAudio();
            return;
        }

        horizontalInput = GetHorizontalInput();

        UpdateFacing(horizontalInput);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            jumpRequested = true;
            PlayJumpSound();
        }

        UpdateWalkAudio();
    }

    private void FixedUpdate()
    {
        // Preserve vertical velocity while controlling horizontal movement.
        rb.linearVelocity = new Vector2(
            horizontalInput * moveSpeed,
            rb.linearVelocity.y);

        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                0f);

            rb.AddForce(
                Vector2.up * jumpForce,
                ForceMode2D.Impulse);

            jumpRequested = false;
        }
    }

    /// <summary>
    /// Reads A/D only. Arrow keys remain reserved for UI.
    /// </summary>
    private float GetHorizontalInput()
    {
        float value = 0f;

        if (Input.GetKey(KeyCode.A))
            value -= 1f;

        if (Input.GetKey(KeyCode.D))
            value += 1f;

        return value;
    }

    /// <summary>
    /// Updates facing direction based on movement input.
    /// </summary>
    private void UpdateFacing(float horizontal)
    {
        if (horizontal > 0f && CurrentFacing != FacingDirection.Right)
        {
            SetFacing(FacingDirection.Right);
        }
        else if (horizontal < 0f && CurrentFacing != FacingDirection.Left)
        {
            SetFacing(FacingDirection.Left);
        }
    }

    /// <summary>
    /// Forces the player to face a particular direction.
    /// </summary>
    public void SetFacing(FacingDirection direction)
    {
        CurrentFacing = direction;
        ApplyFacing();
    }

    /// <summary>
    /// Applies the current facing direction to the sprite.
    /// </summary>
    private void ApplyFacing()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = CurrentFacing == FacingDirection.Left;
        }
    }

    /// <summary>
    /// Fades the looping footstep sound in while walking on the ground, and out otherwise.
    /// </summary>
    private void UpdateWalkAudio()
    {
        if (walkAudioSource == null) return;

        bool isWalking = isGrounded && Mathf.Abs(horizontalInput) > 0.01f;
        float targetVolume = isWalking ? walkVolume : 0f;

        if (isWalking && !walkAudioSource.isPlaying)
        {
            walkAudioSource.Play();
        }

        walkAudioSource.volume = Mathf.MoveTowards(
            walkAudioSource.volume,
            targetVolume,
            walkVolumeLerpSpeed * Time.deltaTime);

        if (!isWalking && walkAudioSource.volume <= 0f && walkAudioSource.isPlaying)
        {
            walkAudioSource.Stop();
        }
    }

    /// <summary>
    /// Plays a one-shot jump sound via SoundFXManager, if both are available.
    /// </summary>
    private void PlayJumpSound()
    {
        if (jumpClip == null) return;

        if (SoundFXManager.instance == null)
        {
            Debug.LogWarning("[PlayerController2D] SoundFXManager.instance is null.", this);
            return;
        }

        SoundFXManager.instance.PlaySoundFXClip(jumpClip, transform, jumpVolume);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius);
    }
}