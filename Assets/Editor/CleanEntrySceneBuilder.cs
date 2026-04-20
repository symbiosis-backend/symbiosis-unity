using MahjongGame;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MahjongGame.EditorTools
{
    public static class CleanEntrySceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Entry.unity";

        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Entry";

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.055f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.tag = "MainCamera";

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif

            GameObject canvasObject = new GameObject("EntryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject backdrop = CreatePanel(canvasObject.transform, "Backdrop", new Color(0.025f, 0.035f, 0.055f, 1f));
            CreateCenteredText(backdrop.transform, "Title", "Symbiosis", 56f, new Vector2(0.5f, 0.58f), new Vector2(900f, 90f));
            CreateCenteredText(backdrop.transform, "Subtitle", "Loading", 24f, new Vector2(0.5f, 0.49f), new Vector2(520f, 52f));

            GameObject loadingPanel = CreatePanel(canvasObject.transform, "LoadingPanel", new Color(0.025f, 0.035f, 0.055f, 0.86f));
            CreateCenteredText(loadingPanel.transform, "LoadingText", "Loading", 34f, new Vector2(0.5f, 0.5f), new Vector2(520f, 72f));
            loadingPanel.SetActive(false);

            GameObject profilePanel = CreatePanel(canvasObject.transform, "ProfileSetupPanel", new Color(0f, 0f, 0f, 0f));
            profilePanel.AddComponent<ProfileSetupUI>();
            profilePanel.SetActive(false);

            GameObject fadeObject = CreatePanel(canvasObject.transform, "FadePanel", Color.black);
            CanvasGroup fadeGroup = fadeObject.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;

            GameObject servicesObject = new GameObject("Services");
            servicesObject.AddComponent<AppSettings>();
            servicesObject.AddComponent<ProfileService>();

            GameObject bootstrapObject = new GameObject("Bootstrap");
            ProfileBootstrap bootstrap = bootstrapObject.AddComponent<ProfileBootstrap>();
            EntryCinematicIntro intro = bootstrapObject.AddComponent<EntryCinematicIntro>();

            ConfigureBootstrap(bootstrap, loadingPanel, profilePanel, fadeGroup, intro);
            ConfigureIntro(intro);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[CleanEntrySceneBuilder] Rebuilt " + ScenePath);
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = color.a > 0.001f;
            return panel;
        }

        private static TextMeshProUGUI CreateCenteredText(Transform parent, string name, string text, float fontSize, Vector2 anchor, Vector2 size)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = fontSize;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static void ConfigureBootstrap(
            ProfileBootstrap bootstrap,
            GameObject loadingPanel,
            GameObject profilePanel,
            CanvasGroup fadeGroup,
            EntryCinematicIntro intro)
        {
            SerializedObject serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("loadingPanel").objectReferenceValue = loadingPanel;
            serialized.FindProperty("languageSelectionPanel").objectReferenceValue = null;
            serialized.FindProperty("profileSetupPanel").objectReferenceValue = profilePanel;
            serialized.FindProperty("fadeGroup").objectReferenceValue = fadeGroup;
            serialized.FindProperty("lobbySceneName").stringValue = "Main";
            serialized.FindProperty("startDelay").floatValue = 0.1f;
            serialized.FindProperty("fadeDuration").floatValue = 0.18f;
            serialized.FindProperty("inputSettleFramesBeforeSceneLoad").intValue = 2;
            serialized.FindProperty("unloadUnusedAssetsBeforeLobby").boolValue = false;
            serialized.FindProperty("playEntryIntro").boolValue = true;
            serialized.FindProperty("playEntryIntroOnMobile").boolValue = true;
            serialized.FindProperty("entryIntro").objectReferenceValue = intro;
            serialized.FindProperty("autoCreateLanguagePanel").boolValue = true;
            serialized.FindProperty("autoCreateProfileService").boolValue = true;
            serialized.FindProperty("requireServerProfile").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureIntro(EntryCinematicIntro intro)
        {
            SerializedObject serialized = new SerializedObject(intro);
            serialized.FindProperty("duration").floatValue = 5f;
            serialized.FindProperty("fadeOutDuration").floatValue = 0.35f;
            serialized.FindProperty("playSound").boolValue = false;
            serialized.FindProperty("playGeneratedSoundOnMobile").boolValue = false;
            serialized.FindProperty("showSkipButton").boolValue = true;
            serialized.FindProperty("showIntroText").boolValue = true;
            serialized.FindProperty("enableStarTwinkle").boolValue = false;
            serialized.FindProperty("enableMotionParallax").boolValue = false;
            serialized.FindProperty("backgroundSprite").objectReferenceValue = null;
            serialized.FindProperty("backgroundScrollLoop").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
