using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CustomGravity))]
[RequireComponent(typeof(CapsuleCollider))]

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody _rigidBody;
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private FirstPersonCamLooker _camLooker;
    [SerializeField] private CustomGravity _customGravity;
    [SerializeField] private CapsuleCollider _capsuleCollider;

    [Header("Locomotion")]
    [SerializeField] private float _maxSpeed = 5.0f;
    [SerializeField] private float _groundControl = 1.0f;
    [SerializeField] private float _airControl = 0.5f;
    [SerializeField] private float _chargePunchTime = 0.5f;
    private bool _canChargePunch = false;
    [SerializeField] private float _groundGravityMultiplier = 5.0f;
    [SerializeField] private float _slopeGravity = 80.0f;

    [Header("Jump")]
    [SerializeField] private float _jumpForce = 5.0f;

    [Header("Floating")]
    [SerializeField] private bool _canFloat = true;
    [SerializeField] private float _floatRayDistance = 2.0f;
    [SerializeField] private float _stepReachForce = 25.0f;
    [SerializeField] private float _stepHeight = 1.0f;

    [Header("Mesh Turning")]
    [SerializeField] private float _rotationSpeed = 5.0f;
    [SerializeField] private float _turnSpeedMultiplier = 1.0f;
    [SerializeField] private bool _canTurn = true;

    [Header("Ground Check")]
    [SerializeField] private Vector3 _originOffset;
    [SerializeField] private float _groundCheckDistance = 2.0f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _maxSlopeAngle;
    private bool _isOnSlope = false;
    private bool _wasOnSlopeLastFrame = false;
    private Vector3 _groundPoint;
    private Vector3 _previousNormals;

    [Header("Punch")]
    [SerializeField] private PunchData _normalPunchData;
    [SerializeField] private PunchData _chargePunchData;
    private Vector3 _aimPosition;
    [SerializeField] private float _timeToChargePunch = 2.0f;

    [Header("Events")]
    [SerializeField] public UnityEvent OnLanded = new UnityEvent();
    [SerializeField] public UnityEvent PunchEvent = new UnityEvent();

    [Header("Debug")]
    [SerializeField] private bool _disableGravityWhenGrounded = false;
    private float groundMaxDist;

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
        if(_capsuleCollider == null) _capsuleCollider = GetComponent<CapsuleCollider>();
    }

    private void Start()
    {
        _startPositon = transform.position;
    }

    public void OnMove(InputValue inputValue)
    {
        _moveInput2D = inputValue.Get<Vector2>();
    }

    public void OnLook(InputValue inputValue)
    {
        _camLooker.SetLookInput(inputValue.Get<Vector2>());
    }

    public void OnPunch(InputValue inputValue)
    {
        Punch(_normalPunchData);
    }

    public void OnChargePunch(InputValue inputValue)
    {
        if (inputValue.isPressed)
        {
            StartCoroutine(ChargePunchRoutine());
        }
        else
        {
            if(!_canChargePunch) return;
            Punch(_chargePunchData);
            _canChargePunch = false;
        }
    }

    public void OnJump()
    {
        TryJump();
    }

    public void OnSprint()
    {
        Teleport(_startPositon);
    }

    public void OnDebug()
    {
        if(Time.timeScale  < 1.0f)
        {
            Time.timeScale = 1.0f;
        }
        else
        {
            Time.timeScale = 0.4f;
        }
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
        //Vector3 forward = Vector3.ProjectOnPlane(input, _groundNormal);

        Vector3 targetVelocity = forward * _maxSpeed;

        Vector3 velocityDiff = targetVelocity - _rigidBody.linearVelocity;
        velocityDiff.y = 0;
        Vector3 controlledDiff = velocityDiff * _currentControl;

        //controlledDiff = Vector3.ProjectOnPlane(controlledDiff, _groundNormal);
        controlledDiff += _groundNormal * _customGravity.HandleGravity();

        if (_isGrounded || _isOnSlope)
        {
            _customGravity.EnableGravity();
            _customGravity.ResetGravity();
            _customGravity.DisableGravityAcceleration();
        }
        else
        {
            _customGravity.ChangeGravityMultiplier(1.0f);
            _customGravity.EnableGravityAcceleration();
            _customGravity.EnableGravity();
            if (_hasMoveInput) _currentControl = _airControl;
            else _currentControl = 0.0f;
        }

        if (_isOnSlope)
        {
            if(_rigidBody.linearVelocity.y > 0)
            {
                _rigidBody.AddForce(Vector3.down * _slopeGravity, ForceMode.Force);
            }
        }
        _rigidBody.AddForce(controlledDiff * _rigidBody.mass, ForceMode.Force);
        //if(_canFloat) FloatCapsule3();

        Debug.DrawLine(transform.position, transform.position + targetVelocity, Color.yellow);
        //Debug.DrawLine(transform.position, transform.position + controlledDiff, Color.cyan);
    }



    private void FloatCapsule()
    {
        Vector3 worldCapsuleCenter = _capsuleCollider.bounds.center;
        Vector3 localCapsuleCenter = _capsuleCollider.center;

        Ray floatRay = new Ray(worldCapsuleCenter, Vector3.down);

        // casts ray down
        if (Physics.Raycast(floatRay, out RaycastHit hitInfo, _floatRayDistance, _groundLayer, QueryTriggerInteraction.Ignore))
        {
            // calculates distance from goal
            float distanceToFloatingPoint = localCapsuleCenter.y * transform.localScale.y - hitInfo.distance;

            // doesn't float you if you are on goal point
            if (distanceToFloatingPoint == 0.0f) return;

            float amountToLift = (distanceToFloatingPoint * _stepReachForce) - _rigidBody.linearVelocity.y;

            //Vector3 liftForce = new Vector3(0, amountToLift, 0);
            Vector3 liftForce = _groundNormal * amountToLift;

            _rigidBody.AddForce(liftForce, ForceMode.VelocityChange);
        }
    }

    private void FloatCapsule3()
    {
        Vector3 worldCapsuleCenter = _capsuleCollider.bounds.center;
        Vector3 localCapsuleCenter = _capsuleCollider.center;

        Ray floatRay = new Ray(worldCapsuleCenter, Vector3.down);

        // casts ray down
        if (Physics.Raycast(floatRay, out RaycastHit hitInfo, _floatRayDistance, _groundLayer, QueryTriggerInteraction.Ignore))
        {

            // calculates distance from goal
            // Y velocity diff to goal
            // you're floating point is the middle of your capsule
            float distanceToFloatingPoint = localCapsuleCenter.y * transform.localScale.y - hitInfo.distance;
            //Vector3 distanceToFloatingPoint = (localCapsuleCenter - _groundPoint).normalized;

            Debug.DrawLine(worldCapsuleCenter, new Vector3(worldCapsuleCenter.x, worldCapsuleCenter.y * distanceToFloatingPoint, worldCapsuleCenter.z));

            //doesn't float you if you are on goal point
            if (distanceToFloatingPoint == 0.0f) return;
            //if (distanceToFloatingPoint == Vector3.zero) return;

            float amountToLift = (distanceToFloatingPoint * _stepReachForce) - _rigidBody.linearVelocity.y;

            //Debug.Log(distanceToFloatingPoint);

            //Vector3 liftForce = new Vector3(0, amountToLift, 0);
            Vector3 liftForce = _groundNormal * amountToLift;

            Debug.Log(amountToLift);

            _rigidBody.AddForce(liftForce, ForceMode.VelocityChange);
        }
    }

    private bool GroundCheck()
    {
        bool hasHitGround = Physics.Raycast(_rigidBody.position + _originOffset, -_rigidBody.transform.up, out RaycastHit hitInfo, _groundCheckDistance, _groundLayer);
        _groundNormal = Vector3.up;
        _groundPoint = _rigidBody.transform.position;
        _wasOnSlopeLastFrame = _isOnSlope;
        _isOnSlope = false;
        if (!hasHitGround) return false;

        //_groundPoint = hitInfo.point;

        Vector3 localGroundNormal = _rigidBody.transform.InverseTransformDirection(hitInfo.normal);
        float groundSlopeAngle = Vector3.Angle(localGroundNormal, _rigidBody.transform.up);

        if (groundSlopeAngle > _maxSlopeAngle) return false;

        if (hasHitGround && groundSlopeAngle <= _maxSlopeAngle)
        {
            _groundNormal = hitInfo.normal;
            _groundPoint = hitInfo.point;
            _lastGroundedTime = Time.timeSinceLevelLoad;
            _lastGroundedPosition = transform.position;
            if (_groundNormal != Vector3.up) _isOnSlope = true;
            return true;
        }
        return false;
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

    private void Landed()
    {
        OnLanded.Invoke();
        _hasLanded = true;
        _currentControl = _groundControl;
    }

    private IEnumerator ChargePunchRoutine()
    {
        float time = 0.0f;
        while (time < _chargePunchTime)
        {
            time += Time.deltaTime;
            yield return null;
        }

        _canChargePunch = true;
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
        //Gizmos.DrawLine(transform.position + _originOffset, transform.position + (-transform.up * groundMaxDist));
        //Gizmos.DrawLine(transform.position + _originOffset, transform.position + (-transform.up * _groundCheckDistance));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(_groundPoint, 0.25f);
    }
}
