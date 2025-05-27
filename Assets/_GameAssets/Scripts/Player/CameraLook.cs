using UnityEngine;

public class CameraLook : MonoBehaviour
{
    private float XMove;
    private float YMove;
    private float XRotation;
    [SerializeField] private Transform PlayerBody;
    public Vector2 LockAxis;
    public float Sensitivity = 20f;
    void LateUpdate()
    {
        XMove = LockAxis.x*Sensitivity*Time.deltaTime;
        YMove = LockAxis.y*Sensitivity*Time.deltaTime;
        XRotation -= YMove;
        XRotation = Mathf.Clamp(XRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(XRotation, 0f, 0f);
        PlayerBody.Rotate(Vector3.up * XMove);
    }

}
