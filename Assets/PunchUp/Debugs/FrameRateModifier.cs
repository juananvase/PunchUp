using UnityEngine;

public class FrameRateModifier : MonoBehaviour
{
    [SerializeField, Tooltip("Change to -1 to revert to uncapped fps")] private int _targetFps = -1;

    private void Awake()
    {
        Application.targetFrameRate = _targetFps;
    }
}
