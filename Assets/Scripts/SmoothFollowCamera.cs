using UnityEngine;

public class SmoothFollowCamera : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public float rotationSpeed = 5.0f;
    private Vector3 offset;
    private float currentRotationAngle = 0f;
    private float currentVerticalAngle = 0f;

    public float minVerticalAngle = -40f;
    public float maxVerticalAngle = 80f;

    public float zoomSpeed = 2f;
    public float minZoom = 2f;
    public float maxZoom = 15f;
    private float currentZoomDistance;

    void Start()
    {
        if (target != null)
        {
            offset = transform.position - target.position;
            currentZoomDistance = offset.magnitude;
        }
        else
        {
            Debug.LogError("not assigned");
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
                float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

                currentRotationAngle += mouseX;
                currentVerticalAngle -= mouseY;
                currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            currentZoomDistance -= scroll * zoomSpeed;
            currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoom, maxZoom);

            Quaternion rotation = Quaternion.Euler(currentVerticalAngle, currentRotationAngle, 0);
            Vector3 direction = rotation * Vector3.forward;
            Vector3 desiredPosition = target.position - direction * currentZoomDistance;

            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

            transform.LookAt(target);
        }
    }
}
