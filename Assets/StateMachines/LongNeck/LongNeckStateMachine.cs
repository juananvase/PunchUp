
using Unity.VisualScripting;
using UnityEditor.VersionControl;

public class LongNeckStateMachine : StateManager<LongNeckStateMachine.LongNeckState>
{
    //States Key
    public enum LongNeckState
    {
        Wandering,
        Investigating,
        Attacking,
        Chasing,
        Lost,
        Killing
    }
    
    //Instantiate States
    private WanderingState _wanderingState = new WanderingState(LongNeckState.Wandering);
    private InvestigatingState _investigatingState = new InvestigatingState(LongNeckState.Investigating);
    private AttackingState _attackingState = new AttackingState(LongNeckState.Attacking);
    private ChasingState _chasingState = new ChasingState(LongNeckState.Chasing);
    private LostState _lostState = new LostState(LongNeckState.Lost);
    private KillingState _killingState = new KillingState(LongNeckState.Killing);

    private void Awake()
    {
        //Initial State definition
        CurrentState = _wanderingState;
        
        //Add States to the Dictionary
        States.Add(LongNeckState.Wandering, _wanderingState);
        States.Add(LongNeckState.Investigating, _investigatingState);
        States.Add(LongNeckState.Attacking, _attackingState);
        States.Add(LongNeckState.Chasing, _chasingState);
        States.Add(LongNeckState.Lost, _lostState);
        States.Add(LongNeckState.Killing, _killingState);
    }
    
    

}
