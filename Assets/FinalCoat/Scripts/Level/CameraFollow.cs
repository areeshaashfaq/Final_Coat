using UnityEngine;

namespace FinalCoat{
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour{
        public float smoothTime = 0.25f;
        public Vector2 offset = new Vector2(0f, 1.5f);
        public float minSize = 5.5f;
        public float maxSize = 9f;
        public float sizePerUnitApart = 0.35f;
        Camera cam;
        Vector3 moveVelocity;
        float zoomVelocity;
        void Awake(){
            cam = GetComponent<Camera>();
            cam.orthographic = true;
        }

        void LateUpdate(){
            var gm = GameManager.Instance;
            if (gm == null || gm.toys == null || gm.toys.Length == 0) return;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (var toy in gm.toys){
                if (toy == null) continue;
                min = Vector2.Min(min, toy.transform.position);
                max = Vector2.Max(max, toy.transform.position);
            }
            Vector2 center = (min + max) * 0.5f + offset;
            Vector3 target = new Vector3(center.x, center.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, target, ref moveVelocity, smoothTime);
            float spread = Mathf.Max(max.x - min.x, (max.y - min.y) * cam.aspect);
            float targetSize = Mathf.Clamp(minSize + spread * sizePerUnitApart, minSize, maxSize);
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref zoomVelocity, smoothTime);
        }
    }
}
