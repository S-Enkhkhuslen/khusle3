using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class TPSAim : MonoBehaviour
{
    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera normalCamera;
    [SerializeField] private CinemachineCamera aimCamera;

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject crosshair;

    [Header("Camera Priority")]
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 10;

    [Header("Player Rotation")]
    [SerializeField] private float rotationSpeed = 15f;

    public bool IsAiming { get; private set; }

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Start()
    {
        SetAimState(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnAim(
        InputAction.CallbackContext context)
    {
        if (context.started)
        {
            SetAimState(true);
        }

        if (context.canceled)
        {
            SetAimState(false);
        }
    }

    private void Update()
    {
        if (IsAiming)
        {
            RotatePlayerTowardsCamera();
        }
    }

    private void SetAimState(bool aiming)
    {
        IsAiming = aiming;

        if (normalCamera != null)
        {
            normalCamera.Priority =
                IsAiming
                    ? inactivePriority
                    : activePriority;
        }

        if (aimCamera != null)
        {
            aimCamera.Priority =
                IsAiming
                    ? activePriority
                    : inactivePriority;
        }

        if (crosshair != null)
        {
            crosshair.SetActive(IsAiming);
        }
    }

    private void RotatePlayerTowardsCamera()
    {
        if (mainCamera == null)
        {
            return;
        }

        Vector3 lookDirection =
            mainCamera.transform.forward;

        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        lookDirection.Normalize();

        Quaternion targetRotation =
            Quaternion.LookRotation(
                lookDirection,
                Vector3.up
            );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}