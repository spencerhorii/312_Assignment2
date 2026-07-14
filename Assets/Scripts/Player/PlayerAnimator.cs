using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimator : MonoBehaviour
{
    private enum AnimationState
    {
        Idle,
        Walk,
        Fall
    }

    [Header("References")]
    [SerializeField] private PlayerController2D controller;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Idle")]
    [SerializeField] private Sprite idleFrame1;
    [SerializeField] private Sprite idleFrame2;
    [SerializeField] private float idleFrameRate = 2f;

    [Header("Walk")]
    [SerializeField] private Sprite[] walkFrames = new Sprite[4];
    [SerializeField] private float walkFrameRate = 8f;

    [Header("Fall")]
    [SerializeField] private Sprite fallingSprite;
    [SerializeField] private float fallDelay = 0.25f;

    private AnimationState currentState;

    private float animationTimer;
    private float fallTimer;

    private int frameIndex;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<PlayerController2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        UpdateState();
        UpdateAnimation();
    }

    private void UpdateState()
    {
        bool walking =
            controller.CanMove &&
            controller.IsGrounded &&
            Mathf.Abs(controller.HorizontalInput) > 0.01f;

        bool falling =
            !controller.IsGrounded &&
            controller.VerticalVelocity < 0f;

        if (falling)
            fallTimer += Time.deltaTime;
        else
            fallTimer = 0f;

        if (fallTimer >= fallDelay)
        {
            SetState(AnimationState.Fall);
        }
        else if (walking)
        {
            SetState(AnimationState.Walk);
        }
        else
        {
            SetState(AnimationState.Idle);
        }
    }

    private void SetState(AnimationState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        animationTimer = 0f;
        frameIndex = 0;

        switch (currentState)
        {
            case AnimationState.Idle:
                spriteRenderer.sprite = idleFrame1;
                break;

            case AnimationState.Walk:
                if (walkFrames.Length > 0)
                    spriteRenderer.sprite = walkFrames[0];
                break;

            case AnimationState.Fall:
                spriteRenderer.sprite = fallingSprite;
                break;
        }
    }

    private void UpdateAnimation()
    {
        switch (currentState)
        {
            case AnimationState.Idle:

                animationTimer += Time.deltaTime;

                if (animationTimer >= 1f / idleFrameRate)
                {
                    animationTimer = 0f;

                    frameIndex = 1 - frameIndex;

                    spriteRenderer.sprite =
                        frameIndex == 0
                        ? idleFrame1
                        : idleFrame2;
                }

                break;

            case AnimationState.Walk:

                animationTimer += Time.deltaTime;

                if (animationTimer >= 1f / walkFrameRate)
                {
                    animationTimer = 0f;

                    frameIndex++;

                    if (frameIndex >= walkFrames.Length)
                        frameIndex = 0;

                    spriteRenderer.sprite = walkFrames[frameIndex];
                }

                break;

            case AnimationState.Fall:
                spriteRenderer.sprite = fallingSprite;
                break;
        }
    }
}
