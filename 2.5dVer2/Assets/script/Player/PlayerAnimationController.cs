using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;
    [Header("Animation Settings")]
    [SerializeField] private float walkSpeed = 0.5f;
    [SerializeField] private float runSpeed = 1f;
    [SerializeField] private float speedSmoothTime = 0.12f;
    [SerializeField] private float landingMinFallSpeed = 1.5f;

    private Controller playerController;
    private CharacterController characterController;
    private Jetpack jetpack;
    private Sprint sprint;
    private TPSAim tpsAim;
    private bool wasGrounded;
    private float previousVerticalSpeed;
    private bool isDead;

    private void Awake()
    {
        playerController = GetComponent<Controller>();
        characterController = GetComponent<CharacterController>();
        jetpack = GetComponent<Jetpack>();
        sprint = GetComponent<Sprint>();
        tpsAim = GetComponent<TPSAim>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;
    }

    private void Start()
    {
        wasGrounded = characterController.isGrounded;
        previousVerticalSpeed = jetpack != null ? jetpack.VerticalVelocity : 0f;
    }

    private void LateUpdate()
    {
        if (animator == null || isDead) return;

        bool moving = playerController != null && playerController.MoveDirection.sqrMagnitude > 0.01f;
        bool boosting = sprint != null && sprint.IsSprinting;
        float speed = moving ? (boosting ? runSpeed : walkSpeed) : 0f;
        animator.SetFloat("Speed", speed, speedSmoothTime, Time.deltaTime);
        animator.SetBool("IsBoost", boosting);
        if (tpsAim != null) animator.SetBool("IsAim", tpsAim.IsAiming);

        bool grounded = characterController.isGrounded;
        float verticalSpeed = jetpack != null ? jetpack.VerticalVelocity : 0f;
        if (jetpack != null)
        {
            animator.SetBool("IsJet", jetpack.IsJetting);
            animator.SetBool("IsGrounded", grounded);
            animator.SetFloat("VerticalSpeed", verticalSpeed);
        }
        // Зөвхөн агаараас хангалттай хурдтай буухад Land тоглоно.
        if (!wasGrounded && grounded && previousVerticalSpeed < -landingMinFallSpeed)
            animator.SetTrigger("Land");
        wasGrounded = grounded;
        previousVerticalSpeed = verticalSpeed;
    }

    public void PlayShootAnimation()
    {
        if (animator != null && !isDead) animator.SetTrigger("Shoot");
    }

    public void PlayHitAnimation()
    {
        if (animator != null && !isDead) animator.SetTrigger("Hit");
    }

    public void PlayDeathAnimation()
    {
        if (isDead) return;
        isDead = true;
        if (animator == null) return;
        animator.ResetTrigger("Shoot");
        animator.ResetTrigger("Hit");
        animator.ResetTrigger("Land");
        animator.SetBool("IsBoost", false);
        animator.SetBool("IsJet", false);
        animator.SetBool("IsDead", true);
    }
}
