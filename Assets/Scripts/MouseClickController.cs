using UnityEngine;
using UnityEngine.Events;

public class MouseClickController : MonoBehaviour
{
    public Vector3 clickPosition;
    public UnityEvent<Vector3> OnClick;

    private Ray lastRay;
    private bool okClick = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(mouseRay, out RaycastHit hitInfo))
            {
                clickPosition = hitInfo.point;
                lastRay = new Ray(mouseRay.origin, mouseRay.direction);
                okClick = true;

                Debug.Log(clickPosition);

                OnClick?.Invoke(clickPosition);
            }
        }

        if (okClick == true)
        {
            Debug.DrawRay(lastRay.origin, lastRay.direction * 100f, Color.red);
            DebugExtension.DebugWireSphere(clickPosition, Color.green, 0.2f);
        }
    }
}
