using UnityEngine;

namespace FinalCoat
{
    /// <summary>
    /// Saved player settings (PlayerPrefs, so they survive restarts).
    /// Each toy is assigned a device. Two toys can never share a device:
    /// picking one the other toy uses swaps them.
    /// </summary>
    public static class GameSettings
    {
        public const int PlayerCount = 2;
        public const int SchemeCount = 4;

        const string ControlsKey = "FinalCoat.Controls.P";
        const string EnemyLabelsKey = "FinalCoat.ShowEnemyStates";
        const string HelpKey = "FinalCoat.ShowControlsHelp";

        public static readonly string[] PlayerNames = { "Player 1 (red toy)", "Player 2 (blue toy)" };

        public static ControlScheme GetControls(int player)
        {
            int fallback = player == 0 ? (int)ControlScheme.WASD : (int)ControlScheme.Arrows;
            int value = PlayerPrefs.GetInt(ControlsKey + player, fallback);
            return (ControlScheme)Mathf.Clamp(value, 0, SchemeCount - 1);
        }

        public static void SetControls(int player, ControlScheme scheme)
        {
            ControlScheme previous = GetControls(player);
            for (int other = 0; other < PlayerCount; other++)
                if (other != player && GetControls(other) == scheme)
                    PlayerPrefs.SetInt(ControlsKey + other, (int)previous);

            PlayerPrefs.SetInt(ControlsKey + player, (int)scheme);
            PlayerPrefs.Save();
        }

        /// <summary>Moves a player on to the next device in the list (used by the Settings buttons).</summary>
        public static void CycleControls(int player)
        {
            var next = (ControlScheme)(((int)GetControls(player) + 1) % SchemeCount);
            SetControls(player, next);
        }

        public static void SwapPlayers()
        {
            var a = GetControls(0);
            var b = GetControls(1);
            PlayerPrefs.SetInt(ControlsKey + 0, (int)b);
            PlayerPrefs.SetInt(ControlsKey + 1, (int)a);
            PlayerPrefs.Save();
        }

        public static bool ShowEnemyStates
        {
            get => PlayerPrefs.GetInt(EnemyLabelsKey, 1) == 1;
            set { PlayerPrefs.SetInt(EnemyLabelsKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool ShowControlsHelp
        {
            get => PlayerPrefs.GetInt(HelpKey, 1) == 1;
            set { PlayerPrefs.SetInt(HelpKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static string DeviceName(ControlScheme scheme)
        {
            switch (scheme)
            {
                case ControlScheme.WASD: return "Keyboard (W A S D)";
                case ControlScheme.Arrows: return "Keyboard (arrow keys)";
                case ControlScheme.Gamepad1: return "Controller 1";
                default: return "Controller 2";
            }
        }

        public static string ControlsHelp(ControlScheme scheme)
        {
            switch (scheme)
            {
                case ControlScheme.WASD: return "A / D move,  W jump,  S hold on,  Left Shift strand";
                case ControlScheme.Arrows: return "Arrows move,  Up jump,  Down hold on,  Right Shift strand";
                default: return "Stick or d-pad move,  A jump,  B hold on,  X strand";
            }
        }
    }
}
