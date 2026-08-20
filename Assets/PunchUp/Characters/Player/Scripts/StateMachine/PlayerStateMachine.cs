using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : StateManager<PlayerStateMachine.EPlayerStates>
{
    // states key
    public enum EPlayerStates
    {
        Walking,
        OnSlope,
        Airborne
    }

    private PlayerStateContext _context;

    //Instantiate States
    //private WalkingState _walkingState = new WalkingState(EPlayerStates.Walking);
    //private OnSlopeState _onSlopeState = new OnSlopeState(EPlayerStates.OnSlope);
    //private AirborneState _airborneState = new AirborneState(EPlayerStates.Airborne);

    [SerializeField] private Rigidbody _rb;
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private FirstPersonCamLooker _camLooker;
    [SerializeField] private CustomGravity _customGravity;
    [SerializeField] private CapsuleCollider _capsuleCollider;
    
    private void OnValidate()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody>();
        if (_playerInput == null) _playerInput = GetComponent<PlayerInput>();
        if (_camLooker == null) _camLooker = GetComponent<FirstPersonCamLooker>();
        if (_customGravity == null) _customGravity = GetComponent<CustomGravity>();
        if (_capsuleCollider == null) _capsuleCollider = GetComponent<CapsuleCollider>();
    }

    private void InitializeStates()
    {
        States.Add(EPlayerStates.Walking, new WalkingState(_context, EPlayerStates.Walking));
        States.Add(EPlayerStates.OnSlope, new WalkingState(_context, EPlayerStates.OnSlope));
        States.Add(EPlayerStates.Airborne, new WalkingState(_context, EPlayerStates.Airborne));

        CurrentState = States[EPlayerStates.Walking];
    }

    private void Awake()
    {
        _context = new PlayerStateContext(_rb, _playerInput, _camLooker, _customGravity, _capsuleCollider);

        InitializeStates();
    }
}
