using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class BattleLobbyChar : MonoBehaviour
    {
        [SerializeField] private Image target;
        [SerializeField] private bool preserveAspect = true;
        [SerializeField] private bool hideOnStart = true;
        [SerializeField] private bool useSelectSpriteIfLobbySpriteMissing = true;

        private bool isConfirmed;
        private bool subscribed;

        private void Reset()
        {
            target = GetComponent<Image>();
        }

        private void Awake()
        {
            if (target == null)
                target = GetComponent<Image>();

            if (hideOnStart && target != null)
                target.enabled = false;
        }

        private void Start()
        {
            Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
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
                return;

            if (!BattleCharacterSelectionService.HasInstance || !BattleCharacterDatabase.HasInstance)
            {
                if (hideOnStart && !isConfirmed)
                    target.enabled = false;

                return;
            }

            string id = BattleCharacterSelectionService.Instance.SelectedCharacterId;
            if (string.IsNullOrWhiteSpace(id))
            {
                target.enabled = false;
                return;
            }

            BattleCharacterDatabase.BattleCharacterData data =
                BattleCharacterDatabase.Instance.GetCharacterOrNull(id);

            if (data == null)
            {
                target.enabled = false;
                return;
            }

            Sprite sprite = data.LobbySprite != null
                ? data.LobbySprite
                : useSelectSpriteIfLobbySpriteMissing
                    ? data.SelectSprite
                    : null;

            if (sprite == null)
            {
                target.enabled = false;
                return;
            }

            target.sprite = sprite;
            target.preserveAspect = preserveAspect;
            target.enabled = true;
        }

        public void HideNow()
        {
            isConfirmed = false;

            if (target != null)
                target.enabled = false;
        }

        private void Subscribe()
        {
            if (subscribed || !BattleCharacterSelectionService.HasInstance)
                return;

            BattleCharacterSelectionService.Instance.SelectedCharacterChanged += OnSelectedCharacterChanged;
            BattleCharacterSelectionService.Instance.SelectionStateChanged += OnSelectionStateChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            if (BattleCharacterSelectionService.HasInstance)
            {
                BattleCharacterSelectionService.Instance.SelectedCharacterChanged -= OnSelectedCharacterChanged;
                BattleCharacterSelectionService.Instance.SelectionStateChanged -= OnSelectionStateChanged;
            }

            subscribed = false;
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
    }
}
