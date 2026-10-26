using UnityEngine;

namespace FinalCoat
{
    public enum BlobKind { Small, Large, SoloPot, Dropped }

    /// <summary>
    /// A blob of paint a toy can collect.
    /// Placed blobs float in the level. Dropped blobs fly out of a toy that got hit,
    /// bounce around with physics and are returned to the pool when collected or expired.
    /// Solo pots can only be collected while the strand is snapped.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PaintBlob : MonoBehaviour
    {
        public BlobKind kind = BlobKind.Small;
        public int value = 10;
        public Transform visual;

        [Tooltip("Trigger that collects the blob when a toy touches it")]
        public CircleCollider2D pickupTrigger;
        [Tooltip("Solid collider used only while a dropped blob bounces around")]
        public CircleCollider2D solidCollider;

        [Header("Dropped blobs")]
        public float droppedLifetime = 8f;
        public float droppedPickupDelay = 0.6f;

        Rigidbody2D rb;
        PaintBlobPool pool;
        float canPickUpAt;
        float despawnAt;
        bool collected;

        SpriteRenderer[] visualRenderers;
        Vector3 baseVisualPos;
        Vector3 baseVisualScale = Vector3.one;
        float bobOffset;
        float lastHintTime = -10f;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (visual != null)
            {
                baseVisualPos = visual.localPosition;
                baseVisualScale = visual.localScale;
                visualRenderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
            }
            bobOffset = Random.value * 10f;

            if (kind != BlobKind.Dropped)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                if (solidCollider != null) solidCollider.enabled = false;
            }
        }

        /// <summary>Called by the pool to throw this blob out of a hit toy.</summary>
        public void Launch(PaintBlobPool owner, Vector2 position, Vector2 velocity, int amount, Color color)
        {
            gameObject.SetActive(true); // activate first so Awake has run

            pool = owner;
            kind = BlobKind.Dropped;
            value = amount;
            collected = false;

            transform.position = position;
            rb.position = position;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.SetVelocity(velocity);
            rb.angularVelocity = 0f;

            if (solidCollider != null)
            {
                solidCollider.enabled = true;
                IgnoreToyCollisions();
            }

            SetVisible(true);
            if (visualRenderers != null)
                foreach (var r in visualRenderers) r.color = color;

            canPickUpAt = Time.time + droppedPickupDelay;
            despawnAt = Time.time + droppedLifetime;
        }

        // Dropped blobs bounce off the level but pass through toys.
        void IgnoreToyCollisions()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            foreach (var toy in gm.toys)
            {
                if (toy == null) continue;
                var c = toy.GetComponent<Collider2D>();
                if (c != null) Physics2D.IgnoreCollision(solidCollider, c, true);
            }
        }

        void Update()
        {
            if (visual == null) return;

            if (kind == BlobKind.Dropped)
            {
                if (Time.time >= despawnAt) { Despawn(); return; }
                // Blink during the last 1.5 seconds so players know it is about to vanish.
                bool blinking = despawnAt - Time.time < 1.5f;
                SetVisible(!blinking || Mathf.Repeat(Time.time, 0.2f) < 0.12f);
                return;
            }

            // Placed blobs bob and pulse gently.
            float t = Time.time * 2.5f + bobOffset;
            visual.localPosition = baseVisualPos + Vector3.up * Mathf.Sin(t) * 0.08f;
            float pulse = 1f + Mathf.Sin(t * 2f) * 0.05f;
            visual.localScale = new Vector3(baseVisualScale.x * pulse, baseVisualScale.y * pulse, 1f);
        }

        void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
        void OnTriggerStay2D(Collider2D other) => TryCollect(other);

        void TryCollect(Collider2D other)
        {
            if (collected || !isActiveAndEnabled || Time.time < canPickUpAt) return;

            var toy = other.GetComponentInParent<ToyController>();
            if (toy == null) return;

            var gm = GameManager.Instance;
            if (gm == null || gm.IsLevelComplete) return;

            if (kind == BlobKind.SoloPot && !gm.IsSolo)
            {
                if (Time.time - lastHintTime > 2.5f)
                {
                    lastHintTime = Time.time;
                    GameEvents.RaiseNotice("Solo paint pot: snap the strand to grab it");
                }
                return;
            }

            collected = true;
            gm.AddPaint(toy, value);
            Despawn();
        }

        void SetVisible(bool visible)
        {
            if (visualRenderers == null) return;
            foreach (var r in visualRenderers) r.enabled = visible;
        }

        public void Despawn()
        {
            // Both colliders can report the same event, so never release twice.
            if (!gameObject.activeSelf) return;
            SetVisible(true);
            if (pool != null) pool.Release(this);
            else gameObject.SetActive(false);
        }
    }
}
