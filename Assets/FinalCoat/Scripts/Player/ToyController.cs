using System.Collections.Generic;
using UnityEngine;

namespace FinalCoat
{
    /// <summary>
    /// One player's toy: running, jumping, holding on, and the strand buttons.
    /// Movement is physics based (Rigidbody2D) so the paint strand can pull and swing it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class ToyController : MonoBehaviour
    {
        [Header("Identity")]
        public int playerIndex;
        [Tooltip("Overwritten at start by the device picked in Settings")]
        public ControlScheme controls;
        public bool useSavedControls = true;
        public Color paintColor = Color.red;
        [Tooltip("Child that holds the sprites. It is squashed and stretched as a simple animation.")]
        public Transform visual;

        [Header("Running")]
        public float moveSpeed = 7f;
        public float groundAcceleration = 70f;
        public float groundDeceleration = 80f;
        public float airAcceleration = 30f;

        [Header("Jumping")]
        public float jumpVelocity = 12.5f;
        public float fallGravityMultiplier = 1.6f;
        [Tooltip("Grace time after walking off a ledge where a jump still works")]
        public float coyoteTime = 0.1f;
        [Tooltip("A jump pressed just before landing still happens")]
        public float jumpBuffer = 0.12f;

        [Header("Strand button")]
        [Tooltip("Press shorter than this = tug (or rejoin when snapped)")]
        public float tapMaxDuration = 0.3f;
        [Tooltip("Hold this long = snap the strand")]
        public float snapHoldDuration = 1f;

        [Header("Hold on")]
        public float holdMass = 8f;

        [Header("Getting hit")]
        public float invulnerableTime = 1.2f;
        public float knockbackSpeed = 7f;

        public Rigidbody2D Body { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsHolding { get; private set; }
        public int Facing { get; private set; } = 1;
        public bool IsInvulnerable => Time.time < invulnerableUntil;

        /// <summary>0 to 1 while the strand button is held, used by the HUD to show the snap charge.</summary>
        public float SnapCharge => tugDown && !snapTriggered ? Mathf.Clamp01((Time.time - tugPressTime) / snapHoldDuration) : 0f;

        BoxCollider2D box;
        float normalMass;
        float baseGravity;

        float moveInput;
        bool holdInput;
        float lastGroundedTime = -10f;
        float lastJumpPressTime = -10f;

        bool tugDown;
        bool snapTriggered;
        float tugPressTime;

        float invulnerableUntil;

        Vector3 visualBaseScale = Vector3.one;
        float squash; // above 0 stretches tall, below 0 squashes flat
        SpriteRenderer[] visualRenderers;

        // Reused every physics step, so the ground check allocates no memory.
        readonly List<Collider2D> overlapResults = new List<Collider2D>(8);
        ContactFilter2D solidOnly;

        void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            if (useSavedControls) controls = GameSettings.GetControls(playerIndex);

            Body.freezeRotation = true;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            normalMass = Body.mass;
            baseGravity = Body.gravityScale;

            // No friction, so toys slide along walls instead of sticking to them.
            box.sharedMaterial = new PhysicsMaterial2D("Toy (no friction)") { friction = 0f, bounciness = 0f };

            // A blank filter that only skips triggers (works on every Unity version).
            solidOnly = new ContactFilter2D { useTriggers = false };

            if (visual != null)
            {
                visualBaseScale = visual.localScale;
                visualRenderers = visual.GetComponentsInChildren<SpriteRenderer>();
            }
        }

        void Update()
        {
            var gm = GameManager.Instance;
            bool locked = gm != null && gm.InputLocked;

            moveInput = locked ? 0f : ToyInput.Horizontal(controls);
            holdInput = !locked && ToyInput.Held(controls, GameKey.Hold);
            if (moveInput != 0f) Facing = moveInput > 0f ? 1 : -1;

            if (!locked)
            {
                if (ToyInput.Pressed(controls, GameKey.Jump)) lastJumpPressTime = Time.time;
                HandleStrandButton();
            }

            AnimateVisual();
        }

        /// <summary>
        /// One button does three things:
        /// tap = tug your partner toward you, hold 1 second = snap the strand,
        /// tap while touching your partner after a snap = rejoin.
        /// </summary>
        void HandleStrandButton()
        {
            var strand = GameManager.Instance != null ? GameManager.Instance.strand : null;
            if (strand == null) return;

            if (ToyInput.Pressed(controls, GameKey.Tug))
            {
                tugDown = true;
                snapTriggered = false;
                tugPressTime = Time.time;
            }

            if (tugDown && !snapTriggered && !strand.IsSnapped && Time.time - tugPressTime >= snapHoldDuration)
            {
                snapTriggered = true;
                strand.TrySnap(this);
            }

            if (tugDown && ToyInput.Released(controls, GameKey.Tug))
            {
                tugDown = false;
                bool wasTap = !snapTriggered && Time.time - tugPressTime <= tapMaxDuration;
                if (wasTap)
                {
                    if (strand.IsSnapped) strand.TryRejoin(this);
                    else strand.Tug(this);
                }
            }
        }

        void FixedUpdate()
        {
            CheckGround();

            float dt = Time.fixedDeltaTime;
            Vector2 v = Body.GetVelocity();

            // Holding on: plant your feet and get heavy so your partner can swing from you.
            IsHolding = holdInput && IsGrounded;
            Body.mass = IsHolding ? holdMass : normalMass;
            Body.constraints = IsHolding
                ? RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation
                : RigidbodyConstraints2D.FreezeRotation;

            if (IsHolding)
            {
                v.x = 0f;
            }
            else
            {
                float target = moveInput * moveSpeed;
                bool hasInput = Mathf.Abs(moveInput) > 0.01f;

                if (hasInput)
                {
                    // In the air, input only speeds you up. It never brakes a fast swing.
                    bool canSteer = IsGrounded
                        || Mathf.Sign(target) != Mathf.Sign(v.x)
                        || Mathf.Abs(v.x) < moveSpeed;
                    if (canSteer)
                        v.x = Mathf.MoveTowards(v.x, target, (IsGrounded ? groundAcceleration : airAcceleration) * dt);
                }
                else if (IsGrounded)
                {
                    v.x = Mathf.MoveTowards(v.x, 0f, groundDeceleration * dt);
                }
                // In the air with no input, keep the momentum so swings feel natural.
            }

            if (IsGrounded) lastGroundedTime = Time.time;

            bool coyoteOk = Time.time - lastGroundedTime <= coyoteTime;
            bool jumpQueued = Time.time - lastJumpPressTime <= jumpBuffer;
            if (coyoteOk && jumpQueued && !IsHolding)
            {
                v.y = jumpVelocity;
                lastJumpPressTime = -10f;
                lastGroundedTime = -10f;
                squash = 0.25f;
            }

            // Fall faster than you rise, which makes jumps feel less floaty.
            Body.gravityScale = v.y < 0f ? baseGravity * fallGravityMultiplier : baseGravity;
            Body.SetVelocity(v);
        }

        void CheckGround()
        {
            bool wasGrounded = IsGrounded;
            Bounds b = box.bounds;
            Vector2 center = new Vector2(b.center.x, b.min.y - 0.04f);
            Vector2 size = new Vector2(b.size.x * 0.85f, 0.08f);

            overlapResults.Clear();
            Physics2D.OverlapBox(center, size, 0f, solidOnly, overlapResults);

            IsGrounded = false;
            foreach (var c in overlapResults)
            {
                if (c != box && !c.transform.IsChildOf(transform))
                {
                    IsGrounded = true;
                    break;
                }
            }

            if (IsGrounded && !wasGrounded && Body.GetVelocity().y <= 0.1f)
                squash = -0.22f; // landing squash
        }

        /// <summary>Called by enemies. Knocks the toy back and asks the GameManager to drop paint.</summary>
        public void TakeHit(Vector2 fromPosition)
        {
            var gm = GameManager.Instance;
            if (IsInvulnerable || gm == null || gm.IsLevelComplete) return;

            invulnerableUntil = Time.time + invulnerableTime;

            Vector2 away = (Vector2)transform.position - fromPosition;
            away.x = Mathf.Abs(away.x) < 0.01f ? Facing * -1f : away.x;
            away.y = Mathf.Max(away.y, 0.6f);
            Body.SetVelocity(away.normalized * knockbackSpeed);
            squash = -0.3f;

            gm.OnToyHit(this);
        }

        public void Teleport(Vector2 position)
        {
            transform.position = position;
            Body.position = position;
            Body.SetVelocity(Vector2.zero);
            invulnerableUntil = Time.time + invulnerableTime;
        }

        // Code-driven animation for the prototype: squash and stretch, a running wobble,
        // facing direction and a blink while invulnerable. Sprite animations replace this later.
        void AnimateVisual()
        {
            if (visual == null) return;

            squash = Mathf.MoveTowards(squash, 0f, Time.deltaTime * 1.5f);
            float s = IsHolding ? -0.15f : squash;

            bool running = IsGrounded && Mathf.Abs(moveInput) > 0.01f;
            float wobble = running ? Mathf.Sin(Time.time * 18f) * 0.05f : Mathf.Sin(Time.time * 3f) * 0.02f;

            visual.localScale = new Vector3(
                visualBaseScale.x * (1f - s) * Facing,
                visualBaseScale.y * (1f + s + wobble),
                1f);
            visual.localRotation = Quaternion.Euler(0f, 0f, IsGrounded ? 0f : -moveInput * 8f);

            if (visualRenderers != null)
            {
                bool show = !IsInvulnerable || Mathf.Repeat(Time.time, 0.2f) < 0.1f;
                foreach (var r in visualRenderers) r.enabled = show;
            }
        }

        void OnDrawGizmosSelected()
        {
            var b = GetComponent<BoxCollider2D>();
            if (b == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(new Vector3(b.bounds.center.x, b.bounds.min.y - 0.04f), new Vector3(b.bounds.size.x * 0.85f, 0.08f));
        }
    }
}
