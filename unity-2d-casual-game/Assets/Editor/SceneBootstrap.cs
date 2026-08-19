using System.Collections.Generic;
using System.IO;
using Game.Core;
using Game.Gameplay;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.EditorTools
{
    /// <summary>
    /// 플레이 가능한 샘플 씬과 프리팹을 코드로 생성한다.
    /// 아트 에셋 없이 단색 쿼드만 쓰므로 저장소에 바이너리를 넣지 않고도
    /// 클론 직후 바로 Play 를 눌러 확인할 수 있다.
    /// </summary>
    public static class SceneBootstrap
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string PrefabDirectory = "Assets/Prefabs";
        private const string MaterialDirectory = "Assets/Art/Materials";

        private const float GroundSurfaceY = -3.4f;
        private const float TileWidth = 20f;
        private const string PlayerTag = "Player";

        [MenuItem("Tools/2D Runner/샘플 씬 생성", priority = 0)]
        public static void CreateSampleScene()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog(
                    "샘플 씬 재생성",
                    $"{ScenePath} 를 덮어씁니다. 계속할까요?",
                    "덮어쓰기",
                    "취소"))
            {
                return;
            }

            EnsureFolders();
            EnsureTag(PlayerTag);

            // 프리팹을 만들면 열려 있는 씬이 더러워지므로 빈 씬을 먼저 연다.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Material playerMaterial = CreateMaterial("Player", new Color(0.29f, 0.56f, 0.95f));
            Material groundMaterial = CreateMaterial("Ground", new Color(0.29f, 0.18f, 0.13f));
            Material obstacleMaterial = CreateMaterial("Obstacle", new Color(0.85f, 0.29f, 0.27f));
            Material coinMaterial = CreateMaterial("Coin", new Color(0.98f, 0.78f, 0.24f));

            Scroller obstaclePrefab = CreateObstaclePrefab(obstacleMaterial);
            Scroller coinPrefab = CreateCoinPrefab(coinMaterial);

            CreateCamera();
            GroundScroller ground = CreateGround(groundMaterial);
            PlayerController2D player = CreatePlayer(playerMaterial);
            Spawner spawner = CreateSpawner(obstaclePrefab, coinPrefab);
            GameManager game = CreateGameManager(player, spawner);
            CreateUi();

            // 씬 안에서 서로를 찾지 않도록 참조를 미리 꽂아 둔다 (FindObjectOfType 제거).
            Undo.ClearAll();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SceneBootstrap] 샘플 씬 생성 완료 — {ScenePath} " +
                      $"(ground: {ground.name}, game: {game.name})");
        }

        private static void EnsureFolders()
        {
            CreateFolderRecursive("Assets/Scenes");
            CreateFolderRecursive(PrefabDirectory);
            CreateFolderRecursive(MaterialDirectory);
        }

        private static void CreateFolderRecursive(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent))
            {
                CreateFolderRecursive(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = $"{MaterialDirectory}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                // Unlit 셰이더를 쓰면 라이트 없이도 색이 그대로 나온다 — 2D 프로토타입에 충분하다.
                material = new Material(Shader.Find("Unlit/Color"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateQuad(string name, Vector3 position, Vector2 size, Material material)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;

            // 3D 메시 콜라이더는 필요 없다 — 물리는 전부 2D 로 처리한다.
            Object.DestroyImmediate(quad.GetComponent<MeshCollider>());

            quad.transform.position = position;
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = material;
            return quad;
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.68f, 0.85f, 0.93f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static GroundScroller CreateGround(Material material)
        {
            var root = new GameObject("Ground", typeof(GroundScroller));
            var tiles = new List<Transform>();

            for (int i = 0; i < 3; i++)
            {
                GameObject tile = CreateQuad(
                    $"GroundTile_{i}",
                    new Vector3(i * TileWidth - TileWidth, GroundSurfaceY - 1f, 0f),
                    new Vector2(TileWidth, 2f),
                    material);

                tile.transform.SetParent(root.transform);
                tile.AddComponent<BoxCollider2D>();
                tiles.Add(tile.transform);
            }

            GroundScroller scroller = root.GetComponent<GroundScroller>();
            scroller.Configure(tiles.ToArray(), TileWidth, -TileWidth);
            return scroller;
        }

        private static PlayerController2D CreatePlayer(Material material)
        {
            GameObject player = CreateQuad(
                "Player",
                new Vector3(-2f, GroundSurfaceY + 1.5f, 0f),
                new Vector2(0.9f, 0.9f),
                material);

            player.tag = PlayerTag;

            var body = player.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 3.2f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            player.AddComponent<BoxCollider2D>();
            return player.AddComponent<PlayerController2D>();
        }

        private static Scroller CreateObstaclePrefab(Material material)
        {
            GameObject obstacle = CreateQuad("Obstacle", Vector3.zero, new Vector2(0.9f, 1.6f), material);

            var collider = obstacle.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            obstacle.AddComponent<Scroller>();
            obstacle.AddComponent<Obstacle>();

            return SaveAsPrefab(obstacle, $"{PrefabDirectory}/Obstacle.prefab").GetComponent<Scroller>();
        }

        private static Scroller CreateCoinPrefab(Material material)
        {
            GameObject coin = CreateQuad("Coin", Vector3.zero, new Vector2(0.5f, 0.5f), material);

            var collider = coin.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;

            coin.AddComponent<Scroller>();
            coin.AddComponent<Coin>();

            return SaveAsPrefab(coin, $"{PrefabDirectory}/Coin.prefab").GetComponent<Scroller>();
        }

        private static GameObject SaveAsPrefab(GameObject source, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
            return prefab;
        }

        private static Spawner CreateSpawner(Scroller obstaclePrefab, Scroller coinPrefab)
        {
            var spawnerObject = new GameObject("Spawner", typeof(Spawner));
            Spawner spawner = spawnerObject.GetComponent<Spawner>();
            spawner.Configure(obstaclePrefab, coinPrefab, GroundSurfaceY);
            return spawner;
        }

        private static GameManager CreateGameManager(PlayerController2D player, Spawner spawner)
        {
            var managerObject = new GameObject(
                "GameManager",
                typeof(GameManager),
                typeof(AudioSource),
                typeof(AudioManager));

            GameManager manager = managerObject.GetComponent<GameManager>();
            SetReference(manager, "player", player);
            SetReference(manager, "spawner", spawner);
            return manager;
        }

        private static void CreateUi()
        {
            var canvasObject = new GameObject(
                "Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            Transform root = canvasObject.transform;

            Text score = CreateText(root, "ScoreText", "0", 96, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(600f, 140f));
            Text coins = CreateText(root, "CoinText", "◎ 0", 56, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(200f, -60f), new Vector2(320f, 80f));
            Text best = CreateText(root, "HighScoreText", "BEST 0", 44, TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(-200f, -60f), new Vector2(360f, 80f));

            GameObject readyPanel = CreatePanel(root, "ReadyPanel", new Color(0f, 0f, 0f, 0.25f));
            CreateText(readyPanel.transform, "ReadyText", "TAP TO START", 72, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 200f));

            GameObject gameOverPanel = CreatePanel(root, "GameOverPanel", new Color(0f, 0f, 0f, 0.55f));
            Text result = CreateText(gameOverPanel.transform, "ResultText", "GAME OVER", 64,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 220f),
                new Vector2(900f, 400f));
            Button restart = CreateButton(gameOverPanel.transform, "RestartButton", "RETRY",
                new Vector2(0f, -80f));
            Button home = CreateButton(gameOverPanel.transform, "HomeButton", "HOME",
                new Vector2(0f, -260f));

            GameObject pausePanel = CreatePanel(root, "PausePanel", new Color(0f, 0f, 0f, 0.55f));
            Button resume = CreateButton(pausePanel.transform, "ResumeButton", "RESUME", Vector2.zero);

            Button pause = CreateButton(root, "PauseButton", "II", new Vector2(-120f, -180f));
            AnchorTopRight(pause.GetComponent<RectTransform>(), new Vector2(160f, 160f));

            var hud = canvasObject.AddComponent<HudController>();
            hud.Configure(score, coins, best, result,
                readyPanel, gameOverPanel, pausePanel,
                restart, home, pause, resume);

            // 시작 상태는 HudController 가 Start 에서 맞추지만, 에디터에서도 보기 좋게 정리해 둔다.
            gameOverPanel.SetActive(false);
            pausePanel.SetActive(false);
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(Image));
            panel.transform.SetParent(parent, false);

            panel.GetComponent<Image>().color = color;

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return panel;
        }

        private static Text CreateText(
            Transform parent, string name, string content, int fontSize, TextAnchor alignment,
            Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            var textObject = new GameObject(name, typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = BuiltinFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position)
        {
            var buttonObject = new GameObject(name, typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            buttonObject.GetComponent<Image>().color = new Color(0.29f, 0.56f, 0.95f);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(520f, 140f);

            CreateText(buttonObject.transform, "Label", label, 56, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 140f));

            return buttonObject.GetComponent<Button>();
        }

        private static void AnchorTopRight(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.sizeDelta = size;
        }

        private static Font BuiltinFont()
        {
            // 에디터 버전에 따라 내장 폰트 이름이 다르다.
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void SetReference(Object target, string fieldName, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);

            if (property == null)
            {
                Debug.LogWarning($"[SceneBootstrap] {target.GetType().Name}.{fieldName} 필드를 찾지 못했습니다.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RegisterSceneInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            if (scenes.Exists(scene => scene.path == ScenePath))
            {
                return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureTag(string tag)
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tags = tagManager.FindProperty("tags");

            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    return;
                }
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
