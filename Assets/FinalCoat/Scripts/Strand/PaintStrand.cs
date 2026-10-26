using UnityEngine;

namespace FinalCoat
{
    /// <summary>
    /// The strand of wet paint joining two toys.
    /// Physics: a DistanceJoint2D that only stops the toys getting further apart than maxLength,
    /// so they can walk freely but get pulled back, hang and swing at full stretch.
    /// Look: a LineRenderer that sags when slack, thins when stretched and flashes near the limit.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class PaintStrand : MonoBehaviour
    {
        public ToyController toyA;
        public ToyController toyB;

        [Header("Length")]
        public float maxLength = 4.5f;

        [Header("Tug")]
        public float tugImpulse = 8f;
        [Tooltip("Adds some upward pull so a tugged toy lifts over small bumps")]
        public float tugLift = 0.35f;
        public float tugCooldown = 0.6f;

        [Header("Snap and rejoin")]
        [Tooltip("How close the toys must be to rejoin after a snap")]
        public float rejoinDistance = 1.4f;
        [Tooltip("After rejoining, the strand cannot be snapped again for this long")]
        public float rejoinLockout = 5f;

        [Header("Look")]
        public float thickWidth = 0.22f;
        public float thinWidth = 0.05f;
        public float sag = 0.9f;
        public int segments = 20;

        public bool IsSnapped { get; private set; }
        /// <summary>0 when the toys are together, 1 at full stretch.</summary>
        public float Stretch01 { get; private set; }

        DistanceJoint2D joint;
        LineRenderer line;
        Material ownedMaterial;
        Gradient gradient;
        readonly GradientColorKey[] colorKeys = new GradientColorKey[2];
        GradientAlphaKey[] alphaKeys;

        float nextTugTime;
        float noSnapUntil;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = segments + 1;
            line.numCapVertices = 4;
            line.numCornerVertices = 2;
            line.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
            line.sortingOrder = 5;
            ownedMaterial = CreateUnlitMaterial();
            line.sharedMaterial = ownedMaterial;

            gradient = new Gradient();
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) };

            // Rigidbodies are read directly because the toys' own Awake may not have run yet.
            joint = toyA.gameObject.AddComponent<DistanceJoint2D>();
            joint.connectedBody = toyB.GetComponent<Rigidbody2D>();
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = Vector2.zero;
            joint.connectedAnchor = Vector2.zero;
            joint.autoConfigureDistance = false;
            joint.distance = maxLength;
            joint.maxDistanceOnly = true;
            joint.enableCollision = true;
        }

        void OnDestroy()
        {
            // Materials made in code are not cleaned up automatically.
            if (ownedMaterial != null) Destroy(ownedMaterial);
        }

        static Material CreateUnlitMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            return new Material(shader) { name = "Paint Strand (runtime)" };
        }

        public ToyController Other(ToyController toy) => toy == toyA ? toyB : toyA;

        public float Distance => Vector2.Distance(toyA.transform.position, toyB.transform.position);

        void LateUpdate()
        {
            if (IsSnapped) return;

            Vector2 a = toyA.transform.position;
            Vector2 b = toyB.transform.position;
            Stretch01 = Mathf.Clamp01(Vector2.Distance(a, b) / maxLength);

            // Slack strands droop in the middle, tight strands go straight.
            float slack = 1f - Stretch01;
            Vector2 control = (a + b) * 0.5f + Vector2.down * sag * slack;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector2 p = QuadraticBezier(a, control, b, t);
                line.SetPosition(i, new Vector3(p.x, p.y, 0f));
            }

            line.widthMultiplier = Mathf.Lerp(thickWidth, thinWidth, Stretch01 * Stretch01);

            // Colour fades from one toy's paint to the other's, and flashes white near the limit.
            float flash = Stretch01 > 0.95f ? Mathf.Sin(Time.time * 30f) * 0.5f + 0.5f : 0f;
            colorKeys[0] = new GradientColorKey(Color.Lerp(toyA.paintColor, Color.white, flash), 0f);
            colorKeys[1] = new GradientColorKey(Color.Lerp(toyB.paintColor, Color.white, flash), 1f);
            gradient.SetKeys(colorKeys, alphaKeys);
            line.colorGradient = gradient;
        }

        static Vector2 QuadraticBezier(Vector2 p0, Vector2 c, Vector2 p1, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * c + t * t * p1;
        }

        /// <summary>Yanks the other toy toward the one who pressed the button.</summary>
        public void Tug(ToyController from)
        {
            if (IsSnapped || Time.time < nextTugTime) return;
            nextTugTime = Time.time + tugCooldown;

            var other = Other(from);
            Vector2 dir = (Vector2)from.transform.position - (Vector2)other.transform.position;
            if (dir.sqrMagnitude < 0.001f) return;
            dir = dir.normalized;
            dir.y += tugLift;

            // A toy that is holding on is much heavier, so it barely moves. That is the counter-play.
            other.Body.AddForce(dir.normalized * tugImpulse, ForceMode2D.Impulse);
        }

        public bool TrySnap(ToyController by)
        {
            if (IsSnapped) return false;
            if (Time.time < noSnapUntil)
            {
                GameEvents.RaiseNotice($"The strand is still drying. Wait {Mathf.CeilToInt(noSnapUntil - Time.time)}s");
                return false;
            }

            IsSnapped = true;
            joint.enabled = false;
            line.enabled = false;
            GameEvents.RaiseStrandChanged(true);
            GameEvents.RaiseNotice($"Player {by.playerIndex + 1} snapped the strand! Everyone is solo");
            return true;
        }

        public bool TryRejoin(ToyController by)
        {
            if (!IsSnapped) return false;
            if (Distance > rejoinDistance)
            {
                GameEvents.RaiseNotice("Touch your partner to rejoin the strand");
                return false;
            }

            IsSnapped = false;
            joint.enabled = true;
            line.enabled = true;
            noSnapUntil = Time.time + rejoinLockout;
            GameEvents.RaiseStrandChanged(false);
            GameEvents.RaiseNotice("Rejoined!");
            return true;
        }
    }
}
