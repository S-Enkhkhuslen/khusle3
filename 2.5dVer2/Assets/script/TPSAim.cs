using UnityEngine;

public class TPSAim : MonoBehaviour
{
    public Transform normalCameraPos;
    public Transform aimCameraPos;

    public Transform cameraTransform;

    public float smoothSpeed = 10f;

    public GameObject crosshair;

    public Camera cam;

    public float normalFOV = 60f;
    public float aimFOV = 30f;

    bool isAiming;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        isAiming = Input.GetMouseButton(1);

        if (isAiming)
        {
            cameraTransform.position =
                Vector3.Lerp(cameraTransform.position,
                             aimCameraPos.position,
                             smoothSpeed * Time.deltaTime);

            cam.fieldOfView =
                Mathf.Lerp(cam.fieldOfView,
                           aimFOV,
                           smoothSpeed * Time.deltaTime);

            crosshair.SetActive(true);
        }
        else
        {
            cameraTransform.position =
                Vector3.Lerp(cameraTransform.position,
                             normalCameraPos.position,
                             smoothSpeed * Time.deltaTime);

            cam.fieldOfView =
                Mathf.Lerp(cam.fieldOfView,
                           normalFOV,
                           smoothSpeed * Time.deltaTime);

            crosshair.SetActive(false);
        }
    }
}