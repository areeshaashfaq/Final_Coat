#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FinalCoat.EditorTools{
    public static class PrototypeSceneBuilder{
        const string RootFolder = "Assets/FinalCoat";
        const string SpriteFolder = RootFolder + "/Placeholders";
        const string SceneFolder = RootFolder + "/Scenes";
        const string ScenePath = SceneFolder + "/Prototype.unity";
        static readonly Color Background = new Color(0.95f, 0.91f, 0.84f);
        static readonly Color WallPaper = new Color(0.89f, 0.84f, 0.92f);
        static readonly Color Floor = new Color(0.62f, 0.45f, 0.32f);
        static readonly Color Shelf = new Color(0.74f, 0.56f, 0.38f);
        static readonly Color Crate = new Color(0.86f, 0.66f, 0.36f);
        static readonly Color Ball = new Color(0.95f, 0.45f, 0.35f);
        static readonly Color Outline = new Color(0.2f, 0.18f, 0.22f);
        static readonly Color SmallBlob = new Color(0.2f, 0.75f, 0.85f);
        static readonly Color LargeBlob = new Color(0.98f, 0.78f, 0.15f);
        static readonly Color SoloPot = new Color(0.65f, 0.3f, 0.85f);
        static readonly Color Player1 = new Color(0.93f, 0.3f, 0.3f);
        static readonly Color Player2 = new Color(0.25f, 0.5f, 0.95f);
        static Sprite square;
        static Sprite circle;
        [MenuItem("Final Coat/Build Prototype Scene")]
        public static void Build(){
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog(
                    "Rebuild the prototype scene?",
                    "This replaces " + ScenePath + ". Anything changed by hand in that scene will be lost.",
                    "Rebuild", "Cancel"))
                return;
            EnsureFolder(SpriteFolder);
            EnsureFolder(SceneFolder);
            square = MakeSprite("Square", 32, false);
            circle = MakeSprite("Circle", 64, true);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var level = new GameObject("Level").transform;
            var props = new GameObject("Props").transform;
            var pickups = new GameObject("Paint").transform;
            var enemies = new GameObject("Enemies").transform;
            var players = new GameObject("Players").transform;
            BuildLevel(level);
            BuildProps(props);
            BuildPaint(pickups);
            var start = new GameObject("Start Point").transform;
            start.position = new Vector3(-6f, 1f, 0f);
            var p1 = MakeToy(0, new Vector2(-6.6f, 1f), Player1, ControlScheme.WASD, players);
            var p2 = MakeToy(1, new Vector2(-5.2f, 1f), Player2, ControlScheme.Arrows, players);
            var strandGo = new GameObject("Paint Strand");
            strandGo.AddComponent<LineRenderer>();
            var strand = strandGo.AddComponent<PaintStrand>();
            strand.toyA = p1;
            strand.toyB = p2;
            MakeWindUp("Wind-up Toy A", new Vector2(18f, 0.4f), 2f, enemies);
            MakeWindUp("Wind-up Toy B", new Vector2(34f, 0.4f), 2.5f, enemies);
            var managers = new GameObject("Game Manager");
            var gm = managers.AddComponent<GameManager>();
            gm.toys = new[] { p1, p2 };
            gm.strand = strand;
            gm.startPoint = start;
            managers.AddComponent<PrototypeHUD>();
            MakeCamera();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Selection.activeGameObject = managers;
            Debug.Log("Final Coat: prototype scene built at " + ScenePath + ". Press Play.");
        }

        static void BuildLevel(Transform parent){
            Box("Wallpaper", new Vector2(17f, 6f), new Vector2(64f, 20f), WallPaper, parent, -20, false);
            Box("Floor 1", new Vector2(0f, -1f), new Vector2(16f, 2f), Floor, parent);
            Box("Floor 2", new Vector2(15.75f, -1f), new Vector2(10.5f, 2f), Floor, parent);
            Box("Floor 3", new Vector2(34.75f, -1f), new Vector2(15.5f, 2f), Floor, parent);
            Box("Left Wall", new Vector2(-8.5f, 4f), new Vector2(1f, 12f), Floor, parent);
            Box("Right Wall", new Vector2(43f, 4f), new Vector2(1f, 12f), Floor, parent);
            Box("Low Shelf", new Vector2(3f, 2.2f), new Vector2(3f, 0.4f), Shelf, parent);
            Box("High Ledge", new Vector2(14f, 3.7f), new Vector2(2.5f, 0.4f), Shelf, parent);
            Box("Solo Ledge", new Vector2(31f, 2.6f), new Vector2(2f, 0.4f), Shelf, parent);
            Box("Seesaw Pillar", new Vector2(24f, -2.6f), new Vector2(0.4f, 4f), Shelf, parent);
            var plank = Box("Seesaw Plank", new Vector2(24f, -0.25f), new Vector2(5.6f, 0.3f), Crate, parent);
            var plankBody = plank.AddComponent<Rigidbody2D>();
            plankBody.mass = 0.6f;
            plankBody.gravityScale = 1.5f;
            var hinge = plank.AddComponent<HingeJoint2D>();
            hinge.useLimits = true;
            hinge.limits = new JointAngleLimits2D { min = -35f, max = 35f };
            var checkpoint = new GameObject("Checkpoint");
            checkpoint.transform.SetParent(parent, false);
            checkpoint.transform.position = new Vector2(20f, 1f);
            var cpTrigger = checkpoint.AddComponent<BoxCollider2D>();
            cpTrigger.isTrigger = true;
            cpTrigger.size = new Vector2(1f, 2f);
            Shape("Pole", square, checkpoint.transform, new Vector2(0f, 0f), new Vector2(0.1f, 2f), Outline, 2);
            var flag = Shape("Flag", square, checkpoint.transform, new Vector2(0.35f, 0.7f), new Vector2(0.6f, 0.4f), Color.gray, 3);
            checkpoint.AddComponent<Checkpoint>().flag = flag;
            var exit = new GameObject("Exit");
            exit.transform.SetParent(parent, false);
            exit.transform.position = new Vector2(41f, 1.25f);
            var exitTrigger = exit.AddComponent<BoxCollider2D>();
            exitTrigger.isTrigger = true;
            exitTrigger.size = new Vector2(1.6f, 2.5f);
            Shape("Frame", square, exit.transform, Vector2.zero, new Vector2(1.7f, 2.6f), Outline, 1);
            var door = Shape("Door", square, exit.transform, Vector2.zero, new Vector2(1.4f, 2.3f), Color.gray, 2);
            exit.AddComponent<LevelExit>().door = door;
            var kill = new GameObject("Kill Zone");
            kill.transform.SetParent(parent, false);
            kill.transform.position = new Vector2(17f, -9f);
            var killCol = kill.AddComponent<BoxCollider2D>();
            killCol.isTrigger = true;
            killCol.size = new Vector2(90f, 2f);
            kill.AddComponent<KillZone>();
        }

        static void BuildProps(Transform parent){
            MakeCrate("Crate A", new Vector2(5.5f, 0.5f), parent);
            MakeCrate("Crate B (push under the high ledge)", new Vector2(11.5f, 0.5f), parent);
            var ball = new GameObject("Rolling Ball");
            ball.transform.SetParent(parent, false);
            ball.transform.position = new Vector2(29f, 0.5f);
            var sr = ball.AddComponent<SpriteRenderer>();
            sr.sprite = circle;
            sr.color = Ball;
            sr.sortingOrder = 4;
            ball.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            var col = ball.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.sharedMaterial = SaveMaterial("Ball Bouncy", 0.05f, 0.75f);
            var rb = ball.AddComponent<Rigidbody2D>();
            rb.mass = 0.8f;
            rb.gravityScale = 2f;
            ball.AddComponent<Respawnable>();
        }

        static void BuildPaint(Transform parent){
            MakeBlob("Blob", new Vector2(-4f, 0.6f), BlobKind.Small, 10, SmallBlob, 0.45f, parent);
            MakeBlob("Blob", new Vector2(-2.5f, 0.6f), BlobKind.Small, 10, SmallBlob, 0.45f, parent);
            MakeBlob("Blob", new Vector2(-1f, 0.6f), BlobKind.Small, 10, SmallBlob, 0.45f, parent);
            MakeBlob("Blob", new Vector2(2.5f, 2.9f), BlobKind.Small, 10, SmallBlob, 0.45f, parent);
            MakeBlob("Blob", new Vector2(3.5f, 2.9f), BlobKind.Small, 10, SmallBlob, 0.45f, parent);
            MakeBlob("Large Blob", new Vector2(9.25f, 3f), BlobKind.Large, 50, LargeBlob, 0.75f, parent);
            MakeBlob("Large Blob", new Vector2(14f, 4.5f), BlobKind.Large, 50, LargeBlob, 0.75f, parent);
            MakeBlob("Blob", new Vector2(16.5f, 0.6f), BlobKind.Small, 10, SmallBlob, 0.45f, parent);
            MakeBlob("Blob", new Vector2(18.5f, 0.6f), BlobKind.Small, 10, SmallBlob, 0.45f, parent);
            MakeBlob("Blob", new Vector2(22f, 0.6f), BlobKind.Small, 10, SmallBlob, 0.45f, parent);
            MakeBlob("Blob", new Vector2(26f, 0.6f), BlobKind.Small, 10, SmallBlob, 0.45f, parent);
            MakeBlob("Solo Paint Pot", new Vector2(31f, 3.35f), BlobKind.SoloPot, 75, SoloPot, 0.7f, parent);
            MakeBlob("Blob", new Vector2(33f, 0.6f), BlobKind.Small, 10, SmallBlob, 0.45f, parent);
            MakeBlob("Blob", new Vector2(35f, 0.6f), BlobKind.Small, 10, SmallBlob, 0.45f, parent);
            MakeBlob("Large Blob", new Vector2(38f, 2.2f), BlobKind.Large, 50, LargeBlob, 0.75f, parent);
        }

        static ToyController MakeToy(int index, Vector2 position, Color paint, ControlScheme controls, Transform parent){
            var go = new GameObject($"Player {index + 1}");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.mass = 1f;
            rb.gravityScale = 2.5f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.9f);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            Shape("Outline", square, visual, Vector2.zero, new Vector2(0.88f, 0.98f), Outline, 9);
            var bodySr = Shape("Body", square, visual, Vector2.zero, new Vector2(0.8f, 0.9f), Color.white, 10);
            Shape("Eye L", circle, visual, new Vector2(0.06f, 0.14f), new Vector2(0.13f, 0.18f), Outline, 11);
            Shape("Eye R", circle, visual, new Vector2(0.26f, 0.14f), new Vector2(0.13f, 0.18f), Outline, 11);
            Shape("Player Dot", circle, visual, new Vector2(0f, 0.56f), new Vector2(0.2f, 0.2f), paint, 11);
            var toy = go.AddComponent<ToyController>();
            toy.playerIndex = index;
            toy.controls = controls;
            toy.paintColor = paint;
            toy.visual = visual;
            var toyPaint = go.AddComponent<ToyPaint>();
            toyPaint.toy = toy;
            toyPaint.body = bodySr;
            return toy;
        }

        static void MakeWindUp(string name, Vector2 position, float patrolDistance, Transform parent){
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.mass = 3f;
            rb.gravityScale = 2.5f;
            rb.freezeRotation = true;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 0.7f);
            var body = new GameObject("Body").transform;
            body.SetParent(go.transform, false);
            var bodySr = Shape("Shell", square, body, Vector2.zero, new Vector2(1f, 0.7f), Color.white, 10);
            Shape("Eye", circle, body, new Vector2(0.3f, 0.12f), new Vector2(0.22f, 0.22f), Color.white, 11);
            Shape("Pupil", circle, body, new Vector2(0.34f, 0.12f), new Vector2(0.1f, 0.1f), Outline, 12);
            Shape("Wheel L", circle, body, new Vector2(-0.3f, -0.38f), new Vector2(0.24f, 0.24f), Outline, 11);
            Shape("Wheel R", circle, body, new Vector2(0.3f, -0.38f), new Vector2(0.24f, 0.24f), Outline, 11);
            var key = new GameObject("Wind-up Key").transform;
            key.SetParent(go.transform, false);
            key.localPosition = new Vector3(-0.62f, 0.1f, 0f);
            Shape("Key Bar", square, key, Vector2.zero, new Vector2(0.1f, 0.5f), new Color(0.8f, 0.65f, 0.3f), 9);
            Shape("Key Grip", circle, key, new Vector2(0f, 0.25f), new Vector2(0.22f, 0.22f), new Color(0.8f, 0.65f, 0.3f), 9);
            var enemy = go.AddComponent<WindUpToy>();
            enemy.body = body;
            enemy.key = key;
            enemy.bodyRenderer = bodySr;
            enemy.patrolDistance = patrolDistance;
        }

        static PaintBlob MakeBlob(string name, Vector2 position, BlobKind kind, int value, Color color, float size, Transform parent){
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            var trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = size * 0.6f;
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            if (kind == BlobKind.SoloPot){
                Shape("Pot", square, visual, new Vector2(0f, -0.1f), new Vector2(size, size * 0.75f), Outline, 7);
                Shape("Paint", circle, visual, new Vector2(0f, 0.18f), new Vector2(size * 0.9f, size * 0.55f), color, 8);
            }
            else{
                Shape("Drop", circle, visual, Vector2.zero, new Vector2(size, size), color, 8);
            }
            var blob = go.AddComponent<PaintBlob>();
            blob.kind = kind;
            blob.value = value;
            blob.visual = visual;
            blob.pickupTrigger = trigger;
            return blob;
        }

        static void MakeCrate(string name, Vector2 position, Transform parent){
            var crate = Box(name, position, Vector2.one, Crate, parent, 4);
            Shape("Lid", square, crate.transform, new Vector2(0f, 0.4f), new Vector2(1f, 0.12f), Shelf, 5);
            var rb = crate.AddComponent<Rigidbody2D>();
            rb.mass = 1f;
            rb.gravityScale = 2.5f;
            crate.AddComponent<Respawnable>();
        }

        static void MakeCamera(){
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(-4f, 2f, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
            go.AddComponent<AudioListener>();
            go.AddComponent<CameraFollow>();
        }

        static GameObject Box(string name, Vector2 center, Vector2 size, Color color, Transform parent, int order = 0, bool withCollider = true){
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = color;
            sr.sortingOrder = order;
            if (withCollider) go.AddComponent<BoxCollider2D>();
            return go;
        }

        static SpriteRenderer Shape(string name, Sprite sprite, Transform parent, Vector2 localPosition, Vector2 size, Color color, int order){
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Vector3 ps = parent.lossyScale;
            go.transform.localPosition = new Vector3(localPosition.x / ps.x, localPosition.y / ps.y, 0f);
            go.transform.localScale = new Vector3(size.x / ps.x, size.y / ps.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        static Sprite MakeSprite(string name, int size, bool round){
            string path = $"{SpriteFolder}/{name}.png";
            if (!File.Exists(path)){
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                float r = size * 0.5f;
                for (int y = 0; y < size; y++){
                    for (int x = 0; x < size; x++){
                        byte alpha = 255;
                        if (round){
                            float dx = x + 0.5f - r, dy = y + 0.5f - r;
                            float edge = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy));
                            alpha = (byte)(edge * 255f);
                        }
                        pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                    }
                }
                tex.SetPixels32(pixels);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = size;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static PhysicsMaterial2D SaveMaterial(string name, float friction, float bounciness){
            string path = $"{SpriteFolder}/{name}.physicsMaterial2D";
            var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (existing != null) return existing;
            var mat = new PhysicsMaterial2D(name) { friction = friction, bounciness = bounciness };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void EnsureFolder(string path){
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void AddToBuildSettings(string path){
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}

#endif
