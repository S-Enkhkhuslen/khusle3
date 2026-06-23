using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] GameObject Tank;
    public int damage = 10;
    public float health = 100f;

    public void TakeDamage(float amount)
    {
        health -= amount;

        if (health <= 0f)
        {
            Die();
        }
    }
    private void spawItem()
    {
        Vector3 spawnItem = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
        GameObject TankClone = Instantiate(Tank, spawnItem, Quaternion.identity);
    }

    void Die()
    {
        Destroy(gameObject);
        spawItem();
    }

    private void OnTriggerEnter(Collider other)
    {
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
