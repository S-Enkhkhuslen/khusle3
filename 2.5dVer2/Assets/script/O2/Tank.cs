using UnityEngine;

public class Tank : MonoBehaviour
{
    [SerializeField] private float oxygenAmount = 100f;
    private bool collected;

    private void Update()
    {
        transform.Rotate(new Vector3(15f, 30f, 45f) * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        Oxygen oxygen = other.GetComponentInParent<Oxygen>();
        if (oxygen == null) return;
        collected = true;
        oxygen.AddOxygen(oxygenAmount);
        Destroy(gameObject);
    }
}
