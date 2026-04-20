using UnityEngine;

public class CustomGravity : MonoBehaviour
{
    [field: SerializeField] public float MinGravity { get; private set; } = -1.0f;
    [field: SerializeField] public float MaxGravity { get; private set; } = -10.0f;
    [field: SerializeField] public float GravityAcceleration { get; private set; } = -9.0f;
    public float CurrentGravity { get; private set; }
    public bool IsGravityEnabled { get; private set; } = true;

    public float HandleGravity()
    {
        float gravity = CurrentGravity;

        if (!IsGravityEnabled)
        {
            gravity = MinGravity;
            CurrentGravity = gravity;
        }
        else
        {
            if (CurrentGravity > MaxGravity)
            {
                CurrentGravity += GravityAcceleration * Time.deltaTime;
                Mathf.Clamp(CurrentGravity, MaxGravity, MinGravity);
            }

            gravity = CurrentGravity;
        }
        return gravity;
    }

    public void DisableGravity()
    {
        IsGravityEnabled = false;
    }

    public void EnableGravity()
    {
        IsGravityEnabled = true;
    }

    public void ResetGravity()
    {
        CurrentGravity = MinGravity;
    }
}
