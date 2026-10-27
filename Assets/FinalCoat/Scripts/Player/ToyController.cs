using System.Collections.Generic;
using UnityEngine;

namespace FinalCoat{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class ToyController : MonoBehaviour{
        [Header("Identity")]
        public int playerIndex;
        public ControlScheme controls;
        public Color paintColor = Color.red;
        public Transform visual;
        [Header("Running")]
        public float moveSpeed = 7f;
        public float groundAcceleration = 70f;
        public float groundDeceleration = 80f;
        public float airAcceleration = 30f;
        [Header("Jumping")]
        public float jumpVelocity = 12.5f;
        public float fallGravityMultiplier = 1.6f;
        [Header("Strand button")]
        public float tapMaxDuration = 0.3f;
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
        public float SnapCharge => tugDown && !snapTriggered ? Mathf.Clamp01((Time.time - tugPressTime) / snapHoldDuration) : 0f;
        BoxCollider2D box;
        float normalMass;
        float baseGravity;
        float moveInput;
        bool holdInput;
        bool jumpPressed;
        bool tugDown;
        bool snapTriggered;
        float tugPressTime;
        float invulnerableUntil;
        Vector3 visualBaseScale = Vector3.one;
        SpriteRenderer[] visualRenderers;
        readonly List<Collider2D> overlapResults = new List<Collider2D>(8);
        ContactFilter2D solidOnly;
        void Awake(){
            Body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            Body.freezeRotation = true;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            normalMass = Body.mass;
            baseGravity = Body.gravityScale;
            box.sharedMaterial = new PhysicsMaterial2D("Toy (no friction)") { friction = 0f, bounciness = 0f };
            solidOnly = new ContactFilter2D { useTriggers = false };
            if (visual != null){
                visualBaseScale = visual.localScale;
                visualRenderers = visual.GetComponentsInChildren<SpriteRenderer>();
            }
        }

        void Update(){
            var gm = GameManager.Instance;
            bool locked = gm != null && gm.InputLocked;
            moveInput = locked ? 0f : ToyInput.Horizontal(controls);
            holdInput = !locked && ToyInput.Held(controls, GameKey.Hold);
            if (moveInput != 0f) Facing = moveInput > 0f ? 1 : -1;
            if (!locked){
                if (ToyInput.Pressed(controls, GameKey.Jump)) jumpPressed = true;
                HandleStrandButton();
            }
            AnimateVisual();
        }

        void HandleStrandButton(){
            var strand = GameManager.Instance != null ? GameManager.Instance.strand : null;
            if (strand == null) return;
            if (ToyInput.Pressed(controls, GameKey.Tug)){
                tugDown = true;
                snapTriggered = false;
                tugPressTime = Time.time;
            }
            if (tugDown && !snapTriggered && !strand.IsSnapped && Time.time - tugPressTime >= snapHoldDuration){
                snapTriggered = true;
                strand.TrySnap(this);
            }
            if (tugDown && ToyInput.Released(controls, GameKey.Tug)){
                tugDown = false;
                bool wasTap = !snapTriggered && Time.time - tugPressTime <= tapMaxDuration;
                if (wasTap){
                    if (strand.IsSnapped) strand.TryRejoin(this);
                    else strand.Tug(this);
                }
            }
        }

        void FixedUpdate(){
            CheckGround();
            float dt = Time.fixedDeltaTime;
            Vector2 v = Body.GetVelocity();
            IsHolding = holdInput && IsGrounded;
            Body.mass = IsHolding ? holdMass : normalMass;
            Body.constraints = IsHolding
                ? RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation
                : RigidbodyConstraints2D.FreezeRotation;
            if (IsHolding){
                v.x = 0f;
            }
            else{
                float target = moveInput * moveSpeed;
                bool hasInput = Mathf.Abs(moveInput) > 0.01f;
                if (hasInput){
                    bool canSteer = IsGrounded
                        || Mathf.Sign(target) != Mathf.Sign(v.x)
                        || Mathf.Abs(v.x) < moveSpeed;
                    if (canSteer)
                        v.x = Mathf.MoveTowards(v.x, target, (IsGrounded ? groundAcceleration : airAcceleration) * dt);
                }
                else if (IsGrounded){
                    v.x = Mathf.MoveTowards(v.x, 0f, groundDeceleration * dt);
                }
            }
            if (jumpPressed && IsGrounded && !IsHolding) v.y = jumpVelocity;
            jumpPressed = false;
            Body.gravityScale = v.y < 0f ? baseGravity * fallGravityMultiplier : baseGravity;
            Body.SetVelocity(v);
        }

        void CheckGround(){
            Bounds b = box.bounds;
            Vector2 center = new Vector2(b.center.x, b.min.y - 0.04f);
            Vector2 size = new Vector2(b.size.x * 0.85f, 0.08f);
            overlapResults.Clear();
            Physics2D.OverlapBox(center, size, 0f, solidOnly, overlapResults);
            IsGrounded = false;
            foreach (var c in overlapResults){
                if (c != box && !c.transform.IsChildOf(transform)){
                    IsGrounded = true;
                    break;
                }
            }
        }

        public void TakeHit(Vector2 fromPosition){
            var gm = GameManager.Instance;
            if (IsInvulnerable || gm == null || gm.IsLevelComplete) return;
            invulnerableUntil = Time.time + invulnerableTime;
            Vector2 away = (Vector2)transform.position - fromPosition;
            away.x = Mathf.Abs(away.x) < 0.01f ? Facing * -1f : away.x;
            away.y = Mathf.Max(away.y, 0.6f);
            Body.SetVelocity(away.normalized * knockbackSpeed);
            gm.OnToyHit(this);
        }

        public void Teleport(Vector2 position){
            transform.position = position;
            Body.position = position;
            Body.SetVelocity(Vector2.zero);
            invulnerableUntil = Time.time + invulnerableTime;
        }

        void AnimateVisual(){
            if (visual == null) return;
            visual.localScale = new Vector3(visualBaseScale.x * Facing, visualBaseScale.y, 1f);
            if (visualRenderers != null){
                bool show = !IsInvulnerable || Mathf.Repeat(Time.time, 0.2f) < 0.1f;
                foreach (var r in visualRenderers) r.enabled = show;
            }
        }

        void OnDrawGizmosSelected(){
            var b = GetComponent<BoxCollider2D>();
            if (b == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(new Vector3(b.bounds.center.x, b.bounds.min.y - 0.04f), new Vector3(b.bounds.size.x * 0.85f, 0.08f));
        }
    }
}
