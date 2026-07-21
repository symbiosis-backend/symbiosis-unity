using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame.Orbiosis
{
    [DisallowMultipleComponent]
    public sealed class OrbiosisBestiaryUI : MonoBehaviour
    {
        private static readonly Color Space = new Color(0.004f, 0.009f, 0.024f, 0.97f);
        private static readonly Color Panel = new Color(0.018f, 0.038f, 0.062f, 0.96f);
        private static readonly Color Card = new Color(0.032f, 0.060f, 0.088f, 0.92f);
        private static readonly Color LockedCard = new Color(0.026f, 0.030f, 0.040f, 0.92f);
        private static readonly Color Ink = new Color(0.92f, 0.97f, 1f, 1f);
        private static readonly Color MutedInk = new Color(0.58f, 0.70f, 0.82f, 1f);
        private static readonly Color Cyan = new Color(0.22f, 0.90f, 1f, 1f);
        private static readonly Color Gold = new Color(1f, 0.76f, 0.22f, 1f);
        private static readonly Color Danger = new Color(1f, 0.28f, 0.24f, 1f);

        private readonly List<Button> entryButtons = new List<Button>(8);
        private readonly List<TextMeshProUGUI> entryLabels = new List<TextMeshProUGUI>(8);
        private readonly List<Image> entryPortraits = new List<Image>(8);

        private RectTransform root;
        private RectTransform shellRoot;
        private RectTransform gridRoot;
        private RectTransform profileRoot;
        private RectTransform portraitFrame;
        private RectTransform closeButtonRoot;
        private Image profilePortrait;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI subtitleText;
        private TextMeshProUGUI profileText;
        private TextMeshProUGUI progressText;
        private Action closeAction;
        private string selectedId;
        private Vector2 lastRootSize;

        public static OrbiosisBestiaryUI Ensure(Transform parent, Action closeAction)
        {
            OrbiosisBestiaryUI existing = parent.GetComponentInChildren<OrbiosisBestiaryUI>(true);
            if (existing != null)
            {
                existing.closeAction = closeAction;
                return existing;
            }

            GameObject go = new GameObject("BestiaryWindow", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            OrbiosisBestiaryUI ui = go.AddComponent<OrbiosisBestiaryUI>();
            ui.closeAction = closeAction;
            ui.Build();
            return ui;
        }

        public void Show()
        {
            if (root == null)
                Build();

            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            ApplyResponsiveLayout();
            Refresh();
        }

        private void Update()
        {
            if (root == null || !root.gameObject.activeSelf)
                return;

            Vector2 size = root.rect.size;
            if (Vector2.SqrMagnitude(size - lastRootSize) > 4f)
                ApplyResponsiveLayout();
        }

        public void Hide()
        {
            if (root != null)
                root.gameObject.SetActive(false);
        }

        public void Refresh()
        {
            OrbiosisBestiaryEntry[] entries = OrbiosisBestiaryLibrary.All();
            if (string.IsNullOrEmpty(selectedId) || !OrbiosisBestiaryProgress.IsUnlocked(selectedId))
                selectedId = FirstUnlockedId(entries);

            if (string.IsNullOrEmpty(selectedId) && entries.Length > 0)
                selectedId = entries[0].Id;

            for (int i = 0; i < entryButtons.Count && i < entries.Length; i++)
                RefreshEntryCard(i, entries[i]);

            if (progressText != null)
                progressText.text = "DISCOVERED " + OrbiosisBestiaryProgress.UnlockedCount() + "/" + OrbiosisBestiaryLibrary.Count;

            RefreshProfile();
        }

        private void Build()
        {
            root = transform as RectTransform;
            Stretch(root);

            Image background = gameObject.AddComponent<Image>();
            background.color = Space;
            background.raycastTarget = true;

            shellRoot = CreatePanel(root, "Shell", Panel);
            AddOutline(shellRoot, new Color(0.15f, 0.74f, 0.90f, 0.55f), new Vector2(3f, -3f));

            TextMeshProUGUI header = CreateText(shellRoot, "Title", "BESTIARY", 44f, FontStyles.Bold, Ink);
            header.alignment = TextAlignmentOptions.Left;
            SetRect(header.rectTransform, new Vector2(0.035f, 0.895f), new Vector2(0.42f, 0.975f));

            progressText = CreateText(shellRoot, "Progress", string.Empty, 24f, FontStyles.Bold, Gold);
            progressText.alignment = TextAlignmentOptions.Right;
            SetRect(progressText.rectTransform, new Vector2(0.54f, 0.895f), new Vector2(0.80f, 0.975f));

            Button closeButton = CreateButton(shellRoot, "Close", "BACK", new Vector2(0.825f, 0.885f), new Vector2(0.965f, 0.975f), 24f, Close);
            closeButtonRoot = closeButton.transform as RectTransform;

            gridRoot = CreatePanel(shellRoot, "EnemyGrid", new Color(0.006f, 0.017f, 0.030f, 0.70f));
            AddOutline(gridRoot, new Color(0.15f, 0.74f, 0.90f, 0.28f), new Vector2(2f, -2f));

            BuildEntryCards(gridRoot);

            profileRoot = CreatePanel(shellRoot, "Profile", new Color(0.010f, 0.026f, 0.044f, 0.82f));
            AddOutline(profileRoot, new Color(1f, 0.28f, 0.24f, 0.36f), new Vector2(2f, -2f));

            portraitFrame = CreatePanel(profileRoot, "PortraitFrame", new Color(0.020f, 0.050f, 0.074f, 0.92f));
            AddOutline(portraitFrame, new Color(0.16f, 0.80f, 0.92f, 0.38f), new Vector2(2f, -2f));

            profilePortrait = CreateImage(portraitFrame, "Portrait", Color.white);
            Stretch(profilePortrait.rectTransform);
            profilePortrait.rectTransform.offsetMin = new Vector2(16f, 16f);
            profilePortrait.rectTransform.offsetMax = new Vector2(-16f, -16f);
            profilePortrait.preserveAspect = true;
            profilePortrait.raycastTarget = false;

            titleText = CreateText(profileRoot, "EnemyName", string.Empty, 38f, FontStyles.Bold, Ink);
            titleText.alignment = TextAlignmentOptions.Left;

            subtitleText = CreateText(profileRoot, "EnemyClass", string.Empty, 24f, FontStyles.Bold, Gold);
            subtitleText.alignment = TextAlignmentOptions.Left;

            profileText = CreateText(profileRoot, "EnemyBody", string.Empty, 23f, FontStyles.Normal, Ink);
            profileText.alignment = TextAlignmentOptions.TopLeft;
            profileText.textWrappingMode = TextWrappingModes.Normal;
            profileText.lineSpacing = 8f;

            ApplyResponsiveLayout();
            root.gameObject.SetActive(false);
        }

        private void BuildEntryCards(RectTransform grid)
        {
            OrbiosisBestiaryEntry[] entries = OrbiosisBestiaryLibrary.All();
            for (int i = 0; i < entries.Length; i++)
            {
                Button button = CreateButton(grid, "EnemyCard_" + entries[i].Id, string.Empty, Vector2.zero, Vector2.one, 20f, null);
                int captured = i;
                button.onClick.AddListener(() => Select(entries[captured].Id));
                entryButtons.Add(button);

                Image portrait = CreateImage(button.transform, "Portrait", Color.white);
                portrait.rectTransform.anchorMin = new Vector2(0.055f, 0.17f);
                portrait.rectTransform.anchorMax = new Vector2(0.33f, 0.83f);
                portrait.rectTransform.offsetMin = Vector2.zero;
                portrait.rectTransform.offsetMax = Vector2.zero;
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;
                entryPortraits.Add(portrait);

                TextMeshProUGUI label = CreateText(button.transform, "Name", string.Empty, 22f, FontStyles.Bold, Ink);
                label.alignment = TextAlignmentOptions.Left;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.rectTransform.anchorMin = new Vector2(0.38f, 0.16f);
                label.rectTransform.anchorMax = new Vector2(0.95f, 0.84f);
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
                entryLabels.Add(label);
            }

            LayoutEntryCards(false);
        }

        private void ApplyResponsiveLayout()
        {
            if (root == null || shellRoot == null)
                return;

            lastRootSize = root.rect.size;
            bool portrait = lastRootSize.y > lastRootSize.x * 1.08f;

            SetRect(shellRoot, portrait ? new Vector2(0.035f, 0.045f) : new Vector2(0.035f, 0.055f), portrait ? new Vector2(0.965f, 0.955f) : new Vector2(0.965f, 0.945f));
            SetRect(progressText.rectTransform, portrait ? new Vector2(0.38f, 0.900f) : new Vector2(0.54f, 0.895f), portrait ? new Vector2(0.72f, 0.975f) : new Vector2(0.80f, 0.975f));
            SetRect(closeButtonRoot, portrait ? new Vector2(0.745f, 0.895f) : new Vector2(0.825f, 0.885f), portrait ? new Vector2(0.965f, 0.975f) : new Vector2(0.965f, 0.975f));

            if (portrait)
            {
                SetRect(gridRoot, new Vector2(0.045f, 0.575f), new Vector2(0.955f, 0.865f));
                SetRect(profileRoot, new Vector2(0.045f, 0.055f), new Vector2(0.955f, 0.545f));
                SetRect(portraitFrame, new Vector2(0.055f, 0.565f), new Vector2(0.38f, 0.91f));
                SetRect(titleText.rectTransform, new Vector2(0.43f, 0.775f), new Vector2(0.94f, 0.91f));
                SetRect(subtitleText.rectTransform, new Vector2(0.43f, 0.645f), new Vector2(0.94f, 0.755f));
                SetRect(profileText.rectTransform, new Vector2(0.055f, 0.070f), new Vector2(0.94f, 0.525f));
            }
            else
            {
                SetRect(gridRoot, new Vector2(0.035f, 0.075f), new Vector2(0.405f, 0.850f));
                SetRect(profileRoot, new Vector2(0.435f, 0.075f), new Vector2(0.965f, 0.850f));
                SetRect(portraitFrame, new Vector2(0.045f, 0.165f), new Vector2(0.39f, 0.86f));
                SetRect(titleText.rectTransform, new Vector2(0.43f, 0.735f), new Vector2(0.94f, 0.88f));
                SetRect(subtitleText.rectTransform, new Vector2(0.43f, 0.625f), new Vector2(0.94f, 0.725f));
                SetRect(profileText.rectTransform, new Vector2(0.43f, 0.145f), new Vector2(0.94f, 0.590f));
            }

            LayoutEntryCards(portrait);
        }

        private void LayoutEntryCards(bool portrait)
        {
            int count = entryButtons.Count;
            if (count == 0)
                return;

            int columns = portrait ? 3 : 1;
            int rows = Mathf.CeilToInt(count / (float)columns);
            float gapX = portrait ? 0.020f : 0.035f;
            float gapY = portrait ? 0.055f : 0.030f;
            float cardWidth = (1f - gapX * (columns + 1)) / columns;
            float cardHeight = portrait ? 0.40f : (1f - gapY * (rows + 1)) / rows;

            for (int i = 0; i < count; i++)
            {
                int column = portrait ? i % columns : 0;
                int row = portrait ? i / columns : i;
                float minX = gapX + column * (cardWidth + gapX);
                float maxX = minX + cardWidth;
                float maxY = 1f - gapY - row * (cardHeight + gapY);
                float minY = maxY - cardHeight;

                RectTransform buttonRect = entryButtons[i].transform as RectTransform;
                SetRect(buttonRect, new Vector2(minX, Mathf.Max(0.02f, minY)), new Vector2(maxX, Mathf.Min(0.98f, maxY)));

                RectTransform portraitRect = entryPortraits[i].rectTransform;
                RectTransform labelRect = entryLabels[i].rectTransform;
                if (portrait)
                {
                    SetRect(portraitRect, new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.88f));
                    SetRect(labelRect, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.42f));
                    entryLabels[i].alignment = TextAlignmentOptions.Center;
                }
                else
                {
                    SetRect(portraitRect, new Vector2(0.055f, 0.17f), new Vector2(0.30f, 0.83f));
                    SetRect(labelRect, new Vector2(0.36f, 0.16f), new Vector2(0.95f, 0.84f));
                    entryLabels[i].alignment = TextAlignmentOptions.Left;
                }
            }
        }

        private void RefreshEntryCard(int index, OrbiosisBestiaryEntry entry)
        {
            bool unlocked = OrbiosisBestiaryProgress.IsUnlocked(entry.Id);
            bool selected = entry.Id == selectedId;
            Button button = entryButtons[index];
            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = selected ? new Color(0.055f, 0.145f, 0.18f, 0.96f) : unlocked ? Card : LockedCard;

            TextMeshProUGUI label = entryLabels[index];
            if (label != null)
            {
                label.text = unlocked ? entry.DisplayName + "\n<size=72%><color=#93B5CFFF>" + entry.ClassName + "</color></size>" : "UNKNOWN\n<size=72%><color=#6E7A8AFF>Encounter to unlock</color></size>";
                label.color = unlocked ? Ink : MutedInk;
            }

            Image portrait = entryPortraits[index];
            if (portrait != null)
            {
                ApplySprite(portrait, entry.SpriteResourcePath);
                portrait.color = unlocked ? Color.white : new Color(0.20f, 0.24f, 0.30f, 0.58f);
            }
        }

        private void RefreshProfile()
        {
            OrbiosisBestiaryEntry entry = OrbiosisBestiaryLibrary.Find(selectedId);
            bool unlocked = entry != null && OrbiosisBestiaryProgress.IsUnlocked(entry.Id);
            if (entry == null || !unlocked)
            {
                if (titleText != null)
                    titleText.text = "UNKNOWN CONTACT";
                if (subtitleText != null)
                    subtitleText.text = "Profile locked";
                if (profileText != null)
                    profileText.text = "Encounter this enemy during a run to unlock its profile, HP, behavior, threat level, and combat notes.";
                if (profilePortrait != null)
                    profilePortrait.color = new Color(0.20f, 0.24f, 0.30f, 0.58f);
                return;
            }

            ApplySprite(profilePortrait, entry.SpriteResourcePath);
            profilePortrait.color = Color.white;
            titleText.text = entry.DisplayName;
            subtitleText.text = entry.ClassName + "  /  " + entry.Threat;
            profileText.text =
                entry.Description +
                "\n\n<size=92%><color=#FFD05C>HP</color>  " + entry.HitPoints +
                "\n<color=#FFD05C>DOES</color>  " + entry.Behavior +
                "\n<color=#FFD05C>TACTIC</color>  " + entry.Tactics + "</size>";
        }

        private void Select(string id)
        {
            selectedId = id;
            Refresh();
        }

        private void Close()
        {
            Hide();
            if (closeAction != null)
                closeAction.Invoke();
        }

        private static string FirstUnlockedId(OrbiosisBestiaryEntry[] entries)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (OrbiosisBestiaryProgress.IsUnlocked(entries[i].Id))
                    return entries[i].Id;
            }

            return string.Empty;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, float fontSize, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.GetComponent<Image>();
            image.color = Card;
            AddOutline(rect, new Color(0.16f, 0.80f, 0.92f, 0.30f), new Vector2(2f, -2f));

            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.80f, 0.96f, 1f, 1f);
            colors.pressedColor = new Color(0.62f, 0.78f, 0.90f, 1f);
            button.colors = colors;
            if (action != null)
                button.onClick.AddListener(action);

            if (!string.IsNullOrEmpty(label))
            {
                TextMeshProUGUI text = CreateText(go.transform, "Label", label, fontSize, FontStyles.Bold, Ink);
                Stretch(text.rectTransform);
                text.rectTransform.offsetMin = new Vector2(8f, 0f);
                text.rectTransform.offsetMax = new Vector2(-8f, 0f);
            }

            return button;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            Image image = CreateImage(parent, name, color);
            image.raycastTarget = true;
            return image.rectTransform;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string value, float size, FontStyles style, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(13f, size * 0.58f);
            text.fontSizeMax = size;
            text.raycastTarget = false;
            MainLobbyButtonStyle.ApplyFont(text);
            return text;
        }

        private static void ApplySprite(Image image, string resourcePath)
        {
            if (image == null)
                return;

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                    sprite.name = resourcePath.Replace("/", "_") + "_BestiarySprite";
                }
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static void AddOutline(RectTransform rect, Color color, Vector2 distance)
        {
            Outline outline = rect.gameObject.GetComponent<Outline>();
            if (outline == null)
                outline = rect.gameObject.AddComponent<Outline>();

            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
