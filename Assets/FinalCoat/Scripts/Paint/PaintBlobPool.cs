using System.Collections.Generic;
using UnityEngine;

namespace FinalCoat
{
    /// <summary>
    /// Object pool for dropped paint blobs. Blobs are created once and reused,
    /// instead of being created and destroyed every time a toy gets hit.
    /// </summary>
    public class PaintBlobPool : MonoBehaviour
    {
        [Tooltip("An inactive blob in the scene used as the pattern for new blobs")]
        public PaintBlob template;
        public int prewarm = 24;

        [Header("Spray")]
        public float sprayUpSpeed = 7f;
        public float spraySideSpeed = 4f;

        readonly Stack<PaintBlob> available = new Stack<PaintBlob>();

        public int CreatedCount { get; private set; }
        public int ActiveCount { get; private set; }

        void Start()
        {
            for (int i = 0; i < prewarm; i++)
                available.Push(CreateNew());
        }

        PaintBlob CreateNew()
        {
            var blob = Instantiate(template, transform);
            blob.gameObject.SetActive(false);
            blob.name = $"Dropped Blob {CreatedCount}";
            CreatedCount++;
            return blob;
        }

        PaintBlob Get()
        {
            ActiveCount++;
            return available.Count > 0 ? available.Pop() : CreateNew();
        }

        public void Release(PaintBlob blob)
        {
            blob.gameObject.SetActive(false);
            available.Push(blob);
            ActiveCount = Mathf.Max(0, ActiveCount - 1);
        }

        /// <summary>Splits an amount of paint into blobs and throws them out of a point, in the colour of the toy that lost it.</summary>
        public void Spray(Vector2 origin, int totalPaint, int paintPerBlob, Color color)
        {
            int remaining = totalPaint;
            while (remaining > 0)
            {
                int amount = Mathf.Min(paintPerBlob, remaining);
                remaining -= amount;

                Vector2 velocity = new Vector2(
                    Random.Range(-spraySideSpeed, spraySideSpeed),
                    Random.Range(sprayUpSpeed * 0.6f, sprayUpSpeed));

                Get().Launch(this, origin + Vector2.up * 0.4f, velocity, amount, color);
            }
        }
    }
}
