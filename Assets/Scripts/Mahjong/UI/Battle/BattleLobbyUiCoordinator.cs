using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MahjongGame
{
    public enum BattleLobbyModalKind
    {
        None,
        RandomMatch,
        RankedMatch,
        WifiMatch,
        DuelChallenge,
        Shop,
        Rewards,
        DailyHeroBonus,
        CharacterCarousel,
        Tournament,
        Settings,
        Auction,
        Forge,
        LoreTutorial
    }

    public static class BattleLobbyUiCoordinator
    {
        private const string BattleLobbySceneName = "LobbyMahjongBattle";
        private const string PopupCanvasName = "BattleLobbyPopupCanvas";
        private const int PopupSortingOrder = 30120;
        private static readonly Vector2 ReferenceResolution = new Vector2(2400f, 1080f);
        private static readonly string[] AuxiliaryOpenButtonNames =
        {
            "ButtonBattleStoneAuction",
            "ButtonBattleStoneForge",
            "ButtonBattleLoreTutorial"
        };

        private static BattleLobbyModalKind activeModal = BattleLobbyModalKind.None;
        private static bool leavingLobbyScene;

        public static BattleLobbyModalKind ActiveModal => activeModal;
        public static bool HasModalOpen => activeModal != BattleLobbyModalKind.None;

        public static void ResetForLobbyEntry()
        {
            leavingLobbyScene = false;
            activeModal = BattleLobbyModalKind.None;
            CleanupPopupRoots();
            ApplyLobbySuppression(false);
            EnsureEventSystem();
        }

        public static void EnsureInputReady()
        {
            EnsureEventSystem();
        }

        public static void OpenModal(BattleLobbyModalKind modal)
        {
            activeModal = modal;
            if (modal != BattleLobbyModalKind.None && modal != BattleLobbyModalKind.Settings)
                SettingsMenuUI.ForceCloseAllSettingsMenus();

            ApplyLobbySuppression(modal != BattleLobbyModalKind.None);
            EnsureEventSystem();
        }

        public static void CloseModal(BattleLobbyModalKind modal)
        {
            if (activeModal == modal)
                activeModal = BattleLobbyModalKind.None;

            if (leavingLobbyScene)
                return;

            if (activeModal == BattleLobbyModalKind.None)
                ApplyLobbySuppression(false);
        }

        public static void CloseAllModals()
        {
            activeModal = BattleLobbyModalKind.None;
            ApplyLobbySuppression(false);
        }

        public static void PrepareForSceneExit(string targetSceneName)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!string.Equals(activeScene.name, BattleLobbySceneName, System.StringComparison.Ordinal) ||
                string.Equals(targetSceneName, BattleLobbySceneName, System.StringComparison.Ordinal))
                return;

            leavingLobbyScene = true;
            activeModal = BattleLobbyModalKind.None;
        }

        public static Canvas GetOrCreatePopupCanvas()
        {
            if (leavingLobbyScene)
                return null;

            Scene scene = SceneManager.GetActiveScene();
            Canvas existing = FindPopupCanvas(scene);
            if (existing != null)
            {
                ConfigurePopupCanvas(existing);
                return existing;
            }

            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            GameObject canvasObject = new GameObject(
                PopupCanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);

            RectTransform rect = canvasObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            ConfigurePopupCanvas(canvas);
            return canvas;
        }

        public static void CleanupPopupRoots()
        {
            CleanupObjectsByName("RandomBattleLobbyOverlay");
            CleanupObjectsByName("RankedLeagueSelectOverlay");
            CleanupObjectsByName("OnlineRankedBattleLobbyOverlay");
            CleanupObjectsByName("LocalWifiBattleLobbyOverlay");
            CleanupObjectsByName("DuelChallengeOverlay");
            CleanupObjectsByName("TournamentLobbyOverlay");
            CleanupObjectsByName("BattleStoneAuctionOverlay");
            CleanupObjectsByName("BattleStoneForgeOverlay");
            CleanupObjectsByName("BattleLoreTutorialOverlay");
            CleanupObjectsByName("DuelIncomingIndicator");
            CleanupObjectsByName("RandomBattleCanvas");
            CleanupObjectsByName("OnlineRankedCanvas");
            CleanupObjectsByName("LocalWifiCanvas");
            CleanupObjectsByName(PopupCanvasName);
        }

        private static void ConfigurePopupCanvas(Canvas canvas)
        {
            if (canvas == null)
                return;

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = PopupSortingOrder;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        private static Canvas FindPopupCanvas(Scene scene)
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas != null
                    && canvas.gameObject.scene == scene
                    && string.Equals(canvas.gameObject.name, PopupCanvasName, System.StringComparison.Ordinal))
                {
                    return canvas;
                }
            }

            return null;
        }

        private static void ApplyLobbySuppression(bool suppressed)
        {
            if (leavingLobbyScene)
                return;

            BattleLobbyUI[] lobbies = Object.FindObjectsByType<BattleLobbyUI>(FindObjectsInactive.Include);
            for (int i = 0; i < lobbies.Length; i++)
            {
                if (lobbies[i] != null)
                    lobbies[i].SetMatchButtonsSuppressedBySettings(suppressed);
            }

            SetAuxiliaryOpenButtonsVisible(!suppressed);
        }

        private static void SetAuxiliaryOpenButtonsVisible(bool visible)
        {
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject candidate = objects[i];
                if (candidate == null || !candidate.scene.IsValid())
                    continue;

                for (int nameIndex = 0; nameIndex < AuxiliaryOpenButtonNames.Length; nameIndex++)
                {
                    if (string.Equals(candidate.name, AuxiliaryOpenButtonNames[nameIndex], System.StringComparison.Ordinal))
                    {
                        bool candidateVisible = visible;
                        if (string.Equals(candidate.name, "ButtonBattleLoreTutorial", System.StringComparison.Ordinal))
                            candidateVisible = visible && !BattleLoreTutorialSession.IsTrainingComplete;

                        candidate.SetActive(candidateVisible);
                        break;
                    }
                }
            }
        }

        private static void CleanupObjectsByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return;

            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject candidate = objects[i];
                if (candidate == null
                    || !string.Equals(candidate.name, objectName, System.StringComparison.Ordinal)
                    || !candidate.scene.IsValid())
                {
                    continue;
                }

                candidate.SetActive(false);
                Object.Destroy(candidate);
            }
        }

        private static void EnsureEventSystem()
        {
            EventSystemInputModeGuard.EnsureCompatibleEventSystems();
        }
    }
}
