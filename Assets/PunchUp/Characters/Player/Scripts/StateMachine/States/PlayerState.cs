using UnityEngine;

public abstract class PlayerState : BaseState<PlayerStateMachine.EPlayerStates>
{
    protected PlayerStateContext Context;

    public PlayerState(PlayerStateContext context, PlayerStateMachine.EPlayerStates stateKey) : base(stateKey)
    {
        Context = context;
    }
}
