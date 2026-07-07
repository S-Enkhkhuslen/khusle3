using UnityEngine;

public class ChangeO2 : MonoBehaviour
{
    [SerializeField] private float consumptionChange = 0.5f;

    private void Update()
    {
        transform.Rotate(
            new Vector3(15f, 30f, 45f) *
            Time.deltaTime
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Oxygen normalConsumptionRate =
            other.GetComponentInParent<Oxygen>();

        if (normalConsumptionRate == null)
        {
            Debug.LogWarning(
                "Player дээр Oxygen component алга!"
            );

            return;
        }
        normalConsumptionRate.ChangeConsume(consumptionChange);

        Debug.Log(
            $"Oxygen +{normalConsumptionRate}"
        );

        Destroy(gameObject);
    }
}