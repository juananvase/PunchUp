using UnityEngine;

[CreateAssetMenu(fileName = "PunchData", menuName = "Scriptable Objects/PunchData")]
public class PunchData : ScriptableObject
{
    public float PunchDistance = 2.0f;
    public LayerMask PunchMask;
    public float PunchForce = 10.0f;
    public float PunchReflectionForce = 10.0f;
    public float ChargeTime = 0.0f;
}
