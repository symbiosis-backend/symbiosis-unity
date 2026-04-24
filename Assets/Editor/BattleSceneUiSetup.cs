using MahjongGame;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BattleSceneUiSetup
{
    private const string ScenePath = "Assets/Scenes/GameMahjongBattle.unity";
    private const string ParryPanelPath = "Assets/Scripts/Mahjong/Sprites/Parry/ParryPanel.png";
    private const string ParryHeadPath = "Assets/Scripts/Mahjong/Sprites/Parry/ParryHead.png";
    private const string ParryBodyPath = "Assets/Scripts/Mahjong/Sprites/Parry/ParryBody.png";
    private const string ParryLegsPath = "Assets/Scripts/Mahjong/Sprites/Parry/ParryLegs.png";

    [MenuItem("Tools/Mahjong/Setup Battle Scene UI")]
    public static void SetupBattleSceneUi()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        BattleMatchController controller = Object.FindAnyObjectByType<BattleMatchController>();
        BattleCombatSystem combatSystem = Object.FindAnyObjectByType<BattleCombatSystem>();

        if (canvas == null || controller == null)
        {
            Debug.LogError("[BattleSceneUiSetup] Canvas or BattleMatchController not found.");
            return;
        }

        GameObject playerRoot = EnsurePanel(
            canvas.transform,
            "PlayerProfileHUD",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-14f, -54f),
            new Vector2(360f, 112f),
            new Color(0f, 0f, 0f, 0.42f),
            true);

        Image playerSprite = EnsureImage(
            playerRoot.transform,
            "PlayerBattleSprite",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-18f, -13f),
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
            new Vector2(20f, -18f),
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
            new Vector2(20f, -58f),
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
            new Vector2(20f, -92f),
            new Vector2(230f, 16f),
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

        playerHpFill.type = Image.Type.Simple;

        TMP_Text playerHpText = EnsureText(
            playerRoot.transform,
            "PlayerHpBarText",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(20f, -74f),
            new Vector2(230f, 18f),
            "100/100",
            16f,
            Color.white,
            TextAlignmentOptions.Center);

        GameObject opponentRoot = EnsurePanel(
            canvas.transform,
            "OpponentProfileHUD",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, 1f),
            new Vector2(14f, -54f),
            new Vector2(360f, 112f),
            new Color(0f, 0f, 0f, 0.42f),
            true);

        Image opponentSprite = EnsureImage(
            opponentRoot.transform,
            "OpponentBattleSprite",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(18f, -13f),
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
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            new Color(0f, 0f, 0f, 0.9f),
            true);

        RectTransform resultOverlayRect = resultPanel.GetComponent<RectTransform>();
        resultOverlayRect.offsetMin = Vector2.zero;
        resultOverlayRect.offsetMax = Vector2.zero;

        GameObject resultWindow = EnsurePanel(
            resultPanel.transform,
            "BattleResultWindow",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(620f, 380f),
            new Color(0.055f, 0.058f, 0.064f, 0.96f),
            true);

        TMP_Text resultTitle = EnsureText(
            resultWindow.transform,
            "BattleResultTitle",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 108f),
            new Vector2(532f, 74f),
            "VICTORY",
            56f,
            new Color(0.3f, 1f, 0.38f, 1f),
            TextAlignmentOptions.Center);

        TMP_Text resultReward = EnsureText(
            resultWindow.transform,
            "BattleResultReward",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 34f),
            new Vector2(512f, 40f),
            "+100 Gold",
            30f,
            Color.white,
            TextAlignmentOptions.Center);

        TMP_Text resultExperience = EnsureText(
            resultWindow.transform,
            "BattleResultExperience",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -8f),
            new Vector2(512f, 34f),
            "+100 XP  Level 1",
            23f,
            Color.white,
            TextAlignmentOptions.Center);

        Button resultButton = EnsureButton(
            resultWindow.transform,
            "BattleLobbyButton",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-142f, -126f),
            new Vector2(252f, 58f),
            "Menu");

        Button newMatchButton = EnsureButton(
            resultWindow.transform,
            "BattleNewMatchButton",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(142f, -126f),
            new Vector2(252f, 58f),
            "New Match");

        resultPanel.SetActive(false);

        BattleParryChoiceUI parryChoiceUi = EnsureParryChoiceUi(canvas.transform);

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
        SetObject(serialized, "resultRewardText", resultReward);
        SetObject(serialized, "resultExperienceText", resultExperience);
        SetObject(serialized, "resultBattleLobbyButton", resultButton);
        SetObject(serialized, "resultNewMatchButton", newMatchButton);
        SetObject(serialized, "parryChoiceUi", parryChoiceUi);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        if (combatSystem != null)
        {
            SerializedObject combatSerialized = new SerializedObject(combatSystem);
            SetObject(combatSerialized, "parryChoiceUi", parryChoiceUi);
            combatSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(combatSystem);
        }

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        EditorSceneManager.SaveScene(controller.gameObject.scene);

        Debug.Log("[BattleSceneUiSetup] Battle scene UI objects created and linked.");
    }

    private static BattleParryChoiceUI EnsureParryChoiceUi(Transform canvas)
    {
        GameObject host = FindChild(canvas, "BattleParryChoiceUI");
        if (host == null)
            host = new GameObject("BattleParryChoiceUI", typeof(RectTransform));

        host.transform.SetParent(canvas, false);

        RectTransform hostRect = host.GetComponent<RectTransform>();
        hostRect.anchorMin = Vector2.zero;
        hostRect.anchorMax = Vector2.one;
        hostRect.offsetMin = Vector2.zero;
        hostRect.offsetMax = Vector2.zero;

        BattleParryChoiceUI ui = host.GetComponent<BattleParryChoiceUI>();
        if (ui == null)
            ui = host.AddComponent<BattleParryChoiceUI>();

        Sprite parryPanelSprite = LoadParrySprite(ParryPanelPath);
        Sprite parryHeadSprite = LoadParrySprite(ParryHeadPath);
        Sprite parryBodySprite = LoadParrySprite(ParryBodyPath);
        Sprite parryLegsSprite = LoadParrySprite(ParryLegsPath);

        GameObject root = EnsurePanel(
            host.transform,
            "BattleParryChoiceRoot",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            parryPanelSprite != null ? new Vector2(760f, 520f) : new Vector2(900f, 430f),
            parryPanelSprite != null ? new Color(0f, 0f, 0f, 0f) : new Color(0f, 0f, 0f, 0.82f),
            true);
        Image rootImage = root.GetComponent<Image>();
        rootImage.sprite = null;
        rootImage.preserveAspect = false;

        TMP_Text title = EnsureText(
            root.transform,
            "Title",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 214f),
            new Vector2(440f, 42f),
            "Choose zone",
            34f,
            Color.white,
            TextAlignmentOptions.Center);

        TMP_Text hint = EnsureText(
            root.transform,
            "Hint",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 178f),
            new Vector2(440f, 28f),
            "You attack, enemy parries",
            20f,
            Color.white,
            TextAlignmentOptions.Center);

        TMP_Text timer = EnsureText(
            root.transform,
            "Timer",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 138f),
            new Vector2(120f, 34f),
            "5",
            28f,
            Color.white,
            TextAlignmentOptions.Center);

        Image playerCharacter = EnsureImage(
            root.transform,
            "PlayerParryCharacter",
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(65f, -48f),
            new Vector2(118f, 270f),
            Color.white,
            false,
            true);
        BattleCharacterModelView playerParryModel = EnsureComponent<BattleCharacterModelView>(playerCharacter.gameObject);

        Image opponentCharacter = EnsureImage(
            root.transform,
            "OpponentParryCharacter",
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-65f, -48f),
            new Vector2(118f, 270f),
            Color.white,
            false,
            true);
        opponentCharacter.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        BattleCharacterModelView opponentParryModel = EnsureComponent<BattleCharacterModelView>(opponentCharacter.gameObject);

        Transform playerPanel = EnsureParryZonePanel(root.transform, "PlayerZones", new Vector2(-130f, -58f), "You attack", parryPanelSprite, out TMP_Text playerLabel);
        Transform opponentPanel = EnsureParryZonePanel(root.transform, "OpponentZones", new Vector2(130f, -58f), "Enemy parries", parryPanelSprite, out TMP_Text opponentLabel);

        Button[] playerButtons = EnsureParryZoneButtons(playerPanel, parryHeadSprite, parryBodySprite, parryLegsSprite);
        Button[] opponentButtons = EnsureParryZoneButtons(opponentPanel, parryHeadSprite, parryBodySprite, parryLegsSprite);

        SerializedObject serialized = new SerializedObject(ui);
        SetObject(serialized, "root", root);
        SetObject(serialized, "titleText", title);
        SetObject(serialized, "hintText", hint);
        SetObject(serialized, "timerText", timer);
        SetObject(serialized, "playerLabelText", playerLabel);
        SetObject(serialized, "opponentLabelText", opponentLabel);
        SetObject(serialized, "playerCharacterImage", playerCharacter);
        SetObject(serialized, "opponentCharacterImage", opponentCharacter);
        SetObject(serialized, "playerCharacterModelView", playerParryModel);
        SetObject(serialized, "opponentCharacterModelView", opponentParryModel);
        SetObject(serialized, "parryPanelSprite", parryPanelSprite);
        SetObject(serialized, "parryHeadSprite", parryHeadSprite);
        SetObject(serialized, "parryBodySprite", parryBodySprite);
        SetObject(serialized, "parryLegsSprite", parryLegsSprite);
        SetObjectArray(serialized, "playerZoneButtons", playerButtons);
        SetObjectArray(serialized, "opponentZoneButtons", opponentButtons);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(false);
        EditorUtility.SetDirty(ui);
        return ui;
    }

    private static Transform EnsureParryZonePanel(
        Transform parent,
        string name,
        Vector2 position,
        string labelText,
        Sprite panelSprite,
        out TMP_Text label)
    {
        GameObject panel = EnsurePanel(
            parent,
            name,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            position,
            panelSprite != null ? new Vector2(190f, 285f) : new Vector2(270f, 260f),
            panelSprite != null ? Color.white : new Color(1f, 1f, 1f, 0.08f),
            false);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.sprite = panelSprite;
        panelImage.preserveAspect = panelSprite != null;

        label = EnsureText(
            panel.transform,
            "Label",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 130f),
            new Vector2(186f, 24f),
            labelText,
            18f,
            Color.white,
            TextAlignmentOptions.Center);

        return panel.transform;
    }

    private static Button[] EnsureParryZoneButtons(Transform parent, Sprite headSprite, Sprite bodySprite, Sprite legsSprite)
    {
        Button[] buttons = new Button[3];
        buttons[0] = EnsureButton(
            parent,
            "TopZone",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 70f),
            new Vector2(66f, 66f),
            "Top",
            headSprite);
        buttons[1] = EnsureButton(
            parent,
            "MidZone",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -2f),
            new Vector2(66f, 66f),
            "Mid",
            bodySprite);
        buttons[2] = EnsureButton(
            parent,
            "LowZone",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -74f),
            new Vector2(66f, 66f),
            "Low",
            legsSprite);

        return buttons;
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
        string label,
        Sprite sprite = null)
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
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : new Color(0.94f, 0.92f, 0.86f, 1f);
        image.preserveAspect = sprite != null;
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
            22f,
            sprite != null ? new Color(1f, 1f, 1f, 0f) : new Color(0.06f, 0.06f, 0.07f, 1f),
            TextAlignmentOptions.Center);

        return button;
    }

    private static Sprite LoadParrySprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite nestedSprite)
                return nestedSprite;
        }

        return null;
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

    private static void SetObjectArray(SerializedObject serialized, string propertyName, Object[] values)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || values == null)
            return;

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
            component = target.AddComponent<T>();

        return component;
    }
}
