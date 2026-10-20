
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] GameObject Tank;
    public int damage = 10;
    public float health = 100f;

    // ANIMATION: Animator байгаа Enemy л animation тоглуулна
    [Header("Animation")]
    [SerializeField] private Animator animator;

    // Death animation дуусахыг хүлээх хугацаа
    [SerializeField] private float deathDelay = 3f;

    // Melee Enemy-д contact damage-ийг унтрааж болно
    [SerializeField] private bool damageOnContact = true;

    private bool isDead = false;
    public bool IsDead => isDead;

    // Бүх Enemy-д үхсэн тухай мэдэгдэнэ
    public event System.Action OnEnemyDied;

    private void Awake()
    {
        // Animator Inspector дээр холбоогүй бол Child-аас хайна
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead)
            return;

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

    private void spawItem()
    {
        Vector3 spawnItem = new Vector3(
            transform.position.x,
            transform.position.y + 1,
            transform.position.z
        );

        GameObject TankClone = Instantiate(
            Tank,
            spawnItem,
            Quaternion.identity
        );
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

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
            spawItem();
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead || !damageOnContact)
            return;

        if (other.CompareTag("Player"))
        {
            PlayerHealth player = other.GetComponent<PlayerHealth>();

            if (player != null)
            {
                player.TakeDamage(damage);
            }
        }
    }
}
