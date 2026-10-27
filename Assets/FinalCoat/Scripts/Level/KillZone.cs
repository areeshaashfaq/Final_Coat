using UnityEngine;

namespace FinalCoat
{
    /// <summary>Invisible strip below the level. Anything that falls in is reset.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            var toy = other.GetComponentInParent<ToyController>();
            if (toy != null)
            {
                if (GameManager.Instance != null) GameManager.Instance.OnToyFell(toy);
                return;
            }

            var enemy = other.GetComponentInParent<WindUpToy>();
            if (enemy != null) { enemy.ResetToHome(); return; }

            var prop = other.GetComponentInParent<Respawnable>();
            if (prop != null) prop.ResetToStart();
        }
    }
}
