﻿using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Items;
using Canyon.Game.States.User;
using System.Collections.Concurrent;

namespace Canyon.Game.States.Mails
{
    public sealed class MailBox
    {
        public const int PageSize = 7;

        private readonly TimeOut checkMailsTimer = new(60);
        private readonly Character user;
        private readonly ConcurrentDictionary<uint, MailMessage> emails = new();

        public MailBox(Character user)
        {
            this.user = user;
        }

        private async Task QueryAsync()
        {
            var mails = await MailRepository.GetAsync(user.Identity);
            foreach (var mail in mails)
            {
                if (!emails.ContainsKey(mail.Id))
                {
                    emails.TryAdd(mail.Id, new MailMessage(mail));
                }
            }
        }

        public async Task InitializeAsync()
        {
            await QueryAsync();
            if (emails.Count > 0)
            {
                await NotifyAsync();
            }
        }

        public static async Task<bool> SendAsync(uint target, string senderName, string header, string body, uint expiration,
            ulong money = 0, uint emoney = 0, bool emoneyMono = false, uint itemId = 0, uint itemType = 0, uint action = 0)
        {
            DbCharacter dbTarget = await CharacterRepository.FindByIdentityAsync(target);
            if (dbTarget == null)
            {
                return false; // target not found
            }

            DbMail mail = new()
            {
                ReceiverId = target,
                SenderName = senderName,
                Title = header,
                Content = body,
                Action = action,
                Money = money,
                ConquerPoints = emoney,
                ExpirationDate = expiration,
                ItemId = itemId,
                EmoneyRecordType = (byte)(emoneyMono ? 1 : 0)
            };

            if (!await ServerDbContext.SaveAsync(mail))
            {
                return false;
            }

            Character targetUser = RoleManager.GetUser(target);
            if (targetUser != null)
            {
                await targetUser.MailBox.QueryAsync();
                await targetUser.MailBox.NotifyAsync();
            }
            return true;
        }

        public async Task SendListAsync(int page)
        {
            int now = UnixTimestamp.Now;
            int from = page;
            MsgMailList msg = new();
            msg.Page = page;
            msg.MaxPages = (ushort)Math.Ceiling(emails.Values.Count / (double)PageSize);
            foreach (var mail in emails.Values
                .Where(x => !x.HasExpired)
                .OrderByDescending(x => x.Order).ThenBy(x => x.Expiration)
                .Skip(from)
                .Take(PageSize))
            {
                msg.MailList.Add(new MsgMailList.MailListStruct
                {
                    EmailIdentity = mail.Identity,
                    SenderName = mail.SenderName,
                    Header = mail.Title[..Math.Min(mail.Title.Length, 32)],
                    Money = (uint)mail.Money,
                    ConquerPoints = mail.ConquerPoints,
                    ItemType = mail.Item,
                    Timestamp = (int)Math.Max(0, mail.Expiration - now),
                    HasAttachment = mail.HasClaimedItem && mail.HasClaimedAction ? 1 : 0
                });
            }
            await user.SendAsync(msg);
        }

        public async Task SendMessageAsync(uint idMail)
        {
            if (!emails.TryGetValue(idMail, out var mail))
            {
                return;
            }

            await user.SendAsync(new MsgMailContent
            {
                Data = idMail,
                Content = mail.Content
            });
        }

        public async Task DeleteMessageAsync(uint idMail)
        {
            if (!emails.TryRemove(idMail, out var mail))
            {
                return;
            }

            await mail.DeleteAsync();
        }

        public async Task ClaimMoneyAsync(uint idMail)
        {
            if (!emails.TryGetValue(idMail, out var mail))
            {
                return;
            }

            if (mail.HasClaimedMoney)
            {
                return;
            }

            await user.AwardMoneyAsync((int)mail.Money);
            mail.Money = 0;
            await mail.SaveAsync();
        }

        public async Task ClaimConquerPointsAsync(uint idMail)
        {
            if (!emails.TryGetValue(idMail, out var mail))
            {
                return;
            }

            if (mail.HasClaimedConquerPoints)
            {
                return;
            }

            if (mail.IsEmoneyMono)
            {
                await user.AwardBoundConquerPointsAsync((int)mail.ConquerPoints);
            }
            else
            {
                await user.AwardConquerPointsAsync((int)mail.ConquerPoints);
            }

            mail.ConquerPoints = 0;
            await mail.SaveAsync();
        }

        public async Task ClaimActionAsync(uint idMail)
        {
            if (!emails.TryGetValue(idMail, out var mail))
            {
                return;
            }

            if (mail.HasClaimedAction)
            {
                return;
            }

            await GameAction.ExecuteActionAsync(mail.Action, user, null, null, null);
            mail.Action = 0;
            await mail.SaveAsync();
        }

        public async Task ClaimItemAsync(uint idMail)
        {
            if (!emails.TryGetValue(idMail, out var mail))
            {
                return;
            }

            if (mail.HasClaimedItem)
            {
                return;
            }

            DbItem dbItem = await ItemRepository.GetByIdAsync(mail.Item);
            if (dbItem?.PlayerId != 0 && dbItem.Position != (int)Item.ItemPosition.Auction)
            {
                mail.Item = 0;
                await mail.SaveAsync();
                return;
            }

            Item item = new(user);
            if (!await item.CreateAsync(dbItem))
            {
                return;
            }

            if (!user.UserPackage.IsPackSpare((int)item.AccumulateNum, item.Type))
            {
                return;
            }

            await user.UserPackage.AddItemAsync(item);
            await ClaimActionAsync(idMail);

            mail.Item = 0;
            await mail.SaveAsync();
        }

        public Task NotifyAsync()
        {
            return user.SendAsync(new MsgMailNotify
            {
                Action = MsgMailNotify.MailNotification.Notification
            });
        }

        public async Task OnTimerAsync()
        {
            if (!checkMailsTimer.ToNextTime())
            {
                return;
            }

            await QueryAsync();
        }
    }
}
