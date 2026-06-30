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

    private bool sprintButtonHeld;

    public bool IsSprinting { get; private set; }

    public float SpeedMultiplier
    {
        get
        {
            return IsSprinting
                ? sprintSpeedMultiplier
                : 1f;
        }
    }

    private void Awake()
    {
        oxygen = GetComponent<Oxygen>();
        playerController = GetComponent<Controller>();
    }

    // Invoke Unity Events-ээс дуудагдана.
    public void OnSprint(InputAction.CallbackContext context)
    {
        // Shift дарагдсан үед true,
        // Shift тавигдсан үед false болно.
        sprintButtonHeld = context.ReadValueAsButton();
    }

    private void Update()
    {
        HandleSprint();
    }

    private void HandleSprint()
    {
        bool isMoving =
            playerController != null &&
            playerController.MoveDirection.sqrMagnitude > 0.01f;

        bool canSprint =
            sprintButtonHeld &&
            isMoving &&
            oxygen != null &&
            oxygen.HasOxygen;

        IsSprinting = canSprint;

        if (!IsSprinting)
        {
            return;
        }

        oxygen.ConsumeOxygen(
            oxygenConsumptionRate * Time.deltaTime
        );
    }
}