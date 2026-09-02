using UnityEngine;

public class EnemySensor : MonoBehaviour
{
    [Header("Sensor Information")]
    [SerializeField] private float SeeLongSensor = 35f;
    [SerializeField] private float SeeShortSensor = 35f;
    [SerializeField] private float ViewAngle = 90f;

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
        // Player tag-тай объектыг автоматаар олно
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            PlayerTransform = player.transform;
        }
    }


    private void MakeSensor()
    {
        bool centerHit = CheckSensor(
            CenterSensorPoint,
            SeeLongSensor
        );

        bool leftHit = CheckSensor(
            LeftSensorPoint,
            SeeShortSensor
        );

        bool rightHit = CheckSensor(
            RightSensorPoint,
            SeeShortSensor
        );


        PlayerDetected =
            centerHit ||
            leftHit ||
            rightHit;


        if (PlayerDetected)
        {
            Debug.Log("PLAYER DETECTED");
        }
    }
    private bool CheckSensor(Transform sensorPoint, float distance)
    {
        if (sensorPoint == null || PlayerTransform == null)
            return false;

        Vector3 directionToPlayer =
            PlayerTransform.position - sensorPoint.position;

        float playerDistance = directionToPlayer.magnitude;

        if (playerDistance > distance)
            return false;

        directionToPlayer.Normalize();


        // Enemy-ийн урд талд байгаа эсэх
        float angle = Vector3.Angle(
            transform.forward,
            directionToPlayer
        );

        if (angle > ViewAngle / 2f)
            return false;


        Debug.DrawRay(
            sensorPoint.position,
            directionToPlayer * distance,
            Color.red
        );


        RaycastHit hit;

        if (Physics.Raycast(
            sensorPoint.position,
            directionToPlayer,
            out hit,
            distance))
        {
            if (hit.collider.CompareTag("Player"))
            {
                return true;
            }
        }

        return false;
    }

    private void FacePlayer()
    {
        if (!ShouldFacePlayer)
            return;

        if (!PlayerDetected)
            return;

        if (PlayerTransform == null)
            return;


        Vector3 direction =
            PlayerTransform.position - transform.position;

        // Дээш доош харахгүй
        direction.y = 0;


        if (direction != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    RotateSpeed * Time.deltaTime
                );
        }
    }


    void Update()
    {
        MakeSensor();
        FacePlayer();
    }
}