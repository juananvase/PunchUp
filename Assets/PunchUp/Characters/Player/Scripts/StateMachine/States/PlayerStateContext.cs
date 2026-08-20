using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateContext
{
    private Rigidbody _rb;
    private PlayerInput _playerInput;
    private FirstPersonCamLooker _camLooker;
    private CustomGravity _customGravity;
    private CapsuleCollider _capsuleCollider;

    public PlayerStateContext(Rigidbody rigidbody, PlayerInput playerInput, 
    FirstPersonCamLooker camLooker, CustomGravity customGravity, CapsuleCollider capsuleCollider)
    {
        _rb = rigidbody;
        _playerInput = playerInput;
        _camLooker = camLooker;
        _customGravity = customGravity;
        _capsuleCollider = capsuleCollider;
    }

    public Rigidbody Rb;
    public PlayerInput PlayerInput;
    public FirstPersonCamLooker CamLooker;
    public CustomGravity CustomGravity;
    public CapsuleCollider CapsuleCollider;
}
