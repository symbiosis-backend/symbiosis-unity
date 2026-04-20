using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class CodexArchiveUI : MonoBehaviour
{
    [Serializable]
    public sealed class Entry
    {
        public string category;
        public string title;
        public string game;
        public string status;
        [TextArea(2, 4)] public string summary;
        public string tags;
        [TextArea(3, 8)] public string chronicles;
    }

    [SerializeField] private string mainSceneName = "Main";
    [SerializeField] private bool buildDefaultLayout = true;
    [SerializeField] private string[] categories = Array.Empty<string>();
    [SerializeField] private Entry[] entries = Array.Empty<Entry>();

    private readonly List<Entry> visibleEntries = new List<Entry>();
    private Button[] categoryButtons = Array.Empty<Button>();
    private TMP_Text[] categoryLabels = Array.Empty<TMP_Text>();
    private Button[] entryButtons = Array.Empty<Button>();
    private TMP_Text[] entryTitles = Array.Empty<TMP_Text>();
    private TMP_Text[] entrySubtitles = Array.Empty<TMP_Text>();
    private TMP_Text sectionTitle;
    private TMP_Text selectedTitle;
    private TMP_Text selectedMeta;
    private TMP_Text selectedSummary;
    private TMP_Text selectedTags;
    private TMP_Text selectedChronicles;
    private TMP_FontAsset titleFont;
    private TMP_FontAsset bodyFont;

    private readonly Color deepInk = new Color(0.045f, 0.055f, 0.075f, 1f);
    private readonly Color panel = new Color(0.10f, 0.12f, 0.15f, 0.88f);
    private readonly Color panelSoft = new Color(0.15f, 0.17f, 0.20f, 0.86f);
    private readonly Color gold = new Color(1f, 0.78f, 0.32f, 1f);
    private readonly Color parchment = new Color(0.92f, 0.86f, 0.72f, 1f);
    private readonly Color blueMist = new Color(0.52f, 0.70f, 0.83f, 1f);
    private int activeCategoryIndex;
    private int activeEntryIndex;

    private void Awake()
    {
        if (buildDefaultLayout)
        {
            EnsureDefaultData();
            BuildLayout();
        }

        BindButtons();
    }

    private void Start()
    {
        ShowCategory(0);
    }

    public void ReturnToMain()
    {
        if (!string.IsNullOrWhiteSpace(mainSceneName) && Application.CanStreamedLevelBeLoaded(mainSceneName))
            SceneManager.LoadScene(mainSceneName);
    }

    public void ShowCategory(int categoryIndex)
    {
        if (categories == null || categories.Length == 0)
            return;

        activeCategoryIndex = Mathf.Clamp(categoryIndex, 0, categories.Length - 1);
        string category = categories[activeCategoryIndex];

        visibleEntries.Clear();
        foreach (Entry entry in entries)
        {
            if (entry != null && string.Equals(entry.category, category, StringComparison.Ordinal))
                visibleEntries.Add(entry);
        }

        if (sectionTitle != null)
            sectionTitle.text = category;

        RefreshCategoryTabs();
        RefreshEntryList();
        SelectEntry(0);
    }

    public void SelectEntry(int entryIndex)
    {
        activeEntryIndex = Mathf.Clamp(entryIndex, 0, Mathf.Max(0, visibleEntries.Count - 1));
        RefreshEntryList();

        if (visibleEntries.Count == 0)
        {
            SetReader("Раздел пуст", "Будущая запись", "Здесь появится описание.", "-", "Хроники еще не добавлены.");
            return;
        }

        Entry entry = visibleEntries[activeEntryIndex];
        SetReader(entry.title, $"{entry.game} / {entry.status}", entry.summary, entry.tags, entry.chronicles);
    }

    private void BindButtons()
    {
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            int index = i;
            if (categoryButtons[i] != null)
                categoryButtons[i].onClick.AddListener(() => ShowCategory(index));
        }

        for (int i = 0; i < entryButtons.Length; i++)
        {
            int index = i;
            if (entryButtons[i] != null)
                entryButtons[i].onClick.AddListener(() => SelectEntry(index));
        }
    }

    private void SetReader(string title, string meta, string summary, string tagLine, string chronicles)
    {
        if (selectedTitle != null)
            selectedTitle.text = title;
        if (selectedMeta != null)
            selectedMeta.text = meta;
        if (selectedSummary != null)
            selectedSummary.text = summary;
        if (selectedTags != null)
            selectedTags.text = tagLine;
        if (selectedChronicles != null)
            selectedChronicles.text = chronicles;
    }

    private void RefreshCategoryTabs()
    {
        for (int i = 0; i < categoryLabels.Length; i++)
        {
            if (categoryLabels[i] == null)
                continue;

            categoryLabels[i].text = i < categories.Length ? categories[i] : string.Empty;
            categoryLabels[i].color = i == activeCategoryIndex ? gold : new Color(0.82f, 0.86f, 0.92f, 0.82f);
        }
    }

    private void RefreshEntryList()
    {
        for (int i = 0; i < entryButtons.Length; i++)
        {
            bool hasEntry = i < visibleEntries.Count;
            if (entryButtons[i] != null)
            {
                entryButtons[i].gameObject.SetActive(hasEntry);
                ColorBlock colors = entryButtons[i].colors;
                colors.normalColor = i == activeEntryIndex
                    ? new Color(0.33f, 0.26f, 0.16f, 0.96f)
                    : new Color(0.13f, 0.16f, 0.20f, 0.90f);
                colors.highlightedColor = new Color(0.42f, 0.33f, 0.20f, 1f);
                colors.pressedColor = new Color(0.50f, 0.38f, 0.20f, 1f);
                entryButtons[i].colors = colors;
            }

            if (!hasEntry)
                continue;

            Entry entry = visibleEntries[i];
            if (i < entryTitles.Length && entryTitles[i] != null)
                entryTitles[i].text = entry.title;
            if (i < entrySubtitles.Length && entrySubtitles[i] != null)
                entrySubtitles[i].text = $"{entry.game} / {entry.status}";
        }
    }

    private void BuildLayout()
    {
        RemoveGeneratedLayout();

        titleFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        bodyFont = titleFont;

        Camera camera = Camera.main != null ? Camera.main : CreateCamera();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = deepInk;
        camera.orthographic = true;

        Canvas canvas = CreateCanvas(camera);
        EnsureEventSystem();

        Image background = CreateImage(canvas.transform, "Archive Background", StretchFull(), deepInk);
        background.raycastTarget = false;
        Image veil = CreateImage(canvas.transform, "Night Reading Veil", StretchFull(), new Color(0.02f, 0.025f, 0.035f, 0.68f));
        veil.raycastTarget = false;
        CreateImage(canvas.transform, "Warm Desk Glow", Anchor(new Vector2(0f, 0f), new Vector2(1f, 0.58f)), new Color(0.38f, 0.25f, 0.12f, 0.20f)).raycastTarget = false;

        RectTransform shell = CreatePanel(canvas.transform, "Codex Shell", Anchor(new Vector2(0.035f, 0.05f), new Vector2(0.965f, 0.955f)), new Color(0.055f, 0.062f, 0.075f, 0.90f));
        AddOutline(shell.gameObject, new Color(0.90f, 0.68f, 0.28f, 0.38f), new Vector2(2f, -2f));

        TMP_Text overline = CreateText(shell, "Overline", "КОЛЛЕКЦИЯ ИГР", 18f, gold, TextAlignmentOptions.Left);
        SetRect(overline.rectTransform, Anchor(new Vector2(0.035f, 0.875f), new Vector2(0.42f, 0.935f)));
        TMP_Text title = CreateText(shell, "Title", "CODEX", 64f, parchment, TextAlignmentOptions.Left);
        title.fontStyle = FontStyles.Bold;
        title.enableAutoSizing = true;
        title.fontSizeMin = 38f;
        title.fontSizeMax = 68f;
        SetRect(title.rectTransform, Anchor(new Vector2(0.035f, 0.76f), new Vector2(0.42f, 0.885f)));
        TMP_Text subtitle = CreateText(shell, "Subtitle", "Архив лора, хроник и справочных данных всех режимов Symbiosis.", 23f, new Color(0.84f, 0.89f, 0.92f, 0.90f), TextAlignmentOptions.Left);
        subtitle.textWrappingMode = TextWrappingModes.Normal;
        SetRect(subtitle.rectTransform, Anchor(new Vector2(0.035f, 0.68f), new Vector2(0.44f, 0.76f)));

        Button backButton = CreateButton(shell, "Back To Main", "НАЗАД", Anchor(new Vector2(0.845f, 0.875f), new Vector2(0.965f, 0.94f)), new Color(0.24f, 0.17f, 0.11f, 0.94f), gold);
        backButton.onClick.AddListener(ReturnToMain);

        RectTransform stats = CreatePanel(shell, "Archive Stats", Anchor(new Vector2(0.50f, 0.79f), new Vector2(0.80f, 0.94f)), new Color(0.08f, 0.10f, 0.13f, 0.70f));
        CreateStat(stats, "5", "игровых веток", new Vector2(0.02f, 0f), new Vector2(0.32f, 1f));
        CreateStat(stats, "4", "раздела архива", new Vector2(0.35f, 0f), new Vector2(0.65f, 1f));
        CreateStat(stats, "12", "первых записей", new Vector2(0.68f, 0f), new Vector2(0.98f, 1f));

        RectTransform tabs = CreatePanel(shell, "Category Tabs", Anchor(new Vector2(0.035f, 0.59f), new Vector2(0.965f, 0.655f)), new Color(0.07f, 0.08f, 0.10f, 0.78f));
        categoryButtons = new Button[categories.Length];
        categoryLabels = new TMP_Text[categories.Length];
        for (int i = 0; i < categories.Length; i++)
        {
            float x0 = i / (float)categories.Length;
            float x1 = (i + 1) / (float)categories.Length;
            Button button = CreateButton(tabs, "Tab " + categories[i], categories[i], Anchor(new Vector2(x0 + 0.006f, 0.12f), new Vector2(x1 - 0.006f, 0.88f)), new Color(0.11f, 0.13f, 0.16f, 0.66f), new Color(0.82f, 0.86f, 0.92f, 0.82f));
            categoryButtons[i] = button;
            categoryLabels[i] = button.GetComponentInChildren<TMP_Text>();
        }

        RectTransform left = CreatePanel(shell, "Index Panel", Anchor(new Vector2(0.035f, 0.08f), new Vector2(0.34f, 0.56f)), panel);
        TMP_Text indexTitle = CreateText(left, "Index Title", "Записи раздела", 28f, gold, TextAlignmentOptions.Left);
        indexTitle.fontStyle = FontStyles.Bold;
        SetRect(indexTitle.rectTransform, Anchor(new Vector2(0.065f, 0.86f), new Vector2(0.92f, 0.97f)));
        TMP_Text indexHint = CreateText(left, "Index Hint", "Выбирай блок, чтобы открыть карточку архива.", 18f, blueMist, TextAlignmentOptions.Left);
        indexHint.textWrappingMode = TextWrappingModes.Normal;
        SetRect(indexHint.rectTransform, Anchor(new Vector2(0.065f, 0.77f), new Vector2(0.92f, 0.86f)));

        entryButtons = new Button[4];
        entryTitles = new TMP_Text[4];
        entrySubtitles = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            float y1 = 0.72f - i * 0.17f;
            float y0 = y1 - 0.135f;
            Button entryButton = CreateButton(left, "Entry Slot " + (i + 1), string.Empty, Anchor(new Vector2(0.06f, y0), new Vector2(0.94f, y1)), panelSoft, gold);
            entryButtons[i] = entryButton;
            entryButton.GetComponentInChildren<TMP_Text>().text = string.Empty;

            TMP_Text entryTitle = CreateText(entryButton.transform, "Title", "Запись", 21f, parchment, TextAlignmentOptions.Left);
            entryTitle.fontStyle = FontStyles.Bold;
            SetRect(entryTitle.rectTransform, Anchor(new Vector2(0.06f, 0.47f), new Vector2(0.95f, 0.88f)));
            TMP_Text entrySubtitle = CreateText(entryButton.transform, "Meta", "Игра / статус", 15f, blueMist, TextAlignmentOptions.Left);
            SetRect(entrySubtitle.rectTransform, Anchor(new Vector2(0.06f, 0.12f), new Vector2(0.95f, 0.44f)));
            entryTitles[i] = entryTitle;
            entrySubtitles[i] = entrySubtitle;
        }

        RectTransform reader = CreatePanel(shell, "Reader Panel", Anchor(new Vector2(0.365f, 0.08f), new Vector2(0.965f, 0.56f)), new Color(0.90f, 0.84f, 0.68f, 0.92f));
        AddOutline(reader.gameObject, new Color(0.16f, 0.10f, 0.055f, 0.45f), new Vector2(1.4f, -1.4f));

        sectionTitle = CreateText(reader, "Section Title", "Коллекция", 18f, new Color(0.35f, 0.20f, 0.08f, 1f), TextAlignmentOptions.Left);
        SetRect(sectionTitle.rectTransform, Anchor(new Vector2(0.055f, 0.88f), new Vector2(0.96f, 0.96f)));
        selectedTitle = CreateText(reader, "Selected Title", "Название записи", 42f, new Color(0.18f, 0.11f, 0.055f, 1f), TextAlignmentOptions.Left);
        selectedTitle.fontStyle = FontStyles.Bold;
        selectedTitle.enableAutoSizing = true;
        selectedTitle.fontSizeMin = 26f;
        selectedTitle.fontSizeMax = 44f;
        SetRect(selectedTitle.rectTransform, Anchor(new Vector2(0.055f, 0.74f), new Vector2(0.96f, 0.89f)));
        selectedMeta = CreateText(reader, "Selected Meta", "Игра / статус", 18f, new Color(0.33f, 0.25f, 0.17f, 1f), TextAlignmentOptions.Left);
        SetRect(selectedMeta.rectTransform, Anchor(new Vector2(0.055f, 0.68f), new Vector2(0.96f, 0.74f)));

        selectedSummary = CreateText(reader, "Selected Summary", "Описание", 22f, new Color(0.12f, 0.10f, 0.075f, 1f), TextAlignmentOptions.Left);
        selectedSummary.textWrappingMode = TextWrappingModes.Normal;
        SetRect(selectedSummary.rectTransform, Anchor(new Vector2(0.055f, 0.42f), new Vector2(0.58f, 0.66f)));

        RectTransform tagPanel = CreatePanel(reader, "Tag Panel", Anchor(new Vector2(0.62f, 0.43f), new Vector2(0.945f, 0.66f)), new Color(0.20f, 0.15f, 0.09f, 0.12f));
        TMP_Text tagTitle = CreateText(tagPanel, "Tag Title", "Метки", 16f, new Color(0.35f, 0.20f, 0.08f, 1f), TextAlignmentOptions.Left);
        SetRect(tagTitle.rectTransform, Anchor(new Vector2(0.06f, 0.68f), new Vector2(0.95f, 0.95f)));
        selectedTags = CreateText(tagPanel, "Selected Tags", "метки", 18f, new Color(0.12f, 0.10f, 0.075f, 1f), TextAlignmentOptions.Left);
        selectedTags.textWrappingMode = TextWrappingModes.Normal;
        SetRect(selectedTags.rectTransform, Anchor(new Vector2(0.06f, 0.08f), new Vector2(0.95f, 0.70f)));

        TMP_Text chroniclesTitle = CreateText(reader, "Chronicles Title", "Хроника", 20f, new Color(0.24f, 0.13f, 0.055f, 1f), TextAlignmentOptions.Left);
        chroniclesTitle.fontStyle = FontStyles.Bold;
        SetRect(chroniclesTitle.rectTransform, Anchor(new Vector2(0.055f, 0.33f), new Vector2(0.96f, 0.40f)));
        selectedChronicles = CreateText(reader, "Selected Chronicles", "Хроника записи", 18f, new Color(0.13f, 0.105f, 0.075f, 1f), TextAlignmentOptions.Left);
        selectedChronicles.textWrappingMode = TextWrappingModes.Normal;
        SetRect(selectedChronicles.rectTransform, Anchor(new Vector2(0.055f, 0.08f), new Vector2(0.945f, 0.33f)));

        RectTransform footer = CreatePanel(shell, "Footer Ledger", Anchor(new Vector2(0.365f, 0.005f), new Vector2(0.965f, 0.055f)), new Color(0.06f, 0.07f, 0.09f, 0.65f));
        TMP_Text footerText = CreateText(footer, "Footer Text", "Структура готова для будущего ввода: новые игры, главы лора, события, персонажи, правила и награды.", 17f, new Color(0.78f, 0.85f, 0.88f, 0.84f), TextAlignmentOptions.Center);
        footerText.textWrappingMode = TextWrappingModes.Normal;
        SetRect(footerText.rectTransform, StretchFull(18f, 2f, 18f, 2f));
    }

    private void EnsureDefaultData()
    {
        if (categories == null || categories.Length == 0)
            categories = new[] { "Коллекция", "Лор", "Хроники", "Системы" };

        if (entries != null && entries.Length > 0)
            return;

        entries = new[]
        {
            CreateEntry("Коллекция", "Mahjong World", "Mahjong", "активный режим", "Главная ветка спокойного маджонга: уровни, прогресс, награды и история путешествия через бамбуковые земли.", "маджонг / прогресс / путешествие", "Основа коллекции. Здесь будут храниться заметки по главам, условия открытия уровней и ключевые сюжетные повороты."),
            CreateEntry("Коллекция", "Mahjong Battle", "Mahjong Battle", "дуэльная ветка", "Боевой режим с персонажами, подбором соперника, здоровьем, атакой и особыми символами на костях.", "персонажи / дуэль / баланс", "Этот раздел станет витриной героев: биографии, стоимость, редкость, роль в бою и история конфликтов."),
            CreateEntry("Коллекция", "Okey Table", "Okey", "настольная ветка", "Архив правил, терминов, наборов костей и будущих хроник матчей для Okey.", "правила / стол / комбинации", "Записи будут описывать партии, обучающие подсказки, традиции стола и особые события."),
            CreateEntry("Коллекция", "AVOYDER", "Void Survivor", "аркадная ветка", "Раздел для уровней, врагов, волн, усилений и истории пустоты.", "void / волны / выживание", "Хроники пустоты фиксируют прохождение зон, типы противников и изменения сложности."),
            CreateEntry("Лор", "Бамбуковая библиотека", "Общий мир", "канон", "Точка сборки всего знания: тихий архив между играми, где каждая механика получает место в истории мира.", "мир / архив / канон", "Первый зал открыт. На полках пока лежат черновики, но структура уже готова принимать полноценные главы."),
            CreateEntry("Лор", "Символы костей", "Mahjong", "черновик", "Справочник значения символов: бамбук, лотос, монеты, птицы, удача и бесконечность.", "символы / значения / набор", "Каждый символ можно будет связать с биографией героя, эффектом боя или отдельной главой."),
            CreateEntry("Лор", "Герои дуэли", "Mahjong Battle", "черновик", "Зона для персонажей, их рангов, ценности, мотиваций и визуальных вариантов.", "герои / биографии / редкость", "Когда база персонажей будет финализирована, записи можно перенести сюда как читабельные карточки."),
            CreateEntry("Хроники", "Первые партии", "Okey", "журнал", "Журнал ранних матчей, тестов и заметок по ощущениям от стола.", "матчи / тесты / заметки", "Хроника фиксирует, что уже проверено: раздача, ход, сброс, победа и поведение ботов."),
            CreateEntry("Хроники", "Тропы маджонга", "Mahjong", "журнал", "Путь игрока по главам Mahjong World: от первого обучения до сложных раскладок.", "главы / уровни / прогресс", "Сюда удобно добавлять даты релизов уровней, названия эпизодов и важные изменения баланса."),
            CreateEntry("Хроники", "Падение в пустоту", "AVOYDER", "журнал", "История уровней Void Survivor, волн врагов и открытых улучшений.", "уровни / враги / волны", "Каждая новая зона может получить отдельную строку хроники с условиями победы."),
            CreateEntry("Системы", "Профиль игрока", "Core", "данные", "Сводка прогресса, титулов, рангов, валют и открытых наград.", "профиль / награды / валюта", "Раздел нужен как справочник того, какие данные игрока уже сохраняются и где они используются."),
            CreateEntry("Системы", "Навигация сцен", "Core", "технический справочник", "Карта переходов между Entry, Main, Lobby, игровыми сценами и Codex.", "сцены / переходы / UI", "Codex возвращается в Main и остается отдельной архивной комнатой внутри общей структуры.")
        };
    }

    private Entry CreateEntry(string category, string title, string game, string status, string summary, string tags, string chronicles)
    {
        return new Entry
        {
            category = category,
            title = title,
            game = game,
            status = status,
            summary = summary,
            tags = tags,
            chronicles = chronicles
        };
    }

    private Camera CreateCamera()
    {
        GameObject go = new GameObject("Main Camera");
        Camera camera = go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);
        camera.orthographic = true;
        return camera;
    }

    private Canvas CreateCanvas(Camera camera)
    {
        GameObject go = new GameObject("Codex Canvas", typeof(RectTransform));
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 10f;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    private void RemoveGeneratedLayout()
    {
        GameObject oldCanvas = GameObject.Find("Codex Canvas");
        if (oldCanvas == null)
            return;

        if (Application.isPlaying)
            Destroy(oldCanvas);
        else
            DestroyImmediate(oldCanvas);
    }

    private RectTransform CreatePanel(Transform parent, string name, RectSpec rect, Color color)
    {
        Image image = CreateImage(parent, name, rect, color);
        return image.rectTransform;
    }

    private Image CreateImage(Transform parent, string name, RectSpec rect, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        SetRect(image.rectTransform, rect);
        return image;
    }

    private Button CreateButton(Transform parent, string name, string text, RectSpec rect, Color color, Color textColor)
    {
        Image image = CreateImage(parent, name, rect, color);
        Button button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = new Color(Mathf.Min(color.r + 0.12f, 1f), Mathf.Min(color.g + 0.10f, 1f), Mathf.Min(color.b + 0.08f, 1f), color.a);
        colors.pressedColor = new Color(Mathf.Max(color.r - 0.05f, 0f), Mathf.Max(color.g - 0.05f, 0f), Mathf.Max(color.b - 0.05f, 0f), color.a);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        TMP_Text label = CreateText(button.transform, "Label", text, 21f, textColor, TextAlignmentOptions.Center);
        label.fontStyle = FontStyles.Bold;
        SetRect(label.rectTransform, StretchFull(8f, 0f, 8f, 0f));
        return button;
    }

    private TMP_Text CreateText(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text tmp = go.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        if (bodyFont != null)
            tmp.font = bodyFont;
        return tmp;
    }

    private void CreateStat(Transform parent, string number, string label, Vector2 min, Vector2 max)
    {
        RectTransform stat = CreatePanel(parent, "Stat " + number, Anchor(min, max), new Color(0f, 0f, 0f, 0f));
        TMP_Text num = CreateText(stat, "Number", number, 38f, gold, TextAlignmentOptions.Center);
        num.fontStyle = FontStyles.Bold;
        SetRect(num.rectTransform, Anchor(new Vector2(0f, 0.42f), new Vector2(1f, 0.92f)));
        TMP_Text text = CreateText(stat, "Label", label, 16f, blueMist, TextAlignmentOptions.Center);
        text.textWrappingMode = TextWrappingModes.Normal;
        SetRect(text.rectTransform, Anchor(new Vector2(0f, 0.12f), new Vector2(1f, 0.48f)));
    }

    private void AddOutline(GameObject go, Color color, Vector2 distance)
    {
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
    }

    private RectSpec StretchFull(float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        return new RectSpec(Vector2.zero, Vector2.one, new Vector2(left, bottom), new Vector2(-right, -top));
    }

    private RectSpec Anchor(Vector2 min, Vector2 max)
    {
        return new RectSpec(min, max, Vector2.zero, Vector2.zero);
    }

    private void SetRect(RectTransform rect, RectSpec spec)
    {
        rect.anchorMin = spec.Min;
        rect.anchorMax = spec.Max;
        rect.offsetMin = spec.OffsetMin;
        rect.offsetMax = spec.OffsetMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private readonly struct RectSpec
    {
        public readonly Vector2 Min;
        public readonly Vector2 Max;
        public readonly Vector2 OffsetMin;
        public readonly Vector2 OffsetMax;

        public RectSpec(Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            Min = min;
            Max = max;
            OffsetMin = offsetMin;
            OffsetMax = offsetMax;
        }
    }
}
