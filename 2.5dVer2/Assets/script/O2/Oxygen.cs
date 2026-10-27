using UnityEngine;
using UnityEngine.UI;

public class Oxygen : MonoBehaviour
{
    [Header("Oxygen Settings")]
    [SerializeField] private float oxygenCapacity = 1000f;
    [SerializeField] private float oxygen = 1000f;
    [Header("Normal Consumption")]
    [SerializeField] private float normalConsumptionRate = 1f;
    [Header("Oxygen Depletion")]
    [SerializeField, Min(0.1f)] private float depletionDamageInterval = 5f;
    [SerializeField, Min(0f)] private float depletionDamage = 5f;
    [Header("Oxygen Bar")]
    [SerializeField] private Slider oxygenBar;

    private PlayerHealth playerHealth;
    private float depletedTime;
    public float CurrentOxygen => oxygen;
    public float Capacity => oxygenCapacity;
    public bool HasOxygen => oxygen > 0f;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        oxygen = Mathf.Clamp(oxygen, 0f, oxygenCapacity);
    }

    private void Update()
    {
        if (playerHealth == null || !playerHealth.IsDead)
            ConsumeOxygen(normalConsumptionRate * Time.deltaTime);
            UpdateOxygenBar() ;
    }

    private void LateUpdate()
    {
        // Run after both sprint and jetpack have consumed oxygen for this frame.
        if (HasOxygen || playerHealth == null || playerHealth.IsDead)
        {
            depletedTime = 0f;
            return;
        }

        depletedTime += Time.deltaTime;
        float interval = Mathf.Max(0.1f, depletionDamageInterval);
        while (depletedTime >= interval && !playerHealth.IsDead)
        {
            depletedTime -= interval;
            playerHealth.TakeDamage(depletionDamage);
        }
    }

    public void ChangeConsume(float multiplier)
    {
        normalConsumptionRate *= Mathf.Max(0f, multiplier);
    }

    public bool ConsumeOxygen(float amount)
    {
        oxygen = Mathf.Clamp(oxygen - Mathf.Max(0f, amount), 0f, oxygenCapacity);
        return HasOxygen;
    }

    public void AddOxygen(float amount)
    {
        oxygen = Mathf.Clamp(oxygen + Mathf.Max(0f, amount), 0f, oxygenCapacity);
        if (HasOxygen)
            depletedTime = 0f;
        UpdateOxygenBar();
    }

    private void UpdateOxygenBar()
    {
        if (oxygenBar == null) return;
        oxygenBar.minValue = 0;
        oxygenBar.maxValue = oxygenCapacity;
        oxygenBar.value = oxygen;
    }
}
