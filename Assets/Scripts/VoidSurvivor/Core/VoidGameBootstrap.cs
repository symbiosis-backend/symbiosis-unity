using System.Collections.Generic;
using MahjongGame;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoidSurvivor
{
    [DisallowMultipleComponent]
    public sealed class VoidGameBootstrap : MonoBehaviour
    {
        [Header("Scene Flow")]
        [SerializeField] private bool buildOnAwake = true;

        [Header("Configs")]
        [SerializeField] private VoidVisualConfig visualConfig;
        [SerializeField] private VoidLevelConfig singleLevel;
        [SerializeField] private VoidLevelConfig[] customLevels;
        [SerializeField] private VoidEnemyConfig[] enemyConfigs;

        [Header("Scene Visuals")]
        [SerializeField] private Sprite playerSprite;
        [SerializeField] private Color playerColor = new Color(0.35f, 0.85f, 1f, 1f);
        [SerializeField] private Vector2 playerSize = new Vector2(0.55f, 0.75f);
        [SerializeField] private Sprite bulletSprite;
        [SerializeField] private Color bulletColor = new Color(0.6f, 0.95f, 1f, 1f);
        [SerializeField] private Vector2 bulletSize = new Vector2(0.13f, 0.42f);
        [SerializeField] private Sprite enemyFallbackSprite;
        [SerializeField] private Color enemyFallbackColor = new Color(0.75f, 0.15f, 1f, 1f);
        [SerializeField] private Vector2 enemyFallbackSize = new Vector2(0.55f, 0.55f);
        [SerializeField] private Sprite explosionSprite;
        [SerializeField] private Color explosionColor = new Color(0.9f, 0.35f, 1f, 0.65f);
        [SerializeField] private Vector2 explosionSize = new Vector2(0.8f, 0.8f);
        [SerializeField] private float explosionLifetime = 0.35f;
        [SerializeField] private Color cameraBackgroundColor = new Color(0.015f, 0.01f, 0.035f, 1f);
        [SerializeField] private float cameraOrthographicSize = 6f;

        [Header("Player Trail")]
        [SerializeField] private bool usePlayerTrail = true;
        [SerializeField] private float trailTime = 0.25f;
        [SerializeField] private float trailStartWidth = 0.28f;
        [SerializeField] private Color trailStartColor = new Color(0.25f, 0.8f, 1f, 0.75f);
        [SerializeField] private Color trailEndColor = new Color(0.25f, 0.8f, 1f, 0f);

        [Header("Player")]
        [SerializeField] private float playerMaxHealth = 100f;
        [SerializeField] private float playerColliderRadius = 0.55f;

        [Header("Player Movement")]
        [SerializeField] private float playerFollowSpeed = 13f;
        [SerializeField] private float playerSmoothing = 0.08f;
        [SerializeField] private float playerScreenPadding = 0.55f;
        [SerializeField] private bool playerRotateTowardPointer = true;
        [SerializeField] private bool playerRequirePressToFollow = true;

        [Header("Weapon")]
        [SerializeField] private float fireInterval = 0.16f;
        [SerializeField] private float targetRange = 14f;
        [SerializeField] private float bulletDamage = 6f;
        [SerializeField] private float bulletSpeed = 15f;
        [SerializeField] private bool autoFire = true;

        [Header("Bullet")]
        [SerializeField] private float bulletLifetime = 2.2f;
        [SerializeField] private float bulletColliderRadius = 0.35f;

        [Header("Enemy Prefab")]
        [SerializeField] private float enemyColliderRadius = 0.55f;

        private Sprite whiteSprite;

        private void Awake()
        {
            if (buildOnAwake)
                Build();
        }

        [ContextMenu("Build Void Scene")]
        public void Build()
        {
            whiteSprite = CreateWhiteSprite();

            Camera cameraRef = BuildCamera();
            ObjectPoolManager pool = GetOrCreate<ObjectPoolManager>("Object Pool");
            GameObject bulletPrefab = BuildBulletPrefab();
            GameObject enemyPrefab = BuildEnemyPrefab();
            GameObject deathFxPrefab = BuildDeathFxPrefab();
            enemyPrefab.GetComponent<EnemyBase>().SetDeathFxPrefab(deathFxPrefab);

            GameObject player = BuildPlayer(cameraRef, pool, bulletPrefab);
            EnemySpawner spawner = GetOrCreate<EnemySpawner>("Enemy Spawner");
            spawner.Bind(cameraRef, player.transform, pool, enemyPrefab);

            WaveManager waveManager = GetOrCreate<WaveManager>("Wave Manager");
            waveManager.Bind(spawner);

            LevelManager levelManager = GetOrCreate<LevelManager>("Level Manager");
            VoidLevelConfig[] levels = ResolveLevelConfigs();
            levelManager.Bind(levels, waveManager);

            VoidGameUi ui = BuildUi(levels);
            ScreenShake2D shake = cameraRef.GetComponent<ScreenShake2D>();
            if (shake == null)
                shake = cameraRef.gameObject.AddComponent<ScreenShake2D>();

            GameManager gameManager = GetOrCreate<GameManager>("Game Manager");
            gameManager.Bind(player.GetComponent<PlayerHealth>(), levelManager, ui, shake);
        }

        private Camera BuildCamera()
        {
            Camera cameraRef = Camera.main;
            if (cameraRef == null)
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                cameraRef = cameraObject.GetComponent<Camera>();
            }

            cameraRef.transform.position = new Vector3(0f, 0f, -10f);
            cameraRef.orthographic = true;
            cameraRef.orthographicSize = Mathf.Max(2f, cameraOrthographicSize);
            cameraRef.backgroundColor = cameraBackgroundColor;
            return cameraRef;
        }

        private GameObject BuildPlayer(Camera cameraRef, ObjectPoolManager pool, GameObject bulletPrefab)
        {
            GameObject player = GameObject.Find("Void Player");
            if (player == null)
            {
                player = new GameObject("Void Player");
                player.transform.position = Vector3.zero;
                Rigidbody2D body = player.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
            }

            CircleCollider2D playerCollider = player.GetComponent<CircleCollider2D>();
            if (playerCollider == null)
                playerCollider = player.AddComponent<CircleCollider2D>();
            playerCollider.isTrigger = true;
            playerCollider.radius = Mathf.Max(0.05f, playerColliderRadius);

            SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = player.AddComponent<SpriteRenderer>();

            renderer.sprite = GetSprite(playerSprite != null ? playerSprite : visualConfig != null ? visualConfig.PlayerSprite : null);
            renderer.color = playerColor;
            player.transform.localScale = new Vector3(playerSize.x, playerSize.y, 1f);

            ApplyPlayerTrail(player);

            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller == null)
                controller = player.AddComponent<PlayerController>();

            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health == null)
                health = player.AddComponent<PlayerHealth>();

            PlayerShooter shooter = player.GetComponent<PlayerShooter>();
            if (shooter == null)
                shooter = player.AddComponent<PlayerShooter>();

            SerializedAssign(health, "maxHealth", Mathf.Max(1f, playerMaxHealth));
            SerializedAssign(controller, "followSpeed", Mathf.Max(0.1f, playerFollowSpeed));
            SerializedAssign(controller, "smoothing", Mathf.Max(0.001f, playerSmoothing));
            SerializedAssign(controller, "screenPadding", Mathf.Max(0f, playerScreenPadding));
            SerializedAssign(controller, "rotateTowardPointer", playerRotateTowardPointer);
            SerializedAssign(controller, "requirePressToFollow", playerRequirePressToFollow);
            SerializedAssign(shooter, "pool", pool);
            SerializedAssign(shooter, "bulletPrefab", bulletPrefab);
            SerializedAssign(shooter, "fireInterval", Mathf.Max(0.02f, fireInterval));
            SerializedAssign(shooter, "targetRange", Mathf.Max(1f, targetRange));
            SerializedAssign(shooter, "bulletDamage", Mathf.Max(0.1f, bulletDamage));
            SerializedAssign(shooter, "bulletSpeed", Mathf.Max(0.1f, bulletSpeed));
            SerializedAssign(shooter, "autoFire", autoFire);
            SerializedAssign(controller, "worldCamera", cameraRef);

            return player;
        }

        private GameObject BuildBulletPrefab()
        {
            GameObject prefab = GameObject.Find("Runtime Bullet Prefab");
            if (prefab == null)
            {
                prefab = new GameObject("Runtime Bullet Prefab");
                prefab.SetActive(false);
            }

            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = prefab.AddComponent<SpriteRenderer>();

            renderer.sprite = GetSprite(bulletSprite != null ? bulletSprite : visualConfig != null ? visualConfig.BulletSprite : null);
            renderer.color = bulletColor;
            prefab.transform.localScale = new Vector3(bulletSize.x, bulletSize.y, 1f);

            Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
            if (body == null)
                body = prefab.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            CircleCollider2D collider = prefab.GetComponent<CircleCollider2D>();
            if (collider == null)
                collider = prefab.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = Mathf.Max(0.02f, bulletColliderRadius);

            if (prefab.GetComponent<PooledObject>() == null)
                prefab.AddComponent<PooledObject>();
            Bullet bullet = prefab.GetComponent<Bullet>();
            if (bullet == null)
                bullet = prefab.AddComponent<Bullet>();
            SerializedAssign(bullet, "lifetime", Mathf.Max(0.05f, bulletLifetime));

            return prefab;
        }

        private GameObject BuildEnemyPrefab()
        {
            GameObject prefab = GameObject.Find("Runtime Enemy Prefab");
            if (prefab == null)
            {
                prefab = new GameObject("Runtime Enemy Prefab");
                prefab.SetActive(false);
            }

            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = prefab.AddComponent<SpriteRenderer>();

            renderer.sprite = GetSprite(enemyFallbackSprite != null ? enemyFallbackSprite : visualConfig != null ? visualConfig.EnemyFallbackSprite : null);
            renderer.color = enemyFallbackColor;
            prefab.transform.localScale = new Vector3(enemyFallbackSize.x, enemyFallbackSize.y, 1f);

            Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
            if (body == null)
                body = prefab.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            CircleCollider2D collider = prefab.GetComponent<CircleCollider2D>();
            if (collider == null)
                collider = prefab.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = Mathf.Max(0.05f, enemyColliderRadius);

            if (prefab.GetComponent<PooledObject>() == null)
                prefab.AddComponent<PooledObject>();
            if (prefab.GetComponent<EnemyHealth>() == null)
                prefab.AddComponent<EnemyHealth>();
            if (prefab.GetComponent<EnemyBase>() == null)
                prefab.AddComponent<EnemyBase>();

            return prefab;
        }

        private GameObject BuildDeathFxPrefab()
        {
            GameObject prefab = GameObject.Find("Runtime Void Burst Prefab");
            if (prefab == null)
            {
                prefab = new GameObject("Runtime Void Burst Prefab");
                prefab.SetActive(false);
            }

            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = prefab.AddComponent<SpriteRenderer>();

            renderer.sprite = GetSprite(explosionSprite != null ? explosionSprite : visualConfig != null ? visualConfig.ExplosionSprite : null);
            renderer.color = explosionColor;
            prefab.transform.localScale = new Vector3(explosionSize.x, explosionSize.y, 1f);

            if (prefab.GetComponent<PooledObject>() == null)
                prefab.AddComponent<PooledObject>();

            VoidAutoDespawn despawn = prefab.GetComponent<VoidAutoDespawn>();
            if (despawn == null)
                despawn = prefab.AddComponent<VoidAutoDespawn>();
            despawn.SetLifetime(Mathf.Max(0.05f, explosionLifetime));

            return prefab;
        }

        private VoidGameUi BuildUi(VoidLevelConfig[] levels)
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Void UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            Transform root = canvas.transform;
            GameObject legacyRestart = root.Find("Restart Button") != null ? root.Find("Restart Button").gameObject : null;
            if (legacyRestart != null)
                legacyRestart.SetActive(false);

            GameObject gameplay = CreatePanel("Gameplay HUD", root, new Color(0f, 0f, 0f, 0f));
            Text hp = CreateUiText("HP Text", gameplay.transform, GameLocalization.Format("void.hp", 100, 100), new Vector2(180f, -55f), new Vector2(320f, 60f), TextAnchor.MiddleLeft, 30);
            Text level = CreateUiText("Level Text", gameplay.transform, GameLocalization.Format("void.level", 1), new Vector2(0f, -55f), new Vector2(280f, 60f), TextAnchor.MiddleCenter, 34);
            Text score = CreateUiText("Score Text", gameplay.transform, GameLocalization.Format("void.score", 0), new Vector2(-180f, -55f), new Vector2(320f, 60f), TextAnchor.MiddleRight, 30);

            GameObject mainMenu = CreateEndPanel("Main Menu Screen", root, GameLocalization.Text("void.title"));
            Button start = CreateButton("Start Button", mainMenu.transform, GameLocalization.Text("void.start"), new Vector2(0f, -70f), new Vector2(330f, 85f));

            GameObject levelSelect = CreateEndPanel("Level Select Screen", root, GameLocalization.Text("void.level_select"));
            Button[] levelButtons = CreateLevelButtons(levelSelect.transform, levels);

            GameObject levelComplete = CreateEndPanel("Level Complete Screen", root, GameLocalization.Text("void.level_complete"));
            Button retry = CreateButton("Retry Level Button", levelComplete.transform, GameLocalization.Text("void.retry"), new Vector2(0f, 25f), new Vector2(330f, 78f));
            Button next = CreateButton("Next Level Button", levelComplete.transform, GameLocalization.Text("void.next"), new Vector2(0f, -75f), new Vector2(330f, 78f));
            Button select = CreateButton("Level Select Button", levelComplete.transform, GameLocalization.Text("void.level_select"), new Vector2(0f, -175f), new Vector2(330f, 78f));

            GameObject victory = CreateEndPanel("Victory Screen", root, GameLocalization.Text("void.victory"));
            GameObject defeat = CreateEndPanel("Defeat Screen", root, GameLocalization.Text("void.defeat"));
            Button restart = CreateButton("Restart Button", defeat.transform, GameLocalization.Text("void.retry"), new Vector2(0f, 25f), new Vector2(330f, 78f));
            Button defeatSelect = CreateButton("Defeat Level Select Button", defeat.transform, GameLocalization.Text("void.level_select"), new Vector2(0f, -75f), new Vector2(330f, 78f));

            VoidGameUi ui = canvas.GetComponent<VoidGameUi>();
            if (ui == null)
                ui = canvas.gameObject.AddComponent<VoidGameUi>();

            ui.Bind(hp, level, score, victory, defeat, restart);
            ui.BindFlow(mainMenu, levelSelect, gameplay, levelComplete, start, retry, next, select, defeatSelect, levelButtons);
            ui.ShowMainMenu();
            return ui;
        }

        private VoidLevelConfig[] ResolveLevelConfigs()
        {
            if (customLevels != null && customLevels.Length > 0)
                return customLevels;

            if (singleLevel != null)
                return new[] { singleLevel };

            return CreateRuntimeLevels();
        }

        private void ApplyPlayerTrail(GameObject player)
        {
            TrailRenderer trail = player.GetComponent<TrailRenderer>();

            if (!usePlayerTrail)
            {
                if (trail != null)
                    trail.enabled = false;
                return;
            }

            if (trail == null)
                trail = player.AddComponent<TrailRenderer>();

            trail.enabled = true;
            trail.time = Mathf.Max(0.01f, trailTime);
            trail.startWidth = Mathf.Max(0.01f, trailStartWidth);
            trail.endWidth = 0f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = trailStartColor;
            trail.endColor = trailEndColor;
        }

        private Sprite GetSprite(Sprite configuredSprite)
        {
            return configuredSprite != null ? configuredSprite : whiteSprite;
        }

        private GameObject CreatePanel(string name, Transform parent, Color color)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.gameObject;

            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            Image image = panel.GetComponent<Image>();
            image.color = color;
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return panel;
        }

        private Button[] CreateLevelButtons(Transform parent, VoidLevelConfig[] levels)
        {
            int count = Mathf.Max(1, levels != null ? levels.Length : 1);
            Button[] buttons = new Button[count];
            float spacingX = 170f;
            float spacingY = 105f;
            int columns = Mathf.Min(5, count);

            for (int i = 0; i < count; i++)
            {
                int row = i / columns;
                int column = i % columns;
                float totalWidth = (columns - 1) * spacingX;
                float x = column * spacingX - totalWidth * 0.5f;
                float y = 45f - row * spacingY;
                int levelNumber = levels != null && i < levels.Length && levels[i] != null ? levels[i].LevelNumber : i + 1;
                buttons[i] = CreateButton($"Level {levelNumber} Button", parent, levelNumber.ToString(), new Vector2(x, y), new Vector2(120f, 76f));
            }

            return buttons;
        }

        private VoidLevelConfig[] CreateRuntimeLevels()
        {
            VoidEnemyConfig[] enemyTypes = ResolveEnemyConfigs();
            List<VoidLevelConfig> levels = new();

            for (int i = 1; i <= 10; i++)
            {
                int typeCount = Mathf.Clamp(1 + i / 2, 1, enemyTypes.Length);
                List<WaveDefinition> waves = new();

                for (int wave = 0; wave < 3 + Mathf.Min(3, i / 3); wave++)
                {
                    List<EnemySpawnEntry> entries = new();
                    int entriesInWave = Mathf.Clamp(1 + wave / 2, 1, typeCount);

                    for (int entry = 0; entry < entriesInWave; entry++)
                    {
                        VoidEnemyConfig config = enemyTypes[(entry + wave + i) % typeCount];
                        int count = 4 + i + wave * 2;
                        float speed = 1f + i * 0.055f + wave * 0.035f;
                        float health = 1f + i * 0.06f;
                        entries.Add(new EnemySpawnEntry(config, count, speed, health));
                    }

                    waves.Add(new WaveDefinition($"L{i} Wave {wave + 1}", 0.7f, Mathf.Max(0.14f, 0.45f - i * 0.018f), entries.ToArray()));
                }

                VoidLevelConfig level = ScriptableObject.CreateInstance<VoidLevelConfig>();
                level.SetRuntime(i, 1f, waves.ToArray());
                levels.Add(level);
            }

            return levels.ToArray();
        }

        private VoidEnemyConfig[] ResolveEnemyConfigs()
        {
            if (enemyConfigs != null && enemyConfigs.Length > 0)
            {
                List<VoidEnemyConfig> configured = new();
                for (int i = 0; i < enemyConfigs.Length; i++)
                {
                    if (enemyConfigs[i] != null)
                        configured.Add(enemyConfigs[i]);
                }

                if (configured.Count > 0)
                    return configured.ToArray();
            }

            return CreateRuntimeEnemies();
        }

        private VoidEnemyConfig[] CreateRuntimeEnemies()
        {
            return new[]
            {
                RuntimeEnemy("Chaser", EnemyMoveKind.Chase, 10f, 2.4f, 10f, 10, new Color(0.68f, 0.2f, 1f), new Vector2(0.55f, 0.55f)),
                RuntimeEnemy("Waver", EnemyMoveKind.Wave, 12f, 2.8f, 12f, 14, new Color(0.15f, 0.8f, 1f), new Vector2(0.5f, 0.5f)),
                RuntimeEnemy("ZigZag", EnemyMoveKind.ZigZag, 14f, 3f, 14f, 18, new Color(1f, 0.35f, 0.8f), new Vector2(0.52f, 0.52f)),
                RuntimeEnemy("Orbiter", EnemyMoveKind.Orbit, 20f, 2.5f, 16f, 24, new Color(0.3f, 1f, 0.55f), new Vector2(0.62f, 0.62f)),
                RuntimeEnemy("Dasher", EnemyMoveKind.Dash, 16f, 2.7f, 18f, 28, new Color(1f, 0.25f, 0.25f), new Vector2(0.48f, 0.48f)),
                RuntimeEnemy("Sleeper", EnemyMoveKind.DelayThenChase, 28f, 3.1f, 20f, 34, new Color(1f, 0.9f, 0.25f), new Vector2(0.7f, 0.7f)),
                RuntimeEnemy("Dropper", EnemyMoveKind.Down, 18f, 3.5f, 15f, 22, new Color(0.4f, 0.55f, 1f), new Vector2(0.5f, 0.7f)),
                RuntimeEnemy("VoidRing", EnemyMoveKind.EdgeCircle, 26f, 2.6f, 20f, 40, new Color(0.95f, 0.55f, 1f), new Vector2(0.75f, 0.75f))
            };
        }

        private VoidEnemyConfig RuntimeEnemy(string id, EnemyMoveKind kind, float hp, float speed, float damage, int score, Color color, Vector2 size)
        {
            VoidEnemyConfig config = ScriptableObject.CreateInstance<VoidEnemyConfig>();
            config.SetRuntime(id, kind, hp, speed, damage, score, color, size);
            return config;
        }

        private Text CreateUiText(string name, Transform parent, string value, Vector2 position, Vector2 size, TextAnchor anchor, int fontSize)
        {
            Transform existing = parent.Find(name);
            Text text = existing != null ? existing.GetComponent<Text>() : null;
            if (text == null)
            {
                GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                go.transform.SetParent(parent, false);
                text = go.GetComponent<Text>();
            }

            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = anchor;
            text.color = new Color(0.85f, 0.92f, 1f);
            SetRect(text.rectTransform, new Vector2(anchor == TextAnchor.MiddleRight ? 1f : anchor == TextAnchor.MiddleLeft ? 0f : 0.5f, 1f), position, size);
            return text;
        }

        private GameObject CreateEndPanel(string name, Transform parent, string text)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.gameObject;

            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            Image image = panel.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.72f);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text label = CreateUiText("Label", panel.transform, text, new Vector2(0f, 120f), new Vector2(760f, 120f), TextAnchor.MiddleCenter, 58);
            label.color = Color.white;
            return panel;
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            Transform existing = parent.Find(name);
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (button != null)
                return button;

            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.22f, 0.58f, 0.95f);
            button = go.GetComponent<Button>();
            SetRect(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), position, size);

            Text text = CreateUiText("Text", go.transform, label, Vector2.zero, size, TextAnchor.MiddleCenter, 34);
            text.color = Color.white;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        private void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private Sprite CreateWhiteSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private T GetOrCreate<T>(string objectName) where T : Component
        {
            T existing = FindAnyObjectByType<T>();
            if (existing != null)
                return existing;

            GameObject go = new GameObject(objectName);
            return go.AddComponent<T>();
        }

        private void SerializedAssign(object target, string fieldName, object value)
        {
            if (target == null)
                return;

            System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
