using MahjongGame;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BattleSceneUiSetup
{
    private const string ScenePath = "Assets/Scenes/GameMahjongBattle.unity";

    [MenuItem("Tools/Mahjong/Setup Battle Scene UI")]
    public static void SetupBattleSceneUi()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        BattleMatchController controller = Object.FindAnyObjectByType<BattleMatchController>();

        if (canvas == null || controller == null)
        {
            Debug.LogError("[BattleSceneUiSetup] Canvas or BattleMatchController not found.");
            return;
        }

        GameObject playerRoot = EnsurePanel(
            canvas.transform,
            "PlayerProfileHUD",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(72f, -54f),
            new Vector2(440f, 112f),
            new Color(0f, 0f, 0f, 0.42f),
            true);

        Image playerSprite = EnsureImage(
            playerRoot.transform,
            "PlayerBattleSprite",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(18f, -13f),
            new Vector2(86f, 86f),
            Color.white,
            false,
            true);

        TMP_Text playerName = EnsureText(
            playerRoot.transform,
            "PlayerName",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(120f, -18f),
            new Vector2(-140f, 34f),
            "Player",
            30f,
            Color.white,
            TextAlignmentOptions.Left);

        TMP_Text playerRank = EnsureText(
            playerRoot.transform,
            "PlayerRank",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(120f, -58f),
            new Vector2(-140f, 30f),
            "Unranked 0 RP",
            22f,
            Color.white,
            TextAlignmentOptions.Left);

        GameObject hpBar = EnsurePanel(
            playerRoot.transform,
            "PlayerHpBar",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(120f, -92f),
            new Vector2(280f, 16f),
            new Color(0f, 0f, 0f, 0.45f),
            false);

        Image playerHpFill = EnsureImage(
            hpBar.transform,
            "PlayerHpBarFill",
            Vector2.zero,
            Vector2.one,
            new Vector2(0f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            new Color(0.3f, 0.9f, 0.35f, 1f),
            false,
            false);

        playerHpFill.type = Image.Type.Filled;
        playerHpFill.fillMethod = Image.FillMethod.Horizontal;
        playerHpFill.fillOrigin = 0;
        playerHpFill.fillAmount = 1f;

        TMP_Text playerHpText = EnsureText(
            playerRoot.transform,
            "PlayerHpBarText",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(120f, -74f),
            new Vector2(280f, 18f),
            "1000/1000",
            16f,
            Color.white,
            TextAlignmentOptions.Center);

        GameObject opponentRoot = EnsurePanel(
            canvas.transform,
            "OpponentProfileHUD",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-72f, -54f),
            new Vector2(440f, 112f),
            new Color(0f, 0f, 0f, 0.42f),
            true);

        Image opponentSprite = EnsureImage(
            opponentRoot.transform,
            "OpponentBattleSprite",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-104f, -13f),
            new Vector2(86f, 86f),
            Color.white,
            false,
            true);

        opponentSprite.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

        TMP_Text opponentName = EnsureText(
            opponentRoot.transform,
            "OpponentName",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(20f, -18f),
            new Vector2(-140f, 34f),
            "Opponent",
            30f,
            Color.white,
            TextAlignmentOptions.Left);

        TMP_Text opponentRank = EnsureText(
            opponentRoot.transform,
            "OpponentRank",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(20f, -58f),
            new Vector2(-140f, 30f),
            "Unranked 0 RP",
            22f,
            Color.white,
            TextAlignmentOptions.Left);

        GameObject resultPanel = EnsurePanel(
            canvas.transform,
            "BattleResultPanel",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(560f, 320f),
            new Color(0f, 0f, 0f, 0.72f),
            true);

        TMP_Text resultTitle = EnsureText(
            resultPanel.transform,
            "BattleResultTitle",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 70f),
            new Vector2(480f, 96f),
            "WIN",
            68f,
            new Color(0.3f, 1f, 0.38f, 1f),
            TextAlignmentOptions.Center);

        Button resultButton = EnsureButton(
            resultPanel.transform,
            "BattleLobbyButton",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -70f),
            new Vector2(300f, 72f),
            "Battle Lobby");

        resultPanel.SetActive(false);

        SerializedObject serialized = new SerializedObject(controller);
        SetObject(serialized, "playerBattleSpriteImage", playerSprite);
        SetObject(serialized, "playerNameText", playerName);
        SetObject(serialized, "playerRankText", playerRank);
        SetObject(serialized, "playerHpBarFill", playerHpFill);
        SetObject(serialized, "playerHpBarText", playerHpText);
        SetObject(serialized, "opponentBattleSpriteImage", opponentSprite);
        SetObject(serialized, "opponentNameText", opponentName);
        SetObject(serialized, "opponentRankText", opponentRank);
        SetObject(serialized, "resultPanelRoot", resultPanel);
        SetObject(serialized, "resultTitleText", resultTitle);
        SetObject(serialized, "resultBattleLobbyButton", resultButton);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        EditorSceneManager.SaveScene(controller.gameObject.scene);

        Debug.Log("[BattleSceneUiSetup] Battle scene UI objects created and linked.");
    }

    private static GameObject EnsurePanel(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size,
        Color color,
        bool raycastTarget)
    {
        GameObject go = FindChild(parent, name);
        if (go == null)
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        if (image == null)
            image = go.AddComponent<Image>();

        image.color = color;
        image.raycastTarget = raycastTarget;

        return go;
    }

    private static Image EnsureImage(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size,
        Color color,
        bool raycastTarget,
        bool preserveAspect)
    {
        GameObject go = FindChild(parent, name);
        if (go == null)
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.offsetMin = anchorMin == Vector2.zero && anchorMax == Vector2.one ? Vector2.zero : rect.offsetMin;
        rect.offsetMax = anchorMin == Vector2.zero && anchorMax == Vector2.one ? Vector2.zero : rect.offsetMax;

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        image.preserveAspect = preserveAspect;

        return image;
    }

    private static TMP_Text EnsureText(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size,
        string textValue,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject go = FindChild(parent, name);
        if (go == null)
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));

        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = textValue;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;

        return text;
    }

    private static Button EnsureButton(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size,
        string label)
    {
        GameObject go = FindChild(parent, name);
        if (go == null)
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));

        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.92f);
        image.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        EnsureText(
            go.transform,
            "BattleLobbyButtonText",
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            label,
            28f,
            Color.black,
            TextAlignmentOptions.Center);

        return button;
    }

    private static GameObject FindChild(Transform parent, string name)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == name)
                return children[i].gameObject;
        }

        return null;
    }

    private static void SetObject(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }
}
