using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class MahjongGameRuntime : MonoBehaviour
    {
        public static MahjongGameRuntime I { get; private set; }

        [Header("Core")]
        [SerializeField] private TileStore store;
        [SerializeField] private Board board;
        [SerializeField] private MahjongMusicPlayer musicPlayer;

        [Header("UI")]
        [SerializeField] private GameObject introPanel;
        [SerializeField] private CanvasGroup introCanvasGroup;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private TMP_Text introTitleText;
        [SerializeField] private TMP_Text introDescriptionText;
        [SerializeField] private Button continueButton;

        [Header("Optional Intro Objects")]
        [SerializeField] private GameObject[] extraIntroObjects;

        [Header("Scenes")]
        [SerializeField] private string lobbySceneName = "LobbyMahjong";

        [Header("Flow")]
        [SerializeField] private float continueDelay = 5f;
        [SerializeField] private float introFadeDuration = 0.2f;

        private int currentLevel = 1;
        private int currentStageIndex = 0;
        private float timer;
        private bool waitingForContinue;
        private bool transitionRunning;
        private bool resultStateReached;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;

            if (introCanvasGroup == null && introPanel != null)
                introCanvasGroup = introPanel.GetComponent<CanvasGroup>();

            if (board == null)
                board = FindAnyObjectByType<Board>();
        }

        private void OnEnable()
        {
            BindBoardEvents();
        }

        private void Start()
        {
            if (store == null)
                store = TileStore.I != null ? TileStore.I : FindAnyObjectByType<TileStore>();

            if (board == null)
                board = FindAnyObjectByType<Board>();

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnClickContinue);
                continueButton.onClick.AddListener(OnClickContinue);
            }

            ResolveLaunch();
        }

        private void OnDisable()
        {
            UnbindBoardEvents();
        }

        private void OnDestroy()
        {
            UnbindBoardEvents();
        }

        private void BindBoardEvents()
        {
            if (board == null)
                board = FindAnyObjectByType<Board>();

            if (board == null)
                return;

            board.WinTriggered -= HandleBoardWin;
            board.LoseTriggered -= HandleBoardLose;

            board.WinTriggered += HandleBoardWin;
            board.LoseTriggered += HandleBoardLose;
        }

        private void UnbindBoardEvents()
        {
            if (board == null)
                return;

            board.WinTriggered -= HandleBoardWin;
            board.LoseTriggered -= HandleBoardLose;
        }

        private void Update()
        {
            if (!waitingForContinue || continueButton == null || continueButton.gameObject.activeSelf)
                return;

            timer -= Time.deltaTime;

            if (timer <= 0f)
                continueButton.gameObject.SetActive(true);
        }

        private void ResolveLaunch()
        {
            if (MahjongSession.LaunchMode == MahjongLaunchMode.Story)
            {
                currentLevel = Mathf.Max(1, MahjongSession.StoryLevel);
                currentStageIndex = Mathf.Max(0, MahjongSession.StoryStage - 1);
            }
            else
            {
                currentLevel = 1;
                currentStageIndex = 0;
                MahjongSession.StartStory(currentLevel, currentStageIndex + 1);
            }

            resultStateReached = false;
            transitionRunning = false;
            waitingForContinue = false;

            if (store != null)
                musicPlayer?.PlayLevelMusic(store.GetMusicForLevel(currentLevel));

            ShowStageIntro();
        }

        private void ShowStageIntro()
        {
            if (store == null || !store.TryGetStageContent(currentLevel, currentStageIndex + 1, out LevelStageContent stage))
            {
                Debug.LogError($"[MahjongGameRuntime] Stage not found | Level={currentLevel} | Stage={currentStageIndex + 1}");
                ReturnToLobby();
                return;
            }

            waitingForContinue = true;
            transitionRunning = false;
            resultStateReached = false;

            ShowObject(introPanel, true);
            ShowObject(gamePanel, false);
            SetExtraIntroObjectsVisible(true);

            if (introCanvasGroup != null)
            {
                introCanvasGroup.alpha = 1f;
                introCanvasGroup.interactable = true;
                introCanvasGroup.blocksRaycasts = true;
            }

            if (introTitleText != null)
            {
                introTitleText.text = string.IsNullOrWhiteSpace(stage.Title)
                    ? $"{store.GetLevelDisplayName(currentLevel)} - Этап {currentStageIndex + 1}"
                    : stage.Title;
            }

            if (introDescriptionText != null)
                introDescriptionText.text = stage.Description;

            if (continueButton != null)
                continueButton.gameObject.SetActive(false);

            timer = continueDelay;

            Debug.Log($"[MahjongGameRuntime] Intro shown | Level={currentLevel} | Stage={currentStageIndex + 1}");
        }

        private void OnClickContinue()
        {
            if (!waitingForContinue || transitionRunning || resultStateReached)
                return;

            if (continueButton != null && !continueButton.gameObject.activeSelf)
                return;

            if (DoorFx.I != null && DoorFx.I.IsReady())
            {
                transitionRunning = true;
                DoorFx.I.RunBetweenLevels(() =>
                {
                    StartCoroutine(StartGameplaySmoothRoutine());
                });
            }
            else
            {
                StartCoroutine(StartGameplaySmoothRoutine());
            }
        }

        private IEnumerator StartGameplaySmoothRoutine()
        {
            transitionRunning = true;
            waitingForContinue = false;
            resultStateReached = false;

            if (board == null)
                board = FindAnyObjectByType<Board>();

            ShowObject(gamePanel, true);

            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            if (board != null)
            {
                board.SetStoryStage(currentLevel, currentStageIndex);
                board.Build();
            }

            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null;

            if (introCanvasGroup != null)
            {
                float time = 0f;
                float duration = Mathf.Max(0.01f, introFadeDuration);

                introCanvasGroup.interactable = false;
                introCanvasGroup.blocksRaycasts = false;

                while (time < duration)
                {
                    time += Time.deltaTime;
                    introCanvasGroup.alpha = Mathf.Lerp(1f, 0f, time / duration);
                    yield return null;
                }

                introCanvasGroup.alpha = 0f;
            }

            ShowObject(introPanel, false);
            SetExtraIntroObjectsVisible(false);

            Canvas.ForceUpdateCanvases();

            transitionRunning = false;

            Debug.Log($"[MahjongGameRuntime] Gameplay started | Level={currentLevel} | Stage={currentStageIndex + 1}");
        }

        private void HandleBoardWin()
        {
            if (resultStateReached)
                return;

            resultStateReached = true;
            waitingForContinue = false;
            transitionRunning = false;

            Debug.Log("[MahjongGameRuntime] Board WIN received.");
        }

        private void HandleBoardLose()
        {
            if (resultStateReached)
                return;

            resultStateReached = true;
            waitingForContinue = false;
            transitionRunning = false;

            Debug.Log("[MahjongGameRuntime] Board LOSE received.");
        }

        public void OnBoardStageComplete()
        {
            if (resultStateReached)
                return;

            resultStateReached = true;
            waitingForContinue = false;
            transitionRunning = false;

            Debug.Log("[MahjongGameRuntime] OnBoardStageComplete called.");
        }

        public void ReturnToLobbyFromRuntime()
        {
            ReturnToLobby();
        }

        private void ReturnToLobby()
        {
            MahjongSession.Clear();
            LoadSceneWithDoor(lobbySceneName);
        }

        private void LoadSceneWithDoor(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[MahjongGameRuntime] Scene name is empty.");
                return;
            }

            if (DoorFx.I != null && DoorFx.I.IsReady())
                DoorFx.I.LoadScene(sceneName);
            else
                SceneManager.LoadScene(sceneName);
        }

        private void ShowObject(GameObject obj, bool value)
        {
            if (obj == null)
                return;

            obj.SetActive(value);

            CanvasGroup group = obj.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = value ? 1f : 0f;
                group.interactable = value;
                group.blocksRaycasts = value;
            }
        }

        private void SetExtraIntroObjectsVisible(bool value)
        {
            if (extraIntroObjects == null)
                return;

            for (int i = 0; i < extraIntroObjects.Length; i++)
            {
                GameObject obj = extraIntroObjects[i];
                if (obj == null)
                    continue;

                obj.SetActive(value);
            }
        }
    }
}
