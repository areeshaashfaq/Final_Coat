using UnityEngine;

namespace FinalCoat{
    public class PrototypeHUD : MonoBehaviour{
        public bool showControls = true;
        public float noticeDuration = 3f;
        string notice;
        float noticeUntil;
        bool strandSnapped;
        Texture2D pixel;
        GUIStyle title, body, centered, banner;
        void OnEnable(){
            GameEvents.Notice += OnNotice;
            GameEvents.StrandChanged += OnStrandChanged;
        }

        void OnDisable(){
            GameEvents.Notice -= OnNotice;
            GameEvents.StrandChanged -= OnStrandChanged;
        }

        void OnDestroy(){
            if (pixel != null) Destroy(pixel);
        }

        void OnNotice(string message){
            notice = message;
            noticeUntil = Time.unscaledTime + noticeDuration;
        }

        void OnStrandChanged(bool snapped) => strandSnapped = snapped;
        void Update(){
            if (ToyInput.HelpPressed) showControls = !showControls;
        }

        void EnsureStyles(){
            if (pixel == null){
                pixel = new Texture2D(1, 1);
                pixel.SetPixel(0, 0, Color.white);
                pixel.Apply();
            }
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            title.normal.textColor = Color.white;
            body = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            body.normal.textColor = Color.white;
            centered = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter, fontSize = 15, fontStyle = FontStyle.Bold };
            banner = new GUIStyle(centered) { fontSize = 30 };
        }

        void Fill(Rect r, Color c){
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, pixel);
            GUI.color = old;
        }

        void OnGUI(){
            var gm = GameManager.Instance;
            if (gm == null || gm.toys == null) return;
            EnsureStyles();
            for (int i = 0; i < gm.toys.Length; i++)
                DrawScorePanel(gm, i);
            string strandText = strandSnapped
                ? "STRAND SNAPPED: everyone is solo (bigger rewards, bigger losses)"
                : "Joined by the paint strand";
            var strandRect = new Rect(Screen.width * 0.5f - 230f, 16f, 460f, 30f);
            Fill(strandRect, strandSnapped ? new Color(0.75f, 0.25f, 0.3f, 0.85f) : new Color(0f, 0f, 0f, 0.55f));
            GUI.Label(strandRect, strandText, centered);
            if (!string.IsNullOrEmpty(notice) && Time.unscaledTime < noticeUntil){
                var r = new Rect(Screen.width * 0.5f - 260f, 56f, 520f, 30f);
                Fill(r, new Color(0f, 0f, 0f, 0.6f));
                GUI.Label(r, notice, centered);
            }
            if (showControls) DrawControls();
            if (gm.IsPaused) DrawOverlay("PAUSED", "Esc  resume     R  restart");
            if (gm.IsLevelComplete) DrawLevelComplete(gm);
        }

        void DrawScorePanel(GameManager gm, int i){
            var toy = gm.toys[i];
            float w = 250f, h = 74f;
            float x = i == 0 ? 16f : Screen.width - w - 16f;
            var panel = new Rect(x, 16f, w, h);
            Fill(panel, new Color(0f, 0f, 0f, 0.55f));
            int score = gm.GetScore(i);
            int stage = gm.GetStage(i);
            GUI.Label(new Rect(x + 10f, 20f, w - 20f, 22f), $"Player {i + 1}: {score} paint", title);
            var thresholds = gm.stageThresholds;
            float progress = 1f;
            if (stage < thresholds.Length - 1){
                float from = thresholds[stage], to = thresholds[stage + 1];
                progress = Mathf.InverseLerp(from, to, score);
            }
            var bar = new Rect(x + 10f, 46f, w - 20f, 12f);
            Fill(bar, new Color(1f, 1f, 1f, 0.2f));
            Fill(new Rect(bar.x, bar.y, bar.width * progress, bar.height), toy.paintColor);
            GUI.Label(new Rect(x + 10f, 60f, w - 20f, 20f), $"Paint stage {stage + 1} of {thresholds.Length}", body);
            if (toy.SnapCharge > 0.05f){
                var charge = new Rect(x + 10f, 16f + h + 4f, (w - 20f) * toy.SnapCharge, 6f);
                Fill(charge, Color.white);
            }
        }

        void DrawControls(){
            string text =
                "P1: A / D move,  W jump,  S hold on,  Left Shift strand\n" +
                "P2: Arrows move,  Up jump,  Down hold on,  Right Shift strand\n" +
                "Strand button: tap = tug, hold 1s = snap, tap near partner = rejoin.   Esc pause,  F1 hide help";
            var r = new Rect(16f, Screen.height - 76f, Screen.width - 32f, 62f);
            Fill(r, new Color(0f, 0f, 0f, 0.5f));
            GUI.Label(new Rect(r.x + 10f, r.y + 6f, r.width - 20f, r.height - 12f), text, body);
        }

        void DrawOverlay(string heading, string sub){
            Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.5f));
            GUI.Label(new Rect(0f, Screen.height * 0.5f - 50f, Screen.width, 50f), heading, banner);
            GUI.Label(new Rect(0f, Screen.height * 0.5f + 5f, Screen.width, 30f), sub, centered);
        }

        void DrawLevelComplete(GameManager gm){
            int leader = gm.LeaderIndex();
            string result = leader < 0
                ? "It's a tie! Nobody gets the final coat... yet"
                : $"Player {leader + 1} is the most painted toy with {gm.GetScore(leader)} paint!";
            DrawOverlay("LEVEL COMPLETE", result + "\nPress R to play again");
        }
    }
}
