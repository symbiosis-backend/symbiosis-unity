using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class FallingFX : MonoBehaviour
{
    [Header("Спрайты (что падает)")]
    [SerializeField] private List<Sprite> sprites = new();

    [Header("Спавн")]
    [SerializeField] private float интервалСпавна = 0.25f;
    [SerializeField] private int максимумОбъектов = 20;
    [SerializeField] private float отступСверху = 40f;
    [SerializeField] private float отступПоБокам = 20f;

    [Header("Скорость падения")]
    [SerializeField] private float минСкоростьПадения = 40f;
    [SerializeField] private float максСкоростьПадения = 80f;

    [Header("Вращение")]
    [SerializeField] private float минСкоростьВращения = -25f;
    [SerializeField] private float максСкоростьВращения = 25f;

    [Header("Размер")]
    [SerializeField] private float минРазмер = 18f;
    [SerializeField] private float максРазмер = 36f;

    [Header("Прозрачность")]
    [SerializeField, Range(0f, 1f)] private float стартоваяПрозрачность = 0.8f;
    [SerializeField] private float скоростьИсчезновения = 2f;

    [Header("Доп движение")]
    [SerializeField] private float горизонтальныйСдвиг = 8f;

    [Header("Граница падения")]
    [SerializeField, Range(-1f, 1f)] private float точкаУдаления = 0f;
    [SerializeField] private float запасПередУдалением = 30f;
    [SerializeField] private bool плавноеИсчезновение = true;

    private RectTransform root;
    private float таймер;
    private readonly List<Item> items = new();

    private sealed class Item
    {
        public RectTransform rect;
        public Image image;
        public float скоростьПадения;
        public float скоростьВращения;
        public float сдвиг;
        public bool исчезает;
        public float точкаУдаленияY;
    }

    private void Awake()
    {
        root = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (root == null || sprites == null || sprites.Count == 0)
            return;

        таймер += Time.deltaTime;

        while (таймер >= интервалСпавна)
        {
            таймер -= интервалСпавна;

            if (items.Count < максимумОбъектов)
                СоздатьОбъект();
        }

        ОбновитьОбъекты();
    }

    private void СоздатьОбъект()
    {
        GameObject go = new GameObject("FX", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        Image img = go.GetComponent<Image>();

        img.sprite = sprites[Random.Range(0, sprites.Count)];
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.color = new Color(1f, 1f, 1f, стартоваяПрозрачность);

        float width = root.rect.width;
        float height = root.rect.height;

        float x = Random.Range(-width * 0.5f + отступПоБокам, width * 0.5f - отступПоБокам);
        float y = height * 0.5f + отступСверху;

        float size = Random.Range(минРазмер, максРазмер);

        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(size, size);
        rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        float destroyY = height * 0.5f * Mathf.Clamp(точкаУдаления, -1f, 1f);

        items.Add(new Item
        {
            rect = rt,
            image = img,
            скоростьПадения = Random.Range(минСкоростьПадения, максСкоростьПадения),
            скоростьВращения = Random.Range(минСкоростьВращения, максСкоростьВращения),
            сдвиг = Random.Range(-горизонтальныйСдвиг, горизонтальныйСдвиг),
            исчезает = false,
            точкаУдаленияY = destroyY
        });
    }

    private void ОбновитьОбъекты()
    {
        float dt = Time.deltaTime;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            var item = items[i];

            if (item == null || item.rect == null)
            {
                items.RemoveAt(i);
                continue;
            }

            Vector2 pos = item.rect.anchoredPosition;
            pos.y -= item.скоростьПадения * dt;
            pos.x += item.сдвиг * dt;
            item.rect.anchoredPosition = pos;

            item.rect.Rotate(0, 0, item.скоростьВращения * dt);

            if (плавноеИсчезновение && pos.y <= item.точкаУдаленияY)
                item.исчезает = true;

            if (item.исчезает)
            {
                var c = item.image.color;
                c.a -= скоростьИсчезновения * dt;
                item.image.color = c;

                if (c.a <= 0 || pos.y <= item.точкаУдаленияY - запасПередУдалением)
                {
                    Удалить(i);
                }
            }
            else if (!плавноеИсчезновение && pos.y <= item.точкаУдаленияY)
            {
                Удалить(i);
            }
        }
    }

    private void Удалить(int index)
    {
        if (items[index].rect != null)
            Destroy(items[index].rect.gameObject);

        items.RemoveAt(index);
    }

    private void OnDisable() => Очистить();
    private void OnDestroy() => Очистить();

    private void Очистить()
    {
        foreach (var item in items)
            if (item.rect != null)
                Destroy(item.rect.gameObject);

        items.Clear();
    }
}