using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{

[DisallowMultipleComponent]
public sealed class CarouselBackgroundOverlay : MonoBehaviour
{
	[Header("Target")]
	[SerializeField]
	private RectTransform carouselRoot;

	[Header("Visual")]
	[SerializeField]
	private Sprite backgroundImage;

	[SerializeField]
	private string defaultBackgroundResourcePath = "Mahjong/Sprites/BattleLobbyParts/WindowForBattleLobby";

	[SerializeField]
	[Range(0f, 1f)]
	private float darkness = 1f;

	[SerializeField]
	private bool forceFullscreenBlack;

	[SerializeField]
	[Range(0f, 1f)]
	private float fullscreenBlackOpacity = 1f;

	[SerializeField]
	private bool preserveAspect;

	[Header("Behavior")]
	[SerializeField]
	private bool createOnAwake = true;

	[SerializeField]
	private bool showOnEnable = true;

	[SerializeField]
	private bool closeOnBackgroundClick;

	[SerializeField]
	private bool destroyOnDisable;

	private GameObject overlayObject;

	private RectTransform overlayRect;

	private Image overlayImage;

	private Button overlayButton;

	private Canvas parentCanvas;

	public bool IsCreated => overlayObject != null;

	public bool IsVisible
	{
		get
		{
			if (overlayObject != null)
			{
				return overlayObject.activeSelf;
			}
			return false;
		}
	}

	private void Reset()
	{
		if (carouselRoot == null)
		{
			carouselRoot = base.transform as RectTransform;
		}
	}

	private void Awake()
	{
		if (carouselRoot == null)
		{
			carouselRoot = base.transform as RectTransform;
		}
		if (createOnAwake)
		{
			EnsureOverlay();
		}
	}

	private void OnEnable()
	{
		if (showOnEnable)
		{
			Show();
		}
	}

	private void OnDisable()
	{
		if (destroyOnDisable)
		{
			DestroyOverlay();
		}
		else
		{
			Hide();
		}
	}

	private void OnDestroy()
	{
		if (overlayObject != null)
		{
			DestroyOverlay();
		}
	}

	public void Show()
	{
		EnsureOverlay();
		ApplyFullscreenRect();
		ApplyVisual();
		if (overlayObject != null)
		{
			overlayObject.SetActive(value: true);
		}
		MoveOverlayBehindCarousel();
	}

	public void Hide()
	{
		if (overlayObject != null)
		{
			overlayObject.SetActive(value: false);
		}
	}

	public void Toggle()
	{
		if (!IsCreated || !IsVisible)
		{
			Show();
		}
		else
		{
			Hide();
		}
	}

	public void SetBackground(Sprite sprite)
	{
		backgroundImage = sprite;
		ApplyVisual();
	}

	public void SetDarkness(float value)
	{
		darkness = Mathf.Clamp01(value);
		ApplyVisual();
	}

	private void EnsureOverlay()
	{
		if (overlayObject != null)
		{
			CacheReferences();
			return;
		}
		if (carouselRoot == null)
		{
			carouselRoot = base.transform as RectTransform;
		}
		parentCanvas = GetComponentInParent<Canvas>(includeInactive: true);
		if (parentCanvas == null)
		{
			Debug.LogWarning("[CarouselBackgroundOverlay] Parent Canvas not found.", this);
			return;
		}
		Transform transform = parentCanvas.transform.Find("CarouselBackgroundOverlay_Auto");
		if (transform != null)
		{
			overlayObject = transform.gameObject;
			CacheReferences();
			ApplyVisual();
			MoveOverlayBehindCarousel();
			return;
		}
		overlayObject = new GameObject("CarouselBackgroundOverlay_Auto", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		overlayObject.transform.SetParent(parentCanvas.transform, worldPositionStays: false);
		CacheReferences();
		ApplyFullscreenRect();
		if (overlayButton != null)
		{
			overlayButton.onClick.RemoveAllListeners();
			overlayButton.onClick.AddListener(OnOverlayClicked);
		}
		ApplyVisual();
		MoveOverlayBehindCarousel();
	}

	private void CacheReferences()
	{
		if (!(overlayObject == null))
		{
			if (overlayRect == null)
			{
				overlayRect = overlayObject.GetComponent<RectTransform>();
			}
			if (overlayImage == null)
			{
				overlayImage = overlayObject.GetComponent<Image>();
			}
			if (overlayButton == null)
			{
				overlayButton = overlayObject.GetComponent<Button>();
			}
		}
	}

	private void ApplyVisual()
	{
		if (!(overlayImage == null))
		{
			if (backgroundImage == null && !string.IsNullOrWhiteSpace(defaultBackgroundResourcePath))
			{
				backgroundImage = LoadLargestSprite(defaultBackgroundResourcePath);
			}
			bool flag = backgroundImage != null || !string.IsNullOrWhiteSpace(defaultBackgroundResourcePath);
			bool flag2 = forceFullscreenBlack && !flag;
			overlayImage.sprite = (flag2 ? null : backgroundImage);
			overlayImage.type = Image.Type.Simple;
			overlayImage.preserveAspect = !flag2 && preserveAspect;
			overlayImage.raycastTarget = true;
			if (flag2)
			{
				overlayImage.color = new Color(0f, 0f, 0f, fullscreenBlackOpacity);
			}
			else if (backgroundImage != null)
			{
				overlayImage.color = new Color(1f, 1f, 1f, darkness);
			}
			else
			{
				overlayImage.color = new Color(0f, 0f, 0f, darkness);
			}
			if (overlayButton != null)
			{
				overlayButton.enabled = true;
				overlayButton.transition = Selectable.Transition.None;
			}
		}
	}

	private static Sprite LoadLargestSprite(string resourcePath)
	{
		Sprite sprite = Resources.Load<Sprite>(resourcePath);
		if (sprite != null)
		{
			return sprite;
		}
		Sprite[] array = Resources.LoadAll<Sprite>(resourcePath);
		if (array == null || array.Length == 0)
		{
			return null;
		}
		Sprite result = null;
		float num = -1f;
		foreach (Sprite sprite2 in array)
		{
			if (!(sprite2 == null))
			{
				float num2 = sprite2.rect.width * sprite2.rect.height;
				if (!(num2 <= num))
				{
					result = sprite2;
					num = num2;
				}
			}
		}
		return result;
	}

	private void ApplyFullscreenRect()
	{
		if (!(overlayRect == null))
		{
			overlayRect.anchorMin = Vector2.zero;
			overlayRect.anchorMax = Vector2.one;
			overlayRect.offsetMin = Vector2.zero;
			overlayRect.offsetMax = Vector2.zero;
			overlayRect.localScale = Vector3.one;
			overlayRect.anchoredPosition3D = Vector3.zero;
		}
	}

	private void MoveOverlayBehindCarousel()
	{
		if (!(overlayObject == null) && !(carouselRoot == null))
		{
			Transform transform = overlayObject.transform;
			Transform canvasDirectChild = GetCanvasDirectChild(carouselRoot);
			if (!(transform.parent == null) && !(canvasDirectChild == null) && !(transform.parent != canvasDirectChild.parent))
			{
				int siblingIndex = canvasDirectChild.GetSiblingIndex();
				int siblingIndex2 = Mathf.Max(0, siblingIndex - 1);
				transform.SetSiblingIndex(siblingIndex2);
			}
		}
	}

	private Transform GetCanvasDirectChild(Transform target)
	{
		if (target == null || parentCanvas == null)
		{
			return null;
		}
		Transform transform = target;
		Transform transform2 = parentCanvas.transform;
		while (transform != null)
		{
			if (transform.parent == transform2)
			{
				return transform;
			}
			transform = transform.parent;
		}
		return null;
	}

	private void OnOverlayClicked()
	{
		if (closeOnBackgroundClick && carouselRoot != null)
		{
			carouselRoot.gameObject.SetActive(value: false);
		}
	}

	private void DestroyOverlay()
	{
		if (!(overlayObject == null))
		{
			Object.Destroy(overlayObject);
			overlayObject = null;
			overlayRect = null;
			overlayImage = null;
			overlayButton = null;
		}
	}
}
}
