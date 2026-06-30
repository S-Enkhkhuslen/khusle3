using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDash : MonoBehaviour
{
    [Header("Dash")]
    [SerializeField] private float dashSpeed = 50f;
    [SerializeField] private float dashDuration = 0.5f;
    [SerializeField] private float dashCooldown = 1f;

    private Controller playerController;

    private bool isDashing;
    private bool canDash = true;
    private Vector3 dashDirection;

    public bool IsDashing => isDashing;

    public Vector3 DashVelocity
    {
        get
        {
            if (!isDashing)
                return Vector3.zero;

            return dashDirection * dashSpeed;
        }
    }

    private void Awake()
    {
        playerController = GetComponent<Controller>();
    }

    public void OnDash(InputValue value)
    {
        if (!value.isPressed)
            return;

        if (!canDash || isDashing)
            return;

        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;

        dashDirection = playerController.MoveDirection;

        // WASD дараагүй үед урагш dash хийнэ
        if (dashDirection.sqrMagnitude < 0.01f)
        {
            dashDirection = transform.forward;
            dashDirection.y = 0f;
        }

        dashDirection.Normalize();

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);

        canDash = true;
    }
}