using System.Collections.Generic;
using UnityEngine;

namespace FinalCoat
{
    /// <summary>
    /// Wind-up toy enemy. Runs a state machine (see EnemyStates.cs) and always chases
    /// the nearest toy. A hit knocks paint loose, then it winds down and can be pushed.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class WindUpToy : MonoBehaviour
    {
        [Header("Patrol")]
        public float patrolDistance = 2.5f;
        public float patrolSpeed = 1.5f;

        [Header("Chase")]
        public float detectRange = 4.5f;
        public float loseRange = 7f;
        [Tooltip("Ignores toys much higher or lower than itself, like toys on another shelf")]
        public float maxHeightDifference = 2f;
        public float chaseSpeed = 3.2f;
        public float retargetInterval = 0.25f;

        [Header("After a hit")]
        public float hitPause = 0.25f;
        public float windDownTime = 2f;
        public float windDownMass = 0.4f;
        public float returnSpeed = 2f;

        [Header("Visuals")]
        public Transform body;
        public Transform key;
        public SpriteRenderer bodyRenderer;
        public Color activeColor = new Color(0.36f, 0.4f, 0.55f);
        public Color woundDownColor = new Color(0.7f, 0.7f, 0.7f);

        [Header("Debug")]
        [Tooltip("Shows the current state above the enemy. Handy for the demo video.")]
        public bool showStateLabel = true;

        public Rigidbody2D Body { get; private set; }
        public Vector2 Home { get; private set; }
        public ToyController Target { get; set; }
        public int Facing { get; private set; } = 1;
        public float KeySpinSpeed { get; set; }

        public PatrolState Patrol { get; private set; }
        public ChaseState Chase { get; private set; }
        public HitState Hit { get; private set; }
        public WindDownState WindDown { get; private set; }
        public ReturnState Return { get; private set; }
        public EnemyState Current { get; private set; }

        BoxCollider2D box;
        float normalMass;
        Vector2 spawnPoint;
        Vector3 bodyBaseScale = Vector3.one;
        float keyOffsetX;
        readonly List<RaycastHit2D> rayHits = new List<RaycastHit2D>(4);
        ContactFilter2D solidOnly;
        GUIStyle labelStyle;

        void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            Body.freezeRotation = true;
            normalMass = Body.mass;
            spawnPoint = transform.position;
            Home = spawnPoint;

            // A blank filter that only skips triggers (works on every Unity version).
            solidOnly = new ContactFilter2D { useTriggers = false };

            if (body != null) bodyBaseScale = body.localScale;
            if (key != null) keyOffsetX = Mathf.Abs(key.localPosition.x);
            if (bodyRenderer != null) bodyRenderer.color = activeColor;

            // One instance of each state, reused for the whole game.
            Patrol = new PatrolState(this);
            Chase = new ChaseState(this);
            Hit = new HitState(this);
            WindDown = new WindDownState(this);
            Return = new ReturnState(this);
        }

        void Start() => ChangeState(Patrol);

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.IsLevelComplete) return;
            Current?.Tick(Time.deltaTime);
            Animate();
        }

        void FixedUpdate()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.IsLevelComplete) { Stop(); return; }
            Current?.FixedTick(Time.fixedDeltaTime);
        }

        public void ChangeState(EnemyState next)
        {
            Current?.Exit();
            Current = next;
            Current.Enter();
        }

        // ---------------- Helpers the states use ----------------

        public ToyController FindNearestToy(float range)
        {
            var gm = GameManager.Instance;
            if (gm == null) return null;

            ToyController best = null;
            float bestDistance = range;
            foreach (var toy in gm.toys)
            {
                if (toy == null) continue;
                Vector2 d = toy.transform.position - transform.position;
                if (Mathf.Abs(d.y) > maxHeightDifference) continue;
                float distance = d.magnitude;
                if (distance <= bestDistance)
                {
                    best = toy;
                    bestDistance = distance;
                }
            }
            return best;
        }

        /// <summary>Walks left (-1) or right (+1). Returns false if it had to stop at a ledge.</summary>
        public bool Walk(int direction, float speed)
        {
            if (direction != 0) Facing = direction;
            Vector2 v = Body.GetVelocity();

            if (direction == 0 || !HasGroundAhead(direction))
            {
                v.x = Mathf.MoveTowards(v.x, 0f, 30f * Time.fixedDeltaTime);
                Body.SetVelocity(v);
                return direction == 0;
            }

            v.x = direction * speed;
            Body.SetVelocity(v);
            return true;
        }

        public void Stop()
        {
            Vector2 v = Body.GetVelocity();
            v.x = 0f;
            Body.SetVelocity(v);
        }

        public bool HasGroundAhead(int direction)
        {
            Bounds b = box.bounds;
            Vector2 origin = new Vector2(direction > 0 ? b.max.x + 0.05f : b.min.x - 0.05f, b.min.y + 0.05f);
            rayHits.Clear();
            int count = Physics2D.Raycast(origin, Vector2.down, solidOnly, rayHits, 0.6f);
            for (int i = 0; i < count; i++)
                if (rayHits[i].collider != box) return true;
            return false;
        }

        public void SetWoundDown(bool woundDown)
        {
            Body.mass = woundDown ? windDownMass : normalMass;
            if (bodyRenderer != null) bodyRenderer.color = woundDown ? woundDownColor : activeColor;
        }

        public void SetHome(Vector2 position) => Home = position;

        /// <summary>Called by the kill zone if a player shoves this enemy off the level.</summary>
        public void ResetToHome()
        {
            transform.position = spawnPoint;
            Body.position = spawnPoint;
            Body.SetVelocity(Vector2.zero);
            Home = spawnPoint;
            ChangeState(Patrol);
        }

        // ---------------- Hitting toys ----------------

        void OnCollisionEnter2D(Collision2D collision) => TryHit(collision.collider);
        void OnCollisionStay2D(Collision2D collision) => TryHit(collision.collider);

        void TryHit(Collider2D other)
        {
            if (!(Current is PatrolState || Current is ChaseState)) return;
            var toy = other.GetComponentInParent<ToyController>();
            if (toy == null || toy.IsInvulnerable) return;

            toy.TakeHit(transform.position);
            Target = toy;
            ChangeState(Hit);
        }

        // ---------------- Code-driven animation (placeholder for sprite animation) ----------------

        void Animate()
        {
            if (key != null)
            {
                key.localPosition = new Vector3(-keyOffsetX * Facing, key.localPosition.y, 0f);
                key.Rotate(0f, 0f, -KeySpinSpeed * Time.deltaTime);
            }

            if (body != null)
            {
                float speed = Mathf.Abs(Body.GetVelocity().x);
                float hop = speed > 0.1f ? Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.08f : 0f;
                body.localPosition = new Vector3(0f, hop, 0f);
                body.localScale = new Vector3(bodyBaseScale.x * Facing, bodyBaseScale.y, 1f);
            }
        }

        void OnGUI()
        {
            if (!showStateLabel || Current == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 0.9f);
            if (screen.z < 0f) return;

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 13
                };
                labelStyle.normal.textColor = new Color(0.15f, 0.15f, 0.2f);
            }

            GUI.Label(new Rect(screen.x - 80f, Screen.height - screen.y - 12f, 160f, 24f), Current.Name, labelStyle);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, detectRange);
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, loseRange);
            Vector3 home = Application.isPlaying ? (Vector3)Home : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(home + Vector3.left * patrolDistance, home + Vector3.right * patrolDistance);
        }
    }
}
