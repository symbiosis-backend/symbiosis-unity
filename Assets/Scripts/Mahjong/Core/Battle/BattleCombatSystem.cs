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
        public event Action<BattleCombatSystem, BattleBoardSide> CombatFinished;
        public event Action<BattleCombatSystem> StateChanged;

        [Header("Links")]
        [SerializeField] private BattleMatchController matchController;
        [SerializeField] private BattleBoard playerBoard;
        [SerializeField] private BattleBoard opponentBoard;
        [SerializeField] private BattleStatsHub statsHub;

        [Header("HP")]
        [SerializeField, Min(1)] private int maxPlayerHp = 10;
        [SerializeField, Min(1)] private int maxOpponentHp = 10;
        [SerializeField, Min(1)] private int damagePerPair = 1;
        [SerializeField, Range(0f, 0.5f)] private float minimumDamagePerPairHpFraction = 0.05f;

        [Header("Character Stats")]
        [SerializeField] private bool useSelectedCharacterStats = true;
        [SerializeField] private bool createStatsHubIfMissing = true;
        [SerializeField] private bool useCharacterDamageStats = true;

        [Header("Bot Battle Stats")]
        [SerializeField] private bool scaleOpponentStatsFromRank = true;
        [SerializeField, Range(0f, 1f)] private float opponentArmor = 0.03f;
        [SerializeField, Range(0f, 1f)] private float opponentParryChance = 0.1f;
        [SerializeField, Range(0f, 1f)] private float opponentCritChance = 0.08f;
        [SerializeField, Min(1f)] private float opponentCritDamageMultiplier = 1.5f;
        [SerializeField] private int opponentRankHpStep = 100;
        [SerializeField] private int opponentHpPerStep = 4;
        [SerializeField] private int opponentRankAttackStep = 150;
        [SerializeField] private int opponentAttackPerStep = 1;
        [SerializeField] private bool clampOpponentStatsToPlayer = true;
        [SerializeField, Min(1f)] private float maxOpponentHpPlayerFactor = 1.2f;
        [SerializeField, Min(1f)] private float maxOpponentAttackPlayerFactor = 1.05f;

        [Header("UI / Optional")]
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text opponentHpText;
        [SerializeField] private string hpPrefix = "HP: ";

        [Header("Parry Zone Duel")]
        [SerializeField] private bool useParryZoneDuel = true;
        [SerializeField] private bool createParryChoiceUiIfMissing = true;
        [SerializeField, Min(0.5f)] private float parryChoiceTimeout = 5f;
        [SerializeField] private BattleParryChoiceUI parryChoiceUi;

        [Header("Debug")]
        [SerializeField] private bool finishMatchDirectlyOnDeath = false;
        [SerializeField] private bool debugLogs = true;

        private int playerHp;
        private int opponentHp;
        private bool combatStarted;
        private bool combatFinished;
        private bool parryChoiceInProgress;
        private bool parryBoardLockActive;
        private bool playerBoardWasLockedBeforeParry;
        private bool opponentBoardWasLockedBeforeParry;
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
            CancelParryChoice();
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
            CancelParryChoice();
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
            CancelParryChoice();

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
            if (!combatStarted || combatFinished)
                return false;

            int damage = Mathf.Max(0, amount);
            if (damage <= 0)
                return false;

            int before = playerHp;
            playerHp = Mathf.Max(0, playerHp - damage);

            PlayerHpChanged?.Invoke(this, playerHp, maxPlayerHp);
            DamageApplied?.Invoke(this, BattleBoardSide.Player, damage, playerHp);

            RefreshUi();
            NotifyStateChanged();

            Log($"Player damaged | {before} -> {playerHp} | Damage={damage}");

            if (playerHp <= 0)
                FinishCombat(BattleBoardSide.Player);

            return true;
        }

        public bool ApplyDamageToOpponent(int amount)
        {
            if (!combatStarted || combatFinished)
                return false;

            int damage = Mathf.Max(0, amount);
            if (damage <= 0)
                return false;

            int before = opponentHp;
            opponentHp = Mathf.Max(0, opponentHp - damage);

            OpponentHpChanged?.Invoke(this, opponentHp, maxOpponentHp);
            DamageApplied?.Invoke(this, BattleBoardSide.Opponent, damage, opponentHp);

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

        private void HandlePlayerPairMatched(BattleBoard _, BattleTile __, BattleTile ___)
        {
            if (networkPlayerPairDamageSuppressed)
                return;

            if (networkPairDamageSuppressed && networkPairDamageSuppressedSide == BattleBoardSide.Player)
                return;

            StartCoroutine(ResolvePairAttackRoutine(
                BattleBoardSide.Player,
                playerStats,
                opponentStats.Armor,
                opponentStats.ParryChance,
                maxOpponentHp));
        }

        private void HandleOpponentPairMatched(BattleBoard _, BattleTile __, BattleTile ___)
        {
            if (networkOpponentPairDamageSuppressed)
                return;

            if (networkPairDamageSuppressed && networkPairDamageSuppressedSide == BattleBoardSide.Opponent)
                return;

            StartCoroutine(ResolvePairAttackRoutine(
                BattleBoardSide.Opponent,
                opponentStats,
                playerStats.Armor,
                playerStats.ParryChance,
                maxPlayerHp));
        }

        private IEnumerator ResolvePairAttackRoutine(
            BattleBoardSide attackerSide,
            BattleStatsHub.BattleStatsSnapshot attacker,
            float targetArmor,
            float targetParryChance,
            int targetMaxHp)
        {
            while (parryChoiceInProgress && combatStarted && !combatFinished)
                yield return null;

            if (!combatStarted || combatFinished)
                yield break;

            BattleBoardSide targetSide = attackerSide == BattleBoardSide.Player
                ? BattleBoardSide.Opponent
                : BattleBoardSide.Player;

            string attackerName = attackerSide == BattleBoardSide.Player ? "Player" : "Opponent";
            string targetName = targetSide == BattleBoardSide.Player ? "Player" : "Opponent";

            BattleDamageCalculator.DamageResult result = useParryZoneDuel
                ? CalculateDamageWithoutParry(attacker, targetArmor, targetMaxHp)
                : CalculateDamage(attacker, targetArmor, targetParryChance, targetMaxHp);

            if (ShouldResolveParryDuel(targetParryChance))
            {
                parryChoiceInProgress = true;
                SetBoardsLockedForParry(true);

                bool choiceComplete = false;
                bool parried = false;
                BattleHitZone attackZone = BattleHitZone.Middle;
                BattleHitZone parryZone = BattleHitZone.Middle;
                BattleHitZone playerZone = GetRandomHitZone();
                BattleHitZone opponentZone = GetRandomHitZone();

                BattleParryChoiceUI ui = ResolveParryChoiceUi();
                if (ui != null)
                {
                    ui.Show(
                        attackerSide,
                        playerZone,
                        opponentZone,
                        parryChoiceTimeout,
                        (chosenAttackZone, chosenParryZone, isParried) =>
                        {
                            attackZone = chosenAttackZone;
                            parryZone = chosenParryZone;
                            parried = isParried;
                            choiceComplete = true;
                        });

                    while (!choiceComplete && combatStarted && !combatFinished)
                        yield return null;

                    if (!combatStarted || combatFinished)
                    {
                        parryChoiceInProgress = false;
                        SetBoardsLockedForParry(false);
                        yield break;
                    }
                }
                else
                {
                    attackZone = attackerSide == BattleBoardSide.Player ? playerZone : opponentZone;
                    parryZone = attackerSide == BattleBoardSide.Player ? opponentZone : playerZone;
                    parried = attackZone == parryZone;
                }

                result.IsParried = parried;
                if (parried)
                    result.FinalDamage = 0;

                parryChoiceInProgress = false;
                SetBoardsLockedForParry(false);

                Log(
                    $"Parry zone duel | Attacker={attackerName} Target={targetName} | " +
                    $"AttackZone={attackZone} ParryZone={parryZone} | Parried={parried}");
            }

            LogDamageRoll(attackerName, targetName, result);

            if (targetSide == BattleBoardSide.Opponent)
                ApplyDamageToOpponent(result.FinalDamage);
            else
                ApplyDamageToPlayer(result.FinalDamage);
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

            playerStats = NormalizeStats(playerStats, maxPlayerHp, damagePerPair);
            maxPlayerHp = playerStats.MaxHp;

            opponentStats = BuildOpponentStats(playerStats);
            maxOpponentHp = opponentStats.MaxHp;
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
                opponentParryChance,
                opponentCritChance,
                opponentCritDamageMultiplier);
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
            float targetParryChance,
            int targetMaxHp)
        {
            if (!useCharacterDamageStats)
                return new BattleDamageCalculator.DamageResult(ResolveMinimumPairDamage(damagePerPair, targetMaxHp), false, false);

            int attack = Mathf.Max(0, attacker.Attack);
            float critChance = Mathf.Clamp01(attacker.CritChance);
            float critMultiplier = Mathf.Max(1f, attacker.CritDamageMultiplier);
            targetArmor = Mathf.Clamp01(targetArmor);
            targetParryChance = Mathf.Clamp01(targetParryChance);

            if (Roll(targetParryChance))
                return new BattleDamageCalculator.DamageResult(0, false, true);

            bool critical = Roll(critChance);
            float damage = attack;

            if (critical)
                damage *= critMultiplier;

            damage *= 1f - targetArmor;

            int finalDamage = Mathf.CeilToInt(damage);
            if (attack > 0 && finalDamage < 1)
                finalDamage = 1;
            if (attack > 0)
                finalDamage = ResolveMinimumPairDamage(finalDamage, targetMaxHp);

            return new BattleDamageCalculator.DamageResult(finalDamage, critical, false);
        }

        private BattleDamageCalculator.DamageResult CalculateDamageWithoutParry(
            BattleStatsHub.BattleStatsSnapshot attacker,
            float targetArmor,
            int targetMaxHp)
        {
            return CalculateDamage(attacker, targetArmor, 0f, targetMaxHp);
        }

        private bool ShouldResolveParryDuel(float targetParryChance)
        {
            if (!useParryZoneDuel || !useCharacterDamageStats)
                return false;

            return Roll(Mathf.Clamp01(targetParryChance));
        }

        private BattleParryChoiceUI ResolveParryChoiceUi()
        {
            if (parryChoiceUi != null)
                return parryChoiceUi;

            parryChoiceUi = FindAnyObjectByType<BattleParryChoiceUI>(FindObjectsInactive.Include);
            if (parryChoiceUi != null || !createParryChoiceUiIfMissing)
                return parryChoiceUi;

            parryChoiceUi = BattleParryChoiceUI.CreateRuntime();
            return parryChoiceUi;
        }

        private void CancelParryChoice()
        {
            parryChoiceInProgress = false;
            SetBoardsLockedForParry(false);

            if (parryChoiceUi != null)
                parryChoiceUi.Cancel();
        }

        private void SetBoardsLockedForParry(bool locked)
        {
            if (locked)
            {
                if (parryBoardLockActive)
                    return;

                parryBoardLockActive = true;
                playerBoardWasLockedBeforeParry = playerBoard != null && playerBoard.IsInteractionLocked;
                opponentBoardWasLockedBeforeParry = opponentBoard != null && opponentBoard.IsInteractionLocked;

                if (playerBoard != null)
                    playerBoard.SetInteractionLocked(true);
                if (opponentBoard != null)
                    opponentBoard.SetInteractionLocked(true);

                return;
            }

            if (!parryBoardLockActive)
                return;

            if (playerBoard != null)
                playerBoard.SetInteractionLocked(playerBoardWasLockedBeforeParry);
            if (opponentBoard != null)
                opponentBoard.SetInteractionLocked(opponentBoardWasLockedBeforeParry);

            parryBoardLockActive = false;
        }

        private static BattleHitZone GetRandomHitZone()
        {
            return (BattleHitZone)UnityEngine.Random.Range(0, 3);
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
                stats.ParryChance,
                stats.CritChance,
                stats.CritDamageMultiplier);
        }

        private static int NormalizeLegacyHp(int hp)
        {
            int safeHp = Mathf.Max(1, hp);
            return safeHp >= 300 ? Mathf.Max(1, Mathf.RoundToInt(safeHp / 10f)) : safeHp;
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
