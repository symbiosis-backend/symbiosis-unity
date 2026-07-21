using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    public static class CurrencyIds
    {
        public const string OzAltin = "OzAltin";
        public const string OzTile = "OzTile";
        public const string OzAmetist = "OzAmetist";
    }

    [Serializable]
    public sealed class CurrencyWalletEntry
    {
        public string CurrencyId;
        public int Amount;

        public CurrencyWalletEntry()
        {
            CurrencyId = string.Empty;
            Amount = 0;
        }

        public CurrencyWalletEntry(string currencyId, int amount)
        {
            CurrencyId = NormalizeCurrencyId(currencyId);
            Amount = Mathf.Max(0, amount);
        }

        public static string NormalizeCurrencyId(string currencyId)
        {
            return string.IsNullOrWhiteSpace(currencyId) ? string.Empty : currencyId.Trim();
        }
    }

    [Serializable]
    public sealed class ExchangeCurrencyDefinition
    {
        public string CurrencyId;
        public string DisplayName;
        public string OwnerGameId;
        public string IconResourcePath;
        public bool Tradable;

        public ExchangeCurrencyDefinition()
        {
            CurrencyId = string.Empty;
            DisplayName = string.Empty;
            OwnerGameId = string.Empty;
            IconResourcePath = string.Empty;
            Tradable = true;
        }
    }

    [Serializable]
    public sealed class ExchangePairConfig
    {
        public string PairId;
        public string BaseCurrencyId;
        public string GameCurrencyId;
        public string SourceGameId;
        public string TreasuryId;
        public bool Enabled;
        public float CurrentRate;
        public float BaseRate;
        public float MinRate;
        public float MaxRate;
        public float InputFeePercent;
        public float OutputFeePercent;
        public float SpreadPercent;
        public int DailyInputLimit;
        public int DailyOutputLimit;
        public float MaxHourlyChangePercent;

        public ExchangePairConfig()
        {
            PairId = string.Empty;
            BaseCurrencyId = CurrencyIds.OzAltin;
            GameCurrencyId = CurrencyIds.OzTile;
            SourceGameId = "Mahjong";
            TreasuryId = "MahjongTreasury";
            Enabled = true;
            CurrentRate = 10f;
            BaseRate = 10f;
            MinRate = 8f;
            MaxRate = 14f;
            InputFeePercent = 0.10f;
            OutputFeePercent = 0.12f;
            SpreadPercent = 0.05f;
            DailyInputLimit = 20000;
            DailyOutputLimit = 1500;
            MaxHourlyChangePercent = 0.02f;
        }
    }

    [Serializable]
    public sealed class ExchangeMarketConfig
    {
        public List<ExchangeCurrencyDefinition> Currencies;
        public List<ExchangePairConfig> Pairs;

        public ExchangeMarketConfig()
        {
            Currencies = new List<ExchangeCurrencyDefinition>();
            Pairs = new List<ExchangePairConfig>();
        }

        public void EnsureDefaults()
        {
            if (Currencies == null)
                Currencies = new List<ExchangeCurrencyDefinition>();

            if (Pairs == null)
                Pairs = new List<ExchangePairConfig>();

            EnsureCurrency(CurrencyIds.OzAltin, "Oz Altin", "Platform", string.Empty, true);
            EnsureCurrency(CurrencyIds.OzTile, "OzTile", "Mahjong", "Mahjong/Sprites/BattleTiles/OzTile", true);
            EnsureCurrency(CurrencyIds.OzAmetist, "Oz Ametist", "Platform", string.Empty, false);

            if (FindPair(CurrencyIds.OzAltin, CurrencyIds.OzTile) == null)
                Pairs.Add(new ExchangePairConfig());
        }

        public ExchangePairConfig FindPair(string fromCurrencyId, string toCurrencyId)
        {
            string from = CurrencyWalletEntry.NormalizeCurrencyId(fromCurrencyId);
            string to = CurrencyWalletEntry.NormalizeCurrencyId(toCurrencyId);
            for (int i = 0; i < Pairs.Count; i++)
            {
                ExchangePairConfig pair = Pairs[i];
                if (pair == null)
                    continue;

                string baseId = CurrencyWalletEntry.NormalizeCurrencyId(pair.BaseCurrencyId);
                string gameId = CurrencyWalletEntry.NormalizeCurrencyId(pair.GameCurrencyId);
                if ((baseId == from && gameId == to) || (baseId == to && gameId == from))
                    return pair;
            }

            return null;
        }

        private void EnsureCurrency(string currencyId, string displayName, string ownerGameId, string iconResourcePath, bool tradable)
        {
            for (int i = 0; i < Currencies.Count; i++)
            {
                if (Currencies[i] != null && Currencies[i].CurrencyId == currencyId)
                    return;
            }

            Currencies.Add(new ExchangeCurrencyDefinition
            {
                CurrencyId = currencyId,
                DisplayName = displayName,
                OwnerGameId = ownerGameId,
                IconResourcePath = iconResourcePath,
                Tradable = tradable
            });
        }
    }

    [Serializable]
    public sealed class ExchangeTreasuryState
    {
        public string TreasuryId;
        public int ReserveOzAltin;

        public ExchangeTreasuryState()
        {
            TreasuryId = string.Empty;
            ReserveOzAltin = 0;
        }
    }

    [Serializable]
    public sealed class ExchangeDailyCounter
    {
        public string PairId;
        public string UtcDate;
        public int InputAmount;
        public int OutputAmount;

        public ExchangeDailyCounter()
        {
            PairId = string.Empty;
            UtcDate = string.Empty;
            InputAmount = 0;
            OutputAmount = 0;
        }
    }

    [Serializable]
    public sealed class ExchangePairRuntimeState
    {
        public string PairId;
        public float CurrentRate;
        public long LastRateUpdateUtcTicks;
        public int CurrentPeriodInputVolume;
        public int CurrentPeriodOutputVolume;

        public ExchangePairRuntimeState()
        {
            PairId = string.Empty;
            CurrentRate = 1f;
            LastRateUpdateUtcTicks = DateTime.UtcNow.Ticks;
            CurrentPeriodInputVolume = 0;
            CurrentPeriodOutputVolume = 0;
        }
    }

    [Serializable]
    public sealed class ExchangeLedgerEntry
    {
        public string PlayerId;
        public string SourceGameId;
        public string FromCurrencyId;
        public string ToCurrencyId;
        public int AmountIn;
        public int AmountOut;
        public int FeeAmount;
        public float Rate;
        public string TimestampUtc;
        public bool Success;
        public string Reason;

        public ExchangeLedgerEntry()
        {
            PlayerId = string.Empty;
            SourceGameId = string.Empty;
            FromCurrencyId = string.Empty;
            ToCurrencyId = string.Empty;
            TimestampUtc = string.Empty;
            Reason = string.Empty;
        }
    }

    [Serializable]
    public sealed class ExchangeMarketData
    {
        private const int MaxLedgerEntries = 120;

        public List<ExchangeTreasuryState> Treasuries;
        public List<ExchangeDailyCounter> DailyCounters;
        public List<ExchangePairRuntimeState> PairStates;
        public List<ExchangeLedgerEntry> Ledger;

        public ExchangeMarketData()
        {
            Treasuries = new List<ExchangeTreasuryState>();
            DailyCounters = new List<ExchangeDailyCounter>();
            PairStates = new List<ExchangePairRuntimeState>();
            Ledger = new List<ExchangeLedgerEntry>();
        }

        public void EnsureData(ExchangeMarketConfig config)
        {
            if (Treasuries == null)
                Treasuries = new List<ExchangeTreasuryState>();
            if (DailyCounters == null)
                DailyCounters = new List<ExchangeDailyCounter>();
            if (PairStates == null)
                PairStates = new List<ExchangePairRuntimeState>();
            if (Ledger == null)
                Ledger = new List<ExchangeLedgerEntry>();

            config?.EnsureDefaults();
            if (config?.Pairs == null)
                return;

            for (int i = 0; i < config.Pairs.Count; i++)
            {
                ExchangePairConfig pair = config.Pairs[i];
                if (pair == null)
                    continue;

                EnsurePairState(pair);
                EnsureTreasury(pair.TreasuryId);
            }

            TrimLedger();
        }

        public ExchangePairRuntimeState EnsurePairState(ExchangePairConfig pair)
        {
            string pairId = ResolvePairId(pair);
            for (int i = 0; i < PairStates.Count; i++)
            {
                ExchangePairRuntimeState state = PairStates[i];
                if (state != null && state.PairId == pairId)
                {
                    float minRate = Mathf.Max(0.01f, pair.MinRate);
                    float maxRate = Mathf.Max(minRate, pair.MaxRate);
                    if (state.CurrentRate <= 0f || state.CurrentRate < minRate || state.CurrentRate > maxRate)
                        state.CurrentRate = Mathf.Max(0.01f, pair.CurrentRate > 0f ? pair.CurrentRate : pair.BaseRate);
                    if (state.LastRateUpdateUtcTicks <= 0)
                        state.LastRateUpdateUtcTicks = DateTime.UtcNow.Ticks;
                    return state;
                }
            }

            ExchangePairRuntimeState created = new ExchangePairRuntimeState
            {
                PairId = pairId,
                CurrentRate = Mathf.Max(0.01f, pair.CurrentRate > 0f ? pair.CurrentRate : pair.BaseRate),
                LastRateUpdateUtcTicks = DateTime.UtcNow.Ticks
            };
            PairStates.Add(created);
            return created;
        }

        public ExchangeTreasuryState EnsureTreasury(string treasuryId)
        {
            string id = string.IsNullOrWhiteSpace(treasuryId) ? "DefaultTreasury" : treasuryId.Trim();
            for (int i = 0; i < Treasuries.Count; i++)
            {
                ExchangeTreasuryState treasury = Treasuries[i];
                if (treasury != null && treasury.TreasuryId == id)
                {
                    treasury.ReserveOzAltin = Mathf.Max(0, treasury.ReserveOzAltin);
                    return treasury;
                }
            }

            ExchangeTreasuryState created = new ExchangeTreasuryState { TreasuryId = id };
            Treasuries.Add(created);
            return created;
        }

        public ExchangeDailyCounter GetDailyCounter(ExchangePairConfig pair)
        {
            string pairId = ResolvePairId(pair);
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            for (int i = 0; i < DailyCounters.Count; i++)
            {
                ExchangeDailyCounter counter = DailyCounters[i];
                if (counter != null && counter.PairId == pairId && counter.UtcDate == today)
                    return counter;
            }

            ExchangeDailyCounter created = new ExchangeDailyCounter
            {
                PairId = pairId,
                UtcDate = today
            };
            DailyCounters.Add(created);
            return created;
        }

        public void AddLedgerEntry(ExchangeLedgerEntry entry)
        {
            if (entry == null)
                return;

            Ledger.Add(entry);
            TrimLedger();
        }

        public static string ResolvePairId(ExchangePairConfig pair)
        {
            if (pair == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(pair.PairId))
                return pair.PairId.Trim();

            return $"{CurrencyWalletEntry.NormalizeCurrencyId(pair.BaseCurrencyId)}_{CurrencyWalletEntry.NormalizeCurrencyId(pair.GameCurrencyId)}";
        }

        private void TrimLedger()
        {
            if (Ledger == null)
                return;

            while (Ledger.Count > MaxLedgerEntries)
                Ledger.RemoveAt(0);
        }
    }

    public sealed class ExchangeQuote
    {
        public bool Success;
        public string FailReason;
        public string FromCurrencyId;
        public string ToCurrencyId;
        public int AmountIn;
        public int AmountOut;
        public int FeeAmount;
        public float Rate;
        public float FeePercent;
        public int DailyLimit;
        public int DailyUsed;
        public bool IsOutput;
        public string SourceGameId;
    }

    public static class ExchangeMarketService
    {
        private static readonly ExchangeMarketConfig defaultConfig = CreateDefaultConfig();

        public static ExchangeMarketConfig Config => defaultConfig;

        public static ExchangeQuote GetExchangeQuote(PlayerProfile profile, string fromCurrencyId, string toCurrencyId, int amount)
        {
            ExchangeQuote quote = BuildQuote(profile, fromCurrencyId, toCurrencyId, amount, validateBalance: true);
            return quote;
        }

        public static bool CanExchange(PlayerProfile profile, string fromCurrencyId, string toCurrencyId, int amount)
        {
            return GetExchangeQuote(profile, fromCurrencyId, toCurrencyId, amount).Success;
        }

        public static bool ExchangeCurrency(PlayerProfile profile, string fromCurrencyId, string toCurrencyId, int amount, out ExchangeQuote quote)
        {
            quote = BuildQuote(profile, fromCurrencyId, toCurrencyId, amount, validateBalance: true);
            if (profile == null)
                return false;

            profile.EnsureData();
            ExchangeMarketData data = profile.ExchangeMarket;
            ExchangePairConfig pair = defaultConfig.FindPair(fromCurrencyId, toCurrencyId);

            if (!quote.Success)
            {
                data.AddLedgerEntry(CreateLedgerEntry(profile, pair, quote, false, quote.FailReason));
                return false;
            }

            if (!profile.Currencies.TryChangeCurrency(quote.FromCurrencyId, -quote.AmountIn))
            {
                quote.Success = false;
                quote.FailReason = "Not enough balance.";
                data.AddLedgerEntry(CreateLedgerEntry(profile, pair, quote, false, quote.FailReason));
                return false;
            }

            profile.Currencies.TryChangeCurrency(quote.ToCurrencyId, quote.AmountOut);

            ExchangeTreasuryState treasury = data.EnsureTreasury(pair.TreasuryId);
            if (quote.IsOutput)
                treasury.ReserveOzAltin = Mathf.Max(0, treasury.ReserveOzAltin - quote.AmountOut);
            else
                treasury.ReserveOzAltin = Mathf.Max(0, treasury.ReserveOzAltin + quote.AmountIn);

            ExchangeDailyCounter counter = data.GetDailyCounter(pair);
            if (quote.IsOutput)
                counter.OutputAmount += quote.AmountIn;
            else
                counter.InputAmount += quote.AmountIn;

            ExchangePairRuntimeState state = data.EnsurePairState(pair);
            if (quote.IsOutput)
                state.CurrentPeriodOutputVolume += Mathf.RoundToInt(quote.AmountIn * quote.Rate);
            else
                state.CurrentPeriodInputVolume += quote.AmountIn;

            data.AddLedgerEntry(CreateLedgerEntry(profile, pair, quote, true, string.Empty));
            return true;
        }

        private static ExchangeQuote BuildQuote(PlayerProfile profile, string fromCurrencyId, string toCurrencyId, int amount, bool validateBalance)
        {
            defaultConfig.EnsureDefaults();
            string from = CurrencyWalletEntry.NormalizeCurrencyId(fromCurrencyId);
            string to = CurrencyWalletEntry.NormalizeCurrencyId(toCurrencyId);

            ExchangeQuote quote = new ExchangeQuote
            {
                FromCurrencyId = from,
                ToCurrencyId = to,
                AmountIn = Mathf.Max(0, amount),
                FailReason = string.Empty
            };

            if (profile == null)
                return Fail(quote, "Profile is not loaded.");

            profile.EnsureData();

            ExchangePairConfig pair = defaultConfig.FindPair(from, to);
            if (pair == null || !pair.Enabled)
                return Fail(quote, "Exchange pair is disabled.");

            if (from == CurrencyIds.OzAmetist || to == CurrencyIds.OzAmetist)
                return Fail(quote, "OzAmetist is not tradable on the market.");

            if (amount <= 0)
                return Fail(quote, "Enter an amount.");

            ExchangeMarketData data = profile.ExchangeMarket;
            data.EnsureData(defaultConfig);
            UpdatePairRate(data, pair);

            ExchangePairRuntimeState state = data.EnsurePairState(pair);
            ExchangeDailyCounter counter = data.GetDailyCounter(pair);
            ExchangeTreasuryState treasury = data.EnsureTreasury(pair.TreasuryId);

            bool output = from == CurrencyWalletEntry.NormalizeCurrencyId(pair.GameCurrencyId)
                && to == CurrencyWalletEntry.NormalizeCurrencyId(pair.BaseCurrencyId);
            bool input = from == CurrencyWalletEntry.NormalizeCurrencyId(pair.BaseCurrencyId)
                && to == CurrencyWalletEntry.NormalizeCurrencyId(pair.GameCurrencyId);
            if (!input && !output)
                return Fail(quote, "Unsupported exchange direction.");

            float rate = Mathf.Clamp(state.CurrentRate, Mathf.Max(0.01f, pair.MinRate), Mathf.Max(pair.MinRate, pair.MaxRate));
            float spread = Mathf.Clamp01(pair.SpreadPercent);
            float feePercent = Mathf.Clamp01(output ? pair.OutputFeePercent : pair.InputFeePercent);
            float effectiveRate = output ? rate * Mathf.Max(0.01f, 1f - spread) : rate * (1f + spread);
            int gross = output
                ? Mathf.FloorToInt(amount * effectiveRate)
                : Mathf.FloorToInt(amount / Mathf.Max(0.01f, effectiveRate));
            int fee = Mathf.RoundToInt(gross * feePercent);
            int receive = Mathf.Max(0, gross - fee);

            quote.Rate = effectiveRate;
            quote.FeePercent = feePercent;
            quote.FeeAmount = fee;
            quote.AmountOut = receive;
            quote.DailyLimit = output ? pair.DailyOutputLimit : pair.DailyInputLimit;
            quote.DailyUsed = output ? counter.OutputAmount : counter.InputAmount;
            quote.IsOutput = output;
            quote.SourceGameId = pair.SourceGameId;

            if (receive <= 0)
                return Fail(quote, "Amount is too small after fee.");

            if (quote.DailyLimit > 0 && quote.DailyUsed + amount > quote.DailyLimit)
                return Fail(quote, "Daily exchange limit reached.");

            if (output && receive > treasury.ReserveOzAltin)
                return Fail(quote, "Treasury reserve is too low.");

            if (validateBalance && !profile.Currencies.CanSpendCurrency(from, amount))
                return Fail(quote, "Not enough balance.");

            quote.Success = true;
            return quote;
        }

        private static ExchangeQuote Fail(ExchangeQuote quote, string reason)
        {
            quote.Success = false;
            quote.FailReason = reason ?? string.Empty;
            return quote;
        }

        private static void UpdatePairRate(ExchangeMarketData data, ExchangePairConfig pair)
        {
            ExchangePairRuntimeState state = data.EnsurePairState(pair);
            DateTime now = DateTime.UtcNow;
            DateTime last = new DateTime(Math.Max(DateTime.MinValue.Ticks, state.LastRateUpdateUtcTicks), DateTimeKind.Utc);
            if ((now - last).TotalHours < 1d)
                return;

            int total = Mathf.Max(0, state.CurrentPeriodInputVolume) + Mathf.Max(0, state.CurrentPeriodOutputVolume);
            if (total > 0)
            {
                float pressure = (state.CurrentPeriodInputVolume - state.CurrentPeriodOutputVolume) / (float)total;
                float maxChange = Mathf.Clamp(pair.MaxHourlyChangePercent, 0f, 0.5f);
                float target = state.CurrentRate * (1f + pressure * maxChange);
                state.CurrentRate = Mathf.Clamp(target, Mathf.Max(0.01f, pair.MinRate), Mathf.Max(pair.MinRate, pair.MaxRate));
            }

            state.CurrentPeriodInputVolume = 0;
            state.CurrentPeriodOutputVolume = 0;
            state.LastRateUpdateUtcTicks = now.Ticks;
        }

        private static ExchangeLedgerEntry CreateLedgerEntry(PlayerProfile profile, ExchangePairConfig pair, ExchangeQuote quote, bool success, string reason)
        {
            return new ExchangeLedgerEntry
            {
                PlayerId = profile != null ? profile.PublicPlayerId : string.Empty,
                SourceGameId = quote != null && !string.IsNullOrWhiteSpace(quote.SourceGameId)
                    ? quote.SourceGameId
                    : pair != null ? pair.SourceGameId : string.Empty,
                FromCurrencyId = quote != null ? quote.FromCurrencyId : string.Empty,
                ToCurrencyId = quote != null ? quote.ToCurrencyId : string.Empty,
                AmountIn = quote != null ? quote.AmountIn : 0,
                AmountOut = quote != null ? quote.AmountOut : 0,
                FeeAmount = quote != null ? quote.FeeAmount : 0,
                Rate = quote != null ? quote.Rate : 0f,
                TimestampUtc = DateTime.UtcNow.ToString("O"),
                Success = success,
                Reason = reason ?? string.Empty
            };
        }

        private static ExchangeMarketConfig CreateDefaultConfig()
        {
            ExchangeMarketConfig config = new ExchangeMarketConfig();
            config.EnsureDefaults();
            return config;
        }
    }
}
