using UnityEngine;

public class EnemySensor : MonoBehaviour
{
    [Header("Sensor Information")]
    [SerializeField] private float SeeLongSensor = 35f;
    [SerializeField] private float SeeShortSensor = 5f;

    [SerializeField] private Transform CenterSensorPoint;
    [SerializeField] private Transform RightSensorPoint;
    [SerializeField] private Transform LeftSensorPoint;

    [Header("Enemy")]
    [SerializeField] private Transform PlayerTransform;
    [SerializeField] private bool ShouldFacePlayer = false;

    [SerializeField] private float RotateSpeed = 5f;

    private bool PlayerDetected;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerTransform = player.transform;
        }
    }

    private void MakeSensor()
    {
 
    }
    private bool CheckSensor(Transform sensorPoint,float distant)
    {
        if(sensorPoint == null) return false;

        RaycastHit hit;

        if (Physics.Raycast(
            sensorPoint.position,
            sensorPoint.forward,
            out hit,
            distant));
        {
            if (hit.collider.CompareTag("Player"))
            {
                return true;
            }
        }
    }
    void Update()
    {
        
    }
}
