using UnityEngine;

public class Oxygen : MonoBehaviour
{
    [Header("Oxygen Settings")]
    [SerializeField] private float oxygenCapacity = 1000f;
    [SerializeField] private float oxygen = 1000f;

    [Header("Normal Consumption")]
    [SerializeField] private float normalConsumptionRate = 1f;

    public float CurrentOxygen => oxygen;
    public float Capacity => oxygenCapacity;
    public bool HasOxygen => oxygen > 0f;

    private void Awake()
    {
        oxygen = Mathf.Clamp(
            oxygen,
            0f,
            oxygenCapacity
        );
    }

    private void Update()
    {
        // Player юу ч хийхгүй байсан oxygen үргэлж хасагдана
        ConsumeOxygen(
            normalConsumptionRate * Time.deltaTime
        );
        //Debug.Log(oxygen);
    }

    public bool ConsumeOxygen(float amount)
    {
        if (oxygen <= 0f)
        {
            oxygen = 0f;
            return false;
        }

        oxygen -= amount;

        oxygen = Mathf.Clamp(
            oxygen,
            0f,
            oxygenCapacity
        );

        return oxygen > 0f;
    }

    public void AddOxygen(float amount)
    {
        oxygen += amount;

        oxygen = Mathf.Clamp(
            oxygen,
            0f,
            oxygenCapacity
        );
    }
}