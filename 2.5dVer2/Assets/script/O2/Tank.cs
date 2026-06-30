using UnityEngine;

public class Tank : MonoBehaviour
{
    [SerializeField] private float oxygenAmount = 100f;

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

        Oxygen oxygen =
            other.GetComponentInParent<Oxygen>();

        if (oxygen == null)
        {
            Debug.LogWarning(
                "Player дээр Oxygen component алга!"
            );

            return;
        }

        oxygen.AddOxygen(oxygenAmount);

        Debug.Log(
            $"Oxygen +{oxygenAmount}"
        );

        Destroy(gameObject);
    }
}