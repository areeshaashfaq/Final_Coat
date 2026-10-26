using System.Collections.Generic;
using UnityEngine;

namespace FinalCoat
{
    /// <summary>
    /// The level exit. The first toy to touch it gets a bonus. The level ends when
    /// every toy is inside, but only while the strand is joined.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        public SpriteRenderer door;
        public Color lockedColor = new Color(0.55f, 0.45f, 0.4f);
        public Color openColor = new Color(0.45f, 0.8f, 0.5f);

        readonly HashSet<ToyController> inside = new HashSet<ToyController>();
        bool bonusGiven;
        float lastHintTime = -10f;

        void OnTriggerEnter2D(Collider2D other)
        {
            var toy = other.GetComponentInParent<ToyController>();
            var gm = GameManager.Instance;
            if (toy == null || gm == null) return;

            inside.Add(toy);
            if (!bonusGiven)
            {
                bonusGiven = true;
                gm.AddPaint(toy, gm.firstToExitBonus);
                GameEvents.RaiseNotice($"Player {toy.playerIndex + 1} reached the exit first: +{gm.firstToExitBonus} paint");
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var toy = other.GetComponentInParent<ToyController>();
            if (toy != null) inside.Remove(toy);
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.IsLevelComplete) return;

            if (door != null) door.color = gm.IsSolo ? lockedColor : openColor;
            if (inside.Count < gm.toys.Length) return;

            if (gm.IsSolo)
            {
                if (Time.time - lastHintTime > 2.5f)
                {
                    lastHintTime = Time.time;
                    GameEvents.RaiseNotice("The exit only opens for joined toys. Rejoin the strand!");
                }
                return;
            }

            gm.CompleteLevel();
        }
    }
}
