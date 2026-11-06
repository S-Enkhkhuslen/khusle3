using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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
    private PlayerHealth playerHealth;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (crosshair == null) return;
        foreach (Graphic graphic in crosshair.GetComponentsInChildren<Graphic>(true))
        {
            // Invisible layout panels must stay invisible when aiming.
            Color tint = GameUI.Sand;
            tint.a = graphic.color.a;
            graphic.color = tint;
            graphic.raycastTarget = false;
        }
    }

    private void Start()
    {
        SetAimState(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            SetAimState(false);
            return;
        }
        if (context.started) SetAimState(true);
        else if (context.canceled) SetAimState(false);
    }

    private void OnDisable() { SetAimState(false); }
    private void Update() { if (IsAiming) RotatePlayerTowardsCamera(); }

    private void SetAimState(bool aiming)
    {
        IsAiming = aiming;
        if (normalCamera != null) normalCamera.Priority = aiming ? inactivePriority : activePriority;
        if (aimCamera != null) aimCamera.Priority = aiming ? activePriority : inactivePriority;
        if (crosshair != null) crosshair.SetActive(aiming);
    }

    private void RotatePlayerTowardsCamera()
    {
        if (mainCamera == null) return;
        Vector3 direction = mainCamera.transform.forward;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;
        Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
    }
}
