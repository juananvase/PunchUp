using UnityEngine;

public class CursorLocker : MonoBehaviour
{
    [SerializeField] private bool _setOnAwake = true;
    [SerializeField] private CursorLockMode _lockMode;
    [SerializeField] private bool _isVisible = false;

    private void Awake()
    {
        if (!_setOnAwake) return;
        Cursor.visible = _isVisible;
        Cursor.lockState = _lockMode;
    }
}
