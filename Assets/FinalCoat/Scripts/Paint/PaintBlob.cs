using UnityEngine;

namespace FinalCoat{
    public enum BlobKind { Small, Large, SoloPot }
    [RequireComponent(typeof(Rigidbody2D))]
    public class PaintBlob : MonoBehaviour{
        public BlobKind kind = BlobKind.Small;
        public int value = 10;
        public Transform visual;
        public CircleCollider2D pickupTrigger;
        bool collected;
        Vector3 baseVisualPos;
        float bobOffset;
        float lastHintTime = -10f;
        void Awake(){
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            if (visual != null) baseVisualPos = visual.localPosition;
            bobOffset = Random.value * 10f;
        }

        void Update(){
            if (visual != null)
                visual.localPosition = baseVisualPos + Vector3.up * Mathf.Sin(Time.time * 2.5f + bobOffset) * 0.08f;
        }

        void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
        void OnTriggerStay2D(Collider2D other) => TryCollect(other);
        void TryCollect(Collider2D other){
            if (collected) return;
            var toy = other.GetComponentInParent<ToyController>();
            if (toy == null) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.IsLevelComplete) return;
            if (kind == BlobKind.SoloPot && !gm.IsSolo){
                if (Time.time - lastHintTime > 2.5f){
                    lastHintTime = Time.time;
                    GameEvents.RaiseNotice("Solo paint pot: snap the strand to grab it");
                }
                return;
            }
            collected = true;
            gm.AddPaint(toy, value);
            gameObject.SetActive(false);
        }
    }
}
