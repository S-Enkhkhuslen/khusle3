using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Controller : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float speed = 5f;
    [SerializeField] private bool shouldFaceMoveDirection = false;
    [SerializeField] private float rotationSpeed = 10f;

    private CharacterController characterController;
    private Jetpack jetpack;
    private PlayerDash dash;

    private Vector2 moveInput;
    private Vector3 moveDirection;

    public Vector3 MoveDirection => moveDirection;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        jetpack = GetComponent<Jetpack>();
        dash = GetComponent<PlayerDash>();
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void Update()
    {
        CalculateMoveDirection();

        Vector3 finalVelocity = moveDirection * speed;

        // Jetpack болон gravity-ийн босоо хөдөлгөөн
        if (jetpack != null)
        {
            finalVelocity.y = jetpack.VerticalVelocity;
        }

        // Dash хийж байгаа үед энгийн хөдөлгөөнийг dash хөдөлгөөнөөр солино
        if (dash != null && dash.IsDashing)
        {
            finalVelocity.x = dash.DashVelocity.x;
            finalVelocity.z = dash.DashVelocity.z;
        }

        characterController.Move(finalVelocity * Time.deltaTime);

        RotatePlayer();
    }

    private void CalculateMoveDirection()
    {
        if (cameraTransform == null)
        {
            Debug.LogError("Camera Transform оруулаагүй байна!");
            moveDirection = Vector3.zero;
            return;
        }

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        moveDirection =
            forward * moveInput.y +
            right * moveInput.x;

        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
    }

    private void RotatePlayer()
    {
        if (!shouldFaceMoveDirection)
            return;

        if (moveDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(moveDirection, Vector3.up);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}