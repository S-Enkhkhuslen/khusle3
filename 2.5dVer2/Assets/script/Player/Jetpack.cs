using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Oxygen))]
public class Jetpack : MonoBehaviour
{
    [Header("Jetpack Settings")]
    [SerializeField] private float jetForce = 8f;
    [SerializeField] private float oxygenConsumptionRate = 20f;

    [Header("Gravity Settings")]
    [SerializeField] private float gravity = -9.8f;
    [SerializeField] private float groundedForce = -2f;

    private CharacterController characterController;
    private Oxygen oxygen;

    private bool jetButtonHeld;
    private float verticalVelocity;

    public float VerticalVelocity => verticalVelocity;
    public bool IsJetting { get; private set; }

    private void Awake()
    {
        characterController =
            GetComponent<CharacterController>();

        oxygen =
            GetComponent<Oxygen>();
    }

    // Invoke Unity Events-ээс дуудагдана.
    public void OnJump(InputAction.CallbackContext context)
    {
        // Space дарагдсан үед true,
        // Space тавигдсан үед false болно.
        jetButtonHeld = context.ReadValueAsButton();
    }

    private void Update()
    {
        HandleJetpack();
        ApplyGravity();
    }

    private void HandleJetpack()
    {
        bool canUseJetpack =
            jetButtonHeld &&
            oxygen != null &&
            oxygen.HasOxygen;

        IsJetting = canUseJetpack;

        if (!IsJetting)
        {
            return;
        }

        verticalVelocity = jetForce;

        oxygen.ConsumeOxygen(
            oxygenConsumptionRate * Time.deltaTime
        );
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity = groundedForce;
            return;
        }

        if (!IsJetting)
        {
            verticalVelocity +=
                gravity * Time.deltaTime;
        }
    }
}