using System;

namespace FinalCoat
{
    /// <summary>
    /// Observer pattern. Systems announce what happened here and anything that cares
    /// listens, so the score, the HUD and the toy colours never need direct references
    /// to each other.
    ///
    /// Memory rule: every listener unsubscribes in OnDisable. Static events outlive
    /// scenes, so a listener that forgets would keep a destroyed object alive.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>(player index, new score)</summary>
        public static event Action<int, int> ScoreChanged;

        /// <summary>true when the strand snaps, false when the toys rejoin</summary>
        public static event Action<bool> StrandChanged;

        /// <summary>A short message for the HUD</summary>
        public static event Action<string> Notice;

        public static event Action LevelCompleted;

        public static void RaiseScoreChanged(int player, int score) => ScoreChanged?.Invoke(player, score);
        public static void RaiseStrandChanged(bool snapped) => StrandChanged?.Invoke(snapped);
        public static void RaiseNotice(string message) => Notice?.Invoke(message);
        public static void RaiseLevelCompleted() => LevelCompleted?.Invoke();

        /// <summary>Safety net called when a level loads, so no listener from an old scene survives.</summary>
        public static void ClearAll()
        {
            ScoreChanged = null;
            StrandChanged = null;
            Notice = null;
            LevelCompleted = null;
        }
    }
}
