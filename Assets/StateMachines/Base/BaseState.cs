using System;
using UnityEngine;

public abstract class BaseState<TEState> where TEState : Enum
{
    public BaseState(TEState key)
    {
        StateKey = key;
    }

    public TEState StateKey { get; private set; }

    public abstract void EnterState();
    public abstract void ExitState();
    public abstract void UpdateState();
    public abstract TEState GetNextState();
    public abstract void OnTriggerEnter(Collider other);
    public abstract void OnTriggerStay(Collider other);
    public abstract void OnTriggerExit(Collider other);
    public abstract void OnCollisionEnter(Collision other);
    public abstract void OnCollisionStay(Collision other);
    public abstract void OnCollisionExit(Collision other);
}
