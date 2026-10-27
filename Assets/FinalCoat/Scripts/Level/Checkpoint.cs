using UnityEngine;

namespace FinalCoat{
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour{
        public SpriteRenderer flag;
        public Color inactiveColor = new Color(0.6f, 0.6f, 0.6f);
        public Color activeColor = new Color(0.3f, 0.8f, 0.4f);
        bool reached;
        void Awake(){
            if (flag != null) flag.color = inactiveColor;
        }

        void OnTriggerEnter2D(Collider2D other){
            if (reached || other.GetComponentInParent<ToyController>() == null) return;
            if (GameManager.Instance == null) return;
            reached = true;
            GameManager.Instance.SetCheckpoint(transform.position);
            if (flag != null) flag.color = activeColor;
        }
    }
}
