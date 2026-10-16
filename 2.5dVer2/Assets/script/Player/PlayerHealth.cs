using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;


    private void Start()
    {
        currentHealth = maxHealth;
    }


    public void TakeDamage(float damage)
    {
        currentHealth -= damage;


        Debug.Log(
            "Player Health: "
            + currentHealth
        );


        if (currentHealth <= 0f)
        {
            Die();
        }
    }


    private void Die()
    {
        Debug.Log("PLAYER DEAD");
    }
}