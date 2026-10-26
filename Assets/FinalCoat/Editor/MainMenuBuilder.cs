#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace FinalCoat.EditorTools
{
    /// <summary>
    /// Menu: Final Coat > Build Main Menu Scene
    /// Builds the home screen (Play, Settings, Credits, Quit) out of standard Unity UI
    /// objects, wires the MainMenu script and puts the scene first in the build.
    /// Everything is a plain Unity UI element, so it can be restyled in the Inspector later.
    /// </summary>
    public static class MainMenuBuilder
    {
        const string ScenePath = "Assets/FinalCoat/Scenes/MainMenu.unity";

        static readonly Color Background = new Color(0.95f, 0.91f, 0.84f);
        static readonly Color Ink = new Color(0.2f, 0.18f, 0.22f);
        static readonly Color ButtonNormal = new Color(0.24f, 0.21f, 0.28f);
        static readonly Color ButtonActive = new Color(0.96f, 0.55f, 0.25f);
        static readonly Color ButtonPressed = new Color(0.82f, 0.42f, 0.18f);
        static readonly Color Player1 = new Color(0.93f, 0.3f, 0.3f);
        static readonly Color Player2 = new Color(0.25f, 0.5f, 0.95f);

        static Font font;
        static Sprite roundedSprite;
        static Sprite circleSprite;

        [MenuItem("Final Coat/Build Main Menu Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog(
                    "Rebuild the main menu?",
                    "This replaces " + ScenePath + ". Anything changed by hand in that scene will be lost.",
                    "Rebuild", "Cancel"))
                return;

            EnsureFolder("Assets/FinalCoat/Scenes");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            MakeCamera();
            var eventSystem = MakeEventSystem();
            var canvas = MakeCanvas();

            var menuGo = new GameObject("Main Menu");
            var menu = menuGo.AddComponent<MainMenu>();
            menu.eventSystem = eventSystem;

            // ---------- Home ----------
            var home = MakePanel(canvas, "Home", new Vector2(900f, 940f));
            MakeText(home, "Title", "FINAL COAT", 120, Ink, FontStyle.Bold, 140f);
            MakeText(home, "Tagline", "Unfinished toys, one strand of wet paint, and only one final coat.", 28, Ink, FontStyle.Italic, 50f);
            MakeToyDecoration(home);
            MakeSpacer(home, 10f);
            menu.playButton = MakeButton(home, "Play");
            menu.settingsButton = MakeButton(home, "Settings");
            menu.creditsButton = MakeButton(home, "Credits");
            menu.quitButton = MakeButton(home, "Quit");
            MakeText(home, "Hint", "Mouse, keyboard (arrows + Enter) or controller (stick + A)", 22, Ink, FontStyle.Normal, 40f);

            // ---------- Settings ----------
            var settings = MakePanel(canvas, "Settings", new Vector2(1000f, 1000f));
            MakeText(settings, "Heading", "Settings", 64, Ink, FontStyle.Bold, 90f);
            MakeText(settings, "Players Heading", "Who plays which toy", 32, Ink, FontStyle.Bold, 46f);
            MakeText(settings, "Players Hint", "Press a player to change their device. Picking a device the other player has swaps you.", 22, Ink, FontStyle.Normal, 56f, 900f);
            menu.player1Button = MakeButton(settings, "Player 1", 860f, 70f, 28, Player1);
            menu.player2Button = MakeButton(settings, "Player 2", 860f, 70f, 28, Player2);
            menu.swapButton = MakeButton(settings, "Swap players", 860f, 60f, 26);
            menu.controllerStatus = MakeText(settings, "Controller Status", "Controllers connected: 0", 22, Ink, FontStyle.Italic, 40f);
            MakeSpacer(settings, 10f);
            MakeText(settings, "Game Heading", "Game", 32, Ink, FontStyle.Bold, 46f);
            menu.enemyLabelsButton = MakeButton(settings, "Show enemy AI states", 860f, 60f, 26);
            menu.controlsHelpButton = MakeButton(settings, "Show controls in game", 860f, 60f, 26);
            menu.fullscreenButton = MakeButton(settings, "Fullscreen", 860f, 60f, 26);
            MakeSpacer(settings, 10f);
            menu.settingsBackButton = MakeButton(settings, "Back");

            // ---------- Credits ----------
            var credits = MakePanel(canvas, "Credits", new Vector2(1000f, 800f));
            MakeText(credits, "Heading", "Credits", 64, Ink, FontStyle.Bold, 90f);
            MakeText(credits, "Team",
                "CS 319, Group 7\n\nAreesha Ashfaq\nMustafa Farhan Qureshi\nSyed Farooq Ahmed",
                30, Ink, FontStyle.Normal, 220f);
            MakeText(credits, "Licenses",
                "Licenses\n\nMade with Unity.\nThis prototype uses no outside assets: every shape is drawn by code.\nFinal art and audio will be listed here with their authors and licenses.",
                24, Ink, FontStyle.Normal, 200f, 900f);
            menu.creditsBackButton = MakeButton(credits, "Back");

            menu.homePanel = home.gameObject;
            menu.settingsPanel = settings.gameObject;
            menu.creditsPanel = credits.gameObject;
            settings.gameObject.SetActive(false);
            credits.gameObject.SetActive(false);

            EditorSceneManager.SaveScene(scene, ScenePath);
            PutFirstInBuild(ScenePath);
            Selection.activeGameObject = menuGo;
            Debug.Log("Final Coat: main menu built at " + ScenePath + ". Press Play.");
        }

        // ---------------- Scene pieces ----------------

        static void MakeCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0f, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
            go.AddComponent<AudioListener>();
        }

        static EventSystem MakeEventSystem()
        {
            var go = new GameObject("EventSystem");
            var es = go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            // Handles mouse, keyboard and controller navigation with the Input System's default UI actions.
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            return es;
        }

        static Transform MakeCanvas()
        {
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            var bg = UI("Background", go.transform);
            bg.anchorMin = Vector2.zero;
            bg.anchorMax = Vector2.one;
            bg.offsetMin = Vector2.zero;
            bg.offsetMax = Vector2.zero;
            var img = bg.gameObject.AddComponent<Image>();
            img.color = Background;
            img.raycastTarget = false;
            return go.transform;
        }

        static RectTransform MakePanel(Transform parent, string name, Vector2 size)
        {
            var rt = UI(name, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;

            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return rt;
        }

        // Two blank toys joined by a paint strand, made from plain UI shapes.
        static void MakeToyDecoration(Transform parent)
        {
            var holder = UI("Toys", parent);
            var le = holder.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 150f;
            le.preferredWidth = 400f;

            var strand = UI("Strand", holder);
            strand.sizeDelta = new Vector2(150f, 10f);
            strand.anchoredPosition = new Vector2(0f, -10f);
            var gradientImage = strand.gameObject.AddComponent<Image>();
            gradientImage.color = Color.Lerp(Player1, Player2, 0.5f);
            gradientImage.raycastTarget = false;

            MakeMenuToy(holder, new Vector2(-110f, 0f), Player1, 1);
            MakeMenuToy(holder, new Vector2(110f, 0f), Player2, -1);
        }

        static void MakeMenuToy(Transform parent, Vector2 position, Color dot, int facing)
        {
            Shape(parent, "Outline", position, new Vector2(98f, 108f), Ink, null);
            Shape(parent, "Body", position, new Vector2(90f, 100f), new Color(0.97f, 0.97f, 0.95f), null);
            Shape(parent, "Eye", position + new Vector2(8f * facing, 16f), new Vector2(14f, 20f), Ink, circleSprite);
            Shape(parent, "Eye", position + new Vector2(30f * facing, 16f), new Vector2(14f, 20f), Ink, circleSprite);
            Shape(parent, "Dot", position + new Vector2(0f, 64f), new Vector2(22f, 22f), dot, circleSprite);
        }

        static void Shape(Transform parent, string name, Vector2 position, Vector2 size, Color color, Sprite sprite)
        {
            var rt = UI(name, parent);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
        }

        static Text MakeText(Transform parent, string name, string content, int size, Color color, FontStyle style, float height, float width = 820f)
        {
            var rt = UI(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.preferredWidth = width;
            return text;
        }

        static Button MakeButton(Transform parent, string label, float width = 520f, float height = 76f, int fontSize = 34, Color? accent = null)
        {
            var rt = UI(label + " Button", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = roundedSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = ButtonNormal;
            colors.highlightedColor = ButtonActive;
            colors.selectedColor = ButtonActive; // what a controller or keyboard user sees
            colors.pressedColor = ButtonPressed;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;

            // Coloured stripe on the left for player rows
            if (accent.HasValue)
            {
                var stripe = UI("Player Colour", rt);
                stripe.anchorMin = new Vector2(0f, 0f);
                stripe.anchorMax = new Vector2(0f, 1f);
                stripe.pivot = new Vector2(0f, 0.5f);
                stripe.sizeDelta = new Vector2(18f, -14f);
                stripe.anchoredPosition = new Vector2(10f, 0f);
                var s = stripe.gameObject.AddComponent<Image>();
                s.color = accent.Value;
                s.raycastTarget = false;
            }

            var text = MakeText(rt, "Label", label, fontSize, Color.white, FontStyle.Bold, height, width);
            var trt = text.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(accent.HasValue ? 36f : 12f, 0f);
            trt.offsetMax = new Vector2(-12f, 0f);
            return button;
        }

        static void MakeSpacer(Transform parent, float height)
        {
            var rt = UI("Spacer", parent);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        static RectTransform UI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // The menu must be scene 0 so the built game opens on it.
        static void PutFirstInBuild(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
