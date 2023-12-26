﻿using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.User;
using System.Collections.Concurrent;

namespace Canyon.Game.States.NeiGong
{
    public sealed class InnerStrength
    {
        public const int FINISH_VALUE = 100;

        private static readonly ILogger logger = LogFactory.CreateLogger<InnerStrength>();

        private readonly Character user;
        private ConcurrentDictionary<ushort, InnerStrengthSecret> secrets = new();

        public InnerStrength(Character user)
        {
            this.user = user;
        }

        public int MaxLife { get; private set; }
        public int Attack { get; private set; }
        public int MagicAttack { get; private set; }
        public int Defense { get; private set; }
        public int MagicDefense { get; private set; }
        public int FinalPhysicalDamage { get; private set; }
        public int FinalMagicalDamage { get; private set; }
        public int FinalPhysicalDefense { get; private set; }
        public int FinalMagicalDefense { get; private set; }
        public int CriticalStrike { get; private set; }
        public int SkillCriticalStrike { get; private set; }
        public int Immunity { get; private set; }
        public int Breakthrough { get; private set; }
        public int Counteraction { get; private set; }

        public int TotalValue => secrets.Values.Sum(x => x.TotalValue);

        private void RefreshPowers()
        {
            MaxLife = 0;
            Attack = 0;
            MagicAttack = 0;
            Defense = 0;
            MagicDefense = 0;
            FinalPhysicalDamage = 0;
            FinalMagicalDamage = 0;
            FinalPhysicalDefense = 0;
            FinalMagicalDefense = 0;
            CriticalStrike = 0;
            SkillCriticalStrike = 0;
            Immunity = 0;
            Breakthrough = 0;
            Counteraction = 0;

            foreach (var secret in secrets.Values)
            {
                var powers = secret.GetPower();
                foreach (var power in powers)
                {
                    switch (power.Key)
                    {
                        case InnerStrengthAttrType.MaxLife:
                            MaxLife += power.Value;
                            break;
                        case InnerStrengthAttrType.Attack:
                            Attack += power.Value;
                            break;
                        case InnerStrengthAttrType.MagicAttack:
                            MagicAttack += power.Value;
                            break;
                        case InnerStrengthAttrType.Defense:
                            Defense += power.Value;
                            break;
                        case InnerStrengthAttrType.MagicDefense:
                            MagicDefense += power.Value;
                            break;
                        case InnerStrengthAttrType.FinalPhysicalDamage:
                            FinalPhysicalDamage += power.Value;
                            break;
                        case InnerStrengthAttrType.FinalMagicalDamage:
                            FinalMagicalDamage += power.Value;
                            break;
                        case InnerStrengthAttrType.FinalPhysicalDefense:
                            FinalPhysicalDefense += power.Value;
                            break;
                        case InnerStrengthAttrType.FinalMagicalDefense:
                            FinalMagicalDefense += power.Value;
                            break;
                        case InnerStrengthAttrType.CriticalStrike:
                            CriticalStrike += power.Value;
                            break;
                        case InnerStrengthAttrType.SkillCriticalStrike:
                            SkillCriticalStrike += power.Value;
                            break;
                        case InnerStrengthAttrType.Immunity:
                            Immunity += power.Value;
                            break;
                        case InnerStrengthAttrType.Breakthrough:
                            Breakthrough += power.Value;
                            break;
                        case InnerStrengthAttrType.Counteraction:
                            Counteraction += power.Value;
                            break;
                    }
                }
            }
        }

        private Dictionary<InnerStrengthAttrType, int> GetInnerPowers(InnerStrengthPower strength)
        {
            Dictionary<InnerStrengthAttrType, int> powers = new();
            if (strength.MaxLife > 0)
            {
                powers.Add(InnerStrengthAttrType.MaxLife, (int)strength.MaxLife);
            }
            if (strength.PhysicAttackNew > 0)
            {
                powers.Add(InnerStrengthAttrType.Attack, (int)strength.PhysicAttackNew);
            }
            if (strength.MagicAttack > 0)
            {
                powers.Add(InnerStrengthAttrType.MagicAttack, (int)strength.MagicAttack);
            }
            if (strength.PhysicDefenseNew > 0)
            {
                powers.Add(InnerStrengthAttrType.Defense, (int)strength.PhysicDefenseNew);
            }
            if (strength.MagicDefense > 0)
            {
                powers.Add(InnerStrengthAttrType.MagicDefense, (int)strength.MagicDefense);
            }
            if (strength.FinalPhysicAdd > 0)
            {
                powers.Add(InnerStrengthAttrType.FinalPhysicalDamage, strength.FinalPhysicAdd);
            }
            if (strength.FinalMagicAdd > 0)
            {
                powers.Add(InnerStrengthAttrType.FinalMagicalDamage, strength.FinalMagicAdd);
            }
            if (strength.FinalPhysicReduce > 0)
            {
                powers.Add(InnerStrengthAttrType.FinalPhysicalDefense, strength.FinalPhysicReduce);
            }
            if (strength.FinalMagicReduce > 0)
            {
                powers.Add(InnerStrengthAttrType.FinalMagicalDefense, strength.FinalMagicReduce);
            }
            if (strength.PhysicCrit > 0)
            {
                powers.Add(InnerStrengthAttrType.CriticalStrike, strength.PhysicCrit);
            }
            if (strength.MagicCrit > 0)
            {
                powers.Add(InnerStrengthAttrType.SkillCriticalStrike, strength.MagicCrit);
            }
            if (strength.DefenseCrit > 0)
            {
                powers.Add(InnerStrengthAttrType.Immunity, strength.DefenseCrit);
            }
            if (strength.SmashRate > 0)
            {
                powers.Add(InnerStrengthAttrType.Breakthrough, strength.SmashRate);
            }
            if (strength.FirmDefenseRate > 0)
            {
                powers.Add(InnerStrengthAttrType.Counteraction, strength.FirmDefenseRate);
            }
            return powers;
        }

        public async Task<bool> InitializeAsync()
        {
            foreach (var secret in await InnerStrenghtRepository.GetPlayerSecretsAsync(user.Identity))
            {
                secrets.TryAdd(secret.SecretType, new InnerStrengthSecret(secret));
            }

            foreach (var power in await InnerStrenghtRepository.GetPlayerInnersAsync(user.Identity))
            {
                var typeInfo = InnerStrengthManager.QueryTypeInfo((byte)power.Type);
                if (typeInfo != null && secrets.TryGetValue(typeInfo.SecretType, out var secret))
                {
                    secret.AddBook(new InnerStrengthPower(power));
                }
            }

            await user.SynchroAttributesAsync(ClientUpdateType.InnerPowerPotency, user.CultureValue);
            RefreshPowers();
            return true;
        }

        public bool HasLearnedStrengthType(int idType)
        {
            return secrets.Values.Any(x => x.HasBook(idType));
        }

        public int GetInnerStrengthLevelByType(int idType)
        {
            foreach (var secret in secrets.Values)
            {
                var book = secret.GetBook(idType);
                if (book != null)
                {
                    return book.Value;
                }
            }
            return 0;
        }

        public InnerStrengthPower FindCheat(int idType)
        {
            foreach (var secret in secrets.Values)
            {
                var book = secret.GetBook(idType);
                if (book != null)
                {
                    return book;
                }
            }
            return null;
        }

        public async Task<bool> UnlockAsync(byte type)
        {
            if (HasLearnedStrengthType(type))
            {
                logger.LogWarning("User [{},{}] attempt to unlock already existent [{}] inner power", user.Identity, user.Name, type);
                return false;
            }

            var typeInfo = InnerStrengthManager.QueryTypeInfo(type);
            if (typeInfo == null)
            {
                logger.LogWarning("User [{},{}] tried to learn invalid inner power type [{}]", user.Identity, user.Name, type);
                return false;
            }

            int requireMetempsychosis = typeInfo.RequiredLevel / 1000;
            int requireLevel = typeInfo.RequiredLevel % 1000;
            if (user.Level < requireLevel && user.Metempsychosis <= requireMetempsychosis)
            {
                await user.SendAsync(StrNeiGongPlayerLevelErr);
                return false;
            }

            if (typeInfo.RequiredPreNeiGong > 0)
            {
                int reqType = typeInfo.RequiredPreNeiGong / 1000;
                int reqTypeLevel = typeInfo.RequiredPreNeiGong % 1000;
                var reqPower = FindCheat(reqType);
                if (reqPower == null || reqPower.Level < reqTypeLevel)
                {
                    await user.SendAsync(StrNeiGongReqPreNg);
                    return false;
                }
            }

            if (!secrets.TryGetValue(typeInfo.SecretType, out var secret))
            {
                // no secret open, open secret
                DbInnerStrenghtSecret dbSecret = new DbInnerStrenghtSecret
                {
                    SecretType = typeInfo.SecretType,
                    PlayerIdentity = user.Identity
                };
                await ServerDbContext.CreateAsync(dbSecret);
                secret = new InnerStrengthSecret(dbSecret);
                secrets.TryAdd(typeInfo.SecretType, secret);
            }

            // enable the power
            var dbPlayer = new DbInnerStrenghtPlayer()
            {
                PlayerId = user.Identity,
                Type = type,
                Level = 0,
            };

            // add power and display data
            if (!await ServerDbContext.CreateAsync(dbPlayer))
            {
                return false;
            }

            secret.AddBook(new InnerStrengthPower(dbPlayer));
            await user.SendAsync(new MsgInnerStrengthOpt
            {
                Action = MsgInnerStrengthOpt.InnerStrengthOptType.UnLock,
                Param = type
            });
            await SendAsync();
            await SendInfoAsync(type);
            return true;
        }

        public async Task<bool> UpLevelAsync(byte type, byte mode)
        {
            if (!HasLearnedStrengthType(type))
            {
                logger.LogWarning("User [{},{}] attempt to uplev non existent [{}] inner power", user.Identity, user.Name, type);
                return false;
            }

            var power = FindCheat(type);
            if (power == null)
            {
                logger.LogWarning("User [{},{}] attempt to uplev non existent [{}] cheat", user.Identity, user.Name, type);
                return false;
            }

            var typeInfo = InnerStrengthManager.QueryTypeInfo(type);
            if (typeInfo == null)
            {
                logger.LogWarning("User [{},{}] tried to uplev invalid inner power type [{}]", user.Identity, user.Name, type);
                return false;
            }

            if (!secrets.ContainsKey(typeInfo.SecretType))
            {
                return false;
            }

            var powerType = InnerStrengthManager.QueryTypeLevInfo(type, power.Level);
            if (powerType == null)
            {
                return false;
            }

            int maxLevel = InnerStrengthManager.GetStrenghtMaxLevel(type);
            int levelLimit = power.Level + 1;
            if (mode == 1)
            {
                levelLimit = maxLevel;
            }

            while (power.Level < levelLimit && await user.SpendCultureAsync((int)powerType.CultureValue))
            {
                power.Level += 1;
                power.Value = (byte)await InnerStrengthManager.CalculateCurrentValueAsync(type, power.Level, power.AbolishNum);
                power.FinishValue = (byte)InnerStrengthManager.CalculateMaxValue(type, power.Value, power.Level, power.AbolishNum);
            }

            UpdatePowerAttributes(power);
            RefreshPowers();
            await SendAsync();
            await SendInfoAsync(typeInfo.SecretType);
            await power.SaveAsync();
            return true;
        }

        public async Task<bool> PerfectAsync(byte type)
        {
            var power = FindCheat(type);
            if (power == null)
            {
                return false;
            }

            if (power.IsPerfect)
            {
                return false;
            }

            InnerStrengthManager.InnerStrengthTypeInfo typeInfo = InnerStrengthManager.QueryTypeInfo(type);
            if (typeInfo == null)
            {
                logger.LogWarning("User [{},{}] tried to uplev invalid inner power type [{}]", user.Identity, user.Name, type);
                return false;
            }

            if (!secrets.ContainsKey(typeInfo.SecretType))
            {
                return false;
            }

            if (power.Level < InnerStrengthManager.GetStrenghtMaxLevel(type))
            {
                return false;
            }

            power.IsPerfect = true;

            UpdatePowerAttributes(power);
            RefreshPowers();
            await power.SaveAsync();
            await SendAsync();
            await SendInfoAsync(typeInfo.SecretType);
            return true;
        }

        public async Task<bool> ReshapeAsync(byte type)
        {
            var power = FindCheat(type);
            if (power == null)
            {
                return false;
            }

            if (!power.IsPerfect)
            {
                return false;
            }

            var typeInfo = InnerStrengthManager.QueryTypeInfo(type);
            if (typeInfo == null)
            {
                logger.LogWarning("Reshape: User [{},{}] tried to uplev invalid inner power type [{}]", user.Identity, user.Name, type);
                return false;
            }

            if (typeInfo.AbolishCount == 0 || typeInfo.AbolishCount <= power.AbolishNum)
            {
                return false;
            }

            power.AbolishNum += 1;
            power.Level = 0;
            power.Value = 0;
            power.FinishValue = 0;
            power.IsPerfect = false;

            UpdatePowerAttributes(power);
            RefreshPowers();
            await SendAsync();
            await SendInfoAsync(typeInfo.SecretType);
            await power.SaveAsync();

            // after all if nothing fails, we give the abolish culture
            await user.AwardCultureAsync(typeInfo.AbolishCulture);
            return true;
        }

        private void UpdatePowerAttributes(InnerStrengthPower power)
        {
            if (power.IsPerfect && power.FinishValue == 100)
            {
                var typeInfo = InnerStrengthManager.QueryTypeInfo((byte)power.Identity);
                if (typeInfo == null)
                {
                    // wont happen
                    return;
                }

                power.PhysicAttackNew = typeInfo.PhysicAttackNew;
                power.MagicAttack = typeInfo.MagicAttack;
                power.PhysicDefenseNew = typeInfo.PhysicDefenseNew;
                power.MagicDefense = typeInfo.MagicDefense;
                power.FinalPhysicAdd = typeInfo.FinalPhysicAdd;
                power.FinalMagicAdd = typeInfo.FinalMagicAdd;
                power.FinalPhysicReduce = typeInfo.FinalPhysicReduce;
                power.FinalMagicReduce = typeInfo.FinalMagicReduce;
                power.PhysicCrit = typeInfo.PhysicCrit;
                power.MagicCrit = typeInfo.MagicCrit;
                power.DefenseCrit = typeInfo.DefenseCrit;
                power.SmashRate = typeInfo.SmashRate;
                power.FirmDefenseRate = typeInfo.FirmDefenseRate;
            }
            else
            {
                List<DbInnerStrenghtTypeLevInfo> powerLevInfo = InnerStrengthManager.QueryTypeLevelInfosForAttributes((byte)power.Identity, power.MaxLevel);
                power.PhysicAttackNew = 0;
                power.MagicAttack = 0;
                power.PhysicDefenseNew = 0;
                power.MagicDefense = 0;
                power.FinalPhysicAdd = 0;
                power.FinalPhysicReduce = 0;
                power.FinalMagicAdd = 0;
                power.FinalMagicReduce = 0;
                power.PhysicCrit = 0;
                power.MagicCrit = 0;
                power.DefenseCrit = 0;
                power.SmashRate = 0;
                power.FirmDefenseRate = 0;

                if (power.Level == 0)
                {
                    return;
                }

                float percent = power.Value / 100f;
                foreach (var info in powerLevInfo)
                {
                    power.PhysicAttackNew += info.PAttackNew;
                    power.MagicAttack += info.MagicAttack;
                    power.PhysicDefenseNew += info.PDefenseNew;
                    power.MagicDefense += info.MagicDefense;
                    power.FinalPhysicAdd += info.FinalDmgAdd;
                    power.FinalPhysicReduce += info.FinalDmgReduce;
                    power.FinalMagicAdd += info.FinalMgcDmgAdd;
                    power.FinalMagicReduce += info.FinalMgcDmgReduce;
                    power.PhysicCrit += info.CriticalAdd;
                    power.MagicCrit += info.MgcCriticalAdd;
                    power.DefenseCrit += info.AntiCriticalAdd;
                    power.SmashRate += info.SmashAdd;
                    power.FirmDefenseRate += info.FirmDefenseAdd;
                }

                if (power.PhysicAttackNew > 0)
                    power.PhysicAttackNew = (int)Math.Max(1, power.PhysicAttackNew * percent);
                if (power.MagicAttack > 0)
                    power.MagicAttack = (int)Math.Max(1, power.MagicAttack * percent);
                if (power.PhysicDefenseNew > 0)
                    power.PhysicDefenseNew = (int)Math.Max(1, power.PhysicDefenseNew * percent);
                if (power.MagicDefense > 0)
                    power.MagicDefense = (int)Math.Max(1, power.MagicDefense * percent);
                if (power.FinalPhysicAdd > 0)
                    power.FinalPhysicAdd = (int)Math.Max(1, power.FinalPhysicAdd * percent);
                if (power.FinalPhysicReduce > 0)
                    power.FinalPhysicReduce = (int)Math.Max(1, power.FinalPhysicReduce * percent);
                if (power.FinalMagicAdd > 0)
                    power.FinalMagicAdd = (int)Math.Max(1, power.FinalMagicAdd * percent);
                if (power.FinalMagicReduce > 0)
                    power.FinalMagicReduce = (int)Math.Max(1, power.FinalMagicReduce * percent);
                if (power.PhysicCrit > 0)
                    power.PhysicCrit = (int)Math.Max(1, power.PhysicCrit * percent);
                if (power.MagicCrit > 0)
                    power.MagicCrit = (int)Math.Max(1, power.MagicCrit * percent);
                if (power.DefenseCrit > 0)
                    power.DefenseCrit = (int)Math.Max(1, power.DefenseCrit * percent);
                if (power.SmashRate > 0)
                    power.SmashRate = (int)Math.Max(1, power.SmashRate * percent);
                if (power.FirmDefenseRate > 0)
                    power.FirmDefenseRate = (int)Math.Max(1, power.FirmDefenseRate * percent);
            }
        }

        private List<InnerStrengthPower> GetInnerStrengthPowerList()
        {
            List<InnerStrengthPower> result = new List<InnerStrengthPower>();
            foreach (var secret in secrets.Values)
            {
                result.AddRange(secret.GetPowers());
            }
            return result;
        }

        public Task SendAsync(Character target = null)
        {
            target ??= user;
            MsgInnerStrengthTotalInfo msg = new MsgInnerStrengthTotalInfo();
            foreach (var power in GetInnerStrengthPowerList())
            {
                msg.InnerStrengths.Add(new MsgInnerStrengthTotalInfo.InnerStrengthInfo
                {
                    Id = (byte)power.Identity,
                    Data = power.Value
                });
            }
            return target.SendAsync(msg);
        }

        public async Task SendInfoAsync(byte secretType, Character target = null)
        {
            target ??= user;

            var secrets = GetInnerStrengthPowerList().Where(x => x.SecretType == secretType);
            MsgInnerStrengthInfo msg = new MsgInnerStrengthInfo
            {
                Identity = user.Identity,
                Action = MsgInnerStrengthInfo.InnerStrenghtInfoType.SendStage,
                Score = TotalValue
            };
            foreach (var power in secrets)
            {
                msg.GongData.Add(new MsgInnerStrengthInfo.InnerStrengthGongData
                {
                    Type = (ushort)power.Identity,
                    Level = power.Level,
                    Value = power.Value,
                    Finished = power.IsPerfect
                });

                foreach (var attr in GetInnerPowers(power))
                {
                    msg.AttrData.Add(new MsgInnerStrengthInfo.InnerStrengthAttrData
                    {
                        Type = (ushort)power.Identity,
                        AttributeType = (byte)attr.Key,
                        Power = attr.Value,
                    });
                }
            }
            await target.SendAsync(msg);

            msg.Action = MsgInnerStrengthInfo.InnerStrenghtInfoType.SendScore;
            msg.Score = TotalValue;
            await target.SendAsync(msg);
        }

        public async Task SendFullAsync()
        {
            await SendAsync();
            foreach (var secret in secrets.Values)
            {
                await SendInfoAsync(secret.SecretType);
            }
        }

        public enum InnerStrengthAttrType
        {
            None,
            MaxLife,
            Attack,
            MagicAttack,
            Defense,
            MagicDefense,
            FinalPhysicalDamage,
            FinalMagicalDamage,
            FinalPhysicalDefense,
            FinalMagicalDefense,
            CriticalStrike,
            SkillCriticalStrike,
            Immunity,
            Breakthrough,
            Counteraction
        }
    }
}