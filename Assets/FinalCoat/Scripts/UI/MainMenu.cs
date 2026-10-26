using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FinalCoat
{
    /// <summary>
    /// Home screen with Play, Settings, Credits and Quit.
    /// Works with mouse, keyboard (arrows + Enter) and controller (stick + A, B to go back).
    ///
    /// Settings: each toy is given a device. Pressing a player's row moves it to the next
    /// device. If the other toy already uses that device, the two swap.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public string gameScene = "Prototype";
        public EventSystem eventSystem;

        [Header("Screens")]
        public GameObject homePanel;
        public GameObject settingsPanel;
        public GameObject creditsPanel;

        [Header("Home")]
        public Button playButton;
        public Button settingsButton;
        public Button creditsButton;
        public Button quitButton;

        [Header("Settings")]
        public Button player1Button;
        public Button player2Button;
        public Button swapButton;
        public Button enemyLabelsButton;
        public Button controlsHelpButton;
        public Button fullscreenButton;
        public Button settingsBackButton;
        public Text controllerStatus;

        [Header("Credits")]
        public Button creditsBackButton;

        GameObject currentPanel;
        Selectable currentFirst;
        int lastPadCount = -1;

        void Awake()
        {
            Time.timeScale = 1f;
            EnsureUIActions();

            playButton.onClick.AddListener(Play);
            settingsButton.onClick.AddListener(() => Show(settingsPanel, player1Button));
            creditsButton.onClick.AddListener(() => Show(creditsPanel, creditsBackButton));
            quitButton.onClick.AddListener(Quit);

            player1Button.onClick.AddListener(() => { GameSettings.CycleControls(0); Refresh(); });
            player2Button.onClick.AddListener(() => { GameSettings.CycleControls(1); Refresh(); });
            swapButton.onClick.AddListener(() => { GameSettings.SwapPlayers(); Refresh(); });
            enemyLabelsButton.onClick.AddListener(() => { GameSettings.ShowEnemyStates = !GameSettings.ShowEnemyStates; Refresh(); });
            controlsHelpButton.onClick.AddListener(() => { GameSettings.ShowControlsHelp = !GameSettings.ShowControlsHelp; Refresh(); });
            fullscreenButton.onClick.AddListener(() => { Screen.fullScreen = !Screen.fullScreen; Invoke(nameof(Refresh), 0.1f); });
            settingsBackButton.onClick.AddListener(ShowHome);
            creditsBackButton.onClick.AddListener(ShowHome);
        }

        // The UI input module needs its default actions for keyboard and controller navigation.
        // Re-assign them at runtime in case the saved scene lost the reference.
        void EnsureUIActions()
        {
#if ENABLE_INPUT_SYSTEM
            if (eventSystem == null) return;
            var module = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (module != null && module.actionsAsset == null) module.AssignDefaultActions();
#endif
        }

        void Start()
        {
            Refresh();
            ShowHome();
        }

        void Update()
        {
            // B / Esc goes back from Settings or Credits.
            if (currentPanel != homePanel && ToyInput.BackPressed) ShowHome();

            // If the mouse clicked empty space, nothing is selected and a controller
            // could not move. Put the selection back on the screen's first button.
            var es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject == null && currentFirst != null)
                es.SetSelectedGameObject(currentFirst.gameObject);

            // Controllers plugged in or out while the menu is open.
            if (ToyInput.ConnectedGamepads != lastPadCount) Refresh();
        }

        public void ShowHome() => Show(homePanel, playButton);

        void Show(GameObject panel, Selectable first)
        {
            homePanel.SetActive(panel == homePanel);
            settingsPanel.SetActive(panel == settingsPanel);
            creditsPanel.SetActive(panel == creditsPanel);
            currentPanel = panel;
            currentFirst = first;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        void Refresh()
        {
            lastPadCount = ToyInput.ConnectedGamepads;
            SetLabel(player1Button, PlayerRow(0));
            SetLabel(player2Button, PlayerRow(1));
            SetLabel(enemyLabelsButton, "Show enemy AI states:  " + OnOff(GameSettings.ShowEnemyStates));
            SetLabel(controlsHelpButton, "Show controls in game:  " + OnOff(GameSettings.ShowControlsHelp));
            SetLabel(fullscreenButton, "Fullscreen:  " + OnOff(Screen.fullScreen));

            if (controllerStatus != null)
            {
                controllerStatus.text = lastPadCount == 0
                    ? "No controllers connected. Plug one in to see it here."
                    : $"Controllers connected: {lastPadCount}";
            }
        }

        static string PlayerRow(int player)
        {
            var scheme = GameSettings.GetControls(player);
            string row = $"{GameSettings.PlayerNames[player]}:   {GameSettings.DeviceName(scheme)}";
            if (!ToyInput.IsConnected(scheme)) row += "  (not connected)";
            return row;
        }

        static string OnOff(bool value) => value ? "On" : "Off";

        static void SetLabel(Button button, string text)
        {
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = text;
        }

        void Play()
        {
            if (!Application.CanStreamedLevelBeLoaded(gameScene))
            {
                Debug.LogWarning($"Scene '{gameScene}' is not in the build. Run Final Coat > Build Prototype Scene first.");
                return;
            }
            SceneManager.LoadScene(gameScene);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
