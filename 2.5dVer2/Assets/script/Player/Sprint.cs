using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Oxygen))]
[RequireComponent(typeof(Controller))]
public class Sprint : MonoBehaviour
{
    [Header("Sprint Settings")]
    [SerializeField] private float sprintSpeedMultiplier = 1.5f;
    [SerializeField] private float oxygenConsumptionRate = 15f;
    private Oxygen oxygen;
    private Controller playerController;
    private PlayerHealth playerHealth;
    private PlayerInput playerInput;
    private bool sprintButtonHeld;
    public bool IsSprinting { get; private set; }
    public float SpeedMultiplier => IsSprinting ? sprintSpeedMultiplier : 1f;

    private void Awake()
    {
        oxygen = GetComponent<Oxygen>();
        playerController = GetComponent<Controller>();
        playerHealth = GetComponent<PlayerHealth>();
        playerInput = GetComponent<PlayerInput>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        sprintButtonHeld = context.ReadValueAsButton();
    }

    // Evaluated independently of the jetpack; both can run in the same frame.
    public void Simulate(float deltaTime)
    {
        bool canReadInput = (playerHealth == null || !playerHealth.IsDead)
            && (playerInput == null || playerInput.inputIsActive);
        InputAction action = playerInput != null && playerInput.actions != null
            ? playerInput.actions.FindAction("Gameplay/Sprint", false) : null;
        bool held = action != null ? action.IsPressed() : sprintButtonHeld;
        held |= Keyboard.current != null
            && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        IsSprinting = canReadInput && held && playerController != null
            && playerController.MoveDirection.sqrMagnitude > 0.01f
            && oxygen != null && oxygen.HasOxygen;
        if (IsSprinting)
            oxygen.ConsumeOxygen(oxygenConsumptionRate * deltaTime);
    }

    public void StopSprinting()
    {
        sprintButtonHeld = false;
        IsSprinting = false;
    }
}
