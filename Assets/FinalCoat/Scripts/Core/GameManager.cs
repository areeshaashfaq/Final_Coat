using UnityEngine;
using UnityEngine.SceneManagement;

namespace FinalCoat
{
    /// <summary>
    /// Singleton that owns the paint scores, the checkpoint and the level state.
    /// Every paint rule from the GDD lives here so the numbers can be tuned in one place.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene references")]
        public ToyController[] toys;
        public PaintStrand strand;
        public Transform startPoint;

        [Header("Paint rules (starting values, tune after playtests)")]
        public int hitLossJoined = 20;
        public int hitLossSolo = 40;
        public int fallLossJoined = 10;
        public int fallLossSolo = 30;
        public int firstToExitBonus = 25;

        [Tooltip("Paint needed to reach each of the 4 colour stages")]
        public int[] stageThresholds = { 0, 40, 100, 180 };

        int[] scores;
        Vector2 checkpoint;
        float lastTeamFallTime = -10f;

        public bool IsPaused { get; private set; }
        public bool IsLevelComplete { get; private set; }
        public bool InputLocked => IsPaused || IsLevelComplete;

        /// <summary>True when the strand is snapped (or there is no strand), so solo rules apply.</summary>
        public bool IsSolo => strand == null || strand.IsSnapped;

        public int StageCount => stageThresholds.Length;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            GameEvents.ClearAll();
            Time.timeScale = 1f;
            scores = new int[toys.Length];
            checkpoint = startPoint != null ? (Vector2)startPoint.position : Vector2.zero;
        }

        void Start()
        {
            for (int i = 0; i < toys.Length; i++)
                GameEvents.RaiseScoreChanged(i, 0);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (ToyInput.PausePressed && !IsLevelComplete) SetPaused(!IsPaused);
            if (ToyInput.RestartPressed && (IsPaused || IsLevelComplete)) Restart();
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else SceneManager.LoadScene(scene.name);
        }

        // ---------------- Scores ----------------

        public int GetScore(int player) => scores[player];

        public int GetStage(int player)
        {
            int stage = 0;
            for (int i = 0; i < stageThresholds.Length; i++)
                if (scores[player] >= stageThresholds[i]) stage = i;
            return stage;
        }

        public void AddPaint(ToyController toy, int amount)
        {
            if (amount <= 0 || IsLevelComplete) return;
            scores[toy.playerIndex] += amount;
            GameEvents.RaiseScoreChanged(toy.playerIndex, scores[toy.playerIndex]);
        }

        /// <summary>Removes paint and returns how much was actually lost. Scores never go below zero.</summary>
        public int RemovePaint(ToyController toy, int amount)
        {
            int lost = Mathf.Min(amount, scores[toy.playerIndex]);
            if (lost <= 0) return 0;
            scores[toy.playerIndex] -= lost;
            GameEvents.RaiseScoreChanged(toy.playerIndex, scores[toy.playerIndex]);
            return lost;
        }

        /// <summary>Index of the player with the most paint, or -1 on a tie.</summary>
        public int LeaderIndex()
        {
            int best = -1, bestScore = -1;
            bool tie = false;
            for (int i = 0; i < scores.Length; i++)
            {
                if (scores[i] > bestScore) { best = i; bestScore = scores[i]; tie = false; }
                else if (scores[i] == bestScore) tie = true;
            }
            return tie ? -1 : best;
        }

        /// <summary>Used later by the vacuum boss, which always targets the leader.</summary>
        public ToyController MostPaintedToy()
        {
            int leader = LeaderIndex();
            return leader >= 0 ? toys[leader] : null;
        }

        // ---------------- Things that happen in the level ----------------

        public void OnToyHit(ToyController toy)
        {
            int lost = RemovePaint(toy, IsSolo ? hitLossSolo : hitLossJoined);

            GameEvents.RaiseNotice(lost > 0
                ? $"Player {toy.playerIndex + 1} got hit and lost {lost} paint"
                : $"Player {toy.playerIndex + 1} got hit");
        }

        public void OnToyFell(ToyController toy)
        {
            if (IsLevelComplete) return;

            if (IsSolo)
            {
                int lost = RemovePaint(toy, fallLossSolo);
                Respawn(toy);
                GameEvents.RaiseNotice($"Player {toy.playerIndex + 1} fell while solo and lost {lost} paint");
                return;
            }

            // Joined toys fall as a team. Both may touch the kill zone in the same
            // physics step, so ignore the second report.
            if (Time.time - lastTeamFallTime < 0.5f) return;
            lastTeamFallTime = Time.time;

            foreach (var t in toys) RemovePaint(t, fallLossJoined);
            foreach (var t in toys) Respawn(t);
            GameEvents.RaiseNotice($"The team fell! Everyone loses {fallLossJoined} paint");
        }

        public void Respawn(ToyController toy)
        {
            Vector2 offset = new Vector2(toy.playerIndex * 1.2f - 0.6f, 0.5f);
            toy.Teleport(checkpoint + offset);
        }

        public void SetCheckpoint(Vector2 position)
        {
            if ((position - checkpoint).sqrMagnitude < 0.01f) return;
            checkpoint = position;
            GameEvents.RaiseNotice("Checkpoint reached");
        }

        public void CompleteLevel()
        {
            if (IsLevelComplete) return;
            IsLevelComplete = true;
            GameEvents.RaiseLevelCompleted();
        }
    }
}
