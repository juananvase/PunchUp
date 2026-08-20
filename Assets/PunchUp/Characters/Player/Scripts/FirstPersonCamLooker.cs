using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCamLooker : MonoBehaviour
{
    [SerializeField] private GameObject _camera;
    [SerializeField] private float _sensX;
    [SerializeField] private float _sensY;
    [SerializeField] private float _maxPitch = 90.0f;

    [SerializeField] private Transform _orientation;

    private Vector2 _lookInput;

    private float xRotation;
    private float yRotation;

    public void OnLook(InputValue inputValue)
    {
        _lookInput = inputValue.Get<Vector2>();
    }

    private void Update()
    {
        float mouseX = _lookInput.x * Time.deltaTime * _sensX;
        float mouseY = _lookInput.y * Time.deltaTime * _sensY;

        yRotation += mouseX;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -_maxPitch, _maxPitch);

        // rotate cam and orientation
        _camera.transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        _orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}
