using UnityEngine;
using UnityEngine.InputSystem;

public class NewMovement : MonoBehaviour
{
    public enum MovementStates
    {
        walking,
        air
    }

    private MovementStates state;
    [SerializeField] private Transform _orientation;

    [Header("Movement")]
    [SerializeField] private float _speed;
    [SerializeField] private float _groundDrag;

    [Header("Jumping")]
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _jumpCooldown;
    [SerializeField] private float _airMultiplier;
    private bool _readyToJump = true;

    [Header("Ground Check")]
    [SerializeField] private float _playerHeight;
    [SerializeField] private LayerMask _groundMask;
    private bool _isGrounded;

    [Header("Slope Handling")]
    [SerializeField] private float _maxSlopeAngle;
    private RaycastHit _slopeHit;
    private bool _exitingSlope;

    private Vector2 _moveInput;

    private float _inputX;
    private float _inputY;

    private Vector3 _moveDir;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;
    }

    private void StateHandler()
    {
        if (_isGrounded)
        {
            state = MovementStates.walking;
        }
        else
        {
            state = MovementStates.air;
        }
    }

    public void OnMove(InputValue inputValue)
    {
        _moveInput = inputValue.Get<Vector2>();
    }

    public void OnJump()
    {
        if(_readyToJump && _isGrounded)
        {
            _readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), _jumpCooldown);
        }
    }

    private void Update()
    {
        HandleInput();
        SpeedControl();
        StateHandler();
    }

    private void FixedUpdate()
    {
        MovePlayer();

        _isGrounded = Physics.Raycast(transform.position, Vector3.down, _playerHeight * 0.5f + 0.2f, _groundMask);

        if (_isGrounded)
        {
            _rb.linearDamping = _groundDrag;
        }
        else
        {
            _rb.linearDamping = 0.0f;
        }
    }

    private void HandleInput()
    {
        _inputX = _moveInput.x;
        _inputY = _moveInput.y;
    }

    private void MovePlayer()
    {
        _moveDir = _orientation.forward * _inputY + _orientation.right * _inputX;

        if (OnSlope() && !_exitingSlope)
        {
            _rb.AddForce(GetSlopeMoveDirection() * _speed * 20.0f, ForceMode.Force);

            if(_rb.linearVelocity.y > 0)
            {
                _rb.AddForce(Vector3.down * 80.0f, ForceMode.Force);
            }
        }

        if (_isGrounded)
        {
            _rb.AddForce(_moveDir.normalized * _speed * 10.0f, ForceMode.Force);
        }
        else
        {
            _rb.AddForce(_moveDir.normalized * _speed * 10.0f * _airMultiplier, ForceMode.Force);
        }

        _rb.useGravity = !OnSlope();
    }
    private void SpeedControl()
    {
        if (OnSlope() && !_exitingSlope)
        {
            if(_rb.linearVelocity.magnitude > _speed)
            {
                _rb.linearVelocity = _rb.linearVelocity.normalized * _speed;
            }
        }
        else
        {
            Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0.0f, _rb.linearVelocity.z);

            if (flatVel.magnitude > _speed)
            {
                Vector3 limitedVel = flatVel.normalized * _speed;
                _rb.linearVelocity = new Vector3(limitedVel.x, _rb.linearVelocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        _exitingSlope = true;
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0.0f, _rb.linearVelocity.z);

        _rb.AddForce(transform.up * _jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        _readyToJump = true;

        _exitingSlope = false;
    }

    private bool OnSlope()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out _slopeHit, _playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
            return angle < _maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(_moveDir, _slopeHit.normal).normalized;
    }
}
