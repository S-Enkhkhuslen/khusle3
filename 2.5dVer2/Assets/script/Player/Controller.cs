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
    private Sprint sprint;
    private TPSAim tpsAim;

    private Vector2 moveInput;
    private Vector3 moveDirection;

    public Vector3 MoveDirection => moveDirection;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        jetpack = GetComponent<Jetpack>();
        sprint = GetComponent<Sprint>();
        tpsAim = GetComponent<TPSAim>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
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
            moveDirection = Vector3.zero;
            return;
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        moveDirection =
            cameraForward * moveInput.y +
            cameraRight * moveInput.x;

        moveDirection =
            Vector3.ClampMagnitude(moveDirection, 1f);
    }

    private void MovePlayer()
    {
        float currentSpeed = speed;

        if (sprint != null)
        {
            currentSpeed *= sprint.SpeedMultiplier;
        }

        Vector3 finalVelocity =
            moveDirection * currentSpeed;

        if (jetpack != null)
        {
            finalVelocity.y =
                jetpack.VerticalVelocity;
        }

        characterController.Move(
            finalVelocity * Time.deltaTime
        );
    }

    private void RotatePlayer()
    {
        if (tpsAim != null && tpsAim.IsAiming)
        {
            return;
        }

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