
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerAnimationController : MonoBehaviour
{
    // =====================================================
    // ANIMATION REFERENCES
    // =====================================================

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Animation Settings")]

    // Walk animation-д өгөх Speed
    [SerializeField] private float walkSpeed = 0.5f;

    // JetBoost идэвхтэй үед өгөх Speed
    [SerializeField] private float runSpeed = 1f;

    // Idle / Walk шилжилтийг зөөлөн болгоно
    [SerializeField] private float speedSmoothTime = 0.12f;

    // Газардалт гэж тооцох хамгийн бага унах хурд
    [SerializeField] private float landingMinFallSpeed = 1.5f;


    // =====================================================
    // REFERENCES
    // =====================================================

    private Controller playerController;
    private CharacterController characterController;
    private Jetpack jetpack;
    private Sprint sprint;
    private TPSAim tpsAim;

    // Өмнөх frame дээр газар байсан эсэх
    private bool wasGrounded;

    // Өмнөх frame-ийн босоо хурд
    private float previousVerticalSpeed;

    // Үхсэн эсэх
    private bool isDead;


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        playerController = GetComponent<Controller>();

        characterController =
            GetComponent<CharacterController>();

        jetpack = GetComponent<Jetpack>();

        sprint = GetComponent<Sprint>();

        tpsAim = GetComponent<TPSAim>();

        // Animator-ийг Inspector дээр
        // холбоогүй бол Child model-оос хайна
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            // Player-ийн байрлалыг Controller.cs удирдана
            animator.applyRootMotion = false;
        }
    }


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        wasGrounded = characterController.isGrounded;

        previousVerticalSpeed =
            jetpack != null ? jetpack.VerticalVelocity : 0f;
    }


    // =====================================================
    // LATE UPDATE
    // =====================================================

    private void LateUpdate()
    {
        if (animator == null || isDead)
            return;

        UpdateMovementAnimation();

        UpdateAimAnimation();

        UpdateJetAnimation();

        UpdateLandingAnimation();
    }


    // =====================================================
    // IDLE / WALK / RUN
    // =====================================================

    private void UpdateMovementAnimation()
    {
        if (playerController == null)
            return;

        bool isMoving =
            playerController.MoveDirection.sqrMagnitude > 0.01f;

        bool isBoosting =
            sprint != null && sprint.IsSprinting;

        float targetSpeed = 0f;

        if (isMoving)
        {
            targetSpeed = isBoosting
                ? runSpeed
                : walkSpeed;
        }

        // Blend Tree-ийн Speed параметр
        animator.SetFloat(
            "Speed",
            targetSpeed,
            speedSmoothTime,
            Time.deltaTime
        );

        // Shift / JetBoost animation
        // Газар болон агаарт ажиллах боломжтой
        animator.SetBool("IsBoost", isBoosting);
    }


    // =====================================================
    // AIM
    // =====================================================

    private void UpdateAimAnimation()
    {
        if (tpsAim == null)
            return;

        // Mouse Right Button барьж байгаа эсэх
        animator.SetBool(
            "IsAim",
            tpsAim.IsAiming
        );
    }


    // =====================================================
    // JETPACK / DESCEND
    // =====================================================

    private void UpdateJetAnimation()
    {
        if (jetpack == null)
            return;

        bool isGrounded =
            characterController.isGrounded;

        bool isJetpacking = jetpack.IsJetting;

        float verticalSpeed =
            jetpack.VerticalVelocity;

        // Jetpack ашиглаж байгаа эсэх
        animator.SetBool(
            "IsJet",
            isJetpacking
        );

        // Газарт байгаа эсэх
        animator.SetBool(
            "IsGrounded",
            isGrounded
        );

        // Дээш / доош хөдөлгөөний хурд
        animator.SetFloat(
            "VerticalSpeed",
            verticalSpeed
        );
    }


    // =====================================================
    // LANDING
    // =====================================================

    private void UpdateLandingAnimation()
    {
        bool isGrounded =
            characterController.isGrounded;

        float currentVerticalSpeed =
            jetpack != null
                ? jetpack.VerticalVelocity
                : 0f;

        // Өмнө нь агаарт байсан,
        // одоо газарт хүрсэн эсэх
        bool justLanded =
            !wasGrounded && isGrounded;

        // Жижиг шатнаас буухад
        // Land animation ажиллуулахгүй
        bool wasFallingFast =
            previousVerticalSpeed < -landingMinFallSpeed;

        if (justLanded && wasFallingFast)
        {
            animator.SetTrigger("Land");
        }

        wasGrounded = isGrounded;

        previousVerticalSpeed = currentVerticalSpeed;
    }


    // =====================================================
    // SHOOT
    // =====================================================

    public void PlayShootAnimation()
    {
        if (animator == null || isDead)
            return;

        animator.SetTrigger("Shoot");
    }


    // =====================================================
    // HIT
    // =====================================================

    public void PlayHitAnimation()
    {
        if (animator == null || isDead)
            return;

        animator.SetTrigger("Hit");
    }


    // =====================================================
    // DEATH
    // =====================================================

    public void PlayDeathAnimation()
    {
        if (isDead)
            return;

        isDead = true;

        if (animator == null)
            return;

        animator.ResetTrigger("Shoot");
        animator.ResetTrigger("Hit");
        animator.ResetTrigger("Land");

        animator.SetBool("IsBoost", false);
        animator.SetBool("IsJet", false);
        animator.SetBool("IsDead", true);
    }
}
