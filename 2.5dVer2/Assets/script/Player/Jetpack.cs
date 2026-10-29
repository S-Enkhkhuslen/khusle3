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
    private PlayerHealth playerHealth;
    private PlayerInput playerInput;
    private InputAction jumpAction;
    private bool jetButtonHeld;
    private float verticalVelocity;
    public float VerticalVelocity => verticalVelocity;
    public bool IsJetting { get; private set; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        oxygen = GetComponent<Oxygen>();
        playerHealth = GetComponent<PlayerHealth>();
        playerInput = GetComponent<PlayerInput>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        jetButtonHeld = context.ReadValueAsButton();
    }

    private void Start()
    {
        jumpAction = playerInput != null && playerInput.actions != null
            ? playerInput.actions.FindAction("Gameplay/jump", false) : null;
    }

    // Controller calls this once, before applying the combined movement.
    public void Simulate(float deltaTime)
    {
        bool canReadInput = (playerHealth == null || !playerHealth.IsDead)
            && (playerInput == null || playerInput.inputIsActive);
        bool held = jumpAction != null ? jumpAction.IsPressed() : jetButtonHeld;
        // Space remains independent of Shift and of an input callback's phase.
        held |= Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        IsJetting = canReadInput && held && oxygen != null && oxygen.HasOxygen;

        if (IsJetting)
        {
            verticalVelocity = jetForce;
            oxygen.ConsumeOxygen(oxygenConsumptionRate * deltaTime);
        }
        else if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedForce;
        else
            verticalVelocity += gravity * deltaTime;
    }

    public void StopJetting()
    {
        jetButtonHeld = false;
        IsJetting = false;
        verticalVelocity = Mathf.Min(0f, verticalVelocity);
    }

    public void ResetVerticalVelocity()
    {
        verticalVelocity = groundedForce;
    }
}
