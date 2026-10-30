using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    private PlayerAnimationController playerAnimation;
    private CharacterEffects effects;

    public bool IsDead { get; private set; }
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        playerAnimation = GetComponent<PlayerAnimationController>();
        if (GetComponent<PlayerHUD>() == null)
            gameObject.AddComponent<PlayerHUD>();
        effects = GetComponent<CharacterEffects>();
        if (effects == null) effects = gameObject.AddComponent<CharacterEffects>();
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        effects?.FlashDamage();
        Debug.Log("Player Health: " + currentHealth);
        if (currentHealth <= 0f)
            Die();
        else if (playerAnimation != null)
            playerAnimation.PlayHitAnimation();
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;
        Debug.Log("PLAYER DEAD");
        GetComponent<PlayerInput>()?.DeactivateInput();
        GetComponent<Sprint>()?.StopSprinting();
        GetComponent<Jetpack>()?.StopJetting();
        var aim = GetComponent<TPSAim>();
        if (aim != null)
            aim.enabled = false;

        // Controller and Jetpack keep gravity running so an airborne body can land.
        if (playerAnimation != null)
            playerAnimation.PlayDeathAnimation();
    }
}
