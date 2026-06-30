using UnityEngine;

public class Tank : MonoBehaviour
{
    [SerializeField] private float oxygenAmount = 100f;

    private void Update()
    {
        transform.Rotate(new Vector3(15f, 30f, 45f) * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Jetpack jetpack = other.GetComponentInParent<Jetpack>();

        if (jetpack == null)
        {
            Debug.LogWarning("Jetpack component олдсонгүй.");
            return;
        }

        jetpack.AddOxygen(oxygenAmount);

        Debug.Log($"Oxygen +{oxygenAmount}");

        Destroy(gameObject);
    }
}