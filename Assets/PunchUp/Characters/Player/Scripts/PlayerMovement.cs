using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CustomGravity))]

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody _rigidBody;
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private FirstPersonCamLooker _camLooker;
    [SerializeField] private CustomGravity _customGravity;

    [Header("Locomotion")]
    [SerializeField] private float _maxSpeed = 5.0f;
    [SerializeField] private float _groundControl = 1.0f;
    [SerializeField] private float _airControl = 0.5f;

    [Header("Jump")]
    [SerializeField] private float _jumpForce = 5.0f;

    [Header("Turning")]
    [SerializeField] private float _rotationSpeed = 5.0f;
    [SerializeField] private float _turnSpeedMultiplier = 1.0f;
    [SerializeField] private bool _canTurn = true;

    [Header("Ground Check")]
    [SerializeField] private Vector3 _originOffset;
    [SerializeField] private float _groundCheckDistance = 2.0f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _maxSlopeAngle;

    [Header("Punch")]
    [SerializeField] private PunchData _normalPunchData;
    [SerializeField] private PunchData _chargePunchData;
    private Vector3 _aimPosition;
    [SerializeField] private float _timeToChargePunch = 2.0f;

    [Header("Events")]
    [SerializeField] public UnityEvent OnLanded = new UnityEvent();
    [SerializeField] public UnityEvent PunchEvent = new UnityEvent();

    // move inputs
    private bool _hasMoveInput;
    private bool _hasTurnInput;
    private Vector3 _moveInput2D;
    private Vector3 _moveInput;
    private Vector3 _localMoveInput;
    private Vector3 _lookDirection;

    // grounded
    private Vector3 _groundNormal;
    private float _lastGroundedTime;
    private Vector3 _lastGroundedPosition;
    private bool _hasLanded = false;
    private bool _wasGroundedLastFrame = false;
    private bool _isGrounded = false;
    private float _currentControl;

    // == DEBUG ==
    private Vector3 _startPositon;

    private void OnValidate()
    {
        if(_rigidBody == null) _rigidBody = GetComponent<Rigidbody>();
        if(_playerInput == null) _playerInput = GetComponent<PlayerInput>();
        if(_camLooker == null) _camLooker = GetComponent<FirstPersonCamLooker>();
        if(_customGravity == null) _customGravity = GetComponent<CustomGravity>();
    }

    public void OnMove(InputValue inputValue)
    {
        _moveInput2D = inputValue.Get<Vector2>();
    }

    public void OnLook(InputValue inputValue)
    {
        _camLooker.SetLookInput(inputValue.Get<Vector2>());
    }

    public void OnPunch()
    {
        Punch(_normalPunchData);
    }

    public void OnJump()
    {
        TryJump();
    }

    public void OnSprint()
    {
        Teleport(_startPositon);
    }

    private void Start()
    {
        _startPositon = transform.position;
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
        _hasLanded = false;
        _wasGroundedLastFrame = _isGrounded;
        _isGrounded = GroundCheck();
        // Called when you land
        if (!_wasGroundedLastFrame && _isGrounded) Landed();

        Vector3 input = _moveInput;
        Vector3 right = Vector3.Cross(transform.up, input);
        Vector3 forward = Vector3.Cross(right, _groundNormal);

        Vector3 targetVelocity = forward * _maxSpeed;

        // TODO: must be consistent rate!
        Vector3 velocityDiff = targetVelocity - _rigidBody.linearVelocity;
        velocityDiff.y = 0;
        Vector3 controlledDiff = velocityDiff * _currentControl;

        if (_isGrounded)
        {
            _customGravity.DisableGravity();
        }
        else if (!_isGrounded)
        {
            _customGravity.EnableGravity();
            if (_hasMoveInput) _currentControl = _airControl;
            else _currentControl = 0.0f;
        }

        controlledDiff += _groundNormal * _customGravity.HandleGravity();

        _rigidBody.AddForce(controlledDiff * _rigidBody.mass);
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

    private bool GroundCheck()
    {
        bool hasHitGround = Physics.Raycast(transform.position + _originOffset, -transform.up, out RaycastHit hitInfo, _groundCheckDistance, _groundLayer);
        _groundNormal = Vector3.up;
        if (!hasHitGround) return false;

        Vector3 localGroundNormal = _rigidBody.transform.InverseTransformDirection(hitInfo.normal);
        float groundSlopeAngle = Vector3.Angle(localGroundNormal, _rigidBody.transform.up);

        if (groundSlopeAngle > _maxSlopeAngle) return false;

        if (hasHitGround && groundSlopeAngle <= _maxSlopeAngle)
        {
            _groundNormal = hitInfo.normal;
            _lastGroundedTime = Time.timeSinceLevelLoad;
            _lastGroundedPosition = transform.position;

            return true;
        }
        return false;
    }

    private void Landed()
    {
        OnLanded.Invoke();
        _hasLanded = true;
        _currentControl = _groundControl;
    }

    private IEnumerator ChargePunchRoutine()
    {
        while (true)
        {
            yield return null;
        }
    }

    private void Punch(PunchData punchData)
    {
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hitInfo, punchData.PunchDistance, punchData.PunchMask))
        {
            _aimPosition = hitInfo.point;
            Vector3 punchedNormal = hitInfo.normal;
            Vector3 reflectionVector = transform.position - punchedNormal;

            PunchEvent.Invoke();

            _currentControl = _groundControl;
            _customGravity.ResetGravity();
            _rigidBody.linearVelocity = new Vector3(_rigidBody.linearVelocity.x, 0, _rigidBody.linearVelocity.z);

            _rigidBody.AddForce(-Camera.main.transform.forward * punchData.PunchReflectionForce, ForceMode.Impulse);
            _currentControl = 0.0f;
        }
    }

    private bool TryJump()
    {
        if (_isGrounded)
        {
            DoJump();
            return true;
        }
        else
        {
            return false;
        }
    }

    private void DoJump()
    {
        _rigidBody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
    }

    // == DEBUG ==
    private void Teleport(Vector3 position)
    {
        _rigidBody.linearVelocity = Vector3.zero;
        transform.position = position;
        _rigidBody.position = position;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + _originOffset, transform.position + (-transform.up * _groundCheckDistance));
    }
}
