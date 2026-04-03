using UnityEngine;

public class AnimationReciever : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerMovement _playerMovement;

    private void OnEnable()
    {
        _playerMovement.PunchEvent.AddListener(SetPunchTrigger);
    }

    private void OnDisable()
    {
        _playerMovement.PunchEvent.RemoveListener(SetPunchTrigger);
    }

    private void SetPunchTrigger()
    {
        _animator.SetTrigger("PunchTrigger");
    }
}
