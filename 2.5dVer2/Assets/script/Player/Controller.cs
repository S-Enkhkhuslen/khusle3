using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Controller : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float speed = 5f;

    [Header("Rotation")]
    [SerializeField] private bool shouldFaceMoveDirection = true;
    [SerializeField] private float rotationSpeed = 10f;

    private CharacterController characterController;
    private Jetpack jetpack;
    private PlayerDash playerDash;
    private TPSAim tpsAim;

    private Vector2 moveInput;
    private Vector3 moveDirection;

    // Dash script хөдөлгөөний чиглэлийг авахад хэрэглэнэ
    public Vector3 MoveDirection => moveDirection;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        jetpack = GetComponent<Jetpack>();
        playerDash = GetComponent<PlayerDash>();
        tpsAim = GetComponent<TPSAim>();
    }

    // PlayerInput Behavior = Send Messages үед InputValue ашиглана
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void Update()
    {
        CalculateMoveDirection();
        MovePlayer();
        RotatePlayer();
    }

    private void CalculateMoveDirection()
    {
        if (cameraTransform == null)
        {
            Debug.LogError(
                "Controller дээр Camera Transform оруулаагүй байна!"
            );

            moveDirection = Vector3.zero;
            return;
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        // Камер дээш, доош харсан байсан ч
        // Player газар дээгүүр хөдөлнө
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        moveDirection =
            cameraForward * moveInput.y +
            cameraRight * moveInput.x;

        // Диагоналиар явахад хурд ихсэхээс хамгаална
        moveDirection =
            Vector3.ClampMagnitude(moveDirection, 1f);
    }

    private void MovePlayer()
    {
        Vector3 finalVelocity = moveDirection * speed;

        // Jetpack болон gravity-ийн босоо хурд
        if (jetpack != null)
        {
            finalVelocity.y = jetpack.VerticalVelocity;
        }

        // Dash хийж байгаа үед X, Z хөдөлгөөнийг
        // Dash-ийн хурд, чиглэлээр солино
        if (playerDash != null && playerDash.IsDashing)
        {
            finalVelocity.x = playerDash.DashVelocity.x;
            finalVelocity.z = playerDash.DashVelocity.z;
        }

        characterController.Move(
            finalVelocity * Time.deltaTime
        );
    }

    private void RotatePlayer()
    {
        // Aim хийж байгаа үед TPSAim script
        // Player-ийг камерын чиглэл рүү эргүүлнэ
        if (tpsAim != null && tpsAim.IsAiming)
        {
            return;
        }

        // Aim хийхгүй үед хөдөлсөн чиглэл рүү эргэнэ
        if (!shouldFaceMoveDirection)
        {
            return;
        }

        if (moveDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                moveDirection,
                Vector3.up
            );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}