using UnityEngine;

public class ChangeO2 : MonoBehaviour
{
    [SerializeField] private float consumptionChange = 0.5f;
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
        oxygen.ChangeConsume(consumptionChange);
        Destroy(gameObject);
    }
}
