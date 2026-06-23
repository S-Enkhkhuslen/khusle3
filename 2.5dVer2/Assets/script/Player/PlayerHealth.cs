using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float HP=100;
    [SerializeField] private float MaxHP=100;
    public void TakeDamage(int amount)
    {
        HP -= amount;
        Debug.Log("Player"+MaxHP +": " + HP);

        if (HP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Player died");
        // энд animation, restart гэх мэт хийж болно
    }
}
