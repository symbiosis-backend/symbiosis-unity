using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    public enum BattleStatIconKind
    {
        Hp,
        Attack,
        Armor,
        Parry,
        Critical,
        CriticalDamage,
        CriticalPower
    }

    public static class BattleStatIconProvider
    {
        private const string ArmorIconResourcePath = "Mahjong/Sprites/Stats/ArmorIcon";
        private const string CriticalIconResourcePath = "Mahjong/Sprites/Stats/CriticalIcon";
        private const string CriticalDamageIconResourcePath = "Mahjong/Sprites/Stats/CriticalDamageIcon";
        private const string HpIconResourcePath = "Mahjong/Sprites/Stats/HpIcon";
        private const string AttackIconResourcePath = "Mahjong/Sprites/Stats/AttackIcon";
        private const string ParryIconResourcePath = "Mahjong/Sprites/Stats/ParryIcon";

        private static Sprite hpIcon;
        private static Sprite attackIcon;
        private static Sprite armorIcon;
        private static Sprite parryIcon;
        private static Sprite criticalIcon;
        private static Sprite criticalDamageIcon;
        private static Sprite criticalPowerIcon;

        public static Sprite GetSprite(BattleStatIconKind kind)
        {
            switch (kind)
            {
                case BattleStatIconKind.Hp:
                    hpIcon = hpIcon != null ? hpIcon : Resources.Load<Sprite>(HpIconResourcePath);
                    return hpIcon;
                case BattleStatIconKind.Attack:
                    attackIcon = attackIcon != null ? attackIcon : Resources.Load<Sprite>(AttackIconResourcePath);
                    return attackIcon;
                case BattleStatIconKind.Parry:
                    parryIcon = parryIcon != null ? parryIcon : Resources.Load<Sprite>(ParryIconResourcePath);
                    return parryIcon;
                case BattleStatIconKind.Critical:
                    criticalIcon = criticalIcon != null ? criticalIcon : Resources.Load<Sprite>(CriticalIconResourcePath);
                    return criticalIcon;
                case BattleStatIconKind.CriticalDamage:
                    criticalDamageIcon = criticalDamageIcon != null ? criticalDamageIcon : Resources.Load<Sprite>(CriticalDamageIconResourcePath);
                    if (criticalDamageIcon == null)
                        criticalDamageIcon = Resources.Load<Sprite>(CriticalIconResourcePath);
                    if (criticalDamageIcon == null)
                        criticalDamageIcon = Resources.Load<Sprite>(AttackIconResourcePath);
                    return criticalDamageIcon;
                case BattleStatIconKind.CriticalPower:
                    criticalPowerIcon = criticalPowerIcon != null ? criticalPowerIcon : Resources.Load<Sprite>(CriticalDamageIconResourcePath);
                    if (criticalPowerIcon == null)
                        criticalPowerIcon = Resources.Load<Sprite>(CriticalIconResourcePath);
                    return criticalPowerIcon;
                default:
                    armorIcon = armorIcon != null ? armorIcon : Resources.Load<Sprite>(ArmorIconResourcePath);
                    return armorIcon;
            }
        }

        public static Image EnsureIcon(Transform parent, string objectName, BattleStatIconKind kind, Vector2 position, Vector2 size)
        {
            if (parent == null)
                return null;

            Transform existing = parent.Find(objectName);
            GameObject obj = existing != null
                ? existing.gameObject
                : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = obj.GetComponent<Image>();
            image.sprite = GetSprite(kind);
            image.enabled = image.sprite != null;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public static void HideIcon(Transform parent, string objectName)
        {
            if (parent == null)
                return;

            Transform existing = parent.Find(objectName);
            if (existing != null)
                existing.gameObject.SetActive(false);
        }

        public static void ShowIcon(Transform parent, string objectName, BattleStatIconKind kind, Vector2 position, Vector2 size)
        {
            Image image = EnsureIcon(parent, objectName, kind, position, size);
            if (image != null)
                image.gameObject.SetActive(true);
        }

        public static string ValueWithIconGap(string value)
        {
            return "     " + value;
        }
    }
}
