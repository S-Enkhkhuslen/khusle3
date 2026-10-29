
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] GameObject Tank;
    public float health = 100f;
    private float startingHealth;
    public float MaxHealth => Mathf.Max(startingHealth, health);

    // ANIMATION: Animator байгаа Enemy л animation тоглуулна
    [Header("Animation")]
    [SerializeField] private Animator animator;

    // Death animation дуусахыг хүлээх хугацаа
    [SerializeField] private float deathDelay = 3f;

    private bool isDead = false;
    public bool IsDead => isDead;

    // Бүх Enemy-д үхсэн тухай мэдэгдэнэ
    public event System.Action OnEnemyDied;

    private void Awake()
    {
        startingHealth = Mathf.Max(1f, health);
        if (GetComponent<EnemyHealthBar>() == null)
            gameObject.AddComponent<EnemyHealthBar>();

        // Animator Inspector дээр холбоогүй бол Child-аас хайна
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        startingHealth = Mathf.Max(startingHealth, health);
        health -= amount;

        if (health <= 0f)
        {
            health = 0f;
            Die();
        }
        else
        {
            // Animator байхгүй Enemy-д алдаа гарахгүй
            if (animator != null)
            {
                animator.SetTrigger("Hit");
            }
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;
        CombatEffects.Death(transform.position + Vector3.up);

        // Бусад AI script-д Enemy үхсэнийг мэдэгдэнэ
        OnEnemyDied?.Invoke();

        if (animator != null)
        {
            animator.ResetTrigger("Hit");
            animator.SetBool("IsDead", true);

            // Animation дууссаны дараа устгана
            Invoke(nameof(FinishDeath), deathDelay);
        }
        else
        {
            // Animation байхгүй бол өмнөх шиг шууд устгана
            FinishDeath();
        }
    }

    private void FinishDeath()
    {
        // Item drop систем
        if (Tank != null)
        {
            Instantiate(Tank, transform.position + Vector3.up, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
