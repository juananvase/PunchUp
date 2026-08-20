using UnityEngine;

public class AirborneState : PlayerState
{
    public AirborneState(PlayerStateContext context, PlayerStateMachine.EPlayerStates key) : base(context, key)
    {
        PlayerStateContext Context = context;
    }

    public override void EnterState() { }

    public override void ExitState() { }

    public override void UpdateState() { }

    public override PlayerStateMachine.EPlayerStates GetNextState()
    {
        return StateKey;
    }

    public override void OnTriggerEnter(Collider other) { }

    public override void OnTriggerStay(Collider other) { }

    public override void OnTriggerExit(Collider other) { }

    public override void OnCollisionEnter(Collision other) { }

    public override void OnCollisionStay(Collision other) { }

    public override void OnCollisionExit(Collision other) { }
}
