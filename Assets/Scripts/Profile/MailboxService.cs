using System;
using System.Collections;
using System.Text;
using MahjongGame.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class MailboxService : MonoBehaviour
    {
        public static MailboxService I { get; private set; }
        public static event Action MailboxChanged;

        private const string WelcomeMessageId = "official-welcome-mail-2026-05-19";
        private const string DenemeRareStoneGiftId = "official-deneme-rare-stones-2026-05-19";
        private const string DenemeEpicStoneGiftId = "official-deneme-epic-stone-2026-05-19";
        private const string DenemeSecondEpicStoneGiftId = "official-deneme-epic-stone-repeat-2026-05-19";
        private const string DenemeGiftEmail = "deneme@deneme.com";
        private const string KeySessionToken = "symbiosis_server_session_token";
        private const string PlaceholderRareStoneGiftId = "rare_stone_gift";
        private const string PlaceholderEpicStoneGiftId = "epic_stone_gift";
        private const string KeyClientGrantPrefix = "symbiosis_mailbox_client_grant_";
        private const string KeyDeletedMessagePrefix = "symbiosis_mailbox_deleted_";
        private bool loggedMissingProfile;
        private bool refreshingFromServer;
        private bool initializingForLoadedProfile;
        private float lastServerRefreshTime = -999f;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            PersistentObjectUtility.DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            ProfileService.ProfileChanged -= OnProfileChanged;
            ProfileService.ProfileChanged += OnProfileChanged;
        }

        private void OnDisable()
        {
            ProfileService.ProfileChanged -= OnProfileChanged;
        }

        private void Start()
        {
            InitializeForLoadedProfile();
        }

        private void OnProfileChanged()
        {
            InitializeForLoadedProfile();
        }

        private void InitializeForLoadedProfile()
        {
            if (initializingForLoadedProfile)
                return;

            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
                return;

            initializingForLoadedProfile = true;
            try
            {
                EnsureSeedMessages(profile);
                RepairClaimedClientAttachmentGifts(profile);
                RefreshFromServer();
            }
            finally
            {
                initializingForLoadedProfile = false;
            }
        }

        public MailboxData GetMailbox()
        {
            return GetMailboxWithoutSeeding();
        }

        public int GetUnreadCount()
        {
            MailboxData mailbox = GetMailboxWithoutSeeding();
            return mailbox != null ? mailbox.CountUnreadInbox() : 0;
        }

        public int GetClaimableCount()
        {
            MailboxData mailbox = GetMailboxWithoutSeeding();
            return mailbox != null ? mailbox.CountClaimableInbox() : 0;
        }

        public void MarkRead(string messageId)
        {
            MailboxMessageData message = FindInboxMessage(messageId);
            if (message == null || message.IsRead)
                return;

            message.IsRead = true;
            SaveAndNotify();
            if (message.ServerRecipientId > 0)
                StartCoroutine(MarkServerRead(message.ServerRecipientId));
        }

        public bool ClaimAttachments(string messageId, out string resultMessage)
        {
            resultMessage = GameLocalization.Text("mail.claim_none");
            MailboxMessageData message = FindInboxMessage(messageId);
            if (message == null)
            {
                resultMessage = GameLocalization.Text("mail.not_found");
                return false;
            }

            if (!message.HasClaimableAttachments)
                return false;

            if (message.ServerRecipientId > 0)
            {
                StartCoroutine(ClaimServerAttachments(message.ServerRecipientId));
                resultMessage = "Забираем подарок...";
                return true;
            }

            if (CurrencyService.I == null)
                ProfileRuntimeBootstrap.EnsureServices();

            if (CurrencyService.I == null)
            {
                resultMessage = GameLocalization.Text("profile.error.service_missing");
                return false;
            }

            bool grantedAll = true;
            for (int i = 0; i < message.Attachments.Count; i++)
            {
                MailboxAttachmentData attachment = message.Attachments[i];
                if (attachment == null || !attachment.IsValid)
                    continue;

                grantedAll &= GrantOrQueueAttachment(message, attachment, i);
            }

            if (!grantedAll)
            {
                resultMessage = "Подарок сохранён. Камень появится в сумке героя после открытия боевой сцены.";
                return false;
            }

            message.IsClaimed = true;
            message.IsRead = true;
            if (HasClientGrantedAttachments(message))
                RecordClientGrant(message);
            resultMessage = GameLocalization.Text("mail.claimed");
            SaveAndNotify();
            return true;
        }

        public bool SubmitPlayerLetter(string subject, string body, out string resultMessage)
        {
            resultMessage = string.Empty;
            string safeSubject = string.IsNullOrWhiteSpace(subject) ? GameLocalization.Text("mail.no_subject") : subject.Trim();
            string safeBody = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();

            if (safeBody.Length < 3)
            {
                resultMessage = GameLocalization.Text("mail.write_body_required");
                return false;
            }

            PlayerProfile profile = GetProfile();
            if (profile == null)
            {
                resultMessage = GameLocalization.Text("profile.error.service_missing");
                return false;
            }

            profile.EnsureData();
            profile.Mailbox.PlayerLetters.Add(new MailboxMessageData
            {
                Id = "player-" + Guid.NewGuid().ToString("N"),
                SenderName = string.IsNullOrWhiteSpace(profile.DisplayName) ? GameLocalization.Text("common.player") : profile.DisplayName,
                Subject = safeSubject,
                Body = safeBody,
                CreatedAtUtc = DateTime.UtcNow.ToString("O"),
                IsFromPlayer = true,
                IsRead = true,
                IsClaimed = true
            });

            profile.Mailbox.EnsureValid();
            resultMessage = GameLocalization.Text("mail.sent_local");
            SaveAndNotify();
            StartCoroutine(SendPlayerLetterToServer(safeSubject, safeBody));
            return true;
        }

        public bool DeleteInboxMessage(string messageId, out string resultMessage)
        {
            resultMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(messageId))
                return false;

            MailboxData mailbox = GetMailboxWithoutSeeding();
            if (mailbox?.Inbox == null)
                return false;

            for (int i = mailbox.Inbox.Count - 1; i >= 0; i--)
            {
                MailboxMessageData message = mailbox.Inbox[i];
                if (message == null || !string.Equals(message.Id, messageId, StringComparison.Ordinal))
                    continue;

                if (message.HasClaimableAttachments)
                {
                    resultMessage = "Сначала забери подарок.";
                    return false;
                }

                RecordDeletedMessage(message);
                mailbox.Inbox.RemoveAt(i);
                resultMessage = "Письмо удалено.";
                SaveAndNotify();
                return true;
            }

            return false;
        }

        public bool AddOfficialMessage(string messageId, string subject, string body, MailboxAttachmentData[] attachments = null)
        {
            return AddOfficialMessage(messageId, string.Empty, subject, body, attachments);
        }

        public bool AddOfficialMessage(string messageId, string targetEmail, string subject, string body, MailboxAttachmentData[] attachments = null)
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return false;

            profile.EnsureData();
            string safeId = string.IsNullOrWhiteSpace(messageId) ? "official-" + Guid.NewGuid().ToString("N") : messageId.Trim();
            if (profile.Mailbox.HasInboxMessage(safeId))
                return false;

            MailboxMessageData message = new MailboxMessageData
            {
                Id = safeId,
                SenderName = "Symbiosis Team",
                TargetEmail = string.IsNullOrWhiteSpace(targetEmail) ? string.Empty : targetEmail.Trim().ToLowerInvariant(),
                Subject = string.IsNullOrWhiteSpace(subject) ? GameLocalization.Text("mail.no_subject") : subject.Trim(),
                Body = body ?? string.Empty,
                CreatedAtUtc = DateTime.UtcNow.ToString("O"),
                IsFromPlayer = false,
                IsRead = false,
                IsClaimed = false,
                Attachments = new System.Collections.Generic.List<MailboxAttachmentData>()
            };

            if (attachments != null)
            {
                for (int i = 0; i < attachments.Length; i++)
                {
                    if (attachments[i] != null)
                        message.Attachments.Add(attachments[i]);
                }
            }

            profile.Mailbox.Inbox.Add(message);
            profile.Mailbox.EnsureValid();
            SaveAndNotify();
            return true;
        }

        public void EnsureSeedMessages()
        {
            PlayerProfile profile = GetProfile();
            if (profile != null)
                EnsureSeedMessages(profile);
        }

        public void RefreshFromServer()
        {
            string token = GetSessionToken();
            if (string.IsNullOrWhiteSpace(token) || refreshingFromServer)
                return;

            if (Time.unscaledTime - lastServerRefreshTime < 5f)
                return;

            lastServerRefreshTime = Time.unscaledTime;
            StartCoroutine(RefreshFromServerRoutine(token));
        }

        private void EnsureSeedMessages(PlayerProfile profile)
        {
            if (profile == null)
                return;

            profile.EnsureData();
            MailboxData mailbox = profile.Mailbox;
            bool changed = false;
            if (!mailbox.HasInboxMessage(WelcomeMessageId))
            {
                mailbox.Inbox.Add(new MailboxMessageData
                {
                    Id = WelcomeMessageId,
                    SenderName = "Symbiosis Team",
                    Subject = GameLocalization.Text("mail.welcome_subject"),
                    Body = GameLocalization.Text("mail.welcome_body"),
                    CreatedAtUtc = DateTime.UtcNow.ToString("O"),
                    IsFromPlayer = false,
                    IsRead = false,
                    IsClaimed = false,
                    Attachments = new System.Collections.Generic.List<MailboxAttachmentData>
                    {
                        CreateCurrencyAttachment(CurrencyIds.OzTile, 100),
                        CreateCurrencyAttachment(CurrencyIds.OzAltin, 25)
                    }
                });
                changed = true;
            }

            changed |= TryAddDenemeRareStoneGift(profile);
            changed |= TryAddDenemeEpicStoneGift(profile);
            changed |= TryAddDenemeSecondEpicStoneGift(profile);
            changed |= UpdateSeedMessageCopy(mailbox, WelcomeMessageId, GameLocalization.Text("mail.welcome_subject"), GameLocalization.Text("mail.welcome_body"));
            changed |= UpdateSeedMessageCopy(mailbox, DenemeEpicStoneGiftId, GameLocalization.Text("mail.epic_gift_subject"), GameLocalization.Text("mail.epic_gift_body"));
            changed |= UpdateSeedMessageCopy(mailbox, DenemeSecondEpicStoneGiftId, GameLocalization.Text("mail.epic_bonus_subject"), GameLocalization.Text("mail.epic_bonus_body"));

            mailbox.EnsureValid();
            if (changed)
                SaveAndNotify();
        }

        private bool TryAddDenemeRareStoneGift(PlayerProfile profile)
        {
            if (profile == null || profile.Mailbox.HasInboxMessage(DenemeRareStoneGiftId))
                return false;

            if (!IsDenemeAccount(profile))
                return false;

            BattleTileStore store = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>(FindObjectsInactive.Include);
            BattleTileData rareTile = FindFirstTileByRarity(store, BattleTileRarity.Rare);
            string tileId = rareTile != null ? rareTile.Id : "battle_tile_rare_gift";
            string tileName = rareTile != null && !string.IsNullOrWhiteSpace(rareTile.DisplayName) ? rareTile.DisplayName : GameLocalization.Text("mail.rare_stone");

            profile.Mailbox.Inbox.Add(new MailboxMessageData
            {
                Id = DenemeRareStoneGiftId,
                SenderName = "Symbiosis Team",
                TargetEmail = DenemeGiftEmail,
                Subject = GameLocalization.Text("mail.deneme_gift_subject"),
                Body = GameLocalization.Text("mail.deneme_gift_body"),
                CreatedAtUtc = DateTime.UtcNow.ToString("O"),
                IsFromPlayer = false,
                IsRead = false,
                IsClaimed = false,
                Attachments = new System.Collections.Generic.List<MailboxAttachmentData>
                {
                    CreateBattleTileAttachment(tileId, tileName, 1)
                }
            });

            return true;
        }

        private bool TryAddDenemeEpicStoneGift(PlayerProfile profile)
        {
            if (profile == null || profile.Mailbox.HasInboxMessage(DenemeEpicStoneGiftId))
                return false;

            if (!IsDenemeAccount(profile))
                return false;

            BattleTileStore store = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>(FindObjectsInactive.Include);
            BattleTileData epicTile = FindFirstTileByRarity(store, BattleTileRarity.Epic);
            string tileId = epicTile != null ? epicTile.Id : "battle_tile_epic_gift";
            string tileName = epicTile != null && !string.IsNullOrWhiteSpace(epicTile.DisplayName) ? epicTile.DisplayName : GameLocalization.Text("mail.epic_stone");

            profile.Mailbox.Inbox.Add(new MailboxMessageData
            {
                Id = DenemeEpicStoneGiftId,
                SenderName = "Symbiosis Team",
                TargetEmail = DenemeGiftEmail,
                Subject = GameLocalization.Text("mail.epic_gift_subject"),
                Body = GameLocalization.Text("mail.epic_gift_body"),
                CreatedAtUtc = DateTime.UtcNow.ToString("O"),
                IsFromPlayer = false,
                IsRead = false,
                IsClaimed = false,
                Attachments = new System.Collections.Generic.List<MailboxAttachmentData>
                {
                    CreateBattleTileAttachment(tileId, tileName, 1)
                }
            });

            return true;
        }

        private bool TryAddDenemeSecondEpicStoneGift(PlayerProfile profile)
        {
            if (profile == null || profile.Mailbox.HasInboxMessage(DenemeSecondEpicStoneGiftId))
                return false;

            if (!IsDenemeAccount(profile))
                return false;

            BattleTileStore store = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>(FindObjectsInactive.Include);
            BattleTileData epicTile = FindFirstTileByRarity(store, BattleTileRarity.Epic);
            string tileId = epicTile != null ? epicTile.Id : PlaceholderEpicStoneGiftId;
            string tileName = epicTile != null && !string.IsNullOrWhiteSpace(epicTile.DisplayName) ? epicTile.DisplayName : GameLocalization.Text("mail.epic_stone");

            profile.Mailbox.Inbox.Add(new MailboxMessageData
            {
                Id = DenemeSecondEpicStoneGiftId,
                SenderName = "Symbiosis Team",
                TargetEmail = DenemeGiftEmail,
                Subject = GameLocalization.Text("mail.epic_bonus_subject"),
                Body = GameLocalization.Text("mail.epic_bonus_body"),
                CreatedAtUtc = DateTime.UtcNow.ToString("O"),
                IsFromPlayer = false,
                IsRead = false,
                IsClaimed = false,
                Attachments = new System.Collections.Generic.List<MailboxAttachmentData>
                {
                    CreateBattleTileAttachment(tileId, tileName, 1)
                }
            });

            return true;
        }

        private static bool UpdateSeedMessageCopy(MailboxData mailbox, string messageId, string subject, string body)
        {
            MailboxMessageData message = mailbox != null ? mailbox.FindInboxMessage(messageId) : null;
            if (message == null)
                return false;

            bool changed = false;
            if (!string.Equals(message.Subject, subject, StringComparison.Ordinal))
            {
                message.Subject = subject;
                changed = true;
            }

            if (!string.Equals(message.Body, body, StringComparison.Ordinal))
            {
                message.Body = body;
                changed = true;
            }

            return changed;
        }

        private static bool IsDenemeAccount(PlayerProfile profile)
        {
            string profileEmail = profile != null ? profile.AccountEmail : string.Empty;
            if (string.Equals(profileEmail, DenemeGiftEmail, System.StringComparison.OrdinalIgnoreCase))
                return true;

            string serviceEmail = ProfileService.I != null ? ProfileService.I.CurrentAccountEmail : string.Empty;
            return string.Equals(serviceEmail, DenemeGiftEmail, System.StringComparison.OrdinalIgnoreCase);
        }

        private static BattleTileData FindFirstTileByRarity(BattleTileStore store, BattleTileRarity rarity)
        {
            if (store == null || store.BattleTiles == null)
                return null;

            for (int i = 0; i < store.BattleTiles.Count; i++)
            {
                BattleTileData tile = store.BattleTiles[i];
                if (tile != null && tile.Rarity == rarity && tile.Prefab != null && !string.IsNullOrWhiteSpace(tile.Id))
                    return tile;
            }

            return null;
        }

        private MailboxMessageData FindInboxMessage(string messageId)
        {
            MailboxData mailbox = GetMailbox();
            return mailbox != null ? mailbox.FindInboxMessage(messageId) : null;
        }

        public static MailboxAttachmentData CreateCurrencyAttachment(string currencyId, int amount)
        {
            string id = CurrencyWalletEntry.NormalizeCurrencyId(currencyId);
            return new MailboxAttachmentData
            {
                Kind = MailboxAttachmentKind.Currency,
                CurrencyId = id,
                Amount = Mathf.Max(0, amount),
                Label = ResolveCurrencyLabel(id),
                IconResourcePath = ResolveCurrencyIconPath(id)
            };
        }

        public static MailboxAttachmentData CreateBattleTileAttachment(string tileId, string label, int amount = 1)
        {
            return new MailboxAttachmentData
            {
                Kind = MailboxAttachmentKind.BattleTile,
                ItemId = string.IsNullOrWhiteSpace(tileId) ? string.Empty : tileId.Trim(),
                Amount = Mathf.Max(1, amount),
                Rarity = ResolveBattleTileRarityId(tileId),
                Label = string.IsNullOrWhiteSpace(label) ? GameLocalization.Text("mail.rare_stone") : label.Trim(),
                IconResourcePath = string.Empty
            };
        }

        private static string ResolveBattleTileRarityId(string tileId)
        {
            if (string.IsNullOrWhiteSpace(tileId))
                return string.Empty;

            string id = tileId.Trim();
            if (string.Equals(id, PlaceholderEpicStoneGiftId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(id, "battle_tile_epic_gift", StringComparison.OrdinalIgnoreCase))
                return "epic";

            if (string.Equals(id, PlaceholderRareStoneGiftId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(id, "battle_tile_rare_gift", StringComparison.OrdinalIgnoreCase))
                return "rare";

            BattleTileStore store = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>(FindObjectsInactive.Include);
            if (store != null && store.TryGetTileDataById(id, out BattleTileData data))
                return data.Rarity.ToString().ToLowerInvariant();

            return string.Empty;
        }

        private static bool GrantAttachment(MailboxAttachmentData attachment)
        {
            if (attachment == null || !attachment.IsValid)
                return false;

            if (attachment.Kind == MailboxAttachmentKind.BattleTile)
                return GrantBattleTile(attachment.ItemId, attachment.Amount);

            if (attachment.Kind == MailboxAttachmentKind.Currency)
            {
                GrantCurrency(attachment.CurrencyId, attachment.Amount);
                return true;
            }

            return false;
        }

        private static bool GrantOrQueueAttachment(MailboxMessageData message, MailboxAttachmentData attachment, int attachmentIndex)
        {
            if (attachment == null || !attachment.IsValid)
                return true;

            if (GrantAttachment(attachment))
                return true;

            if (attachment.Kind == MailboxAttachmentKind.BattleTile)
            {
                QueuePendingBattleTileGrant(message, attachment, attachmentIndex);
                return true;
            }

            return false;
        }

        private static void QueuePendingBattleTileGrant(MailboxMessageData message, MailboxAttachmentData attachment, int attachmentIndex)
        {
            QueuePendingBattleTileGrant(BuildPendingGrantSourceKey(message, attachmentIndex), attachment);
        }

        private static void QueuePendingBattleTileGrant(string sourceKey, MailboxAttachmentData attachment)
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null || attachment == null || !attachment.IsValid || attachment.Kind != MailboxAttachmentKind.BattleTile)
                return;

            profile.EnsureData();
            profile.Mailbox.EnsureValid();
            sourceKey = string.IsNullOrWhiteSpace(sourceKey) ? Guid.NewGuid().ToString("N") : sourceKey.Trim();
            for (int i = 0; i < profile.Mailbox.PendingBattleTileGrants.Count; i++)
            {
                MailboxPendingBattleTileGrantData existing = profile.Mailbox.PendingBattleTileGrants[i];
                if (existing != null && string.Equals(existing.SourceKey, sourceKey, StringComparison.Ordinal))
                    return;
            }

            profile.Mailbox.PendingBattleTileGrants.Add(new MailboxPendingBattleTileGrantData
            {
                SourceKey = sourceKey,
                ItemId = attachment.ItemId,
                Amount = Mathf.Max(1, attachment.Amount),
                CreatedAtUtc = DateTime.UtcNow.ToString("O")
            });
        }

        private static string BuildPendingGrantSourceKey(MailboxMessageData message, int attachmentIndex)
        {
            string messageKey = message == null
                ? Guid.NewGuid().ToString("N")
                : message.ServerRecipientId > 0
                    ? "server:" + message.ServerRecipientId
                    : !string.IsNullOrWhiteSpace(message.ExternalId)
                        ? message.ExternalId
                        : message.Id;

            return messageKey + ":attachment:" + Mathf.Max(0, attachmentIndex);
        }

        private static void GrantCurrency(string currencyId, int amount)
        {
            if (amount <= 0 || CurrencyService.I == null)
                return;

            string id = CurrencyWalletEntry.NormalizeCurrencyId(currencyId);
            if (id == CurrencyIds.OzAltin)
                CurrencyService.I.AddOzAltin(amount);
            else if (id == CurrencyIds.OzAmetist)
                CurrencyService.I.AddOzAmetist(amount);
            else if (id == CurrencyIds.OzTile)
                CurrencyService.I.AddOzTile(amount);
        }

        private static bool GrantBattleTile(string tileId, int amount)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(tileId))
                return false;

            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            BattleTileStore store = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>(FindObjectsInactive.Include);
            if (profile == null || store == null)
                return false;

            string resolvedTileId = ResolveGiftBattleTileId(store, tileId);
            if (string.IsNullOrWhiteSpace(resolvedTileId))
                return false;

            bool granted = false;
            for (int i = 0; i < amount; i++)
                granted |= BattleTileInventoryService.GrantTileCopy(profile, store, resolvedTileId, out _);

            return granted;
        }

        public static string ResolveGiftBattleTileId(BattleTileStore store, string tileId)
        {
            if (store == null || string.IsNullOrWhiteSpace(tileId))
                return string.Empty;

            string id = tileId.Trim();
            if (store.TryGetTileDataById(id, out BattleTileData direct) && direct?.Prefab != null)
                return id;

            if (string.Equals(id, PlaceholderRareStoneGiftId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(id, "battle_tile_rare_gift", StringComparison.OrdinalIgnoreCase))
            {
                BattleTileData rareTile = FindFirstTileByRarity(store, BattleTileRarity.Rare);
                return rareTile != null ? rareTile.Id : string.Empty;
            }

            if (string.Equals(id, PlaceholderEpicStoneGiftId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(id, "battle_tile_epic_gift", StringComparison.OrdinalIgnoreCase))
            {
                BattleTileData epicTile = FindFirstTileByRarity(store, BattleTileRarity.Epic);
                return epicTile != null ? epicTile.Id : string.Empty;
            }

            return string.Empty;
        }

        private static string ResolveCurrencyLabel(string currencyId)
        {
            string id = CurrencyWalletEntry.NormalizeCurrencyId(currencyId);
            if (id == CurrencyIds.OzAltin)
                return "Oz Altin";
            if (id == CurrencyIds.OzAmetist)
                return "Oz Ametist";
            if (id == CurrencyIds.OzTile)
                return "OzTile";
            return id;
        }

        private static string ResolveCurrencyIconPath(string currencyId)
        {
            string id = CurrencyWalletEntry.NormalizeCurrencyId(currencyId);
            if (id == CurrencyIds.OzAmetist)
                return "Mahjong/Sprites/Money/OzAmetist";
            if (id == CurrencyIds.OzTile)
                return "Mahjong/Sprites/BattleTiles/OzTile";
            if (id == CurrencyIds.OzAltin)
                return "Mahjong/Sprites/Money/OzAlt\u0131n";
            return string.Empty;
        }

        private PlayerProfile GetProfile()
        {
            if (ProfileService.I == null)
                ProfileRuntimeBootstrap.EnsureServices();

            if (ProfileService.I == null)
            {
                LogMissingProfileOnce();
                return null;
            }

            PlayerProfile profile = ProfileService.I.Current;
            if (profile == null)
            {
                ProfileRuntimeBootstrap.TryLoadCachedProfile();
                profile = ProfileService.I.Current;
            }

            if (profile == null)
            {
                LogMissingProfileOnce();
                return null;
            }

            loggedMissingProfile = false;
            return profile;
        }

        private MailboxData GetMailboxWithoutSeeding()
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return null;

            profile.EnsureData();
            profile.Mailbox.EnsureValid();
            return profile.Mailbox;
        }

        private void SaveAndNotify()
        {
            if (ProfileService.I != null)
            {
                ProfileService.I.Save();
                ProfileService.I.NotifyProfileChanged();
            }

            MailboxChanged?.Invoke();
        }

        private IEnumerator RefreshFromServerRoutine(string token)
        {
            refreshingFromServer = true;
            using UnityWebRequest request = UnityWebRequest.Get(BuildUrl("/mailbox?token=" + UnityWebRequest.EscapeURL(token)));
            request.timeout = 10;
            yield return request.SendWebRequest();
            refreshingFromServer = false;

            if (RequestFailed(request))
                yield break;

            ServerMailboxResponse response = Parse<ServerMailboxResponse>(request.downloadHandler.text);
            if (response == null || !response.success || response.messages == null)
                yield break;

            ApplyServerMailbox(response);
        }

        private IEnumerator MarkServerRead(int serverRecipientId)
        {
            ServerMailboxMessageRequest payload = new ServerMailboxMessageRequest
            {
                token = GetSessionToken(),
                messageId = serverRecipientId
            };
            yield return PostServer("/mailbox/read", payload, null);
        }

        private IEnumerator ClaimServerAttachments(int serverRecipientId)
        {
            ServerMailboxMessageRequest payload = new ServerMailboxMessageRequest
            {
                token = GetSessionToken(),
                messageId = serverRecipientId
            };

            string responseText = null;
            yield return PostServer("/mailbox/claim", payload, text => responseText = text);
            ServerMailboxClaimResponse response = Parse<ServerMailboxClaimResponse>(responseText);
            if (response == null || !response.success)
                yield break;

            ApplyServerBalances(response.user);
            ApplyServerGrantedClientAttachments(serverRecipientId, response.attachments);
            if (response.messages != null)
                ApplyServerMailbox(new ServerMailboxResponse { success = true, messages = response.messages });
            else
                RefreshFromServer();
        }

        private IEnumerator SendPlayerLetterToServer(string subject, string body)
        {
            string token = GetSessionToken();
            if (string.IsNullOrWhiteSpace(token))
                yield break;

            ServerPlayerLetterRequest payload = new ServerPlayerLetterRequest
            {
                token = token,
                subject = subject ?? string.Empty,
                body = body ?? string.Empty
            };

            yield return PostServer("/mailbox/player/send", payload, null);
        }

        private void ApplyServerMailbox(ServerMailboxResponse response)
        {
            PlayerProfile profile = GetProfile();
            if (profile == null || response == null || response.messages == null)
                return;

            profile.EnsureData();
            profile.Mailbox.Inbox.RemoveAll(message => message != null && message.ServerRecipientId > 0);
            bool hasServerDenemeGift = false;
            for (int i = 0; i < response.messages.Length; i++)
            {
                MailboxMessageData message = ConvertServerMessage(response.messages[i]);
                if (message == null)
                    continue;

                if (IsDeletedMessage(message))
                    continue;

                if (string.Equals(message.ExternalId, DenemeRareStoneGiftId, StringComparison.OrdinalIgnoreCase))
                    hasServerDenemeGift = true;

                profile.Mailbox.Inbox.Add(message);
            }

            if (hasServerDenemeGift)
                profile.Mailbox.Inbox.RemoveAll(message => message != null && message.ServerRecipientId <= 0 && string.Equals(message.Id, DenemeRareStoneGiftId, StringComparison.OrdinalIgnoreCase));

            profile.Mailbox.EnsureValid();
            RepairClaimedClientAttachmentGifts(profile);
            SaveAndNotify();
        }

        private static MailboxMessageData ConvertServerMessage(ServerMailboxMessage server)
        {
            if (server == null || server.id <= 0)
                return null;

            MailboxMessageData message = new MailboxMessageData
            {
                Id = "server:" + server.id,
                ServerRecipientId = server.id,
                ExternalId = server.externalId ?? string.Empty,
                SenderName = server.senderName ?? "Symbiosis Team",
                TargetEmail = server.targetEmail ?? string.Empty,
                Subject = server.subject ?? string.Empty,
                Body = server.body ?? string.Empty,
                CreatedAtUtc = server.createdAt ?? DateTime.UtcNow.ToString("O"),
                IsFromPlayer = string.Equals(server.category, "player", StringComparison.OrdinalIgnoreCase),
                IsRead = server.isRead,
                IsClaimed = server.isClaimed,
                Attachments = new System.Collections.Generic.List<MailboxAttachmentData>()
            };

            if (server.attachments != null)
            {
                for (int i = 0; i < server.attachments.Length; i++)
                {
                    MailboxAttachmentData attachment = ConvertServerAttachment(server.attachments[i]);
                    if (attachment != null)
                        message.Attachments.Add(attachment);
                }
            }

            message.EnsureValid();
            return message;
        }

        private static MailboxAttachmentData ConvertServerAttachment(ServerMailboxAttachment server)
        {
            if (server == null)
                return null;

            MailboxAttachmentKind kind = MailboxAttachmentKind.Currency;
            string rawKind = server.kind ?? string.Empty;
            if (rawKind.Equals("battle_tile", StringComparison.OrdinalIgnoreCase) || rawKind.Equals("stone", StringComparison.OrdinalIgnoreCase))
                kind = MailboxAttachmentKind.BattleTile;
            else if (rawKind.Equals("item", StringComparison.OrdinalIgnoreCase))
                kind = MailboxAttachmentKind.Item;

            MailboxAttachmentData attachment = new MailboxAttachmentData
            {
                Kind = kind,
                CurrencyId = server.currencyId ?? string.Empty,
                ItemId = server.itemId ?? string.Empty,
                Amount = Mathf.Max(0, server.amount),
                Rarity = server.rarity ?? string.Empty,
                Label = server.label ?? string.Empty,
                IconResourcePath = server.iconResourcePath ?? string.Empty
            };
            attachment.EnsureValid();
            return attachment.IsValid ? attachment : null;
        }

        private static void ApplyServerGrantedClientAttachments(int serverRecipientId, ServerMailboxAttachment[] attachments)
        {
            if (attachments == null)
                return;

            bool grantedAnyClientAttachment = false;
            for (int i = 0; i < attachments.Length; i++)
            {
                MailboxAttachmentData attachment = ConvertServerAttachment(attachments[i]);
                if (attachment != null && attachment.Kind != MailboxAttachmentKind.Currency)
                {
                    if (GrantAttachment(attachment))
                    {
                        grantedAnyClientAttachment = true;
                    }
                    else if (attachment.Kind == MailboxAttachmentKind.BattleTile)
                    {
                        QueuePendingBattleTileGrant("server:" + serverRecipientId + ":attachment:" + i, attachment);
                        grantedAnyClientAttachment = true;
                    }
                }
            }

            if (grantedAnyClientAttachment && serverRecipientId > 0)
                PlayerPrefs.SetInt(KeyClientGrantPrefix + serverRecipientId, 1);
        }

        public void RepairClaimedClientAttachmentGifts()
        {
            PlayerProfile profile = GetProfile();
            if (profile != null)
                RepairClaimedClientAttachmentGifts(profile);
        }

        private static void RepairClaimedClientAttachmentGifts(PlayerProfile profile)
        {
            if (profile == null)
                return;

            BattleTileStore store = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>(FindObjectsInactive.Include);
            if (store == null)
                return;

            profile.EnsureData();
            bool changed = ApplyPendingBattleTileGrants(profile, store);
            changed |= RepairPlaceholderBattleTileStack(profile, store);
            MailboxData mailbox = profile.Mailbox;
            if (mailbox?.Inbox != null)
            {
                for (int i = 0; i < mailbox.Inbox.Count; i++)
                {
                    MailboxMessageData message = mailbox.Inbox[i];
                if (message == null || !message.IsClaimed || !HasClientGrantedAttachments(message) || (IsClientGrantRecorded(message) && HasAllClientAttachmentsInInventory(message, store)))
                    continue;

                    changed |= GrantMessageClientAttachments(message);
                    RecordClientGrant(message);
                }
            }

            if (changed && ProfileService.I != null)
            {
                ProfileService.I.Save();
                ProfileService.I.NotifyProfileChanged();
                MailboxChanged?.Invoke();
            }
        }

        private static bool RepairPlaceholderBattleTileStack(PlayerProfile profile, BattleTileStore store)
        {
            MahjongBattleTileInventoryData inventory = BattleTileInventoryService.GetOrCreateInventory(profile);
            string resolvedRareId = ResolveGiftBattleTileId(store, PlaceholderRareStoneGiftId);
            string resolvedEpicId = ResolveGiftBattleTileId(store, PlaceholderEpicStoneGiftId);
            if (inventory == null)
                return false;

            int placeholderRareCount = 0;
            int placeholderEpicCount = 0;
            if (inventory.TileStacks != null)
            {
                for (int i = inventory.TileStacks.Count - 1; i >= 0; i--)
                {
                    MahjongBattleTileStackData stack = inventory.TileStacks[i];
                    if (stack == null)
                        continue;

                    if (string.Equals(stack.TileId, PlaceholderRareStoneGiftId, StringComparison.OrdinalIgnoreCase))
                    {
                        placeholderRareCount += Mathf.Max(0, stack.Count);
                        inventory.TileStacks.RemoveAt(i);
                    }
                    else if (string.Equals(stack.TileId, PlaceholderEpicStoneGiftId, StringComparison.OrdinalIgnoreCase))
                    {
                        placeholderEpicCount += Mathf.Max(0, stack.Count);
                        inventory.TileStacks.RemoveAt(i);
                    }
                }
            }

            inventory.ActiveTileIds?.RemoveAll(id => string.Equals(id, PlaceholderRareStoneGiftId, StringComparison.OrdinalIgnoreCase));
            inventory.ReserveTileIds?.RemoveAll(id => string.Equals(id, PlaceholderRareStoneGiftId, StringComparison.OrdinalIgnoreCase));
            inventory.ActiveTileIds?.RemoveAll(id => string.Equals(id, PlaceholderEpicStoneGiftId, StringComparison.OrdinalIgnoreCase));
            inventory.ReserveTileIds?.RemoveAll(id => string.Equals(id, PlaceholderEpicStoneGiftId, StringComparison.OrdinalIgnoreCase));
            if ((placeholderRareCount <= 0 || string.IsNullOrWhiteSpace(resolvedRareId)) &&
                (placeholderEpicCount <= 0 || string.IsNullOrWhiteSpace(resolvedEpicId)))
                return false;

            for (int i = 0; i < placeholderRareCount && !string.IsNullOrWhiteSpace(resolvedRareId); i++)
                BattleTileInventoryService.GrantTileCopy(profile, store, resolvedRareId, out _);

            for (int i = 0; i < placeholderEpicCount && !string.IsNullOrWhiteSpace(resolvedEpicId); i++)
                BattleTileInventoryService.GrantTileCopy(profile, store, resolvedEpicId, out _);

            return true;
        }

        public static bool ApplyPendingBattleTileGrants(BattleTileStore store)
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            bool changed = ApplyPendingBattleTileGrants(profile, store);
            if (changed && ProfileService.I != null)
            {
                ProfileService.I.Save();
                ProfileService.I.NotifyProfileChanged();
                MailboxChanged?.Invoke();
            }

            return changed;
        }

        private static bool ApplyPendingBattleTileGrants(PlayerProfile profile, BattleTileStore store)
        {
            if (profile == null || store == null)
                return false;

            profile.EnsureData();
            profile.Mailbox.EnsureValid();
            if (profile.Mailbox.PendingBattleTileGrants == null || profile.Mailbox.PendingBattleTileGrants.Count == 0)
                return false;

            bool changed = false;
            for (int i = profile.Mailbox.PendingBattleTileGrants.Count - 1; i >= 0; i--)
            {
                MailboxPendingBattleTileGrantData grant = profile.Mailbox.PendingBattleTileGrants[i];
                if (grant == null)
                {
                    profile.Mailbox.PendingBattleTileGrants.RemoveAt(i);
                    changed = true;
                    continue;
                }

                grant.EnsureValid();
                string resolvedId = ResolveGiftBattleTileId(store, grant.ItemId);
                if (string.IsNullOrWhiteSpace(resolvedId))
                    continue;

                bool granted = false;
                for (int copy = 0; copy < grant.Amount; copy++)
                    granted |= BattleTileInventoryService.GrantTileCopy(profile, store, resolvedId, out _);

                if (granted)
                {
                    profile.Mailbox.PendingBattleTileGrants.RemoveAt(i);
                    changed = true;
                }
            }

            return changed;
        }

        private static bool HasClientGrantedAttachments(MailboxMessageData message)
        {
            if (message?.Attachments == null)
                return false;

            for (int i = 0; i < message.Attachments.Count; i++)
            {
                MailboxAttachmentData attachment = message.Attachments[i];
                if (attachment != null && attachment.IsValid && attachment.Kind != MailboxAttachmentKind.Currency)
                    return true;
            }

            return false;
        }

        private static bool GrantMessageClientAttachments(MailboxMessageData message)
        {
            if (message?.Attachments == null)
                return false;

            bool changed = false;
            for (int i = 0; i < message.Attachments.Count; i++)
            {
                MailboxAttachmentData attachment = message.Attachments[i];
                if (attachment != null && attachment.IsValid && attachment.Kind != MailboxAttachmentKind.Currency)
                    changed |= GrantOrQueueAttachment(message, attachment, i);
            }

            return changed;
        }

        private static bool IsClientGrantRecorded(MailboxMessageData message)
        {
            string key = GetClientGrantKey(message);
            return !string.IsNullOrWhiteSpace(key) && PlayerPrefs.GetInt(key, 0) == 1;
        }

        private static void RecordClientGrant(MailboxMessageData message)
        {
            string key = GetClientGrantKey(message);
            if (!string.IsNullOrWhiteSpace(key))
                PlayerPrefs.SetInt(key, 1);
        }

        private static string GetClientGrantKey(MailboxMessageData message)
        {
            if (message == null)
                return string.Empty;

            if (message.ServerRecipientId > 0)
                return KeyClientGrantPrefix + message.ServerRecipientId;

            return string.IsNullOrWhiteSpace(message.Id) ? string.Empty : KeyClientGrantPrefix + message.Id;
        }

        private static bool IsDeletedMessage(MailboxMessageData message)
        {
            string key = GetDeletedMessageKey(message);
            return !string.IsNullOrWhiteSpace(key) && PlayerPrefs.GetInt(key, 0) == 1;
        }

        private static void RecordDeletedMessage(MailboxMessageData message)
        {
            string key = GetDeletedMessageKey(message);
            if (!string.IsNullOrWhiteSpace(key))
                PlayerPrefs.SetInt(key, 1);
        }

        private static string GetDeletedMessageKey(MailboxMessageData message)
        {
            if (message == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(message.ExternalId))
                return KeyDeletedMessagePrefix + message.ExternalId;

            if (message.ServerRecipientId > 0)
                return KeyDeletedMessagePrefix + message.ServerRecipientId;

            return string.IsNullOrWhiteSpace(message.Id) ? string.Empty : KeyDeletedMessagePrefix + message.Id;
        }

        private static bool HasAllClientAttachmentsInInventory(MailboxMessageData message, BattleTileStore store)
        {
            if (message?.Attachments == null)
                return true;

            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            MahjongBattleTileInventoryData inventory = BattleTileInventoryService.GetOrCreateInventory(profile);
            if (inventory == null || store == null)
                return false;

            for (int i = 0; i < message.Attachments.Count; i++)
            {
                MailboxAttachmentData attachment = message.Attachments[i];
                if (attachment == null || !attachment.IsValid || attachment.Kind != MailboxAttachmentKind.BattleTile)
                    continue;

                string resolvedId = ResolveGiftBattleTileId(store, attachment.ItemId);
                if (string.IsNullOrWhiteSpace(resolvedId) || BattleTileInventoryService.GetOwnedCount(inventory, resolvedId) <= 0)
                    return false;
            }

            return true;
        }

        private static void ApplyServerBalances(ServerMailboxUser user)
        {
            if (user == null || CurrencyService.I == null)
                return;

            CurrencyService.I.SetOzAltin(Mathf.Max(0, user.goldBalance));
            CurrencyService.I.SetOzAmetist(Mathf.Max(0, user.amethystBalance));
            CurrencyService.I.SetOzTile(Mathf.Max(0, user.ozTileBalance));
        }

        private static IEnumerator PostServer(string path, object payload, Action<string> completed)
        {
            string json = JsonUtility.ToJson(payload);
            using UnityWebRequest request = new UnityWebRequest(BuildUrl(path), "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (!RequestFailed(request))
                completed?.Invoke(request.downloadHandler.text);
        }

        private static string BuildUrl(string path)
        {
            return BackendEndpoints.BuildUrl(BackendEndpoints.PrimaryBaseUrl, path);
        }

        private static string GetSessionToken()
        {
            return PlayerPrefs.GetString(KeySessionToken, string.Empty);
        }

        private static bool RequestFailed(UnityWebRequest request)
        {
            return request == null || request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError;
        }

        private static T Parse<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch
            {
                return null;
            }
        }

        private void LogMissingProfileOnce()
        {
            if (loggedMissingProfile)
                return;

            Debug.LogWarning("[MailboxService] Profile is not loaded.");
            loggedMissingProfile = true;
        }

        [Serializable] private sealed class ServerMailboxMessageRequest { public string token; public int messageId; }
        [Serializable] private sealed class ServerPlayerLetterRequest { public string token; public string subject; public string body; }
        [Serializable] private sealed class ServerMailboxResponse { public bool success; public string error; public ServerMailboxMessage[] messages; public int unreadCount; public int claimableCount; }
        [Serializable] private sealed class ServerMailboxClaimResponse { public bool success; public string error; public bool claimed; public int messageId; public ServerMailboxAttachment[] attachments; public ServerMailboxUser user; public ServerMailboxMessage[] messages; }
        [Serializable] private sealed class ServerMailboxUser { public int goldBalance; public int amethystBalance; public int ozTileBalance; }

        [Serializable]
        private sealed class ServerMailboxMessage
        {
            public int id;
            public int messageId;
            public string externalId;
            public string senderName;
            public string category;
            public string targetEmail;
            public string subject;
            public string body;
            public bool isRead;
            public bool isClaimed;
            public string createdAt;
            public string expiresAt;
            public ServerMailboxAttachment[] attachments;
        }

        [Serializable]
        private sealed class ServerMailboxAttachment
        {
            public int id;
            public string kind;
            public string currencyId;
            public string itemId;
            public int amount;
            public string label;
            public string iconResourcePath;
            public string rarity;
            public string metadataJson;
        }
    }
}
