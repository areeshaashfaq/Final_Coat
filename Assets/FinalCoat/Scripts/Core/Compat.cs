using UnityEngine;

namespace FinalCoat
{
    /// <summary>
    /// Hides the small API differences between Unity 2022 and Unity 6,
    /// so the scripts compile on whichever version the team installed.
    /// </summary>
    public static class Compat
    {
        public static Vector2 GetVelocity(this Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetVelocity(this Rigidbody2D rb, Vector2 value)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = value;
#else
            rb.velocity = value;
#endif
        }
    }
}
