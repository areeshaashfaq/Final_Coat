using System;

namespace FinalCoat{
    public static class GameEvents{
        public static event Action<int, int> ScoreChanged;
        public static event Action<bool> StrandChanged;
        public static event Action<string> Notice;
        public static event Action LevelCompleted;
        public static void RaiseScoreChanged(int player, int score) => ScoreChanged?.Invoke(player, score);
        public static void RaiseStrandChanged(bool snapped) => StrandChanged?.Invoke(snapped);
        public static void RaiseNotice(string message) => Notice?.Invoke(message);
        public static void RaiseLevelCompleted() => LevelCompleted?.Invoke();
        public static void ClearAll(){
            ScoreChanged = null;
            StrandChanged = null;
            Notice = null;
            LevelCompleted = null;
        }
    }
}
