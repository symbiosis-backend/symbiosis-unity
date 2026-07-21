using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    public enum MailboxAttachmentKind
    {
        Currency = 0,
        BattleTile = 1,
        Item = 2
    }

    [Serializable]
    public sealed class MailboxAttachmentData
    {
        public MailboxAttachmentKind Kind;
        public string CurrencyId;
        public string ItemId;
        public string Rarity;
        public int Amount;
        public string Label;
        public string IconResourcePath;

        public bool IsValid => Amount > 0 && (Kind != MailboxAttachmentKind.Currency || !string.IsNullOrWhiteSpace(CurrencyId));

        public void EnsureValid()
        {
            CurrencyId = string.IsNullOrWhiteSpace(CurrencyId) ? string.Empty : CurrencyId.Trim();
            ItemId = string.IsNullOrWhiteSpace(ItemId) ? string.Empty : ItemId.Trim();
            Rarity = string.IsNullOrWhiteSpace(Rarity) ? string.Empty : Rarity.Trim().ToLowerInvariant();
            Label = string.IsNullOrWhiteSpace(Label) ? ResolveDefaultLabel() : Label.Trim();
            IconResourcePath = string.IsNullOrWhiteSpace(IconResourcePath) ? string.Empty : IconResourcePath.Trim();
            Amount = Mathf.Max(0, Amount);
        }

        private string ResolveDefaultLabel()
        {
            if (Kind == MailboxAttachmentKind.BattleTile)
                return string.IsNullOrWhiteSpace(ItemId) ? GameLocalization.Text("mail.attachment_stone") : ItemId;

            return string.IsNullOrWhiteSpace(CurrencyId) ? GameLocalization.Text("mail.attachment_item") : CurrencyId;
        }
    }

    [Serializable]
    public sealed class MailboxMessageData
    {
        public string Id;
        public int ServerRecipientId;
        public string ExternalId;
        public string SenderName;
        public string TargetEmail;
        public string Subject;
        public string Body;
        public string CreatedAtUtc;
        public bool IsFromPlayer;
        public bool IsRead;
        public bool IsClaimed;
        public List<MailboxAttachmentData> Attachments;

        public bool HasClaimableAttachments
        {
            get
            {
                if (IsClaimed || Attachments == null)
                    return false;

                for (int i = 0; i < Attachments.Count; i++)
                {
                    if (Attachments[i] != null && Attachments[i].IsValid)
                        return true;
                }

                return false;
            }
        }

        public void EnsureValid()
        {
            Id = string.IsNullOrWhiteSpace(Id) ? Guid.NewGuid().ToString("N") : Id.Trim();
            ServerRecipientId = Mathf.Max(0, ServerRecipientId);
            ExternalId = string.IsNullOrWhiteSpace(ExternalId) ? string.Empty : ExternalId.Trim();
            SenderName = string.IsNullOrWhiteSpace(SenderName) ? "Symbiosis" : SenderName.Trim();
            TargetEmail = string.IsNullOrWhiteSpace(TargetEmail) ? string.Empty : TargetEmail.Trim().ToLowerInvariant();
            Subject = string.IsNullOrWhiteSpace(Subject) ? GameLocalization.Text("mail.no_subject") : Subject.Trim();
            Body = Body ?? string.Empty;
            CreatedAtUtc = string.IsNullOrWhiteSpace(CreatedAtUtc) ? DateTime.UtcNow.ToString("O") : CreatedAtUtc.Trim();

            if (Attachments == null)
                Attachments = new List<MailboxAttachmentData>();

            for (int i = Attachments.Count - 1; i >= 0; i--)
            {
                MailboxAttachmentData attachment = Attachments[i];
                if (attachment == null)
                {
                    Attachments.RemoveAt(i);
                    continue;
                }

                attachment.EnsureValid();
                if (!attachment.IsValid)
                    Attachments.RemoveAt(i);
            }

            if (!HasClaimableAttachments)
                IsClaimed = IsClaimed || Attachments.Count == 0;
        }
    }

    [Serializable]
    public sealed class MailboxPendingBattleTileGrantData
    {
        public string SourceKey;
        public string ItemId;
        public int Amount;
        public string CreatedAtUtc;

        public bool IsValid => Amount > 0 && !string.IsNullOrWhiteSpace(ItemId);

        public void EnsureValid()
        {
            SourceKey = string.IsNullOrWhiteSpace(SourceKey) ? Guid.NewGuid().ToString("N") : SourceKey.Trim();
            ItemId = string.IsNullOrWhiteSpace(ItemId) ? string.Empty : ItemId.Trim();
            Amount = Mathf.Max(0, Amount);
            CreatedAtUtc = string.IsNullOrWhiteSpace(CreatedAtUtc) ? DateTime.UtcNow.ToString("O") : CreatedAtUtc.Trim();
        }
    }

    [Serializable]
    public sealed class MailboxData
    {
        public int DataVersion;
        public List<MailboxMessageData> Inbox;
        public List<MailboxMessageData> PlayerLetters;
        public List<MailboxPendingBattleTileGrantData> PendingBattleTileGrants;

        public MailboxData()
        {
            DataVersion = 1;
            Inbox = new List<MailboxMessageData>();
            PlayerLetters = new List<MailboxMessageData>();
            PendingBattleTileGrants = new List<MailboxPendingBattleTileGrantData>();
        }

        public void EnsureValid()
        {
            DataVersion = Mathf.Max(1, DataVersion);

            if (Inbox == null)
                Inbox = new List<MailboxMessageData>();

            if (PlayerLetters == null)
                PlayerLetters = new List<MailboxMessageData>();

            if (PendingBattleTileGrants == null)
                PendingBattleTileGrants = new List<MailboxPendingBattleTileGrantData>();

            SanitizeList(Inbox);
            SanitizeList(PlayerLetters);
            SanitizePendingBattleTileGrants(PendingBattleTileGrants);
        }

        public MailboxMessageData FindInboxMessage(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || Inbox == null)
                return null;

            for (int i = 0; i < Inbox.Count; i++)
            {
                MailboxMessageData message = Inbox[i];
                if (message != null && string.Equals(message.Id, id, StringComparison.Ordinal))
                    return message;
            }

            return null;
        }

        public bool HasInboxMessage(string id)
        {
            return FindInboxMessage(id) != null;
        }

        public int CountUnreadInbox()
        {
            int count = 0;
            if (Inbox == null)
                return count;

            for (int i = 0; i < Inbox.Count; i++)
            {
                if (Inbox[i] != null && !Inbox[i].IsRead)
                    count++;
            }

            return count;
        }

        public int CountClaimableInbox()
        {
            int count = 0;
            if (Inbox == null)
                return count;

            for (int i = 0; i < Inbox.Count; i++)
            {
                if (Inbox[i] != null && Inbox[i].HasClaimableAttachments)
                    count++;
            }

            return count;
        }

        private static void SanitizeList(List<MailboxMessageData> messages)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                MailboxMessageData message = messages[i];
                if (message == null)
                {
                    messages.RemoveAt(i);
                    continue;
                }

                message.EnsureValid();
                if (!seen.Add(message.Id))
                    messages.RemoveAt(i);
            }

            messages.Sort((a, b) => string.CompareOrdinal(b.CreatedAtUtc, a.CreatedAtUtc));
        }

        private static void SanitizePendingBattleTileGrants(List<MailboxPendingBattleTileGrantData> grants)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = grants.Count - 1; i >= 0; i--)
            {
                MailboxPendingBattleTileGrantData grant = grants[i];
                if (grant == null)
                {
                    grants.RemoveAt(i);
                    continue;
                }

                grant.EnsureValid();
                if (!grant.IsValid || !seen.Add(grant.SourceKey))
                    grants.RemoveAt(i);
            }
        }
    }
}
