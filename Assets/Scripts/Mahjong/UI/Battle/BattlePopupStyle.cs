using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{

public static class BattlePopupStyle
{
	private enum BattleButtonAtlasCell
	{
		Normal,
		Highlighted,
		Pressed,
		Premium,
		Secondary,
		Danger,
		DisabledGold,
		SecondaryAlt,
		DangerAlt,
		DisabledSilver,
		Locked,
		Inactive
	}

	private const string WindowResourcePath = "Mahjong/Sprites/BattleLobbyParts/WindowForBattleLobby";

	private const string FrontResourcePath = "Mahjong/Sprites/BattleLobbyParts/PartSquare";

	private const string CloseIconResourcePath = "Mahjong/Sprites/BattleLobbyParts/XCloseIcon";

	private const string FallbackWindowResourcePath = "Mahjong/Sprites/BattleLobbyUI/BattleSettingsWindowV2";

	private const string ButtonResourcePath = "Mahjong/Sprites/BattleLobbyUI/BattleLobbyButtonsV2";

	private const string BattleSingleButtonResourcePath = "Mahjong/Sprites/BattleLobbyParts/PartButtonWide";

	private const string BattleButtonAtlasResourcePath = "Mahjong/Sprites/BattleUI/ButtonsForBattleMahjong";

	private const string MainFontResourcePath = "Fonts/Philosopher-Regular";

	private const string FallbackFontResourcePath = "Fonts/Trade SDF";

	private const string WindowSpriteName = "BattleSettingsWindowV2_0";

	private const string LongButtonSpriteName = "BattleButtonLong_0";

	private const string SquareButtonSpriteName = "BattleButtonSquare_0";

	private const string MediumButtonSpriteName = "BattleButtonMedium_0";

	private const string SmallButtonSpriteName = "BattleButtonSmall_0";

	private static readonly Vector4 BattleWindowBorder = new Vector4(132f, 116f, 132f, 116f);

	private static readonly Vector4 BattleFrontBorder = new Vector4(62f, 62f, 62f, 62f);

	private static readonly Vector4 BattleSingleButtonBorder = new Vector4(96f, 56f, 96f, 56f);

	private static readonly Vector4 BattleAtlasButtonBorder = new Vector4(58f, 38f, 58f, 38f);

	private static readonly Rect BattleWindowUsefulRect = Rect.zero;

	private static readonly Rect BattleFrontUsefulRect = Rect.zero;

	private static readonly Rect BattleButtonUsefulRect = Rect.zero;

	private static readonly Rect BattleLobbyButtonUsefulRect = Rect.zero;

	private static readonly Vector4 BattleLobbyButtonBorder = new Vector4(150f, 78f, 150f, 78f);

	private static readonly Vector4 BattleLobbyUtilityButtonMargin = new Vector4(78f, 14f, 78f, 16f);

	private static readonly Vector4 CompactButtonMargin = new Vector4(52f, 9f, 52f, 11f);

	private static readonly Vector4 LargeButtonMargin = new Vector4(78f, 14f, 78f, 16f);

	private static Sprite cachedWindowSourceSprite;

	private static Sprite cachedWindowSprite;

	private static Sprite cachedFrontSprite;

	private static Sprite cachedLongButtonSprite;

	private static Sprite cachedSquareButtonSprite;

	private static Sprite cachedMediumButtonSprite;

	private static Sprite cachedSmallButtonSprite;

	private static Sprite cachedBattleLobbyButtonSprite;

	private static Sprite cachedBattleSingleButtonSprite;

	private static Sprite cachedCloseIconSprite;

	private static Sprite[] cachedBattleButtonAtlasSprites;

	private static TMP_FontAsset cachedFont;

	public static Sprite WindowSprite => LoadWindowSprite();

	public static Sprite FrontSprite => LoadFrontSprite();

	public static Sprite ButtonSprite => LoadMediumButtonSprite();

	public static Sprite LongButtonSprite => LoadLongButtonSprite();

	public static Sprite SquareButtonSprite => LoadSquareButtonSprite();

	public static Sprite MediumButtonSprite => LoadMediumButtonSprite();

	public static Sprite SmallButtonSprite => LoadSmallButtonSprite();

	public static TMP_FontAsset Font => LoadFont();

	public static bool ApplyWindow(Image image, bool raycastTarget = true)
	{
		return ApplyFrameImage(image, LoadWindowSprite(), raycastTarget);
	}

	public static bool ApplyFront(Image image, bool raycastTarget = false)
	{
		return ApplyFrameImage(image, LoadFrontSprite(), raycastTarget);
	}

	public static bool ApplyButton(Button button, bool preserveCurrentColor = false, bool keepLabelVisible = true)
	{
		if (button == null || button.image == null)
		{
			return false;
		}
		Color color = (preserveCurrentColor ? button.image.color : Color.white);
		Sprite sprite = LoadBattleSingleButtonSprite() ?? LoadBattleButtonAtlasSprite(BattleButtonAtlasCell.Normal) ?? LoadButtonSpriteForButton(button);
		if (sprite == null)
		{
			return false;
		}
		button.image.sprite = sprite;
		button.image.type = ((!(sprite == cachedBattleSingleButtonSprite) && sprite.border.sqrMagnitude > 0.01f) ? Image.Type.Sliced : Image.Type.Simple);
		button.image.preserveAspect = false;
		button.image.color = color;
		button.image.raycastTarget = true;
		button.targetGraphic = button.image;
		ApplyBattleButtonSpriteState(button, color);
		TMP_Text componentInChildren = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
		if (componentInChildren != null)
		{
			ApplyText(componentInChildren, silver: true);
			componentInChildren.alignment = TextAlignmentOptions.Center;
			componentInChildren.enableAutoSizing = true;
			componentInChildren.fontSizeMin = Mathf.Max(12f, componentInChildren.fontSize * 0.58f);
			componentInChildren.fontSizeMax = Mathf.Max(componentInChildren.fontSize, componentInChildren.fontSizeMax);
			componentInChildren.textWrappingMode = TextWrappingModes.NoWrap;
			componentInChildren.overflowMode = TextOverflowModes.Truncate;
			componentInChildren.margin = (IsLargeButton(button) ? LargeButtonMargin : CompactButtonMargin);
			componentInChildren.gameObject.SetActive(keepLabelVisible);
		}
		return true;
	}

	public static bool ApplyBattleLobbyUtilityButton(Button button, float labelFontSize = 46f)
	{
		if (button == null || button.image == null)
		{
			return false;
		}
		Sprite sprite = LoadBattleLobbyButtonSprite();
		if (sprite == null)
		{
			return ApplyButton(button);
		}
		button.image.sprite = sprite;
		button.image.type = Image.Type.Simple;
		button.image.preserveAspect = false;
		button.image.color = Color.white;
		button.image.raycastTarget = true;
		button.targetGraphic = button.image;
		button.transition = Selectable.Transition.ColorTint;
		ColorBlock colors = button.colors;
		colors.normalColor = Color.white;
		colors.highlightedColor = new Color(1.08f, 1.05f, 0.92f, 1f);
		colors.pressedColor = new Color(0.82f, 0.76f, 0.64f, 1f);
		colors.selectedColor = new Color(1.04f, 1.02f, 0.9f, 1f);
		colors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.82f);
		colors.colorMultiplier = 1f;
		button.colors = colors;
		TMP_Text componentInChildren = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
		if (componentInChildren != null)
		{
			ApplyText(componentInChildren, silver: true);
			componentInChildren.alignment = TextAlignmentOptions.Center;
			componentInChildren.fontSize = labelFontSize;
			componentInChildren.enableAutoSizing = true;
			componentInChildren.fontSizeMin = Mathf.Max(18f, labelFontSize * 0.6f);
			componentInChildren.fontSizeMax = Mathf.Max(labelFontSize, 50f);
			componentInChildren.textWrappingMode = TextWrappingModes.NoWrap;
			componentInChildren.overflowMode = TextOverflowModes.Truncate;
			componentInChildren.margin = BattleLobbyUtilityButtonMargin;
			componentInChildren.raycastTarget = false;
			componentInChildren.gameObject.SetActive(value: true);
		}
		return true;
	}

	public static bool ApplyCloseIconButton(Button button)
	{
		if (button == null || button.image == null)
		{
			return false;
		}
		Sprite sprite = LoadCloseIconSprite();
		if (sprite == null)
		{
			return ApplyDangerButton(button);
		}
		button.image.enabled = true;
		button.image.sprite = sprite;
		button.image.type = Image.Type.Simple;
		button.image.preserveAspect = true;
		button.image.color = Color.white;
		button.image.raycastTarget = true;
		button.targetGraphic = button.image;
		TMP_Text componentInChildren = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
		if (componentInChildren != null)
		{
			componentInChildren.gameObject.SetActive(value: false);
		}
		ApplyBattleButtonSpriteState(button, Color.white);
		return true;
	}

	public static void ApplyButtonLabel(Button button, float fallbackFontSize = 22f, bool keepLabelVisible = true)
	{
		TMP_Text tMP_Text = ((button != null) ? button.GetComponentInChildren<TMP_Text>(includeInactive: true) : null);
		if (!(tMP_Text == null))
		{
			ApplyText(tMP_Text, silver: true);
			tMP_Text.alignment = TextAlignmentOptions.Center;
			tMP_Text.enableAutoSizing = true;
			tMP_Text.fontSizeMin = Mathf.Max(11f, fallbackFontSize * 0.52f);
			tMP_Text.fontSizeMax = Mathf.Max(fallbackFontSize, tMP_Text.fontSizeMax);
			tMP_Text.textWrappingMode = TextWrappingModes.NoWrap;
			tMP_Text.overflowMode = TextOverflowModes.Truncate;
			tMP_Text.margin = (IsLargeButton(button) ? LargeButtonMargin : CompactButtonMargin);
			tMP_Text.gameObject.SetActive(keepLabelVisible);
		}
	}

	public static void ApplyText(TMP_Text text, bool silver = false)
	{
		if (!(text == null))
		{
			ApplyFontOnly(text);
			if (silver)
			{
				MainLobbyButtonStyle.ApplySilverTextEffect(text);
				return;
			}
			text.enableVertexGradient = false;
			text.color = Color.white;
		}
	}

	public static void ApplyFontOnly(TMP_Text text)
	{
		if (!(text == null))
		{
			TMP_FontAsset tMP_FontAsset = LoadFont();
			if (!(tMP_FontAsset == null))
			{
				text.font = tMP_FontAsset;
				text.fontSharedMaterial = tMP_FontAsset.material;
			}
		}
	}

	private static bool ApplyFrameImage(Image image, Sprite sprite, bool raycastTarget)
	{
		if (image == null || sprite == null)
		{
			return false;
		}
		image.enabled = true;
		image.sprite = sprite;
		image.type = ((sprite.border.sqrMagnitude > 0.01f) ? Image.Type.Sliced : Image.Type.Simple);
		image.preserveAspect = false;
		image.color = Color.white;
		image.raycastTarget = raycastTarget;
		return true;
	}

	private static bool IsLargeButton(Button button)
	{
		RectTransform rectTransform = ((button != null) ? button.GetComponent<RectTransform>() : null);
		if (rectTransform == null)
		{
			return false;
		}
		if (!(rectTransform.rect.width >= 250f))
		{
			return rectTransform.rect.height >= 70f;
		}
		return true;
	}

	private static void FreezeButtonColors(Button button, Color color)
	{
		if (!(button == null))
		{
			button.transition = Selectable.Transition.None;
			ColorBlock colors = button.colors;
			colors.normalColor = color;
			colors.highlightedColor = color;
			colors.pressedColor = color;
			colors.selectedColor = color;
			colors.disabledColor = color;
			colors.colorMultiplier = 1f;
			button.colors = colors;
		}
	}

	private static Sprite LoadWindowSprite()
	{
		if (cachedWindowSprite != null)
		{
			return cachedWindowSprite;
		}
		Sprite sprite = LoadWindowSourceSprite();
		if (sprite == null)
		{
			return null;
		}
		cachedWindowSprite = CreateSlicedSprite(sprite, BattleWindowBorder);
		return cachedWindowSprite;
	}

	private static Sprite LoadFrontSprite()
	{
		if (cachedFrontSprite != null)
		{
			return cachedFrontSprite;
		}
		Sprite sprite = LoadFrontSourceSprite();
		if (sprite == null)
		{
			return null;
		}
		cachedFrontSprite = CreateSlicedSprite(sprite, BattleFrontBorder);
		return cachedFrontSprite;
	}

	public static Sprite GetButtonSpriteForSize(Vector2 size)
	{
		Sprite sprite = LoadBattleSingleButtonSprite();
		if (sprite != null)
		{
			return sprite;
		}
		Sprite sprite2 = LoadBattleButtonAtlasSprite(BattleButtonAtlasCell.Normal);
		if (sprite2 != null)
		{
			return sprite2;
		}
		if (size.x >= 300f)
		{
			return LoadLongButtonSprite();
		}
		if (size.x <= 145f || Mathf.Abs(size.x - size.y) <= 35f)
		{
			return LoadSquareButtonSprite() ?? LoadSmallButtonSprite();
		}
		if (size.x <= 175f)
		{
			return LoadSmallButtonSprite() ?? LoadMediumButtonSprite();
		}
		return LoadMediumButtonSprite() ?? LoadLongButtonSprite();
	}

	public static bool ApplyPremiumButton(Button button)
	{
		return ApplyButtonVariant(button, BattleButtonAtlasCell.Premium);
	}

	public static bool ApplySecondaryButton(Button button)
	{
		return ApplyButtonVariant(button, BattleButtonAtlasCell.Secondary);
	}

	public static bool ApplyDangerButton(Button button)
	{
		return ApplyButtonVariant(button, BattleButtonAtlasCell.Danger);
	}

	public static bool ApplyLockedButton(Button button)
	{
		bool result = ApplyButtonVariant(button, BattleButtonAtlasCell.Locked);
		if (button != null)
		{
			button.interactable = false;
		}
		return result;
	}

	public static bool ApplyInactiveButton(Button button)
	{
		bool result = ApplyButtonVariant(button, BattleButtonAtlasCell.Inactive);
		if (button != null)
		{
			button.interactable = false;
		}
		return result;
	}

	private static bool ApplyButtonVariant(Button button, BattleButtonAtlasCell cell)
	{
		if (button == null || button.image == null)
		{
			return false;
		}
		Sprite sprite = LoadBattleSingleButtonSprite() ?? LoadBattleButtonAtlasSprite(cell);
		if (sprite == null)
		{
			return ApplyButton(button);
		}
		button.image.sprite = sprite;
		button.image.type = ((!(sprite == cachedBattleSingleButtonSprite) && sprite.border.sqrMagnitude > 0.01f) ? Image.Type.Sliced : Image.Type.Simple);
		button.image.preserveAspect = false;
		button.image.color = Color.white;
		button.image.raycastTarget = true;
		button.targetGraphic = button.image;
		ApplyBattleButtonSpriteState(button, Color.white);
		ApplyButtonLabel(button);
		return true;
	}

	private static Sprite LoadButtonSpriteForButton(Button button)
	{
		RectTransform rectTransform = ((button != null) ? button.GetComponent<RectTransform>() : null);
		return GetButtonSpriteForSize((rectTransform != null) ? rectTransform.rect.size : Vector2.zero);
	}

	private static Sprite LoadBattleSingleButtonSprite()
	{
		if (cachedBattleSingleButtonSprite != null)
		{
			return cachedBattleSingleButtonSprite;
		}
		Sprite sprite = LoadCroppedSprite("Mahjong/Sprites/BattleLobbyParts/PartButtonWide", BattleButtonUsefulRect);
		cachedBattleSingleButtonSprite = ((BattleSingleButtonBorder.sqrMagnitude > 0.01f) ? CreateSlicedSprite(sprite, BattleSingleButtonBorder) : sprite);
		return cachedBattleSingleButtonSprite;
	}

	private static void ApplyBattleButtonSpriteState(Button button, Color color)
	{
		if (!(button == null))
		{
			Sprite sprite = LoadBattleSingleButtonSprite();
			SpriteState spriteState = button.spriteState;
			spriteState.highlightedSprite = ((sprite != null) ? sprite : LoadBattleButtonAtlasSprite(BattleButtonAtlasCell.Highlighted));
			spriteState.pressedSprite = ((sprite != null) ? sprite : LoadBattleButtonAtlasSprite(BattleButtonAtlasCell.Pressed));
			spriteState.selectedSprite = spriteState.highlightedSprite;
			spriteState.disabledSprite = null;
			button.spriteState = spriteState;
			button.transition = ((sprite != null) ? Selectable.Transition.ColorTint : Selectable.Transition.SpriteSwap);
			ColorBlock colors = button.colors;
			colors.normalColor = color;
			colors.highlightedColor = ((sprite != null) ? (color * 1.08f) : color);
			colors.pressedColor = ((sprite != null) ? (color * 0.86f) : color);
			colors.selectedColor = ((sprite != null) ? (color * 1.04f) : color);
			colors.disabledColor = new Color(color.r, color.g, color.b, 0.58f);
			colors.colorMultiplier = 1f;
			button.colors = colors;
		}
	}

	private static Sprite LoadBattleButtonAtlasSprite(BattleButtonAtlasCell cell)
	{
		if (cachedBattleButtonAtlasSprites == null)
		{
			cachedBattleButtonAtlasSprites = LoadBattleButtonAtlasSprites();
		}
		if (cachedBattleButtonAtlasSprites == null || cell < BattleButtonAtlasCell.Normal || (int)cell >= cachedBattleButtonAtlasSprites.Length)
		{
			return null;
		}
		return cachedBattleButtonAtlasSprites[(int)cell];
	}

	private static Sprite[] LoadBattleButtonAtlasSprites()
	{
		Texture2D texture2D = Resources.Load<Texture2D>("Mahjong/Sprites/BattleUI/ButtonsForBattleMahjong");
		if (texture2D == null)
		{
			return new Sprite[0];
		}
		int num = 12;
		float cellWidth = (float)texture2D.width / 3f;
		float cellHeight = (float)texture2D.height / 4f;
		Sprite[] array = new Sprite[num];
		for (int i = 0; i < num; i++)
		{
			int column = i % 3;
			int row = i / 3;
			Rect battleButtonAtlasRect = GetBattleButtonAtlasRect(texture2D.height, cellWidth, cellHeight, column, row);
			array[i] = Sprite.Create(texture2D, battleButtonAtlasRect, new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, BattleAtlasButtonBorder);
		}
		return array;
	}

	private static Rect GetBattleButtonAtlasRect(float textureHeight, float cellWidth, float cellHeight, int column, int row)
	{
		float x = (float)column * cellWidth + 66f;
		float num = (float)row * cellHeight + 88f;
		float width = 426f;
		float num2 = 160f;
		float y = textureHeight - num - num2;
		return new Rect(x, y, width, num2);
	}

	private static Sprite LoadLongButtonSprite()
	{
		if (cachedLongButtonSprite != null)
		{
			return cachedLongButtonSprite;
		}
		cachedLongButtonSprite = LoadNamedSprite("Mahjong/Sprites/BattleLobbyUI/BattleLobbyButtonsV2", "BattleButtonLong_0");
		return cachedLongButtonSprite;
	}

	private static Sprite LoadSquareButtonSprite()
	{
		if (cachedSquareButtonSprite != null)
		{
			return cachedSquareButtonSprite;
		}
		cachedSquareButtonSprite = LoadNamedSprite("Mahjong/Sprites/BattleLobbyUI/BattleLobbyButtonsV2", "BattleButtonSquare_0");
		return cachedSquareButtonSprite;
	}

	private static Sprite LoadMediumButtonSprite()
	{
		if (cachedMediumButtonSprite != null)
		{
			return cachedMediumButtonSprite;
		}
		cachedMediumButtonSprite = LoadNamedSprite("Mahjong/Sprites/BattleLobbyUI/BattleLobbyButtonsV2", "BattleButtonMedium_0");
		return cachedMediumButtonSprite;
	}

	private static Sprite LoadSmallButtonSprite()
	{
		if (cachedSmallButtonSprite != null)
		{
			return cachedSmallButtonSprite;
		}
		cachedSmallButtonSprite = LoadNamedSprite("Mahjong/Sprites/BattleLobbyUI/BattleLobbyButtonsV2", "BattleButtonSmall_0");
		return cachedSmallButtonSprite;
	}

	private static Sprite LoadBattleLobbyButtonSprite()
	{
		if (cachedBattleLobbyButtonSprite != null)
		{
			return cachedBattleLobbyButtonSprite;
		}
		Texture2D texture2D = Resources.Load<Texture2D>("Mahjong/Sprites/BattleLobbyUI/BattleLobbyButtonsV2");
		if (texture2D != null)
		{
			cachedBattleLobbyButtonSprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, BattleLobbyButtonBorder);
			return cachedBattleLobbyButtonSprite;
		}
		Sprite sprite = Resources.Load<Sprite>("Mahjong/Sprites/BattleLobbyUI/BattleLobbyButtonsV2");
		if (sprite == null)
		{
			Sprite[] array = Resources.LoadAll<Sprite>("Mahjong/Sprites/BattleLobbyUI/BattleLobbyButtonsV2");
			if (array != null && array.Length != 0)
			{
				sprite = array[0];
			}
		}
		if (sprite == null || sprite.texture == null)
		{
			return null;
		}
		Rect rect = ((BattleLobbyButtonUsefulRect.width <= 0.5f || BattleLobbyButtonUsefulRect.height <= 0.5f) ? sprite.rect : ClampRectToBounds(BattleLobbyButtonUsefulRect, sprite.textureRect));
		cachedBattleLobbyButtonSprite = Sprite.Create(sprite.texture, rect, new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit, 0u, SpriteMeshType.FullRect, BattleLobbyButtonBorder);
		return cachedBattleLobbyButtonSprite;
	}

	private static TMP_FontAsset LoadFont()
	{
		if (cachedFont != null)
		{
			MahjongGame.LocalizedTextStyle.EnsureRuntimeFallbacks(cachedFont);
			return cachedFont;
		}
		cachedFont = Resources.Load<TMP_FontAsset>("Fonts/Philosopher-Regular");
		if (cachedFont == null)
		{
			Font font = Resources.Load<Font>("Fonts/Philosopher-Regular");
			if (font != null)
			{
				cachedFont = TMP_FontAsset.CreateFontAsset(font);
				if (cachedFont != null)
				{
					cachedFont.name = "Philosopher Battle Runtime SDF";
				}
			}
		}
		if (cachedFont == null)
		{
			cachedFont = Resources.Load<TMP_FontAsset>("Fonts/Trade SDF");
		}
		if (cachedFont == null)
		{
			cachedFont = TMP_Settings.defaultFontAsset;
		}
		MahjongGame.LocalizedTextStyle.EnsureRuntimeFallbacks(cachedFont);
		return cachedFont;
	}

	private static Sprite LoadWindowSourceSprite()
	{
		if (cachedWindowSourceSprite != null)
		{
			return cachedWindowSourceSprite;
		}
		cachedWindowSourceSprite = LoadCroppedSprite("Mahjong/Sprites/BattleLobbyParts/WindowForBattleLobby", BattleWindowUsefulRect);
		if (cachedWindowSourceSprite == null)
		{
			cachedWindowSourceSprite = LoadNamedSprite("Mahjong/Sprites/BattleLobbyUI/BattleSettingsWindowV2", "BattleSettingsWindowV2_0");
		}
		return cachedWindowSourceSprite;
	}

	private static Sprite LoadFrontSourceSprite()
	{
		return LoadCroppedSprite("Mahjong/Sprites/BattleLobbyParts/PartSquare", BattleFrontUsefulRect) ?? LoadWindowSourceSprite();
	}

	private static Sprite LoadCloseIconSprite()
	{
		if (cachedCloseIconSprite != null)
		{
			return cachedCloseIconSprite;
		}
		cachedCloseIconSprite = LoadCroppedSprite("Mahjong/Sprites/BattleLobbyParts/XCloseIcon", Rect.zero);
		return cachedCloseIconSprite;
	}

	private static Sprite LoadNamedSprite(string resourcePath, string preferredSpriteName)
	{
		return LoadLargestSprite(resourcePath, preferredSpriteName);
	}

	private static Sprite LoadLargestSprite(string resourcePath, string preferredSpriteName)
	{
		Sprite[] array = Resources.LoadAll<Sprite>(resourcePath);
		if (array != null && array.Length != 0)
		{
			if (!string.IsNullOrWhiteSpace(preferredSpriteName))
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] != null && string.Equals(array[i].name, preferredSpriteName, StringComparison.Ordinal))
					{
						return array[i];
					}
				}
			}
			Sprite sprite = null;
			float num = 0f;
			foreach (Sprite sprite2 in array)
			{
				if (!(sprite2 == null))
				{
					float num2 = sprite2.rect.width * sprite2.rect.height;
					if (sprite == null || num2 > num)
					{
						sprite = sprite2;
						num = num2;
					}
				}
			}
			if (sprite != null)
			{
				return sprite;
			}
		}
		return Resources.Load<Sprite>(resourcePath);
	}

	private static Sprite CreateRuntimeSpriteVariant(Sprite source, Rect targetRect)
	{
		if (source == null || source.texture == null)
		{
			return source;
		}
		Rect textureRect = source.textureRect;
		Rect rect = ClampRectToBounds(targetRect, textureRect);
		if (Mathf.Approximately(rect.x, textureRect.x) && Mathf.Approximately(rect.y, textureRect.y) && Mathf.Approximately(rect.width, textureRect.width) && Mathf.Approximately(rect.height, textureRect.height))
		{
			return source;
		}
		return Sprite.Create(source.texture, rect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0u, SpriteMeshType.FullRect);
	}

	private static Sprite LoadCroppedSprite(string resourcePath, Rect usefulRect)
	{
		if (usefulRect.width <= 0.5f || usefulRect.height <= 0.5f)
		{
			Texture2D texture2D = Resources.Load<Texture2D>(resourcePath);
			if (texture2D != null)
			{
				return Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
			}
		}
		Sprite sprite = LoadLargestSprite(resourcePath, null);
		if (sprite == null)
		{
			Texture2D texture2D2 = Resources.Load<Texture2D>(resourcePath);
			if (texture2D2 != null)
			{
				sprite = Sprite.Create(texture2D2, new Rect(0f, 0f, texture2D2.width, texture2D2.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
			}
		}
		if (sprite == null)
		{
			return null;
		}
		if (usefulRect.width <= 0.5f || usefulRect.height <= 0.5f)
		{
			return sprite;
		}
		return CreateRuntimeSpriteVariant(sprite, usefulRect);
	}

	private static Sprite CreateSlicedSprite(Sprite source, Vector4 border)
	{
		if (source == null || source.texture == null)
		{
			return source;
		}
		return Sprite.Create(source.texture, source.rect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0u, SpriteMeshType.FullRect, border);
	}

	private static Rect ClampRectToBounds(Rect targetRect, Rect bounds)
	{
		float num = Mathf.Clamp(targetRect.x, bounds.xMin, bounds.xMax - 1f);
		float num2 = Mathf.Clamp(targetRect.y, bounds.yMin, bounds.yMax - 1f);
		float width = Mathf.Clamp(targetRect.width, 1f, bounds.xMax - num);
		float height = Mathf.Clamp(targetRect.height, 1f, bounds.yMax - num2);
		return new Rect(num, num2, width, height);
	}
}

public static class BattleTileUpgradeVisual
{
	private const string RootName = "UpgradeStars";

	private const string UpgradeMarkResourcePath = "Mahjong/Sprites/BattleUI/BattleTileUpgradeDiamond";

	private static Sprite cachedUpgradeMarkSprite;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void CleanupRuntimeOrphans()
	{
		CleanupOrphanedRoots(immediate: false);
	}

#if UNITY_EDITOR
	[UnityEditor.InitializeOnLoadMethod]
	private static void ScheduleEditorOrphanCleanup()
	{
		UnityEditor.EditorApplication.delayCall -= CleanupEditorOrphans;
		UnityEditor.EditorApplication.delayCall += CleanupEditorOrphans;
	}

	private static void CleanupEditorOrphans()
	{
		if (!Application.isPlaying)
		{
			CleanupOrphanedRoots(immediate: true);
		}
	}
#endif

	public static void Apply(Transform parent, Vector2 facePosition, Vector2 faceSize, int upgradeLevel, bool visible = true)
	{
		if (parent == null || !parent.gameObject.scene.IsValid())
		{
			return;
		}

#if UNITY_EDITOR
		if (UnityEditor.EditorUtility.IsPersistent(parent.gameObject) || UnityEditor.PrefabUtility.IsPartOfPrefabAsset(parent.gameObject))
		{
			return;
		}
#endif

		Sprite upgradeMarkSprite = LoadUpgradeMarkSprite();
		bool flag = visible && upgradeLevel > 0 && upgradeMarkSprite != null;
		Transform existingRoot = parent.Find(RootName);
		if (!flag)
		{
			if (existingRoot != null)
			{
				existingRoot.gameObject.SetActive(false);
			}
			return;
		}
		RectTransform orCreateRoot = GetOrCreateRoot(parent);
		if (orCreateRoot == null || orCreateRoot.parent != parent)
		{
			return;
		}
		orCreateRoot.anchoredPosition = facePosition;
		orCreateRoot.sizeDelta = faceSize;
		orCreateRoot.SetAsLastSibling();
		orCreateRoot.gameObject.SetActive(flag);
		float num = faceSize.x * 0.3f;
		float num2 = faceSize.y * 0.34f;
		Vector2[] array = new Vector2[4]
		{
			new Vector2(0f - num, num2),
			new Vector2(num, num2),
			new Vector2(0f - num, 0f - num2),
			new Vector2(num, 0f - num2)
		};
		float num3 = Mathf.Clamp(Mathf.Min(faceSize.x, faceSize.y) * 0.19f, 18f, 54f);
		int num4 = Mathf.Min(upgradeLevel, 4);
		for (int i = 0; i < 4; i++)
		{
			Image orCreateStar = GetOrCreateStar(orCreateRoot, i);
			orCreateStar.rectTransform.anchoredPosition = array[i];
			orCreateStar.rectTransform.sizeDelta = new Vector2(num3, num3);
			orCreateStar.sprite = upgradeMarkSprite;
			orCreateStar.color = Color.white;
			orCreateStar.preserveAspect = true;
			orCreateStar.gameObject.SetActive(i < num4);
			Transform oldCore = orCreateStar.transform.Find("Core");
			if (oldCore != null)
			{
				oldCore.gameObject.SetActive(false);
			}
			TMP_Text orCreateOverflowLevel = GetOrCreateOverflowLevel(orCreateStar.transform);
			bool flag2 = i == 3 && upgradeLevel > 4;
			orCreateOverflowLevel.gameObject.SetActive(flag2);
			if (flag2)
			{
				orCreateOverflowLevel.text = upgradeLevel.ToString();
				orCreateOverflowLevel.fontSize = Mathf.Clamp(num3 * 0.48f, 10f, 24f);
				orCreateOverflowLevel.fontSizeMax = orCreateOverflowLevel.fontSize;
			}
		}
	}

	public static void SetVisible(Transform parent, bool visible)
	{
		Transform transform = ((parent != null) ? parent.Find(RootName) : null);
		if (transform != null)
		{
			transform.gameObject.SetActive(visible);
		}
	}

	private static RectTransform GetOrCreateRoot(Transform parent)
	{
		Transform transform = parent.Find(RootName);
		GameObject gameObject = ((transform != null) ? transform.gameObject : new GameObject(RootName, typeof(RectTransform)));
		if (transform == null)
		{
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			if (gameObject.transform.parent != parent)
			{
				DestroyVisualObject(gameObject, immediateInEditor: true);
				return null;
			}
		}
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.localScale = Vector3.one;
		component.localRotation = Quaternion.identity;
		return component;
	}

	private static void CleanupOrphanedRoots(bool immediate)
	{
		RectTransform[] roots = Resources.FindObjectsOfTypeAll<RectTransform>();
		for (int i = 0; i < roots.Length; i++)
		{
			RectTransform candidate = roots[i];
			if (candidate != null && candidate.parent == null && string.Equals(candidate.name, RootName, StringComparison.Ordinal) && candidate.gameObject.scene.IsValid())
			{
				DestroyVisualObject(candidate.gameObject, immediate);
			}
		}
	}

	private static void DestroyVisualObject(GameObject target, bool immediateInEditor)
	{
		if (target == null)
		{
			return;
		}
#if UNITY_EDITOR
		if (immediateInEditor && !Application.isPlaying)
		{
			UnityEngine.Object.DestroyImmediate(target);
			return;
		}
#endif
		UnityEngine.Object.Destroy(target);
	}

	private static Image GetOrCreateStar(Transform parent, int index)
	{
		string text = "UpgradeStar" + (index + 1);
		Transform transform = parent.Find(text);
		GameObject gameObject = ((transform != null) ? transform.gameObject : new GameObject(text, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)));
		if (transform == null)
		{
			gameObject.transform.SetParent(parent, worldPositionStays: false);
		}
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.localScale = Vector3.one;
		component.localRotation = Quaternion.identity;
		Image component2 = gameObject.GetComponent<Image>();
		component2.raycastTarget = false;
		Outline outline = gameObject.GetComponent<Outline>();
		if (outline != null)
		{
			outline.enabled = false;
		}
		return component2;
	}

	private static TMP_Text GetOrCreateOverflowLevel(Transform parent)
	{
		Transform transform = parent.Find("Level");
		GameObject gameObject = ((transform != null) ? transform.gameObject : new GameObject("Level", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)));
		if (transform == null)
		{
			gameObject.transform.SetParent(parent, worldPositionStays: false);
		}
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		component.localScale = Vector3.one;
		component.localRotation = Quaternion.identity;
		TextMeshProUGUI component2 = gameObject.GetComponent<TextMeshProUGUI>();
		component2.alignment = TextAlignmentOptions.Center;
		component2.enableAutoSizing = true;
		component2.fontSizeMin = 8f;
		component2.color = Color.white;
		component2.fontStyle |= FontStyles.Bold;
		component2.outlineColor = new Color(0.06f, 0.025f, 0.005f, 1f);
		component2.outlineWidth = 0.18f;
		component2.raycastTarget = false;
		BattlePopupStyle.ApplyFontOnly(component2);
		return component2;
	}

	private static Sprite LoadUpgradeMarkSprite()
	{
		if (cachedUpgradeMarkSprite == null)
		{
			cachedUpgradeMarkSprite = Resources.Load<Sprite>(UpgradeMarkResourcePath);
		}
		return cachedUpgradeMarkSprite;
	}

}

[DisallowMultipleComponent]
public sealed class BattleTotemRequirementUI : MonoBehaviour
{
	private const string LobbySceneName = "LobbyMahjongBattle";
	private static BattleTotemRequirementUI instance;
	private GameObject overlayRoot;
	private TMP_Text titleText;
	private TMP_Text messageText;
	private bool openCharacterSelectionOnConfirm;

	public static bool HasSelectedBattleCharacter()
	{
		return BattleCharacterSelectionService.HasInstance &&
			BattleCharacterSelectionService.Instance.HasSelectedCharacter();
	}

	public static bool HasSelectedTotem()
	{
		if (BattleLoreTutorialSession.IsActive)
			return true;
		PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
		if (profile == null)
			return false;
		MahjongBattleTileInventoryData inventory = BattleTileInventoryService.GetOrCreateInventory(profile);
		if (inventory == null || string.IsNullOrWhiteSpace(inventory.TotemTileId) || BattleTileInventoryService.GetOwnedCount(inventory, inventory.TotemTileId) <= 0)
			return false;
		BattleTileStore store = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>(FindObjectsInactive.Include);
		return store != null && BattleTileInventoryService.GetTotemTileData(profile, store) != null;
	}

	public static bool EnsureBattleReady()
	{
		if (!EnsureBattleCharacterReady())
			return false;

		if (BattleLoreTutorialSession.IsActive)
			return true;

		if (!HasCompleteActiveDeck(out int activeCount))
		{
			Show(
				Localized("Набор не готов", "Loadout incomplete", "Deste hazır değil", "Set nicht vollständig"),
				Localized(
					$"Для боя нужно 18 активных камней. Сейчас выбрано: {activeCount}/18. Заполните активный набор в сумке.",
					$"Battle requires 18 active stones. Selected: {activeCount}/18. Complete the active loadout in the bag.",
					$"Savaş için 18 aktif taş gerekir. Seçili: {activeCount}/18. Çantadaki aktif desteyi tamamlayın.",
					$"Für den Kampf werden 18 aktive Steine benötigt. Ausgewählt: {activeCount}/18. Vervollständige das aktive Set in der Tasche."));
			return false;
		}

		if (HasSelectedTotem())
			return true;
		Show();
		return false;
	}

	public static bool EnsureBattleCharacterReady()
	{
		if (HasSelectedBattleCharacter())
			return true;

		Show(
			Localized("Сначала выберите персонажа", "Choose a character first", "Önce bir karakter seçin", "Wähle zuerst einen Charakter"),
			Localized(
				"Перед первым боем выберите и подтвердите доступного персонажа. Без приобретённого и выбранного героя вход в бой недоступен.",
				"Before your first battle, choose and confirm an available character. Battle is unavailable without an owned and selected hero.",
				"İlk savaştan önce kullanılabilir bir karakter seçip onaylayın. Sahip olunan ve seçilmiş bir kahraman olmadan savaşa girilemez.",
				"Wähle und bestätige vor dem ersten Kampf einen verfügbaren Charakter. Ohne einen freigeschalteten und ausgewählten Helden ist der Kampf nicht verfügbar."),
			openCharacterSelection: true);
		return false;
	}

	public static bool HasCompleteActiveDeck(out int activeCount)
	{
		activeCount = 0;
		if (BattleLoreTutorialSession.IsActive)
		{
			activeCount = BattleTileInventoryService.RequiredActiveTiles;
			return true;
		}

		PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
		BattleTileStore store = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>(FindObjectsInactive.Include);
		if (profile == null || store == null)
			return false;

		IReadOnlyList<BattleTileData> active = BattleTileInventoryService.GetActiveTileData(profile, store);
		activeCount = active != null ? active.Count : 0;
		if (activeCount != BattleTileInventoryService.RequiredActiveTiles)
			return false;

		HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal);
		for (int i = 0; i < active.Count; i++)
		{
			BattleTileData tile = active[i];
			if (tile?.Prefab == null || string.IsNullOrWhiteSpace(tile.Id) || !uniqueIds.Add(tile.Id))
				return false;
		}

		return true;
	}

	public static void Show()
	{
		Show(
			Localized("Установите тотем", "Set a Totem", "Totem Seçin", "Totem festlegen"),
			Localized(
				"Назначьте тотемом один из 18 активных камней. Он останется в наборе и будет играть на поле как обычная пара.",
				"Assign one of the 18 active stones as the Totem. It remains in the loadout and plays on the board as a normal pair.",
				"18 aktif taştan birini Totem olarak seçin. Taş dizilimde kalır ve sahada normal bir çift olarak oynanır.",
				"Bestimme einen der 18 aktiven Steine zum Totem. Er bleibt im Set und wird als normales Paar gespielt."));
	}

	private static void Show(string title, string message, bool openCharacterSelection = false)
	{
		if (instance == null)
			instance = FindAnyObjectByType<BattleTotemRequirementUI>(FindObjectsInactive.Include);
		if (instance == null)
		{
			GameObject host = new GameObject("BattleTotemRequirementUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			instance = host.AddComponent<BattleTotemRequirementUI>();
		}
		instance.BuildIfNeeded();
		instance.openCharacterSelectionOnConfirm = openCharacterSelection;
		if (instance.titleText != null)
			instance.titleText.text = title;
		if (instance.messageText != null)
			instance.messageText.text = message;
		if (instance.overlayRoot != null)
		{
			instance.overlayRoot.SetActive(true);
			instance.overlayRoot.transform.SetAsLastSibling();
		}
	}

	private void Awake()
	{
		if (instance != null && instance != this)
		{
			Destroy(gameObject);
			return;
		}
		instance = this;
		BuildIfNeeded();
	}

	private void OnDestroy()
	{
		if (instance == this)
			instance = null;
	}

	private void BuildIfNeeded()
	{
		if (overlayRoot != null)
			return;
		Canvas canvas = GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.overrideSorting = true;
		canvas.sortingOrder = 32000;
		CanvasScaler scaler = GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
		MainLobbyUiCoordinator.ConfigureOverlayScaler(scaler);
		if (GetComponent<GraphicRaycaster>() == null)
			gameObject.AddComponent<GraphicRaycaster>();

		overlayRoot = new GameObject("TotemRequirementOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		overlayRoot.transform.SetParent(transform, false);
		RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
		overlayRect.anchorMin = Vector2.zero;
		overlayRect.anchorMax = Vector2.one;
		overlayRect.offsetMin = Vector2.zero;
		overlayRect.offsetMax = Vector2.zero;
		Image overlayImage = overlayRoot.GetComponent<Image>();
		overlayImage.color = new Color(0f, 0f, 0f, 0.88f);
		overlayImage.raycastTarget = true;

		GameObject panel = new GameObject("TotemRequirementWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		panel.transform.SetParent(overlayRoot.transform, false);
		RectTransform panelRect = panel.GetComponent<RectTransform>();
		panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
		panelRect.anchoredPosition = Vector2.zero;
		panelRect.sizeDelta = new Vector2(980f, 460f);
		Image panelImage = panel.GetComponent<Image>();
		if (!BattlePopupStyle.ApplyWindow(panelImage))
		{
			panelImage.color = new Color(0.09f, 0.055f, 0.025f, 0.98f);
			panelImage.raycastTarget = true;
		}

		titleText = CreateRequirementText(panel.transform, "Title", Localized("Установите тотем", "Set a Totem", "Totem Seçin", "Totem festlegen"), new Vector2(0f, 122f), new Vector2(760f, 76f), 52f);
		titleText.color = new Color(1f, 0.78f, 0.34f, 1f);
		titleText.fontStyle = FontStyles.Bold;
		messageText = CreateRequirementText(panel.transform, "Message", Localized("Перед выходом в бой выберите камень-тотем в инвентаре. Без установленного тотема бой недоступен.", "Choose a totem stone in the inventory before battle. Battle is unavailable without a totem.", "Savaştan önce envanterden bir totem taşı seçin. Totem olmadan savaş kullanılamaz.", "Wähle vor dem Kampf einen Totemstein im Inventar. Ohne Totem ist der Kampf nicht verfügbar."), new Vector2(0f, 22f), new Vector2(800f, 140f), 32f);
		messageText.color = new Color(0.96f, 0.9f, 0.76f, 1f);
		messageText.enableAutoSizing = true;
		messageText.fontSizeMin = 24f;
		messageText.fontSizeMax = 32f;

		GameObject buttonObject = new GameObject("ConfirmButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		buttonObject.transform.SetParent(panel.transform, false);
		RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
		buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(0.5f, 0.5f);
		buttonRect.anchoredPosition = new Vector2(0f, -132f);
		buttonRect.sizeDelta = new Vector2(430f, 88f);
		Button button = buttonObject.GetComponent<Button>();
		button.targetGraphic = buttonObject.GetComponent<Image>();
		button.onClick.AddListener(Confirm);
		TMP_Text buttonLabel = CreateRequirementText(buttonObject.transform, "Label", Localized("Понятно", "OK", "Tamam", "Verstanden"), Vector2.zero, new Vector2(390f, 70f), 36f);
		buttonLabel.fontStyle = FontStyles.Bold;
		BattlePopupStyle.ApplyButton(button);
	}

	private void Confirm()
	{
		bool outsideLobby = !string.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, LobbySceneName, StringComparison.Ordinal);
		bool shouldOpenCharacterSelection = openCharacterSelectionOnConfirm;
		openCharacterSelectionOnConfirm = false;
		if (overlayRoot != null)
			overlayRoot.SetActive(false);
		instance = null;
		if (outsideLobby)
		{
			Destroy(gameObject);
			UnityEngine.SceneManagement.SceneManager.LoadScene(LobbySceneName);
			return;
		}
		BattleLobbyUI lobby = shouldOpenCharacterSelection
			? FindAnyObjectByType<BattleLobbyUI>(FindObjectsInactive.Include)
			: null;
		Destroy(gameObject);
		if (lobby != null)
			lobby.OpenCharacterCarousel();
	}

	private static TMP_Text CreateRequirementText(Transform parent, string objectName, string value, Vector2 position, Vector2 size, float fontSize)
	{
		GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		textObject.transform.SetParent(parent, false);
		RectTransform rect = textObject.GetComponent<RectTransform>();
		rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
		TMP_Text text = textObject.GetComponent<TMP_Text>();
		text.text = value;
		text.fontSize = fontSize;
		text.alignment = TextAlignmentOptions.Center;
		text.textWrappingMode = TextWrappingModes.Normal;
		text.raycastTarget = false;
		BattlePopupStyle.ApplyFontOnly(text);
		return text;
	}

	private static string Localized(string ru, string en, string tr, string de)
	{
		return ((AppSettings.I != null) ? AppSettings.I.Language : GameLanguage.Russian) switch
		{
			GameLanguage.English => en,
			GameLanguage.Turkish => tr,
			GameLanguage.German => de,
			_ => ru
		};
	}
}
}
