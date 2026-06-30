using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Jetpack : MonoBehaviour
{
    [Header("Jetpack")]
    [SerializeField] private float jetForce = 8f;
    [SerializeField] private float gravity = -9.8f;
    [SerializeField] private float groundedForce = -2f;

    [Header("Oxygen")]
    [SerializeField] private float oxygenCapacity = 1000f;
    [SerializeField] private float oxygen = 1000f;
    [SerializeField] private float normalOxygenConsumptionRate = 5f;
    [SerializeField] private float jetOxygenConsumptionRate = 20f;

    private CharacterController characterController;

    private bool isJetting;
    private float verticalVelocity;

    public float VerticalVelocity => verticalVelocity;
    public float Oxygen => oxygen;
    public float OxygenCapacity => oxygenCapacity;
    public bool IsJetting => isJetting;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        oxygen = Mathf.Clamp(oxygen, 0f, oxygenCapacity);
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && oxygen > 0f)
        {
            isJetting = true;
        }
    }

    private void Update()
    {
        // Space тавихад jetpack унтарна
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            isJetting = false;
        }

        ConsumeNormalOxygen();
        HandleJetpack();
        ApplyGravity();
    }

    private void HandleJetpack()
    {
        if (isJetting && oxygen > 0f)
        {
            verticalVelocity = jetForce;

            oxygen -= jetOxygenConsumptionRate * Time.deltaTime;
            oxygen = Mathf.Max(oxygen, 0f);
        }

        if (oxygen <= 0f)
        {
            oxygen = 0f;
            isJetting = false;
        }
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedForce;
        }
        else if (!isJetting)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private void ConsumeNormalOxygen()
    {
        if (oxygen <= 0f)
        {
            oxygen = 0f;
            return;
        }

        oxygen -= normalOxygenConsumptionRate * Time.deltaTime;
        oxygen = Mathf.Clamp(oxygen, 0f, oxygenCapacity);
        Debug.Log(oxygen);
    }

    public void AddOxygen(float amount = 100f)
    {
        oxygen += amount;
        oxygen = Mathf.Clamp(oxygen, 0f, oxygenCapacity);
    }
}