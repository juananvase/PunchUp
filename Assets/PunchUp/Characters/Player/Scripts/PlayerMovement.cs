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

    private Vector3 _horizontalVelocity => new Vector3(_rigidBody.linearVelocity.x, 0.0f, _rigidBody.linearVelocity.z);
    private Vector3 _verticalVelocity => Vector3.Project(_rigidBody.linearVelocity, _rigidBody.transform.up);
    private float _verticalSpeed => Vector3.Dot(_rigidBody.linearVelocity, _rigidBody.transform.up);

    [Header("Locomotion")]
    [SerializeField] private float _maxSpeed = 5.0f;
    [SerializeField] private float _groundControl = 1.0f;
    [SerializeField] private float _airControl = 0.5f;
    [SerializeField] private float _chargePunchTime = 0.5f;
    private bool _canChargePunch = false;

    [Header("Floating Debug")]
    [SerializeField] private float _floatRayDistance = 2.0f;
    [SerializeField] private float _stepHeight = 25.0f;
    [SerializeField] private Vector3 _snapLeanience;
    [SerializeField] private float _springTargetHeight;
    [SerializeField] private float _springStrength;
    [SerializeField] private float _springDamper;
    private float _modifiedSpringTargetHeight;
    private RaycastHit rayHit;

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
        _modifiedSpringTargetHeight = _springTargetHeight;

        _hasLanded = false;
        _wasGroundedLastFrame = _isGrounded;
        _isGrounded = GroundCheck();
        // Called when you land
        if (!_wasGroundedLastFrame && _isGrounded) Landed();
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
        
        FloatCapsule();
        
        Debug.Log(_isGrounded);

        // Get current surface normals using sphere cast or ray cast
        Quaternion fromTo = Quaternion.FromToRotation(_previousNormals, _groundNormal);
        _rigidBody.linearVelocity = fromTo * _rigidBody.linearVelocity;
        _previousNormals = _groundNormal;

        Vector3 input = _moveInput;
        Vector3 right = Vector3.Cross(transform.up, input);
        Vector3 forward = Vector3.Cross(right, _groundNormal);
        //Vector3 forward = Vector3.ProjectOnPlane(input, _groundNormal).normalized;

        Vector3 targetVelocity = forward * _maxSpeed;

        Vector3 velocityDiff = targetVelocity - _rigidBody.linearVelocity;
        velocityDiff.y = 0;
        Vector3 controlledDiff = velocityDiff * _currentControl;

        controlledDiff = Vector3.ProjectOnPlane(controlledDiff, _groundNormal);
        controlledDiff += _groundNormal * _customGravity.HandleGravity();

        if (_isGrounded)
        {
            if (_disableGravityWhenGrounded)
            {
                _customGravity.DisableGravity();
            }
            else
            {
                _customGravity.ResetGravity();
                _customGravity.DisableGravityAcceleration();
            }
        }
        else
        {
            _customGravity.EnableGravityAcceleration();
            _customGravity.EnableGravity();
            if (_hasMoveInput) _currentControl = _airControl;
            else _currentControl = 0.0f;
        }

        _rigidBody.AddForce(controlledDiff * _rigidBody.mass);

        Debug.DrawLine(transform.position, transform.position + targetVelocity, Color.yellow);
        Debug.DrawLine(transform.position, transform.position + controlledDiff, Color.cyan);

        
        StartCoroutine(LateFixedUpdateRoutine());

        IEnumerator LateFixedUpdateRoutine()
        {
            yield return new WaitForFixedUpdate();

            LateFixedUpdate();
        }
    }

    private void LateFixedUpdate()
    {


        
    }

    // when starting to move on a slope it breaks
    // WIP
    private void FloatCapsule()
    {
        //transform.up = _groundNormal;

        //Vector3 goal = new Vector3(_groundPoint.x, _groundPoint.y + _stepHeight, _groundPoint.z);
        Vector3 goal;
        if (_isGrounded)
        {
            goal = _groundPoint + transform.up * _stepHeight;
        }
        else
        {
            goal = new Vector3(_groundPoint.x, _groundPoint.y + _stepHeight, _groundPoint.z);
        }

        Vector3 difference = goal - _rigidBody.transform.position;

        if(_rigidBody.SweepTest(difference, out _, difference.magnitude, QueryTriggerInteraction.Ignore)) return;

        if (_isGrounded)
        {
            _rigidBody.transform.position = goal;
        }
        else
        {
            _rigidBody.transform.position = _groundPoint;
        }
    }

    private void FloatTest()
    {
        // this works but super buggy and you fly into the air when you are not grounded
        // double check the sonic one perhaps
        // something here with -transform.up
        float a = _groundPoint.y + _stepHeight;
        Vector3 newPosition = new Vector3(_rigidBody.position.x, a, _rigidBody.position.z);

        //transform.up = _groundNormal;
        // ^ works but needs to work with current rotation method ^


        Vector3 difference = (_groundPoint - _snapLeanience) - _rigidBody.position;

        if (_rigidBody.SweepTest(difference, out RaycastHit hitInfo, difference.magnitude, QueryTriggerInteraction.Ignore)) return;
        Debug.Log("SWEEP TEST WORKI");
        //Vector3 diff = _groundPoint - _rigidBody.position;
        //Vector3 b = transform.up * _stepHeight;
        //var c = diff + b;

        //var d = _groundNormal * _stepHeight;

        if (!_isGrounded) return;
        _rigidBody.transform.position = newPosition;

        //FloatCapsule3();
    }

    private void SpringController()
    {

        Ray _ray = new Ray(transform.position, -transform.up);
        RaycastHit _rayHit;
        //isGrounded = false;


        bool _rayDidHit = Physics.Raycast(_ray, out _rayHit, _groundCheckDistance, _groundLayer);
        if (!_rayDidHit)
        return;

        bool isGrounded = Vector3.Distance(_rayHit.point, transform.position) < _springTargetHeight && !_rayHit.collider.isTrigger;

        Debug.DrawLine(transform.position, transform.position + -transform.up * _groundCheckDistance, Color.red);

        if (isGrounded)
        {
            rayHit = _rayHit;
            Vector3 _vel = _rigidBody.linearVelocity;
            Vector3 _rayDir = transform.TransformDirection(-transform.up);

            Debug.DrawLine(transform.position, _rayHit.point, Color.green);

            Vector3 _otherVel = Vector3.zero;
            Rigidbody _hitBody = _rayHit.rigidbody;

            if (_hitBody != null)
            {
                _otherVel = _hitBody.linearVelocity;
            }

            float _rayDirVel = Vector3.Dot(_rayDir, _vel);
            float _otherDirVel = Vector3.Dot(_rayDir, _otherVel);

            float _relVel = _rayDirVel - _otherDirVel;

            float x = _rayHit.distance - _modifiedSpringTargetHeight;

            float _springForce = (x * _springStrength) - (_relVel * _springDamper);

            //Debug.DrawLine(origin.position, origin.position + (_rayDir * _springForce), Color.yellow);

            _rigidBody.AddForce(_rayDir * _springForce);

            if (_hitBody != null)
            {
                _hitBody.AddForceAtPosition(_rayDir * -_springForce, _rayHit.point);
            }
        }
        else
        {
            rayHit = new RaycastHit();
        }
    }

    private void FloatCapsule2()
    {
        Vector3 capsuleCenter = _capsuleCollider.bounds.center;
        Vector3 localCapsuleCenter = _capsuleCollider.center;

        Ray floatRay = new Ray(capsuleCenter, Vector3.down);

        if (Physics.Raycast(floatRay, out RaycastHit hitInfo, _floatRayDistance, _groundLayer, QueryTriggerInteraction.Ignore))
        {
            float distanceToFloatingPoint = localCapsuleCenter.y * transform.localScale.y - hitInfo.distance;

            if (distanceToFloatingPoint == 0.0f) return;

            float amountToLift = distanceToFloatingPoint * _stepHeight - _verticalVelocity.y;

            Vector3 liftForce = new Vector3(0.0f, amountToLift, 0.0f);

            Debug.Log(liftForce);
            _rigidBody.AddForce(liftForce, ForceMode.VelocityChange);
        }
    }

    // Sonic tutorial
    private void FloatCapsule3()
    {
        //_rigidBody.transform.up = _groundNormal;
        Vector3 goal = _groundPoint;

        Vector3 difference = goal - _rigidBody.position;

        if (_rigidBody.SweepTest(difference, out RaycastHit hitInfo, difference.magnitude, QueryTriggerInteraction.Ignore)) return;

        _rigidBody.transform.position = goal + (transform.up * _stepHeight);
    }

    // set position one that worked but was finicky
    private void FloatCapsule4()
    {
        // this works but super buggy and you fly into the air when you are not grounded
        // double check the sonic one perhaps
        float a = _groundPoint.y + _stepHeight;
        Vector3 newPosition = new Vector3(_rigidBody.position.x, a, _rigidBody.position.z);

        Vector3 diff = _groundPoint - _rigidBody.position;
        Vector3 b = transform.up * _stepHeight;
        var c = diff + b;


        var d = _groundNormal * _stepHeight;

        if (!_isGrounded) return;
        _rigidBody.position = newPosition;
    }

    private bool GroundCheck()
    {
        float maxDistance = Mathf.Max(_rigidBody.centerOfMass.y, 0f) + (_rigidBody.sleepThreshold * Time.fixedDeltaTime);

        if (_verticalSpeed < _rigidBody.sleepThreshold) maxDistance += _groundCheckDistance;
        groundMaxDist = maxDistance;

        bool hasHitGround = Physics.Raycast(_rigidBody.worldCenterOfMass + _originOffset, -_rigidBody.transform.up, out RaycastHit hitInfo, maxDistance, _groundLayer);
        _groundNormal = Vector3.up;
        _groundPoint = _rigidBody.transform.position;
        if (!hasHitGround) return false;

        _groundPoint = hitInfo.point;

        Vector3 localGroundNormal = _rigidBody.transform.InverseTransformDirection(hitInfo.normal);
        float groundSlopeAngle = Vector3.Angle(localGroundNormal, _rigidBody.transform.up);

        if (groundSlopeAngle > _maxSlopeAngle) return false;

        if (hasHitGround && groundSlopeAngle <= _maxSlopeAngle)
        {
            _groundNormal = hitInfo.normal;
            _lastGroundedTime = Time.timeSinceLevelLoad;
            _lastGroundedPosition = transform.position;
            _groundPoint = hitInfo.point;

            return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(_groundPoint, 0.4f);
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
        Gizmos.DrawLine(transform.position + _originOffset, transform.position + (-transform.up * groundMaxDist));
    }
}
