using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace MahjongGame
{
    // API: Commands | State | Events
    [DisallowMultipleComponent]
    public sealed class BattleCombatSystem : MonoBehaviour
    {
        public event Action<BattleCombatSystem> CombatStarted;
        public event Action<BattleCombatSystem> CombatReset;
        public event Action<BattleCombatSystem, int, int> PlayerHpChanged;
        public event Action<BattleCombatSystem, int, int> OpponentHpChanged;
        public event Action<BattleCombatSystem, BattleBoardSide, int, int> DamageApplied;
        public event Action<BattleCombatSystem, BattleBoardSide, BattleDamageCalculator.DamageResult, int> DamageResultApplied;
        public event Action<BattleCombatSystem, BattleBoardSide> CombatFinished;
        public event Action<BattleCombatSystem> StateChanged;

        [Header("Links")]
        [SerializeField] private BattleMatchController matchController;
        [SerializeField] private BattleBoard playerBoard;
        [SerializeField] private BattleBoard opponentBoard;
        [SerializeField] private BattleStatsHub statsHub;

        [Header("HP")]
        [SerializeField, Min(1)] private int maxPlayerHp = 480;
        [SerializeField, Min(1)] private int maxOpponentHp = 480;
        [SerializeField, Min(1)] private int damagePerPair = 36;
        [SerializeField, Range(0f, 0.5f)] private float minimumDamagePerPairHpFraction = 0.05f;

        [Header("Character Stats")]
        [SerializeField] private bool useSelectedCharacterStats = true;
        [SerializeField] private bool createStatsHubIfMissing = true;
        [SerializeField] private bool useCharacterDamageStats = true;

        [Header("Bot Battle Stats")]
        [SerializeField] private bool scaleOpponentStatsFromRank = true;
        [SerializeField, Range(0f, 1f)] private float opponentArmor = 0.03f;
        [SerializeField, Range(0f, 1f)] private float opponentCritChance = 0.08f;
        [SerializeField, Min(1f)] private float opponentCritDamageMultiplier = 1.5f;
        [SerializeField] private int opponentRankHpStep = 100;
        [SerializeField] private int opponentHpPerStep = 12;
        [SerializeField] private int opponentRankAttackStep = 150;
        [SerializeField] private int opponentAttackPerStep = 1;
        [SerializeField] private bool clampOpponentStatsToPlayer = true;
        [SerializeField, Min(1f)] private float maxOpponentHpPlayerFactor = 1.2f;
        [SerializeField, Min(1f)] private float maxOpponentAttackPlayerFactor = 1.05f;
        [SerializeField] private bool mirrorBotStatsNearPlayer = true;
        [SerializeField, Range(0f, 0.35f)] private float botHpMirrorVariance = 0.08f;
        [SerializeField, Range(0f, 0.35f)] private float botAttackMirrorVariance = 0.08f;
        [SerializeField, Range(0f, 0.25f)] private float botChanceMirrorVariance = 0.04f;
        [SerializeField, Range(0f, 0.5f)] private float botCritPowerMirrorVariance = 0.18f;

        [Header("UI / Optional")]
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text opponentHpText;
        [SerializeField] private string hpPrefix = "HP: ";

        [Header("Debug")]
        [SerializeField] private bool finishMatchDirectlyOnDeath = false;
        [SerializeField] private bool debugLogs = true;

        private int playerHp;
        private int opponentHp;
        private bool combatStarted;
        private bool combatFinished;
        private bool networkPairDamageSuppressed;
        private BattleBoardSide networkPairDamageSuppressedSide = BattleBoardSide.Opponent;
        private bool networkPlayerPairDamageSuppressed;
        private bool networkOpponentPairDamageSuppressed;
        private BattleStatsHub.BattleStatsSnapshot playerStats;
        private BattleStatsHub.BattleStatsSnapshot opponentStats;

        public BattleMatchController MatchController => matchController;
        public BattleBoard PlayerBoard => playerBoard;
        public BattleBoard OpponentBoard => opponentBoard;

        public int MaxPlayerHp => maxPlayerHp;
        public int MaxOpponentHp => maxOpponentHp;
        public int PlayerHp => playerHp;
        public int OpponentHp => opponentHp;
        public int DamagePerPair => damagePerPair;
        public int NetworkDamagePerPair => Mathf.Max(1, combatStarted && useCharacterDamageStats ? playerStats.Attack : damagePerPair);

        public bool IsCombatStarted => combatStarted;
        public bool IsCombatFinished => combatFinished;

        private void Awake()
        {
            AutoResolveLinks();
        }

        private void OnEnable()
        {
            AutoResolveLinks();
            BindBoards();
            RefreshUi();
            NotifyStateChanged();
        }

        private void OnDisable()
        {
            UnbindBoards();
        }

        public void SetBoards(BattleBoard player, BattleBoard opponent)
        {
            UnbindBoards();

            playerBoard = player;
            opponentBoard = opponent;

            BindBoards();
            RefreshUi();
            NotifyStateChanged();
        }

        public void SetMatchController(BattleMatchController controller)
        {
            matchController = controller;
            NotifyStateChanged();
        }

        public void SetHpTexts(TMP_Text playerText, TMP_Text opponentText)
        {
            playerHpText = playerText;
            opponentHpText = opponentText;
            RefreshUi();
            NotifyStateChanged();
        }

        public void SetMaxHp(int playerMax, int opponentMax)
        {
            maxPlayerHp = Mathf.Max(1, playerMax);
            maxOpponentHp = Mathf.Max(1, opponentMax);

            if (combatStarted && !combatFinished)
            {
                playerHp = Mathf.Clamp(playerHp, 0, maxPlayerHp);
                opponentHp = Mathf.Clamp(opponentHp, 0, maxOpponentHp);
                RaiseHpEvents();
                RefreshUi();
            }

            NotifyStateChanged();
        }

        public void SetDamagePerPair(int value)
        {
            damagePerPair = Mathf.Max(1, value);
            NotifyStateChanged();
        }

        public void SetNetworkPairDamageSuppressed(bool value, BattleBoardSide side = BattleBoardSide.Opponent)
        {
            networkPairDamageSuppressed = value;
            networkPairDamageSuppressedSide = side;

            if (side == BattleBoardSide.Player)
                networkPlayerPairDamageSuppressed = value;
            else
                networkOpponentPairDamageSuppressed = value;
        }

        public void SetNetworkPairDamageSuppressedForBoth(bool value)
        {
            networkPairDamageSuppressed = value;
            networkPlayerPairDamageSuppressed = value;
            networkOpponentPairDamageSuppressed = value;
        }

        public void StartCombat()
        {
            ResolveCombatStats();

            combatStarted = true;
            combatFinished = false;

            playerHp = maxPlayerHp;
            opponentHp = maxOpponentHp;

            RaiseHpEvents();
            RefreshUi();

            CombatStarted?.Invoke(this);
            NotifyStateChanged();

            Log(
                $"Combat started | " +
                $"PlayerHP={playerHp} OpponentHP={opponentHp} | " +
                $"PlayerAttack={playerStats.Attack} OpponentAttack={opponentStats.Attack}");
        }

        public void ResetCombat()
        {
            combatStarted = false;
            combatFinished = false;

            playerHp = maxPlayerHp;
            opponentHp = maxOpponentHp;

            RaiseHpEvents();
            RefreshUi();

            CombatReset?.Invoke(this);
            NotifyStateChanged();

            Log("Combat reset");
        }

        public bool ApplyDamageToPlayer(int amount)
        {
            return ApplyDamageToPlayer(new BattleDamageCalculator.DamageResult(amount, false));
        }

        public bool ApplyDamageToPlayer(BattleDamageCalculator.DamageResult result)
        {
            if (!combatStarted || combatFinished)
                return false;

            int damage = Mathf.Max(0, result.FinalDamage);
            if (damage <= 0)
                return false;

            int before = playerHp;
            playerHp = Mathf.Max(0, playerHp - damage);
            int appliedDamage = before - playerHp;
            BattleDamageCalculator.DamageResult appliedResult = new BattleDamageCalculator.DamageResult(
                appliedDamage,
                result.AbsorbedDamage,
                result.IsCritical);

            PlayerHpChanged?.Invoke(this, playerHp, maxPlayerHp);
            DamageApplied?.Invoke(this, BattleBoardSide.Player, appliedDamage, playerHp);
            DamageResultApplied?.Invoke(this, BattleBoardSide.Player, appliedResult, playerHp);

            RefreshUi();
            NotifyStateChanged();

            Log($"Player damaged | {before} -> {playerHp} | Damage={damage}");

            if (playerHp <= 0)
                FinishCombat(BattleBoardSide.Player);

            return true;
        }

        public bool ApplyDamageToOpponent(int amount)
        {
            return ApplyDamageToOpponent(new BattleDamageCalculator.DamageResult(amount, false));
        }

        public bool ApplyDamageToOpponent(BattleDamageCalculator.DamageResult result)
        {
            if (!combatStarted || combatFinished)
                return false;

            int damage = Mathf.Max(0, result.FinalDamage);
            if (damage <= 0)
                return false;

            int before = opponentHp;
            opponentHp = Mathf.Max(0, opponentHp - damage);
            int appliedDamage = before - opponentHp;
            BattleDamageCalculator.DamageResult appliedResult = new BattleDamageCalculator.DamageResult(
                appliedDamage,
                result.AbsorbedDamage,
                result.IsCritical);

            OpponentHpChanged?.Invoke(this, opponentHp, maxOpponentHp);
            DamageApplied?.Invoke(this, BattleBoardSide.Opponent, appliedDamage, opponentHp);
            DamageResultApplied?.Invoke(this, BattleBoardSide.Opponent, appliedResult, opponentHp);

            RefreshUi();
            NotifyStateChanged();

            Log($"Opponent damaged | {before} -> {opponentHp} | Damage={damage}");

            if (opponentHp <= 0)
                FinishCombat(BattleBoardSide.Opponent);

            return true;
        }

        public bool HealPlayer(int amount)
        {
            if (!combatStarted || combatFinished)
                return false;

            int heal = Mathf.Max(0, amount);
            if (heal <= 0)
                return false;

            int before = playerHp;
            playerHp = Mathf.Min(maxPlayerHp, playerHp + heal);

            if (before == playerHp)
                return false;

            PlayerHpChanged?.Invoke(this, playerHp, maxPlayerHp);
            RefreshUi();
            NotifyStateChanged();

            Log($"Player healed | {before} -> {playerHp} | Heal={heal}");
            return true;
        }

        public bool HealOpponent(int amount)
        {
            if (!combatStarted || combatFinished)
                return false;

            int heal = Mathf.Max(0, amount);
            if (heal <= 0)
                return false;

            int before = opponentHp;
            opponentHp = Mathf.Min(maxOpponentHp, opponentHp + heal);

            if (before == opponentHp)
                return false;

            OpponentHpChanged?.Invoke(this, opponentHp, maxOpponentHp);
            RefreshUi();
            NotifyStateChanged();

            Log($"Opponent healed | {before} -> {opponentHp} | Heal={heal}");
            return true;
        }

        public string GetPlayerHpText()
        {
            return $"{hpPrefix}{playerHp}/{maxPlayerHp}";
        }

        public string GetOpponentHpText()
        {
            return $"{hpPrefix}{opponentHp}/{maxOpponentHp}";
        }

        private void HandlePlayerPairMatched(BattleBoard _, BattleTile first, BattleTile second)
        {
            if (networkPlayerPairDamageSuppressed)
                return;

            if (networkPairDamageSuppressed && networkPairDamageSuppressedSide == BattleBoardSide.Player)
                return;

            StartCoroutine(ResolvePairAttackRoutine(
                BattleBoardSide.Player,
                first,
                second,
                playerStats,
                opponentStats.Armor,
                maxOpponentHp));
        }

        private void HandleOpponentPairMatched(BattleBoard _, BattleTile first, BattleTile second)
        {
            if (networkOpponentPairDamageSuppressed)
                return;

            if (networkPairDamageSuppressed && networkPairDamageSuppressedSide == BattleBoardSide.Opponent)
                return;

            StartCoroutine(ResolvePairAttackRoutine(
                BattleBoardSide.Opponent,
                first,
                second,
                opponentStats,
                playerStats.Armor,
                maxPlayerHp));
        }

        private IEnumerator ResolvePairAttackRoutine(
            BattleBoardSide attackerSide,
            BattleTile firstTile,
            BattleTile secondTile,
            BattleStatsHub.BattleStatsSnapshot attacker,
            float targetArmor,
            int targetMaxHp)
        {
            if (!combatStarted || combatFinished)
                yield break;

            BattleBoardSide targetSide = attackerSide == BattleBoardSide.Player
                ? BattleBoardSide.Opponent
                : BattleBoardSide.Player;

            string attackerName = attackerSide == BattleBoardSide.Player ? "Player" : "Opponent";
            string targetName = targetSide == BattleBoardSide.Player ? "Player" : "Opponent";
            int activeSelfHeal = 0;
            BattleStatsHub.BattleStatsSnapshot attackStats = BattleTileInventoryService.ApplyMatchedTileActiveBonuses(
                attacker,
                BattleTileStore.I,
                firstTile,
                secondTile,
                out activeSelfHeal,
                MahjongSession.GetBattleLoadout(attackerSide));

            BattleDamageCalculator.DamageResult result = CalculateDamage(attackStats, targetArmor, targetMaxHp);

            LogDamageRoll(attackerName, targetName, result);

            if (matchController != null)
                yield return matchController.PlayMatchedPairAttackSequence(attackerSide, firstTile, secondTile);

            if (targetSide == BattleBoardSide.Opponent)
                ApplyDamageToOpponent(result);
            else
                ApplyDamageToPlayer(result);

            if (activeSelfHeal > 0)
            {
                if (attackerSide == BattleBoardSide.Player)
                    HealPlayer(activeSelfHeal);
                else
                    HealOpponent(activeSelfHeal);
            }
        }

        private void FinishCombat(BattleBoardSide deadSide)
        {
            if (combatFinished)
                return;

            combatFinished = true;

            CombatFinished?.Invoke(this, deadSide);
            NotifyStateChanged();

            Log($"Combat finished | DeadSide={deadSide}");

            if (finishMatchDirectlyOnDeath && matchController != null)
            {
                bool playerWon = deadSide == BattleBoardSide.Opponent;
                matchController.ForceFinishMatch(playerWon);
            }
        }

        private void BindBoards()
        {
            if (playerBoard != null)
            {
                playerBoard.PairMatched -= HandlePlayerPairMatched;
                playerBoard.PairMatched += HandlePlayerPairMatched;
            }

            if (opponentBoard != null)
            {
                opponentBoard.PairMatched -= HandleOpponentPairMatched;
                opponentBoard.PairMatched += HandleOpponentPairMatched;
            }
        }

        private void UnbindBoards()
        {
            if (playerBoard != null)
                playerBoard.PairMatched -= HandlePlayerPairMatched;

            if (opponentBoard != null)
                opponentBoard.PairMatched -= HandleOpponentPairMatched;
        }

        private void AutoResolveLinks()
        {
            if (matchController == null)
                matchController = GetComponent<BattleMatchController>();

            if (statsHub == null)
                statsHub = BattleStatsHub.HasInstance
                    ? BattleStatsHub.Instance
                    : FindAnyObjectByType<BattleStatsHub>(FindObjectsInactive.Include);

            if (playerBoard == null || opponentBoard == null)
            {
                BattleBoard[] boards = FindObjectsByType<BattleBoard>(FindObjectsInactive.Exclude);
                for (int i = 0; i < boards.Length; i++)
                {
                    BattleBoard board = boards[i];
                    if (board == null)
                        continue;

                    if (board.Side == BattleBoardSide.Player && playerBoard == null)
                        playerBoard = board;
                    else if (board.Side == BattleBoardSide.Opponent && opponentBoard == null)
                        opponentBoard = board;
                }
            }
        }

        private void ResolveCombatStats()
        {
            EnsureStatsHub();
            ApplySelectedCharacterStatsToHub();

            if (useSelectedCharacterStats && statsHub != null)
                playerStats = statsHub.GetSnapshot();
            else
                playerStats = CreateFallbackStats(maxPlayerHp, damagePerPair);

            string selectedCharacterId = BattleCharacterSelectionService.HasInstance
                ? BattleCharacterSelectionService.Instance.SelectedCharacterId
                : string.Empty;
            BattleCharacterDatabase.BattleCharacterData selectedCharacter = BattleCharacterSelectionService.HasInstance
                ? BattleCharacterSelectionService.Instance.GetSelectedCharacter()
                : null;
            MahjongBattleCharacterProgressData selectedProgress = selectedCharacter != null
                ? BattleCharacterProgressionService.GetOrCreateProgress(ProfileService.I?.Current, selectedCharacter.Id)
                : null;
            playerStats = BattleCharacterProgressionService.ApplyProgression(playerStats, selectedProgress);
            playerStats = BattleDailyHeroBonusService.ApplyTodayBonus(playerStats, selectedCharacterId, true);
            playerStats = BattleTileInventoryService.ApplyActiveTileBonuses(playerStats, ProfileService.I?.Current, BattleTileStore.I, selectedCharacter);
            playerStats = NormalizeStats(playerStats, maxPlayerHp, damagePerPair);
            maxPlayerHp = playerStats.MaxHp;

            opponentStats = ResolveOpponentStatsFromLoadout();
            maxOpponentHp = opponentStats.MaxHp;
        }

        public void RefreshOpponentLoadoutStats()
        {
            if (!combatStarted || combatFinished || MahjongSession.OpponentBattleLoadout == null)
                return;

            int previousMaxHp = Mathf.Max(1, maxOpponentHp);
            int previousHp = Mathf.Clamp(opponentHp, 0, previousMaxHp);
            opponentStats = ResolveOpponentStatsFromLoadout();
            maxOpponentHp = opponentStats.MaxHp;
            opponentHp = previousHp <= 0
                ? 0
                : Mathf.Clamp(Mathf.RoundToInt((float)previousHp / previousMaxHp * maxOpponentHp), 1, maxOpponentHp);
            OpponentHpChanged?.Invoke(this, opponentHp, maxOpponentHp);
            RefreshUi();
        }

        private BattleStatsHub.BattleStatsSnapshot ResolveOpponentStatsFromLoadout()
        {
            BattleStatsHub.BattleStatsSnapshot resolved = BuildOpponentStats(playerStats);
            if (matchController != null)
            {
                BattleCharacterDatabase.BattleCharacterData opponentCharacter =
                    BattleCharacterDatabase.HasInstance && !string.IsNullOrWhiteSpace(MahjongSession.BattleOpponentCharacterId)
                        ? BattleCharacterDatabase.Instance.GetCharacterOrNull(MahjongSession.BattleOpponentCharacterId)
                        : null;
                resolved = BattleTileInventoryService.ApplyTileDataBonuses(
                    resolved,
                    matchController.GetAdaptiveOpponentTilesForRound(matchController.CurrentRoundNumber),
                    matchController.GetAdaptiveOpponentTotemTile(),
                    opponentCharacter,
                    MahjongSession.OpponentBattleLoadout);
            }

            resolved = BattleDailyHeroBonusService.ApplyTodayBonus(resolved, MahjongSession.BattleOpponentCharacterId, false);
            resolved = NormalizeStats(resolved, maxOpponentHp, damagePerPair);
            return ClampOpponentStatsNearPlayer(resolved, playerStats);
        }

        private void EnsureStatsHub()
        {
            if (statsHub != null)
                return;

            if (BattleStatsHub.HasInstance)
            {
                statsHub = BattleStatsHub.Instance;
                return;
            }

            statsHub = FindAnyObjectByType<BattleStatsHub>(FindObjectsInactive.Include);

            if (statsHub != null || !createStatsHubIfMissing)
                return;

            GameObject hubObject = new GameObject("BattleStatsHub");
            statsHub = hubObject.AddComponent<BattleStatsHub>();
        }

        private void ApplySelectedCharacterStatsToHub()
        {
            if (!useSelectedCharacterStats || statsHub == null)
                return;

            if (BattleCharacterSelectionService.HasInstance &&
                BattleCharacterSelectionService.Instance.ApplySelectedCharacterStatsToHub())
            {
                Log(
                    $"Selected character stats applied | " +
                    $"Character='{BattleCharacterSelectionService.Instance.SelectedCharacterId}'");
            }
        }

        private BattleStatsHub.BattleStatsSnapshot BuildOpponentStats(
            BattleStatsHub.BattleStatsSnapshot playerSnapshot)
        {
            int rankPoints = Mathf.Max(0, MahjongSession.BattleOpponentRankPoints);
            int hp = maxOpponentHp;
            int attack = damagePerPair;

            if (scaleOpponentStatsFromRank)
            {
                hp += Mathf.Max(0, opponentRankHpStep) > 0
                    ? (rankPoints / Mathf.Max(1, opponentRankHpStep)) * Mathf.Max(0, opponentHpPerStep)
                    : 0;

                attack += Mathf.Max(0, opponentRankAttackStep) > 0
                    ? (rankPoints / Mathf.Max(1, opponentRankAttackStep)) * Mathf.Max(0, opponentAttackPerStep)
                    : 0;
            }

            if (useCharacterDamageStats)
            {
                attack = Mathf.Max(attack, Mathf.RoundToInt(playerSnapshot.Attack * ResolveOpponentAttackFactor()));
            }

            if (clampOpponentStatsToPlayer)
            {
                hp = Mathf.Min(hp, Mathf.CeilToInt(playerSnapshot.MaxHp * maxOpponentHpPlayerFactor));
                attack = Mathf.Min(attack, Mathf.CeilToInt(playerSnapshot.Attack * maxOpponentAttackPlayerFactor));
            }

            return new BattleStatsHub.BattleStatsSnapshot(
                hp,
                attack,
                opponentArmor,
                0f,
                opponentCritChance,
                opponentCritDamageMultiplier);
        }

        private BattleStatsHub.BattleStatsSnapshot ClampOpponentStatsNearPlayer(
            BattleStatsHub.BattleStatsSnapshot opponentSnapshot,
            BattleStatsHub.BattleStatsSnapshot playerSnapshot)
        {
            if (!mirrorBotStatsNearPlayer)
                return opponentSnapshot;

            int hp = ClampIntNear(opponentSnapshot.MaxHp, playerSnapshot.MaxHp, botHpMirrorVariance);
            int attack = ClampIntNear(opponentSnapshot.Attack, playerSnapshot.Attack, botAttackMirrorVariance);
            float armor = ClampFloatNear(opponentSnapshot.Armor, playerSnapshot.Armor, botChanceMirrorVariance, 0f, 1f);
            float crit = ClampFloatNear(opponentSnapshot.CritChance, playerSnapshot.CritChance, botChanceMirrorVariance, 0f, 1f);
            float critPower = ClampFloatNear(opponentSnapshot.CritDamageMultiplier, playerSnapshot.CritDamageMultiplier, botCritPowerMirrorVariance, 1f, 10f);

            return new BattleStatsHub.BattleStatsSnapshot(hp, attack, armor, 0f, crit, critPower);
        }

        private static int ClampIntNear(int value, int center, float variance)
        {
            int safeCenter = Mathf.Max(1, center);
            float safeVariance = Mathf.Clamp01(variance);
            int min = Mathf.Max(1, Mathf.FloorToInt(safeCenter * (1f - safeVariance)));
            int max = Mathf.Max(min, Mathf.CeilToInt(safeCenter * (1f + safeVariance)));
            return Mathf.Clamp(Mathf.Max(1, value), min, max);
        }

        private static float ClampFloatNear(float value, float center, float variance, float minLimit, float maxLimit)
        {
            float min = Mathf.Max(minLimit, center - Mathf.Max(0f, variance));
            float max = Mathf.Min(maxLimit, center + Mathf.Max(0f, variance));
            if (max < min)
                max = min;

            return Mathf.Clamp(value, min, max);
        }

        private float ResolveOpponentAttackFactor()
        {
            string tier = string.IsNullOrWhiteSpace(MahjongSession.BattleOpponentRankTier)
                ? string.Empty
                : MahjongSession.BattleOpponentRankTier.Trim().ToLowerInvariant();

            if (tier == "master")
                return 1.12f;
            if (tier == "jade")
                return 1.07f;
            if (tier == "gold")
                return 1.02f;
            if (tier == "silver")
                return 0.96f;
            if (tier == "bronze")
                return 0.90f;

            int rankPoints = Mathf.Max(0, MahjongSession.BattleOpponentRankPoints);
            if (rankPoints >= 800)
                return 1.12f;
            if (rankPoints >= 500)
                return 1.07f;
            if (rankPoints >= 250)
                return 1.02f;
            if (rankPoints >= 100)
                return 0.96f;

            return 0.90f;
        }

        private BattleDamageCalculator.DamageResult CalculateDamage(
            BattleStatsHub.BattleStatsSnapshot attacker,
            float targetArmor,
            int targetMaxHp)
        {
            if (!useCharacterDamageStats)
                return new BattleDamageCalculator.DamageResult(ResolveMinimumPairDamage(damagePerPair, targetMaxHp), false);

            int attack = Mathf.Max(0, attacker.Attack);
            float critChance = Mathf.Clamp01(attacker.CritChance);
            float critMultiplier = Mathf.Max(1f, attacker.CritDamageMultiplier);
            targetArmor = Mathf.Clamp01(targetArmor);
            bool critical = Roll(critChance);
            float damage = attack;

            if (critical)
                damage *= critMultiplier;

            float damageBeforeArmor = damage;
            damage *= 1f - targetArmor;

            int finalDamage = Mathf.CeilToInt(damage);
            if (attack > 0 && finalDamage < 1)
                finalDamage = 1;
            if (attack > 0)
                finalDamage = ResolveMinimumPairDamage(finalDamage, targetMaxHp);

            int rawDamage = Mathf.CeilToInt(damageBeforeArmor);
            int absorbedDamage = Mathf.Max(0, rawDamage - finalDamage);
            return new BattleDamageCalculator.DamageResult(finalDamage, absorbedDamage, critical);
        }

        private int ResolveMinimumPairDamage(int baseDamage, int targetMaxHp)
        {
            int damage = Mathf.Max(0, baseDamage);
            if (damage <= 0)
                return 0;

            int minimum = Mathf.CeilToInt(Mathf.Max(1, targetMaxHp) * minimumDamagePerPairHpFraction);
            return Mathf.Max(damage, minimum);
        }

        private static BattleStatsHub.BattleStatsSnapshot CreateFallbackStats(int maxHp, int attack)
        {
            return new BattleStatsHub.BattleStatsSnapshot(
                maxHp,
                attack,
                0f,
                0f,
                0.05f,
                1.5f);
        }

        private static BattleStatsHub.BattleStatsSnapshot NormalizeStats(
            BattleStatsHub.BattleStatsSnapshot stats,
            int fallbackHp,
            int fallbackAttack)
        {
            return new BattleStatsHub.BattleStatsSnapshot(
                NormalizeLegacyHp(stats.MaxHp > 0 ? stats.MaxHp : fallbackHp),
                stats.Attack > 0 ? stats.Attack : fallbackAttack,
                stats.Armor,
                0f,
                stats.CritChance,
                stats.CritDamageMultiplier);
        }

        private static int NormalizeLegacyHp(int hp)
        {
            return Mathf.Max(1, hp);
        }

        private static bool Roll(float chance)
        {
            if (chance <= 0f)
                return false;

            if (chance >= 1f)
                return true;

            return UnityEngine.Random.value <= chance;
        }

        private void LogDamageRoll(
            string attackerName,
            string targetName,
            BattleDamageCalculator.DamageResult result)
        {
            if (!debugLogs)
                return;

            if (result.IsParried)
            {
                Log($"{targetName} parried {attackerName} attack");
                return;
            }

            Log(
                $"{attackerName} damage roll -> {targetName} | " +
                $"Damage={result.FinalDamage} | Critical={result.IsCritical}");
        }

        private void RaiseHpEvents()
        {
            PlayerHpChanged?.Invoke(this, playerHp, maxPlayerHp);
            OpponentHpChanged?.Invoke(this, opponentHp, maxOpponentHp);
        }

        private void RefreshUi()
        {
            if (playerHpText != null)
                playerHpText.text = GetPlayerHpText();

            if (opponentHpText != null)
                opponentHpText.text = GetOpponentHpText();
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(this);
        }

        private void Log(string message)
        {
            if (!debugLogs)
                return;

            Debug.Log($"[BattleCombatSystem] {message}", this);
        }
    }
}
