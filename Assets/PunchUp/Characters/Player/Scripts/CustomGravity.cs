using UnityEngine;

public class CustomGravity : MonoBehaviour
{
    [field: SerializeField] public float MinGravity { get; private set; } = -1.0f;
    [field: SerializeField] public float MaxGravity { get; private set; } = -10.0f;
    [field: SerializeField] public float GravityAcceleration { get; private set; } = -9.0f;
    public float GravityMultiplier { get; private set; } = 1.0f;
    public float CurrentGravity { get; private set; } = 0.0f;
    public bool IsGravityEnabled { get; private set; } = true;
    public bool IsGravityAccelerationEnabled { get; private set; } = true;

    public float HandleGravity()
    {
        float gravity = CurrentGravity;

        if (!IsGravityEnabled)
        {
            gravity = 0.0f;
            CurrentGravity = MinGravity;
        }
        else
        {
            if (CurrentGravity > MaxGravity && IsGravityAccelerationEnabled)
            {
                CurrentGravity += GravityAcceleration * Time.fixedDeltaTime;
                CurrentGravity = Mathf.Clamp(CurrentGravity, MaxGravity, MinGravity);
            }

            gravity = CurrentGravity;
        }

        return gravity * GravityMultiplier;
    }

    public void DisableGravity()
    {
        IsGravityEnabled = false;
    }

    public void EnableGravity()
    {
        IsGravityEnabled = true;
    }

    public void DisableGravityAcceleration()
    {
        IsGravityAccelerationEnabled = false;
    }

    public void EnableGravityAcceleration()
    {
        IsGravityAccelerationEnabled = true;
    }

    public void ResetGravity()
    {
        EnableGravityAcceleration();
        CurrentGravity = MinGravity;
    }

    public void ChangeGravityMultiplier(float multiplier)
    {
        GravityMultiplier = multiplier;
    }
}
