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

    void Start()
    {
        if (target != null)
        {
            offset = transform.position - target.position;
        }
        else
        {
            Debug.LogError("Target not assigned in SmoothFollowCamera script.");
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

            Quaternion rotation = Quaternion.Euler(currentVerticalAngle, currentRotationAngle, 0);

            Vector3 desiredPosition = target.position + rotation * offset;

            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

            transform.LookAt(target);
        }
    }
}
