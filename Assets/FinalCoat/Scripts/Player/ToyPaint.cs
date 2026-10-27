using UnityEngine;

namespace FinalCoat{
    public class ToyPaint : MonoBehaviour{
        public ToyController toy;
        public SpriteRenderer body;
        public Color blankColor = new Color(0.97f, 0.97f, 0.95f);
        public float fadeSpeed = 4f;
        Color targetColor;
        void Awake(){
            if (toy == null) toy = GetComponentInParent<ToyController>();
            targetColor = blankColor;
            if (body != null) body.color = blankColor;
        }

        void OnEnable() => GameEvents.ScoreChanged += OnScoreChanged;
        void OnDisable() => GameEvents.ScoreChanged -= OnScoreChanged;
        void OnScoreChanged(int player, int score){
            var gm = GameManager.Instance;
            if (toy == null || gm == null || player != toy.playerIndex) return;
            int stage = gm.GetStage(player);
            float t = stage / (float)Mathf.Max(1, gm.StageCount - 1);
            targetColor = Color.Lerp(blankColor, toy.paintColor, t);
        }

        void Update(){
            if (body != null)
                body.color = Color.Lerp(body.color, targetColor, Time.deltaTime * fadeSpeed);
        }
    }
}
