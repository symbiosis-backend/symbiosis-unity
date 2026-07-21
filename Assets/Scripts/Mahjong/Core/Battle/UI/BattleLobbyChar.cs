using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class BattleLobbyChar : MonoBehaviour
{
	[SerializeField]
	private Image target;

	[SerializeField]
	private BattleCharacterModelView modelView;

	[SerializeField]
	private bool preserveAspect = true;

	[SerializeField]
	private bool hideOnStart = true;

	[SerializeField]
	private bool useSelectSpriteIfLobbySpriteMissing = true;

	[SerializeField]
	private bool showBattleLobbyCharacterAvatar = true;

	[Header("Battle Lobby Placement")]
	[SerializeField]
	private bool applyLargeLeftProfileLayout = true;

	[SerializeField]
	[Range(0.1f, 1f)]
	private float screenHeightPercent = 0.8f;

	[SerializeField]
	[Range(0.1f, 0.8f)]
	private float screenWidthPercent = 0.34f;

	[SerializeField]
	[Range(0.25f, 0.9f)]
	private float targetAspect = 0.55f;

	[SerializeField]
	private Vector2 bottomLeftOffset = new Vector2(-28f, 34f);

	[SerializeField]
	[Range(-0.25f, 0.35f)]
	private float horizontalScreenOffsetPercent = 0.04f;

	[Header("Battle Lobby Profile Framing")]
	[SerializeField]
	[Range(0.85f, 2.2f)]
	private float profileRenderFitPadding = 1.14f;

	[SerializeField]
	[Range(-1f, 2f)]
	private float profileRenderVerticalFrameOffset;

	[SerializeField]
	[Range(-2f, 2f)]
	private float profileRenderHorizontalFrameOffset;

	[SerializeField]
	[Range(-0.5f, 0.6f)]
	private float profileRenderFeetBottomMargin = -0.08f;

	private bool isConfirmed;

	private bool subscribed;

	private bool suppressedByCharacterCarousel;

	private void Reset()
	{
		target = GetComponent<Image>();
	}

	private void Awake()
	{
		if (target == null)
		{
			target = GetComponent<Image>();
		}
		if (modelView == null)
		{
			modelView = GetComponent<BattleCharacterModelView>();
		}
		if (modelView == null)
		{
			modelView = base.gameObject.AddComponent<BattleCharacterModelView>();
		}
		ApplyProfileRenderFrame();
		ApplyLargeLeftLayout();
		if (hideOnStart && target != null)
		{
			target.enabled = false;
		}
	}

	private void Start()
	{
		Refresh();
	}

	private void OnEnable()
	{
		ApplyLargeLeftLayout();
		Subscribe();
		Refresh();
	}

	private void Update()
	{
		if (!subscribed)
			Subscribe();
	}

	private void OnDisable()
	{
		Unsubscribe();
	}

	public void ConfirmAndRefresh()
	{
		isConfirmed = true;
		Refresh();
	}

	public void Refresh()
	{
		if (target == null)
		{
			return;
		}
		ApplyLargeLeftLayout();
		if (suppressedByCharacterCarousel)
		{
			HideVisualOnly();
			return;
		}
		if (!showBattleLobbyCharacterAvatar)
		{
			HideNow();
			return;
		}
		if (!BattleCharacterSelectionService.HasInstance || !BattleCharacterDatabase.HasInstance)
		{
			if (modelView != null)
			{
				modelView.Hide();
			}
			if (hideOnStart && !isConfirmed)
			{
				target.enabled = false;
			}
			return;
		}
		string selectedCharacterId = BattleCharacterSelectionService.Instance.SelectedCharacterId;
		if (string.IsNullOrWhiteSpace(selectedCharacterId))
		{
			if (modelView != null)
			{
				modelView.Hide();
			}
			target.enabled = false;
			return;
		}
		BattleCharacterDatabase.BattleCharacterData characterOrNull = BattleCharacterDatabase.Instance.GetCharacterOrNull(selectedCharacterId);
		if (characterOrNull == null)
		{
			if (modelView != null)
			{
				modelView.Hide();
			}
			target.enabled = false;
			return;
		}
		if (modelView != null)
		{
			ApplyProfileRenderFrame();
			if (modelView.Show(characterOrNull, BattleCharacterModelView.ModelContext.Profile))
			{
				return;
			}
		}
		Sprite sprite = ((characterOrNull.LobbySprite != null) ? characterOrNull.LobbySprite : (useSelectSpriteIfLobbySpriteMissing ? characterOrNull.SelectSprite : null));
		if (sprite == null)
		{
			target.enabled = false;
			return;
		}
		target.sprite = sprite;
		target.preserveAspect = preserveAspect;
		target.enabled = true;
	}

	private void LateUpdate()
	{
		ApplyLargeLeftLayout();
	}

	public void HideNow()
	{
		isConfirmed = false;
		HideVisualOnly();
	}

	public void SetSuppressedByCharacterCarousel(bool suppressed)
	{
		suppressedByCharacterCarousel = suppressed;
		if (suppressed)
		{
			HideVisualOnly();
		}
		else
		{
			Refresh();
		}
	}

	private void HideVisualOnly()
	{
		if (modelView != null)
		{
			modelView.Hide();
		}
		if (target != null)
		{
			target.enabled = false;
		}
	}

	private void Subscribe()
	{
		if (!subscribed && BattleCharacterSelectionService.HasInstance)
		{
			BattleCharacterSelectionService.Instance.SelectedCharacterChanged += OnSelectedCharacterChanged;
			BattleCharacterSelectionService.Instance.SelectionStateChanged += OnSelectionStateChanged;
			subscribed = true;
		}
	}

	private void Unsubscribe()
	{
		if (subscribed)
		{
			if (BattleCharacterSelectionService.HasInstance)
			{
				BattleCharacterSelectionService.Instance.SelectedCharacterChanged -= OnSelectedCharacterChanged;
				BattleCharacterSelectionService.Instance.SelectionStateChanged -= OnSelectionStateChanged;
			}
			subscribed = false;
		}
	}

	private void OnSelectedCharacterChanged(string _)
	{
		isConfirmed = true;
		Refresh();
	}

	private void OnSelectionStateChanged()
	{
		Refresh();
	}

	private void ApplyLargeLeftLayout()
	{
		if (!applyLargeLeftProfileLayout)
		{
			return;
		}
		RectTransform rectTransform = base.transform as RectTransform;
		if (rectTransform == null)
		{
			return;
		}
		RectTransform rectTransform2 = rectTransform.parent as RectTransform;
		if (!(rectTransform2 == null))
		{
			float num = Mathf.Max(1f, rectTransform2.rect.width);
			float num2 = Mathf.Max(1f, rectTransform2.rect.height);
			float a = num * screenWidthPercent;
			float num3 = num2 * screenHeightPercent;
			float num4 = Mathf.Clamp(targetAspect, 0.25f, 0.9f);
			float num5 = Mathf.Min(a, num3 * num4);
			float num6 = num5 / num4;
			if (num6 > num3)
			{
				num6 = num3;
				num5 = num6 * num4;
			}
			float y = Mathf.Max(bottomLeftOffset.y, num2 * 0.055f);
			float num7 = Mathf.Min(bottomLeftOffset.x, num * -0.015f);
			float num8 = num * horizontalScreenOffsetPercent;
			rectTransform.anchorMin = new Vector2(0f, 0f);
			rectTransform.anchorMax = new Vector2(0f, 0f);
			rectTransform.pivot = new Vector2(0f, 0f);
			rectTransform.anchoredPosition = new Vector2(num7 + num8, y);
			rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, num5);
			rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num6);
			rectTransform.localScale = Vector3.one;
		}
	}

	private void ApplyProfileRenderFrame()
	{
		if (!(modelView == null))
		{
			modelView.ConfigureProfileRenderFrame(profileRenderFitPadding, profileRenderVerticalFrameOffset, profileRenderHorizontalFrameOffset, anchorToFeet: true, profileRenderFeetBottomMargin);
		}
	}
}
}
