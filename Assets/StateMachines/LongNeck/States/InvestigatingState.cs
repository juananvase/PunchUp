using UnityEngine;

public class InvestigatingState : BaseState<LongNeckStateMachine.LongNeckState>
{
    public InvestigatingState(LongNeckStateMachine.LongNeckState key) : base(key) { }
    
    public override void EnterState(){}
    
    public override void ExitState(){}
    
    public override void UpdateState(){}

    public override LongNeckStateMachine.LongNeckState GetNextState()
    {
        return LongNeckStateMachine.LongNeckState.Investigating;
    }
    
    public override void OnTriggerEnter(Collider other){}
    
    public override void OnTriggerStay(Collider other){}
    
    public override void OnTriggerExit(Collider other){}
    
    public override void OnCollisionEnter(Collision other){}
    
    public override void OnCollisionStay(Collision other){}
    
    public override void OnCollisionExit(Collision other){}
}
