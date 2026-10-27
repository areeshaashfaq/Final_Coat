using UnityEngine;

namespace FinalCoat{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Respawnable : MonoBehaviour{
        Vector3 startPosition;
        Quaternion startRotation;
        Rigidbody2D rb;
        void Awake(){
            rb = GetComponent<Rigidbody2D>();
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        public void ResetToStart(){
            transform.SetPositionAndRotation(startPosition, startRotation);
            rb.position = startPosition;
            rb.rotation = startRotation.eulerAngles.z;
            rb.SetVelocity(Vector2.zero);
            rb.angularVelocity = 0f;
        }
    }
}
