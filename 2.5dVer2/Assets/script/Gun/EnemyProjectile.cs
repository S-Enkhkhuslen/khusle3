using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifeTime = 5f;


    private Rigidbody rb;

    private float damage;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }


    public void Launch(
        Vector3 direction,
        float speed,
        float damageAmount)
    {
        // EnemyAI-аас ирсэн damage-г хадгална
        damage = damageAmount;


        // Projectile-ийг direction чиглэлд хөдөлгөнө
        rb.linearVelocity =
            direction.normalized * speed;


        // 5 секунд болоод юу ч оноогүй байсан ч устгана
        Destroy(
            gameObject,
            lifeTime
        );
    }


    private void OnTriggerEnter(Collider other)
    {
        // Player-ийг оносон уу?
        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();


        if (playerHealth != null)
        {
            // Player-д damage өгнө
            playerHealth.TakeDamage(
                damage
            );


            // Сумыг устгана
            Destroy(gameObject);

            return;
        }


        // Trigger биш бусад object мөргөвөл
        // сум бас устна
        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}