using UnityEngine;

public class Bump : MonoBehaviour
{
    [SerializeField] private float _bumpForce = 20.0f;
    [SerializeField] private Vector3 _bumpDirection;

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.TryGetComponent(out Rigidbody rb))
        {
            rb.AddForce(_bumpDirection * _bumpForce);
            Debug.Log("bumped");
        }
    }
}
