using UnityEngine;

public class EnemySensor : MonoBehaviour
{
    private bool previousDetectedState;
    // =====================================================
    // REFERENCES
    // =====================================================

    [Header("References")]

    // Raycast хаанаас эхлэх вэ?
    // Enemy-ийн толгой / цээж орчимд Empty Object байрлуулна.
    [SerializeField] private Transform sensorPoint;


    // =====================================================
    // SENSOR SETTINGS
    // =====================================================

    [Header("Sensor Settings")]

    // Enemy хамгийн ихдээ хэдэн метрийн цаадах Player-ийг харах вэ?
    [SerializeField] private float viewDistance = 25f;


    // Enemy-ийн нийт харах өнцөг.
    // Жишээ нь 120 гэвэл:
    // зүүн 60° + баруун 60° = нийт 120°
    [SerializeField, Range(0f, 360f)]
    private float viewAngle = 120f;


    // Player-ийн Transform ихэвчлэн хөл орчим байдаг.
    // Тиймээс Player-ийн цээж / бие рүү Raycast чиглүүлэхийн тулд
    // Y position дээр бага зэрэг өндөр нэмнэ.
    [SerializeField] private float playerTargetHeight = 1f;


    // Sensor ямар Layer-уудтай мөргөлдөх вэ?
    // Player, Wall, Ground зэргийг энд оруулна.
    [SerializeField] private LayerMask sensorMask;


    // =====================================================
    // MEMORY
    // =====================================================

    [Header("Detection Memory")]

    // Player харагдахаа больсон ч шууд мартахгүй.
    // Жишээ нь 1 секунд санана.
    [SerializeField] private float loseSightDelay = 1f;


    // =====================================================
    // PUBLIC INFORMATION
    // =====================================================

    // EnemyAI энэ мэдээллийг ашиглана.
    // Player олдсон бол true.
    public bool PlayerDetected { get; private set; }


    // Олсон Player-ийн Transform.
    public Transform PlayerTransform { get; private set; }


    // Player-ийг хамгийн сүүлд хэдэн секундэд харсныг хадгална.
    private float lastSeenTime = -999f;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        // Scene дотор Tag = Player гэсэн Object-ийг хайна.
        GameObject player =
            GameObject.FindGameObjectWithTag("Player");


        // Player олдсон бол Transform-ийг нь хадгална.
        if (player != null)
        {
            PlayerTransform = player.transform;
        }
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        // Frame бүр Player харагдаж байгаа эсэхийг шалгана.
        DetectPlayer();
        if (PlayerDetected != previousDetectedState)
        {
            if (PlayerDetected)
            {
                Debug.Log("PLAYER DETECTED");
            }
            else
            {
                Debug.Log("PLAYER LOST");
            }

            previousDetectedState = PlayerDetected;
        }
    }


    // =====================================================
    // DETECT PLAYER
    // =====================================================

    private void DetectPlayer()
    {
        // Player эсвэл SensorPoint байхгүй бол
        // sensor ажиллах боломжгүй.
        if (PlayerTransform == null || sensorPoint == null)
        {
            PlayerDetected = false;
            return;
        }


        // ---------------------------------------------
        // 1. Player-ийн онилох position
        // ---------------------------------------------

        Vector3 targetPosition =
            PlayerTransform.position
            + Vector3.up * playerTargetHeight;


        // ---------------------------------------------
        // 2. Sensor -> Player чиглэл
        // ---------------------------------------------

        Vector3 directionToPlayer =
            targetPosition
            - sensorPoint.position;


        // ---------------------------------------------
        // 3. Player хүртэлх зай
        // ---------------------------------------------

        float distanceToPlayer =
            directionToPlayer.magnitude;


        // ---------------------------------------------
        // 4. Харах зай дотор байгаа юу?
        // ---------------------------------------------

        if (distanceToPlayer <= viewDistance)
        {
            // Direction-ийн уртыг 1 болгож,
            // зөвхөн чиглэл болгож байна.
            directionToPlayer.Normalize();


            // -----------------------------------------
            // 5. Player харах өнцөг дотор байгаа юу?
            // -----------------------------------------

            float angle =
                Vector3.Angle(
                    transform.forward,
                    directionToPlayer
                );


            // ViewAngle = 120 бол
            // нэг талдаа 60°.
            if (angle <= viewAngle / 2f)
            {
                Debug.DrawRay(
      sensorPoint.position,
      directionToPlayer * distanceToPlayer,
      Color.yellow
  );

                RaycastHit hit;

                if (Physics.Raycast(
                    sensorPoint.position,
                    directionToPlayer,
                    out hit,
                    distanceToPlayer + 0.5f,
                    sensorMask,
                    QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform.root.CompareTag("Player"))
                    {
                        Debug.DrawRay(
                            sensorPoint.position,
                            directionToPlayer * distanceToPlayer,
                            Color.green
                        );

                        lastSeenTime = Time.time;
                    }
                    else
                    {
                        Debug.DrawRay(
                            sensorPoint.position,
                            directionToPlayer * distanceToPlayer,
                            Color.red
                        );
                    }
                }
            }
        }


        // ---------------------------------------------
        // 8. PlayerDetected утгыг гаргана
        // ---------------------------------------------

        PlayerDetected =
            Time.time - lastSeenTime <= loseSightDelay;
    }


    // =====================================================
    // SCENE VIEW DEBUG
    // =====================================================

    private void OnDrawGizmosSelected()
    {
        if (sensorPoint == null)
            return;


        // Enemy-ийн харах зайг тойрог хэлбэрээр харуулна.
        Gizmos.DrawWireSphere(
            sensorPoint.position,
            viewDistance
        );


        // Нийт харах өнцгийн талыг авна.
        float halfAngle =
            viewAngle / 2f;


        // Зүүн талын харах хязгаар.
        Vector3 leftDirection =
            Quaternion.Euler(
                0f,
                -halfAngle,
                0f
            )
            * transform.forward;


        // Баруун талын харах хязгаар.
        Vector3 rightDirection =
            Quaternion.Euler(
                0f,
                halfAngle,
                0f
            )
            * transform.forward;


        // Scene дээр зүүн шугам.
        Gizmos.DrawRay(
            sensorPoint.position,
            leftDirection * viewDistance
        );


        // Scene дээр баруун шугам.
        Gizmos.DrawRay(
            sensorPoint.position,
            rightDirection * viewDistance
        );
    }
}