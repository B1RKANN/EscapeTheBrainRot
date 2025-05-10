using UnityEngine;

public class TouchController : MonoBehaviour
{
    public FixedTouchField _fixedTouchField;
    public CameraLook _cameraLook;

    void Update()
    {
        _cameraLook.LockAxis = _fixedTouchField.TouchDist;
    }
}

