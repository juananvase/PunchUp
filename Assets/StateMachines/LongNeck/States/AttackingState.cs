using UnityEngine;

public class AttackingState : BaseState<LongNeckStateMachine.LongNeckState>
{
    public AttackingState(LongNeckStateMachine.LongNeckState key) : base(key) { }
    
    public override void EnterState(){}
    
    public override void ExitState(){}
    
    public override void UpdateState(){}

    public override LongNeckStateMachine.LongNeckState GetNextState()
    {
        return LongNeckStateMachine.LongNeckState.Attacking;
    }
    
    public override void OnTriggerEnter(Collider other){}
    
    public override void OnTriggerStay(Collider other){}
    
    public override void OnTriggerExit(Collider other){}
    
    public override void OnCollisionEnter(Collision other){}
    
    public override void OnCollisionStay(Collision other){}
    
    public override void OnCollisionExit(Collision other){}
}
