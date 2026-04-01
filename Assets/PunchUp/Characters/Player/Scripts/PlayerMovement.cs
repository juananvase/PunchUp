using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody _rigidBody;
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private FirstPersonCamLooker _camLooker;

    [Header("Locomotion")]
    [SerializeField] private float _maxSpeed = 5.0f;
    [SerializeField] private float _control = 1.0f;

    [Header("Turning")]
    [SerializeField] private float _rotationSpeed = 5.0f;
    [SerializeField] private float _turnSpeedMultiplier = 1.0f;
    [SerializeField] private bool _canTurn = true;

    private bool _hasMoveInput;
    private bool _hasTurnInput;
    private Vector3 _moveInput2D;
    private Vector3 _moveInput;
    private Vector3 _localMoveInput;
    private Vector3 _lookDirection;

    private void OnValidate()
    {
        if(_rigidBody == null) _rigidBody = GetComponent<Rigidbody>();
        if(_playerInput == null) _playerInput = GetComponent<PlayerInput>();
        if(_camLooker == null) _camLooker = GetComponent<FirstPersonCamLooker>();
    }

    public void OnMove(InputValue inputValue)
    {
        _moveInput2D = inputValue.Get<Vector2>();
    }

    public void OnLook(InputValue inputValue)
    {
        _camLooker.SetLookInput(inputValue.Get<Vector2>());
    }

    private void Update()
    {
        // map 2D input to 3D space before moving character
        Vector3 right = Camera.main.transform.right; // thumb
        Vector3 up = Vector3.up;                     // pointer finger
        Vector3 forward = Vector3.Cross(right, up);  // middle finger
        Vector3 moveInput3D = forward * _moveInput2D.y + right * _moveInput2D.x;
        SetMoveInput(moveInput3D);
    }

    private void FixedUpdate()
    {
        Vector3 input = _moveInput;
        Vector3 right = Vector3.Cross(transform.up, input);
        Vector3 forward = Vector3.Cross(right, Vector3.up /*This will be ground normal*/);

        Vector3 targetVelocity = forward * _maxSpeed;

        // TODO: must be consistent rate!
        Vector3 velocityDiff = targetVelocity - _rigidBody.linearVelocity;
        velocityDiff.y = 0;

        _rigidBody.AddForce(velocityDiff * _rigidBody.mass * _control);
    }

    private void SetMoveInput(Vector3 input)
    {
        input = Vector3.ClampMagnitude(input, 1f);
        _hasMoveInput = input.magnitude > 0.1f;
        input = _hasMoveInput ? input : Vector3.zero;
        // remove y component of movement but retain overall magnitude
        Vector3 flattened = new Vector3(input.x, 0f, input.z);
        flattened = flattened.normalized * input.magnitude;
        _moveInput = flattened;

        // moves world input to local
        _localMoveInput = transform.InverseTransformDirection(_moveInput);
    }

    private void SetLookPosition(Vector3 position)
    {
        Vector3 direction = Vector3.ClampMagnitude(position - transform.position, 1f);
        SetLookDirection(direction);
    }

    private void SetLookDirection(Vector3 direction)
    {
        if (!_canTurn || direction.magnitude < 0.1f)
        {
            _hasTurnInput = false;
            return;
        }
        _hasTurnInput = true;
        _lookDirection = new Vector3(direction.x, 0f, direction.z).normalized;
    }

    private void HandleRotation()
    {
        Quaternion rotation = _rigidBody.rotation;

        if (_hasMoveInput)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_lookDirection);
            rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * _turnSpeedMultiplier * Time.deltaTime);
            _rigidBody.MoveRotation(rotation);
        }
    }
}
