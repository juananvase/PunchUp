using UnityEngine;

public class FirstPersonCamLooker : MonoBehaviour
{
    [SerializeField] private GameObject _playerCam;

    [SerializeField] private bool _canLook = true;
    [SerializeField] private Vector2 _lookSensitivity;
    [SerializeField] private float _maxPitch = 85.0f;

    private Vector2 _lookInput;
    private float _currentPitch = 0.0f;

    public float CurrentPitch
    {
        get => _currentPitch;

        set
        {
            _currentPitch = Mathf.Clamp(value, -_maxPitch, _maxPitch);
        }
    }

    private void FixedUpdate()
    {
        LookUpdate();
    }

    public void SetLookInput(Vector2 lookInput)
    {
        _lookInput = lookInput;
    }

    private void LookUpdate()
    {
        if (!_canLook) return;
        Vector2 input = new Vector2(_lookInput.x * _lookSensitivity.x, _lookInput.y * _lookSensitivity.y);
        // handles look up and down
        CurrentPitch -= input.y * Time.fixedDeltaTime;
        _playerCam.transform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);

        // handles looking side to side
        transform.Rotate(Vector3.up * input.x * Time.deltaTime);
    }
}
